﻿namespace BaristaLabs.BaristaCore.JavaScript
{
    using System;
    using System.Linq;
    using System.Text;

    public sealed class JavaScriptFunction : JavaScriptObject
    {
        public JavaScriptFunction(IJavaScriptEngine engine, JavaScriptContext context, JavaScriptValueSafeHandle value)
            : base(engine, context, value)
        {
        }

        public JavaScriptValue Invoke(params JavaScriptValue[] args)
        {
            var argPtrs = args.Select(a => a.Handle.DangerousGetHandle()).Prepend(Handle.DangerousGetHandle()).ToArray();

            var result = Engine.JsCallFunction(Handle, argPtrs, (ushort)argPtrs.Length);
            return Context.ValuePool.GetOrAdd(result);
        }

        private const string toStringPropertyName = "toString";

        public override string ToString()
        {
            var toStringFunctionPropertyIdHandle = Engine.JsCreatePropertyIdUtf8(toStringPropertyName, new UIntPtr((uint)toStringPropertyName.Length));
            var toStringFnHandle = Engine.JsGetProperty(Handle, toStringFunctionPropertyIdHandle);

            var resultHandle = Engine.JsCallFunction(toStringFnHandle, new IntPtr[] { Handle.DangerousGetHandle() }, 1);

            var size = Engine.JsCopyStringUtf8(resultHandle, null, UIntPtr.Zero);
            if ((int)size > int.MaxValue)
                throw new OutOfMemoryException("Exceeded maximum string length.");

            byte[] result = new byte[(int)size];
            var written = Engine.JsCopyStringUtf8(resultHandle, result, size);
            return Encoding.UTF8.GetString(result, 0, result.Length);
        }
    }
}