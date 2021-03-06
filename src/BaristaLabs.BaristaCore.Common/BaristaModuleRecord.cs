﻿namespace BaristaLabs.BaristaCore
{
    using BaristaLabs.BaristaCore.JavaScript;
    using BaristaLabs.BaristaCore.ModuleLoaders;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a JavaScript Module
    /// </summary>
    public sealed class BaristaModuleRecord : BaristaObject<JavaScriptModuleRecord>
    {
        private readonly string m_name;
        private readonly JavaScriptValueSafeHandle m_moduleSpecifier;
        private readonly BaristaContext m_context;
        private readonly IBaristaModuleRecordFactory m_moduleRecordFactory;
        private readonly IBaristaModuleLoader m_moduleLoader;

        private readonly GCHandle m_fetchImportedModuleCallbackHandle = default(GCHandle);
        private readonly GCHandle m_notifyCallbackHandle = default(GCHandle);

        private readonly Dictionary<string, BaristaModuleRecord> m_importedModules = new Dictionary<string, BaristaModuleRecord>();

        private readonly BaristaModuleRecord m_parentModule;
        private readonly GCHandle m_beforeCollectCallbackDelegateHandle;

        public BaristaModuleRecord(string name, JavaScriptValueSafeHandle moduleSpecifier, BaristaModuleRecord parentModule, IJavaScriptEngine engine, BaristaContext context, IBaristaModuleRecordFactory moduleRecordFactory, IBaristaModuleLoader moduleLoader, JavaScriptModuleRecord moduleRecord)
            : base(engine, moduleRecord)
        {
            m_name = name ?? throw new ArgumentNullException(nameof(name));
            m_moduleSpecifier = moduleSpecifier ?? throw new ArgumentNullException(nameof(moduleSpecifier));
            m_parentModule = parentModule;
            m_context = context ?? throw new ArgumentNullException(nameof(context));
            m_moduleRecordFactory = moduleRecordFactory ?? throw new ArgumentNullException(nameof(moduleRecordFactory));

            //Module loader is not required, but if not specified, imports will fail.
            m_moduleLoader = moduleLoader;

            //Associate functions that will handle module loading
            if (m_parentModule == null)
            {
                //Set the fetch module callback for the module.
                m_fetchImportedModuleCallbackHandle = InitFetchImportedModuleCallback(Handle);

                //Set the notify callback for the module.
                m_notifyCallbackHandle = InitNotifyModuleReadyCallback(Handle);
            }

            //Set the event that will be called prior to the engine collecting the context.
            JavaScriptObjectBeforeCollectCallback beforeCollectCallback = (IntPtr handle, IntPtr callbackState) =>
            {
                OnBeforeCollect(handle, callbackState);
            };

            m_beforeCollectCallbackDelegateHandle = GCHandle.Alloc(beforeCollectCallback);
            Engine.JsSetObjectBeforeCollectCallback(moduleRecord, IntPtr.Zero, beforeCollectCallback);
        }

        private GCHandle InitFetchImportedModuleCallback(JavaScriptModuleRecord moduleRecord)
        {
            JavaScriptFetchImportedModuleCallback fetchImportedModule = (IntPtr referencingModule, IntPtr specifier, out IntPtr dependentModule) =>
            {
                try
                {
                    return FetchImportedModule(new JavaScriptModuleRecord(referencingModule), new JavaScriptValueSafeHandle(specifier), out dependentModule);
                }
                catch (Exception ex)
                {
                    if (Engine.JsHasException() == false)
                    {
                        Engine.JsSetException(Context.CreateError(ex.Message).Handle);
                    }

                    dependentModule = referencingModule;
                    return true;
                }
            };

            var handle = GCHandle.Alloc(fetchImportedModule);
            IntPtr fetchCallbackPtr = Marshal.GetFunctionPointerForDelegate(handle.Target);
            Engine.JsSetModuleHostInfo(moduleRecord, JavaScriptModuleHostInfoKind.FetchImportedModuleCallback, fetchCallbackPtr);
            Engine.JsSetModuleHostInfo(moduleRecord, JavaScriptModuleHostInfoKind.FetchImportedModuleFromScriptCallback, fetchCallbackPtr);
            return handle;
        }

        private GCHandle InitNotifyModuleReadyCallback(JavaScriptModuleRecord moduleRecord)
        {
            JavaScriptNotifyModuleReadyCallback moduleNotifyCallback = (IntPtr referencingModule, IntPtr exceptionVar) =>
            {
                if (exceptionVar != IntPtr.Zero)
                {
                    if (!Engine.JsHasException())
                    {
                        Engine.JsSetException(new JavaScriptValueSafeHandle(exceptionVar));
                    }
                    return true;
                }

                IsReady = true;
                return false;
            };

            var handle = GCHandle.Alloc(moduleNotifyCallback);
            IntPtr notifyCallbackPtr = Marshal.GetFunctionPointerForDelegate(handle.Target);
            Engine.JsSetModuleHostInfo(moduleRecord, JavaScriptModuleHostInfoKind.NotifyModuleReadyCallback, notifyCallbackPtr);
            return handle;
        }

        #region Properties
        /// <summary>
        /// Gets a value that indicates if the module's notify ready callback has been called.
        /// </summary>
        public bool IsReady
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        public string Name
        {
            get { return m_name; }
        }

        /// <summary>
        /// Gets the handle of the module specifier.
        /// </summary>
        public JavaScriptValueSafeHandle Specifier
        {
            get { return m_moduleSpecifier; }
        }

        private BaristaContext Context
        {
            get { return m_context; }
        }
        #endregion

        public JsError ParseModuleSource(string script)
        {
            var scriptBuffer = Encoding.UTF8.GetBytes(script);
            var parseResultHandle = Engine.JsParseModuleSource(Handle, JavaScriptSourceContext.GetNextSourceContext(), scriptBuffer, (uint)scriptBuffer.Length, JavaScriptParseModuleSourceFlags.DataIsUTF8);
            return Context.CreateValue<JsError>(parseResultHandle);
        }

        private bool FetchImportedModule(JavaScriptModuleRecord jsReferencingModuleRecord, JavaScriptValueSafeHandle specifier, out IntPtr dependentModule)
        {
            var moduleName = Context.CreateValue(specifier).ToString();
            var referencingModuleRecord = m_moduleRecordFactory.GetBaristaModuleRecord(jsReferencingModuleRecord);

            //If the current module name is equal to the fetching module name, return this value.
            if (Name == moduleName || referencingModuleRecord != null && referencingModuleRecord.Name == moduleName)
            {
                //Top-level self-referencing module. Reference itself.
                dependentModule = jsReferencingModuleRecord.DangerousGetHandle();
                return false;
            }
            else if (m_importedModules.ContainsKey(moduleName))
            {
                //The module has already been imported, return the existing JavaScriptModuleRecord
                dependentModule = m_importedModules[moduleName].Handle.DangerousGetHandle();
                return false;
            }
            else if (m_moduleLoader != null)
            {
                Task<IBaristaModule> moduleLoaderTask = null;
                try
                {
                    moduleLoaderTask = m_moduleLoader.GetModule(moduleName);
                }
                catch (Exception ex)
                {
                    var error = Context.CreateError($"An error occurred while attempting to load a module named {moduleName}: {ex.Message}");
                    Engine.JsSetException(error.Handle);
                    dependentModule = jsReferencingModuleRecord.DangerousGetHandle();
                    return true;
                }

                if (moduleLoaderTask != null)
                {
                    IBaristaModule module;
                    try
                    {
                        module = moduleLoaderTask.GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        var error = Context.CreateError($"An error occurred while attempting to load a module named {moduleName}: {ex.Message}");
                        Engine.JsSetException(error.Handle);
                        dependentModule = jsReferencingModuleRecord.DangerousGetHandle();
                        return true;
                    }

                    if (module != null)
                    {

                        var newModuleRecord = m_moduleRecordFactory.CreateBaristaModuleRecord(Context, specifier, this, false);
                        m_importedModules.Add(moduleName, newModuleRecord);
                        dependentModule = newModuleRecord.Handle.DangerousGetHandle();

                        switch (module)
                        {
                            //For the built-in Script Module type, parse the string returned by ExportDefault and install it as a module.
                            case IBaristaScriptModule scriptModule:
                                var script = scriptModule.ExportDefault(Context, newModuleRecord) as JsString;
                                if (script == null)
                                {
                                    var error = Context.CreateError($"The module {moduleName} implements IBaristaScriptModule and is expected to return a string based module that exports a default value.");
                                    Engine.JsSetException(error.Handle);
                                    return true;
                                }

                                newModuleRecord.ParseModuleSource(script.ToString());
                                return false;
                            //Otherwise, install the module.
                            default:
                                var result = InstallModule(newModuleRecord, referencingModuleRecord, module, specifier);
                                return result;
                        }
                    }
                }
            }

            dependentModule = jsReferencingModuleRecord.DangerousGetHandle();
            return true;
        }

        private bool InstallModule(BaristaModuleRecord newModuleRecord, BaristaModuleRecord referencingModuleRecord, IBaristaModule module, JavaScriptValueSafeHandle specifier)
        {
            try
            {
                var moduleValue = module.ExportDefault(Context, referencingModuleRecord);
                return CreateSingleValueModule(newModuleRecord, specifier, moduleValue);
            }
            catch (Exception ex)
            {
                var error = Context.CreateError($"An error occurred while obtaining the default export of the native module named {newModuleRecord.Name}: {ex.Message}");
                Engine.JsSetException(error.Handle);
                return true;
            }
        }

        /// <summary>
        /// Creates a module that returns the specified value.
        /// </summary>
        /// <param name="valueSafeHandle"></param>
        /// <param name="referencingModuleRecord"></param>
        /// <param name="specifierHandle"></param>
        /// <param name="dependentModuleRecord"></param>
        /// <returns></returns>
        private bool CreateSingleValueModule(BaristaModuleRecord moduleRecord, JavaScriptValueSafeHandle specifier, JsValue defaultExportedValue)
        {
            var globalId = Guid.NewGuid();
            var exportSymbol = Context.Symbol.For($"$DEFAULTEXPORT_{globalId.ToString()}");
            var exposeNativeValueScript = $@"
const defaultExport = global[Symbol.for('$DEFAULTEXPORT_{globalId.ToString()}')];
export default defaultExport;
";
            Context.Object.DefineProperty(Context.GlobalObject, exportSymbol, new JsPropertyDescriptor() { Configurable = false, Enumerable = false, Writable = false, Value = defaultExportedValue });

            moduleRecord.ParseModuleSource(exposeNativeValueScript);
            return false;
        }

        #region IDisposable Support
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // free managed resources
                    foreach(var importedModule in m_importedModules.Values)
                    {
                        importedModule.Dispose();
                    }
                }

                // free unmanaged resources (unmanaged objects)
                if (m_parentModule == null)
                {
                    try
                    {
                        Engine.JsSetModuleHostInfo(Handle, JavaScriptModuleHostInfoKind.FetchImportedModuleFromScriptCallback, IntPtr.Zero);
                        Engine.JsSetModuleHostInfo(Handle, JavaScriptModuleHostInfoKind.FetchImportedModuleCallback, IntPtr.Zero);
                        Engine.JsSetModuleHostInfo(Handle, JavaScriptModuleHostInfoKind.NotifyModuleReadyCallback, IntPtr.Zero);
                    }
                    catch
                    {
                        //Do Nothing...
                    }
                }

                //Unset the before collect callback.
                Engine.JsSetObjectBeforeCollectCallback(Handle, IntPtr.Zero, null);

                if (m_fetchImportedModuleCallbackHandle != default(GCHandle) && m_fetchImportedModuleCallbackHandle.IsAllocated)
                {
                    m_fetchImportedModuleCallbackHandle.Free();
                }

                if (m_notifyCallbackHandle != default(GCHandle) && m_notifyCallbackHandle.IsAllocated)
                {
                    m_notifyCallbackHandle.Free();
                }

                m_beforeCollectCallbackDelegateHandle.Free();
            }

            base.Dispose(disposing);
        }
        #endregion
    }
}
