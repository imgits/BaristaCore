﻿namespace BaristaLabs.BaristaCore
{
    using BaristaLabs.BaristaCore.Extensions;
    using BaristaLabs.BaristaCore.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public sealed class BaristaTypeConversionStrategy : IBaristaTypeConversionStrategy
    {
        private const string BaristaObjectPropertyName = "__baristaObject";

        public bool TryCreatePrototypeFunction(BaristaContext context, Type typeToConvert, out JsFunction ctor)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (typeToConvert == null)
                throw new ArgumentNullException(nameof(typeToConvert));

            var reflector = new ObjectReflector(typeToConvert);
            var objectName = BaristaObjectAttribute.GetBaristaObjectNameFromType(typeToConvert);
            JsFunction fnCtor;

            var publicConstructors = reflector.GetConstructors();
            if (publicConstructors.Any())
            {
                var constructor = new BaristaFunctionDelegate((isConstructCall, thisObj, args) => {
                    if (!isConstructCall)
                    {
                        var ex = context.ValueFactory.CreateTypeError($"Failed to construct '{objectName}': Please use the 'new' operator, this object constructor cannot be called as a function.");
                        context.CurrentScope.SetException(ex);
                        return context.Undefined;
                    }

                    //Use Object.create to create an object bound to this prototype.
                    var resultValue = context.Object.Create(thisObj);

                    //TODO: Find the best constructor.
                    var result = publicConstructors.First().Invoke(new object[] { });

                    var obj = context.ValueFactory.CreateExternalObject(result);
                    //TODO: This might work better as a symbol.
                    resultValue.SetProperty(BaristaObjectPropertyName, obj);
                    return resultValue;
                });

                fnCtor = context.ValueFactory.CreateFunction(constructor, objectName);
            }
            else
            {
                var constructor = new BaristaFunctionDelegate((isConstructCall, thisObj, args) => {
                    if (!isConstructCall)
                        return context.Undefined;

                    return context.Undefined;
                });

                fnCtor = context.ValueFactory.CreateFunction(constructor, typeToConvert.Name);
            }

            var fnCtorPrototype = fnCtor.Prototype;

            //Project static properties onto the constructor.
            ProjectProperties(context, fnCtor, reflector.GetProperties(false));

            //Project static properties onto the constructor.
            ProjectProperties(context, fnCtorPrototype, reflector.GetProperties(true));

            //Project static methods onto the constructor.
            ProjectMethods(context, fnCtor, reflector.GetMethods(false));

            //Project instance methods on to the constructor prototype;
            ProjectMethods(context, fnCtorPrototype, reflector.GetMethods(true));


            ctor = fnCtor;
            return true;
        }

        private void ProjectProperties(BaristaContext context, JsObject targetObject, IEnumerable<PropertyInfo> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.GetIndexParameters().Length > 0)
                    throw new NotSupportedException("Index properties not supported for projecting CLR to JavaScript objects.");

                var propertyName = prop.Name.Camelize();
                var propertyDescriptor = context.ValueFactory.CreateObject();
                propertyDescriptor.SetProperty("enumerable", context.True);

                if (prop.GetMethod != null)
                {
                    var jsGet = context.ValueFactory.CreateFunction(new BaristaFunctionDelegate((isConstructCall, thisObj, args) =>
                    {
                        object targetObj = null;

                        if (thisObj == null)
                        {
                            context.CurrentScope.SetException(context.ValueFactory.CreateTypeError($"Could not retrieve property '{propertyName}' because there was an invalid 'this' context."));
                            return context.Undefined;
                        }

                        //If the property exists we're probably an instance -- though we should find a way to check this better.
                        if (thisObj.HasProperty(BaristaObjectPropertyName))
                        {
                            var xoObj = thisObj.GetProperty<JsExternalObject>(BaristaObjectPropertyName);
                            targetObj = xoObj.Target;
                        }

                        try
                        {
                            var result = prop.GetValue(targetObj);
                            if (context.Converter.TryFromObject(context, result, out JsValue resultValue))
                            {
                                return resultValue;
                            }
                            else
                            {
                                return context.Undefined;
                            }
                        }
                        catch (Exception ex)
                        {
                            context.CurrentScope.SetException(context.ValueFactory.CreateError(ex.Message));
                            return context.Undefined;
                        }
                    }));

                    propertyDescriptor.SetProperty("get", jsGet);
                }

                if (prop.SetMethod != null)
                {
                    var jsSet = context.ValueFactory.CreateFunction(new BaristaFunctionDelegate((isConstructCall, thisObj, args) =>
                    {
                        object targetObj = null;

                        if (thisObj == null)
                        {
                            context.CurrentScope.SetException(context.ValueFactory.CreateTypeError($"Could not set property '{propertyName}' because there was an invalid 'this' context."));
                            return context.Undefined;
                        }

                        //If the property exists we're probably an instance -- though we should find a way to check this better.
                        if (thisObj.HasProperty(BaristaObjectPropertyName))
                        {
                            var xoObj = thisObj.GetProperty<JsExternalObject>(BaristaObjectPropertyName);
                            targetObj = xoObj.Target;
                        }

                        try
                        {
                            prop.SetValue(targetObj, args.ElementAtOrDefault(0));
                            return context.Undefined;
                        }
                        catch (Exception ex)
                        {
                            context.CurrentScope.SetException(context.ValueFactory.CreateError(ex.Message));
                            return context.Undefined;
                        }
                    }));

                    propertyDescriptor.SetProperty("set", jsSet);
                }

                context.Object.DefineProperty(targetObject, context.ValueFactory.CreateString(propertyName), propertyDescriptor);
            }
        }


        private void ProjectMethods(BaristaContext context, JsObject targetObject, IEnumerable<MethodInfo> methods)
        {
            foreach(var method in methods)
            {
                //method.
                //Activator.CreateInstance(typeof(Func<>).MakeGenericType()
                //var foo = new Func<JsObject, object>((a) => method.Invoke()
                //context.ValueFactory.CreateFunction()
            }
        }
    }
}
