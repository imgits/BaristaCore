﻿namespace BaristaLabs.BaristaCore.JavaScript
{
    using System;

    /// <summary>
    ///     Attributes of a runtime.
    /// </summary>
    [Flags]
    public enum JavaScriptRuntimeAttributes
    {
        /// <summary>
        ///     No special attributes.
        /// </summary>
        None = 0x00000000,
        /// <summary>
        ///     The runtime will not do any work (such as garbage collection) on background threads.
        /// </summary>
        DisableBackgroundWork = 0x00000001,
        /// <summary>
        ///     The runtime should support reliable script interruption. This increases the number of
        ///     places where the runtime will check for a script interrupt request at the cost of a
        ///     small amount of runtime performance.
        /// </summary>
        AllowScriptInterrupt = 0x00000002,
        /// <summary>
        ///     Host will call <c>JsIdle</c>, so enable idle processing. Otherwise, the runtime will
        ///     manage memory slightly more aggressively.
        /// </summary>
        EnableIdleProcessing = 0x00000004,
        /// <summary>
        ///     Runtime will not generate native code.
        /// </summary>
        DisableNativeCodeGeneration = 0x00000008,
        /// <summary>
        ///     Using <c>eval</c> or <c>function</c> constructor will throw an exception.
        /// </summary>
        DisableEval = 0x00000010,
        /// <summary>
        ///     Runtime will enable all experimental features.
        /// </summary>
        EnableExperimentalFeatures = 0x00000020,
        /// <summary>
        ///     Calling <c>JsSetException</c> will also dispatch the exception to the script debugger
        ///     (if any) giving the debugger a chance to break on the exception.
        /// </summary>
        DispatchSetExceptionsToDebugger = 0x00000040,
        /// <summary>
        ///     Disable Failfast fatal error on OOM
        /// </summary>
        JsRuntimeAttributeDisableFatalOnOOM = 0x00000080,
    }
}
