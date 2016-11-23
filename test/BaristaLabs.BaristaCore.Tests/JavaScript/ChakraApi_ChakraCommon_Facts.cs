﻿namespace BaristaLabs.BaristaCore.JavaScript.Tests
{
    using Internal;

    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    using Xunit;

    /// <summary>
    /// Direct tests against the IChakraApi layer
    /// </summary>
    public class ChakraApi_ChakraCommon_Facts
    {
        private IJavaScriptRuntime Jsrt;

        public ChakraApi_ChakraCommon_Facts()
        {
            Jsrt = JavaScriptRuntimeFactory.CreateChakraRuntime();
        }

        [Fact]
        public void JsRuntimeCanBeConstructed()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            Assert.False(runtimeHandle.IsClosed);
            Assert.False(runtimeHandle.IsInvalid);
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsRuntimeCanBeDisposed()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));
            Assert.False(runtimeHandle.IsClosed);
            runtimeHandle.Dispose();
            Assert.True(runtimeHandle.IsClosed);
        }

        [Fact]
        public void JsCollectGarbageCanBeCalled()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));
            Errors.ThrowIfError(Jsrt.JsCollectGarbage(runtimeHandle));

            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsRuntimeMemoryUsageCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            ulong usage;
            Errors.ThrowIfError(Jsrt.JsGetRuntimeMemoryUsage(runtimeHandle, out usage));

            Assert.True(usage > 0);
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsRuntimeMemoryLimitCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            ulong limit;
            Errors.ThrowIfError(Jsrt.JsGetRuntimeMemoryLimit(runtimeHandle, out limit));

            Assert.True(limit == ulong.MaxValue);
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsRuntimeMemoryLimitCanBeSet()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            Errors.ThrowIfError(Jsrt.JsSetRuntimeMemoryLimit(runtimeHandle, 64000));

            ulong limit;
            Errors.ThrowIfError(Jsrt.JsGetRuntimeMemoryLimit(runtimeHandle, out limit));

            Assert.True(64000 == limit);
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsRuntimeMemoryAllocationCallbackIsCalled()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            bool called = false;
            JavaScriptMemoryAllocationCallback callback = (IntPtr callbackState, JavaScriptMemoryEventType allocationEvent, UIntPtr allocationSize) =>
            {
                called = true;
                return true;
            };

            Errors.ThrowIfError(Jsrt.JsSetRuntimeMemoryAllocationCallback(runtimeHandle, IntPtr.Zero, callback));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));

            contextHandle.Dispose();
            runtimeHandle.Dispose();

            Assert.True(called);
        }

        [Fact]
        public void JsRuntimeBeforeCollectCallbackIsCalled()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            bool called = false;
            JavaScriptBeforeCollectCallback callback = (IntPtr callbackState) =>
            {
                called = true;
            };

            Errors.ThrowIfError(Jsrt.JsSetRuntimeBeforeCollectCallback(runtimeHandle, IntPtr.Zero, callback));

            Errors.ThrowIfError(Jsrt.JsCollectGarbage(runtimeHandle));

            runtimeHandle.Dispose();

            Assert.True(called);
        }

        private struct MyPoint
        {
            public int x, y;
        }

        [Fact]
        public void JsValueRefCanBeAdded()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            var myString = "Have you ever questioned the nature of your reality?";
            JavaScriptValueSafeHandle stringHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(myString, new UIntPtr((uint)myString.Length), out stringHandle));

            uint count;
            Errors.ThrowIfError(Jsrt.JsAddValueRef(stringHandle, out count));

            //2 because the safe interface adds a reference.
            Assert.Equal((uint)2, count);

            Errors.ThrowIfError(Jsrt.JsCollectGarbage(runtimeHandle));

            stringHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsRefCanBeAdded()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            var point = new MyPoint()
            {
                x = 64,
                y = 64
            };
            
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<MyPoint>());
            try
            {
                Marshal.StructureToPtr(point, ptr, false);

                uint count;
                Errors.ThrowIfError(Jsrt.JsAddRef(ptr, out count));

                //0 because the ptr isn't associated with the runtime.
                Assert.True(count == 0);

                Errors.ThrowIfError(Jsrt.JsCollectGarbage(runtimeHandle));
            }
            finally
            {
                Marshal.DestroyStructure<MyPoint>(ptr);
                Marshal.FreeHGlobal(ptr);
                contextHandle.Dispose();
                runtimeHandle.Dispose();
            }
        }

        [Fact]
        public void JsObjectBeforeCollectCallbackIsCalled()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            bool called = false;
            JavaScriptObjectBeforeCollectCallback callback = (IntPtr sender, IntPtr callbackState) =>
            {
                called = true;
            };

            JavaScriptValueSafeHandle valueHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8("superman", new UIntPtr((uint)"superman".Length), out valueHandle));

            Errors.ThrowIfError(Jsrt.JsSetObjectBeforeCollectCallback(valueHandle, IntPtr.Zero, callback));

            //The callback is executed during runtime release.
            valueHandle.Dispose();
            Errors.ThrowIfError(Jsrt.JsCollectGarbage(runtimeHandle));

            //Commenting this as apparently on linux/osx, JsCollectGarbage does call the callback,
            //while on windows it does not. Might be related to timing, garbage collection, or idle.
            //Assert.False(called);

            contextHandle.Dispose();
            runtimeHandle.Dispose();

            Assert.True(called);
        }

        [Fact]
        public void JsContextCanBeCreated()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));

            Assert.False(contextHandle.IsClosed);
            Assert.False(contextHandle.IsInvalid);

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsContextCanBeReleased()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));

            Assert.False(contextHandle.IsClosed);
            contextHandle.Dispose();
            Assert.True(contextHandle.IsClosed);

            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCurrentContextCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsGetCurrentContext(out contextHandle));

            Assert.True(contextHandle.IsInvalid);

            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCurrentContextCanBeSet()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));

            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptContextSafeHandle currentContextHandle;
            Errors.ThrowIfError(Jsrt.JsGetCurrentContext(out currentContextHandle));
            Assert.True(currentContextHandle == contextHandle);
            
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsContextOfObjectCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            string str = "I do not fear computers. I fear the lack of them.";
            JavaScriptValueSafeHandle stringHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(str, new UIntPtr((uint)str.Length), out stringHandle));

            JavaScriptContextSafeHandle objectContextHandle;
            Errors.ThrowIfError(Jsrt.JsGetContextOfObject(stringHandle, out objectContextHandle));

            Assert.True(objectContextHandle == contextHandle);

            stringHandle.Dispose();
            objectContextHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JSContextDataCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            IntPtr contextData;
            Errors.ThrowIfError(Jsrt.JsGetContextData(contextHandle, out contextData));

            Assert.True(contextData == IntPtr.Zero);

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JSContextDataCanBeSet()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            string myString = "How inappropriate to call this planet 'Earth', when it is clearly 'Ocean'.";
            var strPtr = Marshal.StringToHGlobalAnsi(myString);
            try
            {
                Errors.ThrowIfError(Jsrt.JsSetContextData(contextHandle, strPtr));

                IntPtr contextData;
                Errors.ThrowIfError(Jsrt.JsGetContextData(contextHandle, out contextData));

                Assert.True(contextData == strPtr);
                Assert.True(myString == Marshal.PtrToStringAnsi(contextData));
            }
            finally
            {
                Marshal.FreeHGlobal(strPtr);
            }

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsRuntimeCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptRuntimeSafeHandle contextRuntimeHandle;
            Errors.ThrowIfError(Jsrt.JsGetRuntime(contextHandle, out contextRuntimeHandle));

            Assert.True(contextRuntimeHandle == runtimeHandle);

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsIdleCanBeCalled()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.EnableIdleProcessing, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            uint nextIdleTick;
            Errors.ThrowIfError(Jsrt.JsIdle(out nextIdleTick));

            var nextTickTime = new DateTime(DateTime.Now.Ticks + nextIdleTick);
            Assert.True(nextTickTime > DateTime.Now);

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsSymbolCanBeRetrievedFromPropertyId()
        {
            string propertyName = "foo";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.EnableIdleProcessing, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle propertyNameHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyNameHandle));

            JavaScriptValueSafeHandle symbolHandle;
            Errors.ThrowIfError(Jsrt.JsCreateSymbol(propertyNameHandle, out symbolHandle));

            JavaScriptPropertyIdSafeHandle propertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsGetPropertyIdFromSymbol(symbolHandle, out propertyIdHandle));

            JavaScriptValueSafeHandle retrievedSymbolHandle;
            Errors.ThrowIfError(Jsrt.JsGetSymbolFromPropertyId(propertyIdHandle, out retrievedSymbolHandle));

            Assert.True(retrievedSymbolHandle != JavaScriptValueSafeHandle.Invalid);

            retrievedSymbolHandle.Dispose();
            propertyIdHandle.Dispose();
            symbolHandle.Dispose();
            propertyNameHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsPropertyIdTypeCanBeDetermined()
        {
            string propertyName = "foo";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.EnableIdleProcessing, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle propertyNameHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyNameHandle));

            JavaScriptValueSafeHandle symbolHandle;
            Errors.ThrowIfError(Jsrt.JsCreateSymbol(propertyNameHandle, out symbolHandle));

            JavaScriptPropertyIdSafeHandle symbolPropertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsGetPropertyIdFromSymbol(symbolHandle, out symbolPropertyIdHandle));

            JavaScriptPropertyIdSafeHandle stringPropertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out stringPropertyIdHandle));

            JavaScriptPropertyIdType symbolPropertyType;
            Errors.ThrowIfError(Jsrt.JsGetPropertyIdType(symbolPropertyIdHandle, out symbolPropertyType));

            Assert.True(symbolPropertyType == JavaScriptPropertyIdType.Symbol);

            JavaScriptPropertyIdType stringPropertyType;
            Errors.ThrowIfError(Jsrt.JsGetPropertyIdType(stringPropertyIdHandle, out stringPropertyType));

            Assert.True(stringPropertyType == JavaScriptPropertyIdType.String);

            stringPropertyIdHandle.Dispose();
            symbolPropertyIdHandle.Dispose();
            symbolHandle.Dispose();
            propertyNameHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsPropertyIdCanBeRetrievedFromASymbol()
        {
            string propertyName = "foo";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.EnableIdleProcessing, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle propertyNameHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyNameHandle));

            JavaScriptValueSafeHandle symbolHandle;
            Errors.ThrowIfError(Jsrt.JsCreateSymbol(propertyNameHandle, out symbolHandle));

            JavaScriptPropertyIdSafeHandle propertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsGetPropertyIdFromSymbol(symbolHandle, out propertyIdHandle));
            
            Assert.True(propertyIdHandle != JavaScriptPropertyIdSafeHandle.Invalid);

            propertyIdHandle.Dispose();
            symbolHandle.Dispose();
            propertyNameHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsSymbolCanBeCreated()
        {
            string propertyName = "foo";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.EnableIdleProcessing, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle propertyNameHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyNameHandle));

            JavaScriptValueSafeHandle symbolHandle;
            Errors.ThrowIfError(Jsrt.JsCreateSymbol(propertyNameHandle, out symbolHandle));
            
            Assert.True(symbolHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(symbolHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Symbol);

            symbolHandle.Dispose();
            propertyNameHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsSymbolDescriptionCanBeRetrieved()
        {
            string propertyName = "foo";
            string toStringPropertyName = "toString";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.EnableIdleProcessing, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle propertyNameHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyNameHandle));

            JavaScriptValueSafeHandle symbolHandle;
            Errors.ThrowIfError(Jsrt.JsCreateSymbol(propertyNameHandle, out symbolHandle));


            JavaScriptPropertyIdSafeHandle toStringFunctionPropertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(toStringPropertyName, new UIntPtr((uint)toStringPropertyName.Length), out toStringFunctionPropertyIdHandle));

            JavaScriptValueSafeHandle symbolObjHandle;
            Errors.ThrowIfError(Jsrt.JsConvertValueToObject(symbolHandle, out symbolObjHandle));

            JavaScriptValueSafeHandle symbolToStringFnHandle;
            Errors.ThrowIfError(Jsrt.JsGetProperty(symbolObjHandle, toStringFunctionPropertyIdHandle, out symbolToStringFnHandle));

            JavaScriptValueSafeHandle resultHandle;
            Errors.ThrowIfError(Jsrt.JsCallFunction(symbolToStringFnHandle, new IntPtr[] { symbolObjHandle.DangerousGetHandle() }, 1, out resultHandle));

            UIntPtr size;
            Errors.ThrowIfError(Jsrt.JsCopyStringUtf8(resultHandle, null, UIntPtr.Zero, out size));
            if ((int)size > int.MaxValue)
                throw new OutOfMemoryException("Exceeded maximum string length.");

            byte[] result = new byte[(int)size];
            UIntPtr written;
            Errors.ThrowIfError(Jsrt.JsCopyStringUtf8(resultHandle, result, size, out written));
            string resultStr = Encoding.UTF8.GetString(result, 0, result.Length);

            Assert.True(resultStr == "Symbol(foo)");

            toStringFunctionPropertyIdHandle.Dispose();
            symbolObjHandle.Dispose();
            symbolToStringFnHandle.Dispose();
            resultHandle.Dispose();

            symbolHandle.Dispose();
            propertyNameHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }


        [Fact]
        public void JsGetOwnPropertySymbols()
        {
            var script = @"(() => {
var sym = Symbol('foo');
var obj = {
    [sym]: 'bar',
    'baz': 'qix'
};
return obj;
})();
";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);

            JavaScriptValueSafeHandle propertySymbols;
            Errors.ThrowIfError(Jsrt.JsGetOwnPropertySymbols(objHandle, out propertySymbols));

            JavaScriptValueType propertySymbolsType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(propertySymbols, out propertySymbolsType));

            Assert.True(propertySymbols != JavaScriptValueSafeHandle.Invalid);
            Assert.True(propertySymbolsType == JavaScriptValueType.Array);

            propertySymbols.Dispose();
            objHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsUndefinedValueCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle undefinedHandle;
            Errors.ThrowIfError(Jsrt.JsGetUndefinedValue(out undefinedHandle));

            Assert.True(undefinedHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(undefinedHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Undefined);

            undefinedHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsNullValueCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle nullHandle;
            Errors.ThrowIfError(Jsrt.JsGetNullValue(out nullHandle));

            Assert.True(nullHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(nullHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Null);

            nullHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsTrueValueCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle trueHandle;
            Errors.ThrowIfError(Jsrt.JsGetTrueValue(out trueHandle));

            Assert.True(trueHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(trueHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Boolean);

            trueHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsFalseValueCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle falseHandle;
            Errors.ThrowIfError(Jsrt.JsGetFalseValue(out falseHandle));

            Assert.True(falseHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(falseHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Boolean);

            falseHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanConvertBoolValueToBoolean()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle trueHandle;
            Errors.ThrowIfError(Jsrt.JsBoolToBoolean(true, out trueHandle));
            Assert.True(trueHandle != JavaScriptValueSafeHandle.Invalid);

            trueHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanConvertBooleanValueToBool()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle trueHandle;
            Errors.ThrowIfError(Jsrt.JsGetTrueValue(out trueHandle));

            JavaScriptValueSafeHandle falseHandle;
            Errors.ThrowIfError(Jsrt.JsGetFalseValue(out falseHandle));

            bool result;
            Errors.ThrowIfError(Jsrt.JsBooleanToBool(trueHandle, out result));
            Assert.True(result);

            Errors.ThrowIfError(Jsrt.JsBooleanToBool(falseHandle, out result));
            Assert.False(result);

            trueHandle.Dispose();
            falseHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanConvertValueToBoolean()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            var stringValue = "true";
            JavaScriptValueSafeHandle stringHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(stringValue, new UIntPtr((uint)stringValue.Length), out stringHandle));

            JavaScriptValueSafeHandle boolHandle;
            Errors.ThrowIfError(Jsrt.JsConvertValueToBoolean(stringHandle, out boolHandle));

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(boolHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Boolean);

            bool result;
            Errors.ThrowIfError(Jsrt.JsBooleanToBool(boolHandle, out result));
            Assert.True(result);

            stringHandle.Dispose();
            boolHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanGetValueType()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            var stringValue = "Dear future generations: Please accept our apologies. We were rolling drunk on petroleum.";
            JavaScriptValueSafeHandle stringHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(stringValue, new UIntPtr((uint)stringValue.Length), out stringHandle));

            JavaScriptValueType result;
            Errors.ThrowIfError(Jsrt.JsGetValueType(stringHandle, out result));

            Assert.True(result == JavaScriptValueType.String);

            stringHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanConvertDoubleValueToNumber()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle doubleHandle;
            Errors.ThrowIfError(Jsrt.JsDoubleToNumber(3.14156, out doubleHandle));

            Assert.True(doubleHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(doubleHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Number);

            doubleHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanConvertIntValueToNumber()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle intHandle;
            Errors.ThrowIfError(Jsrt.JsIntToNumber(3, out intHandle));

            Assert.True(intHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(intHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Number);

            intHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanConvertNumberToDouble()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle doubleHandle;
            Errors.ThrowIfError(Jsrt.JsDoubleToNumber(3.14159, out doubleHandle));

            double result;
            Errors.ThrowIfError(Jsrt.JsNumberToDouble(doubleHandle, out result));

            Assert.True(result == 3.14159);

            doubleHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanConvertNumberToInt()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle intHandle;
            Errors.ThrowIfError(Jsrt.JsIntToNumber(3, out intHandle));

            int result;
            Errors.ThrowIfError(Jsrt.JsNumberToInt(intHandle, out result));

            Assert.True(result == 3);

            intHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanConvertValueToNumber()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            var stringValue = "2.71828";
            JavaScriptValueSafeHandle stringHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(stringValue, new UIntPtr((uint)stringValue.Length), out stringHandle));

            JavaScriptValueSafeHandle numberHandle;
            Errors.ThrowIfError(Jsrt.JsConvertValueToNumber(stringHandle, out numberHandle));

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(numberHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Number);

            double result;
            Errors.ThrowIfError(Jsrt.JsNumberToDouble(numberHandle, out result));

            Assert.True(result == 2.71828);

            stringHandle.Dispose();
            numberHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanGetStringLength()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            var stringValue = "If your brains were dynamite there wouldn't be enough to blow your hat off.";
            JavaScriptValueSafeHandle stringHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(stringValue, new UIntPtr((uint)stringValue.Length), out stringHandle));

            int result;
            Errors.ThrowIfError(Jsrt.JsGetStringLength(stringHandle, out result));

            Assert.True(stringValue.Length == result);

            stringHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanConvertValueToString()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle numberHandle;
            Errors.ThrowIfError(Jsrt.JsDoubleToNumber(2.71828, out numberHandle));

            JavaScriptValueSafeHandle stringHandle;
            Errors.ThrowIfError(Jsrt.JsConvertValueToString(numberHandle, out stringHandle));

            Assert.True(stringHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(stringHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.String);

            //Get the size
            UIntPtr size;
            Errors.ThrowIfError(Jsrt.JsCopyStringUtf8(stringHandle, null, UIntPtr.Zero, out size));
            if ((int)size > int.MaxValue)
                throw new OutOfMemoryException("Exceeded maximum string length.");

            byte[] result = new byte[(int)size];
            UIntPtr written;
            Errors.ThrowIfError(Jsrt.JsCopyStringUtf8(stringHandle, result, new UIntPtr((uint)result.Length), out written));
            string resultStr = Encoding.UTF8.GetString(result, 0, result.Length);

            Assert.True(resultStr == "2.71828");

            stringHandle.Dispose();
            numberHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanRetrieveGlobalObject()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objectHandle;
            Errors.ThrowIfError(Jsrt.JsGetGlobalObject(out objectHandle));

            Assert.True(objectHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(objectHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Object);

            objectHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanCreateObject()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objectHandle;
            Errors.ThrowIfError(Jsrt.JsCreateObject(out objectHandle));

            Assert.True(objectHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(objectHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Object);

            objectHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        public class Foo
        {
            public string Bar
            {
                get;
                set;
            }
        }

        [Fact]
        public void JsCanCreateExternalObject()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            var myFoo = new Foo();
            int size = Marshal.SizeOf(myFoo);
            IntPtr myFooPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(myFoo, myFooPtr, false);

            bool called = false;
            JavaScriptObjectFinalizeCallback callback = (IntPtr ptr) =>
            {
                called = true;
                Assert.True(myFooPtr == ptr);
                Marshal.FreeHGlobal(ptr);
            };

            JavaScriptValueSafeHandle objectHandle;
            Errors.ThrowIfError(Jsrt.JsCreateExternalObject(myFooPtr, callback, out objectHandle));

            Assert.True(objectHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(objectHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Object);

            //The callback is executed during runtime release.
            objectHandle.Dispose();
            Errors.ThrowIfError(Jsrt.JsCollectGarbage(runtimeHandle));

            //Commenting this as apparently on linux/osx, JsCollectGarbage does call the callback,
            //while on windows it does not. Might be related to timing, garbage collection, or idle.
            //Assert.False(called);

            contextHandle.Dispose();
            runtimeHandle.Dispose();

            Assert.True(called);
        }

        [Fact]
        public void JsCanConvertValueToObject()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle numberHandle;
            Errors.ThrowIfError(Jsrt.JsDoubleToNumber(2.71828, out numberHandle));


            JavaScriptValueSafeHandle objectHandle;
            Errors.ThrowIfError(Jsrt.JsConvertValueToObject(numberHandle, out objectHandle));

            Assert.True(objectHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(objectHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Object);

            objectHandle.Dispose();
            numberHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanGetObjectPrototype()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            string stringValue = "Just what do you think you’re doing, Dave?";
            JavaScriptValueSafeHandle stringHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(stringValue, new UIntPtr((uint)stringValue.Length), out stringHandle));

            JavaScriptValueSafeHandle objectHandle;
            Errors.ThrowIfError(Jsrt.JsConvertValueToObject(stringHandle, out objectHandle));

            JavaScriptValueSafeHandle prototypeHandle;
            Errors.ThrowIfError(Jsrt.JsGetPrototype(objectHandle, out prototypeHandle));

            Assert.True(prototypeHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(prototypeHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Object);

            prototypeHandle.Dispose();
            objectHandle.Dispose();
            stringHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanSetObjectPrototype()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            //Create a new mammal object to use as a prototype.
            JavaScriptValueSafeHandle mammalHandle;
            Errors.ThrowIfError(Jsrt.JsCreateObject(out mammalHandle));

            string isMammal = "isMammal";
            JavaScriptPropertyIdSafeHandle isMammalPropertyHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(isMammal, new UIntPtr((uint)isMammal.Length), out isMammalPropertyHandle));

            JavaScriptValueSafeHandle trueHandle;
            Errors.ThrowIfError(Jsrt.JsGetTrueValue(out trueHandle));

            //Set the prototype of cat to be mammal.
            Errors.ThrowIfError(Jsrt.JsSetProperty(mammalHandle, isMammalPropertyHandle, trueHandle, false));

            JavaScriptValueSafeHandle catHandle;
            Errors.ThrowIfError(Jsrt.JsCreateObject(out catHandle));
            
            Errors.ThrowIfError(Jsrt.JsSetPrototype(catHandle, mammalHandle));

            //Assert that the prototype of cat is mammal, and that cat now contains a isMammal property set to true.
            JavaScriptValueSafeHandle catPrototypeHandle;
            Errors.ThrowIfError(Jsrt.JsGetPrototype(catHandle, out catPrototypeHandle));

            Assert.True(catPrototypeHandle == mammalHandle);

            JavaScriptValueSafeHandle catIsMammalHandle;
            Errors.ThrowIfError(Jsrt.JsGetProperty(catHandle, isMammalPropertyHandle, out catIsMammalHandle));

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(catIsMammalHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Boolean);

            bool catIsMammal;
            Errors.ThrowIfError(Jsrt.JsBooleanToBool(catIsMammalHandle, out catIsMammal));

            Assert.True(catIsMammal);

            mammalHandle.Dispose();
            isMammalPropertyHandle.Dispose();
            trueHandle.Dispose();
            catHandle.Dispose();
            catPrototypeHandle.Dispose();
            catIsMammalHandle.Dispose();
            //Whew!

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanDetermineInstanceOf()
        {
            var script = @"
function Mammal() {
  this.isMammal = 'yes';
}

function MammalSpecies(sMammalSpecies) {
  this.species = sMammalSpecies;
}

MammalSpecies.prototype = new Mammal();
MammalSpecies.prototype.constructor = MammalSpecies;

var oCat = new MammalSpecies('Felis');
";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);

            JavaScriptPropertyIdSafeHandle oCatPropertyHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8("oCat", new UIntPtr((uint)"oCat".Length), out oCatPropertyHandle));

            JavaScriptPropertyIdSafeHandle fnMammalSpeciesPropertyHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8("MammalSpecies", new UIntPtr((uint)"MammalSpecies".Length), out fnMammalSpeciesPropertyHandle));


            JavaScriptValueSafeHandle globalHandle;
            Errors.ThrowIfError(Jsrt.JsGetGlobalObject(out globalHandle));

            JavaScriptValueSafeHandle fnMammalSpeciesHandle;
            Errors.ThrowIfError(Jsrt.JsGetProperty(globalHandle, fnMammalSpeciesPropertyHandle, out fnMammalSpeciesHandle));

            JavaScriptValueSafeHandle oCatHandle;
            Errors.ThrowIfError(Jsrt.JsGetProperty(globalHandle, oCatPropertyHandle, out oCatHandle));

            bool result;
            Errors.ThrowIfError(Jsrt.JsInstanceOf(oCatHandle, fnMammalSpeciesHandle, out result));

            Assert.True(result);

            oCatPropertyHandle.Dispose();
            fnMammalSpeciesPropertyHandle.Dispose();

            oCatHandle.Dispose();
            fnMammalSpeciesHandle.Dispose();
            globalHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanDetermineIfObjectIsExtensible()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objectHandle;
            Errors.ThrowIfError(Jsrt.JsCreateObject(out objectHandle));


            bool isExtensible;
            Errors.ThrowIfError(Jsrt.JsGetExtensionAllowed(objectHandle, out isExtensible));

            Assert.True(isExtensible);

            objectHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanMakeObjectNonExtensible()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objectHandle;
            Errors.ThrowIfError(Jsrt.JsCreateObject(out objectHandle));

            Errors.ThrowIfError(Jsrt.JsPreventExtension(objectHandle));

            bool isExtensible;
            Errors.ThrowIfError(Jsrt.JsGetExtensionAllowed(objectHandle, out isExtensible));

            Assert.False(isExtensible);

            objectHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanGetProperty()
        {
            var script = @"(() => {
var obj = {
    'foo': 'bar',
    'baz': 'qux',
    'quux': 'quuz',
    'corge': 'grault',
    'waldo': 'fred',
    'plugh': 'xyzzy',
    'lol': 'kik'
};
return obj;
})();
";
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);

            var propertyName = "plugh";
            JavaScriptPropertyIdSafeHandle propertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyIdHandle));

            JavaScriptValueSafeHandle propertyHandle;
            Errors.ThrowIfError(Jsrt.JsGetProperty(objHandle, propertyIdHandle, out propertyHandle));

            Assert.True(propertyHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(propertyHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.String);

            propertyIdHandle.Dispose();
            propertyHandle.Dispose();
            objHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanGetOwnPropertyDescriptor()
        {
            var script = @"(() => {
var obj = {
    'foo': 'bar',
    'baz': 'qux',
    'quux': 'quuz',
    'corge': 'grault',
    'waldo': 'fred',
    'plugh': 'xyzzy',
    'lol': 'kik'
};
return obj;
})();
";
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);

            var propertyName = "corge";
            JavaScriptPropertyIdSafeHandle propertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyIdHandle));

            JavaScriptValueSafeHandle propertyDescriptorHandle;
            Errors.ThrowIfError(Jsrt.JsGetOwnPropertyDescriptor(objHandle, propertyIdHandle, out propertyDescriptorHandle));

            Assert.True(propertyDescriptorHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(propertyDescriptorHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Object);

            propertyIdHandle.Dispose();
            propertyDescriptorHandle.Dispose();
            objHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanGetOwnPropertyNames()
        {
            var script = @"(() => {
var obj = {
    'foo': 'bar',
    'baz': 'qux',
    'quux': 'quuz',
    'corge': 'grault',
    'waldo': 'fred',
    'plugh': 'xyzzy',
    'lol': 'kik'
};
return obj;
})();
";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);

            JavaScriptValueSafeHandle propertySymbols;
            Errors.ThrowIfError(Jsrt.JsGetOwnPropertyNames(objHandle, out propertySymbols));

            Assert.True(propertySymbols != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType propertySymbolsType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(propertySymbols, out propertySymbolsType));

            Assert.True(propertySymbolsType == JavaScriptValueType.Array);

            propertySymbols.Dispose();
            objHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanSetProperty()
        {
            var script = @"(() => {
var obj = {
    'foo': 'bar',
    'baz': 'qux',
    'quux': 'quuz',
    'corge': 'grault',
    'waldo': 'fred',
    'plugh': 'xyzzy',
    'lol': 'kik'
};
return obj;
})();
";
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);

            var propertyName = "baz";
            JavaScriptPropertyIdSafeHandle propertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyIdHandle));

            JavaScriptValueSafeHandle newPropertyValue;
            Errors.ThrowIfError(Jsrt.JsDoubleToNumber(3.14159, out newPropertyValue));

            //Set the property
            Errors.ThrowIfError(Jsrt.JsSetProperty(objHandle, propertyIdHandle, newPropertyValue, true));


            JavaScriptValueSafeHandle propertyHandle;
            Errors.ThrowIfError(Jsrt.JsGetProperty(objHandle, propertyIdHandle, out propertyHandle));

            Assert.True(propertyHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(propertyHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Number);
            
            propertyIdHandle.Dispose();
            propertyHandle.Dispose();
            objHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanDetermineIfPropertyExists()
        {
            var script = @"(() => {
var obj = {
    'foo': 'bar',
    'baz': 'qux',
    'quux': 'quuz',
    'corge': 'grault',
    'waldo': 'fred',
    'plugh': 'xyzzy',
    'lol': 'kik'
};
return obj;
})();
";
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);

            var propertyName = "lol";
            JavaScriptPropertyIdSafeHandle propertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyIdHandle));

            bool propertyExists;
            Errors.ThrowIfError(Jsrt.JsHasProperty(objHandle, propertyIdHandle, out propertyExists));

            Assert.True(propertyExists);

            propertyName = "asdf";
            propertyIdHandle.Dispose();
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyIdHandle));

            Errors.ThrowIfError(Jsrt.JsHasProperty(objHandle, propertyIdHandle, out propertyExists));
            Assert.False(propertyExists);


            propertyIdHandle.Dispose();
            objHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanDeleteProperty()
        {
            var script = @"(() => {
var obj = {
    'foo': 'bar',
    'baz': 'qux',
    'quux': 'quuz',
    'corge': 'grault',
    'waldo': 'fred',
    'plugh': 'xyzzy',
    'lol': 'kik'
};
return obj;
})();
";
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);

            var propertyName = "waldo";
            JavaScriptPropertyIdSafeHandle propertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyIdHandle));

            JavaScriptValueSafeHandle propertyDeletedHandle;
            Errors.ThrowIfError(Jsrt.JsDeleteProperty(objHandle, propertyIdHandle, true, out propertyDeletedHandle));

            bool wasPropertyDeleted;
            Errors.ThrowIfError(Jsrt.JsBooleanToBool(propertyDeletedHandle,out wasPropertyDeleted));
            Assert.True(wasPropertyDeleted);


            bool propertyExists;
            Errors.ThrowIfError(Jsrt.JsHasProperty(objHandle, propertyIdHandle, out propertyExists));
            Assert.False(propertyExists);

            propertyDeletedHandle.Dispose();
            propertyIdHandle.Dispose();
            objHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanDefineProperty()
        {
            var script = @"(() => {
var obj = {
    'foo': 'bar'
};
return obj;
})();
";
            var propertyDef = @"(() => {
var obj = {
  enumerable: false,
  configurable: false,
  writable: false,
  value: 'static'
};
return obj;
})();
";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle objHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);
            JavaScriptValueSafeHandle propertyDefHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, propertyDef);

            var propertyName = "rico";
            JavaScriptPropertyIdSafeHandle propertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyIdHandle));

            bool result;
            Errors.ThrowIfError(Jsrt.JsDefineProperty(objHandle, propertyIdHandle, propertyDefHandle, out result));

            Assert.True(result);


            bool propertyExists;
            Errors.ThrowIfError(Jsrt.JsHasProperty(objHandle, propertyIdHandle, out propertyExists));
            Assert.True(propertyExists);

            propertyDefHandle.Dispose();
            propertyIdHandle.Dispose();
            objHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanDetermineIfIndexedPropertyExists()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            //Test on array
            JavaScriptValueSafeHandle arrayHandle;
            Errors.ThrowIfError(Jsrt.JsCreateArray(10, out arrayHandle));

            JavaScriptValueSafeHandle arrayIndexHandle;
            Errors.ThrowIfError(Jsrt.JsIntToNumber(0, out arrayIndexHandle));

            //array[0] = 0;
            Errors.ThrowIfError(Jsrt.JsSetIndexedProperty(arrayHandle, arrayIndexHandle, arrayIndexHandle));

            bool hasArrayIndex;
            Errors.ThrowIfError(Jsrt.JsHasIndexedProperty(arrayHandle, arrayIndexHandle, out hasArrayIndex));

            Assert.True(hasArrayIndex);

            Errors.ThrowIfError(Jsrt.JsIntToNumber(10, out arrayIndexHandle));

            Errors.ThrowIfError(Jsrt.JsHasIndexedProperty(arrayHandle, arrayIndexHandle, out hasArrayIndex));

            Assert.False(hasArrayIndex);

            arrayIndexHandle.Dispose();
            arrayHandle.Dispose();

            //Test on object as associative array.
            JavaScriptValueSafeHandle objectHandle;
            Errors.ThrowIfError(Jsrt.JsCreateObject(out objectHandle));

            string propertyName = "foo";
            JavaScriptPropertyIdSafeHandle propertyIdHandle;
            Errors.ThrowIfError(Jsrt.JsCreatePropertyIdUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyIdHandle));

            JavaScriptValueSafeHandle propertyNameHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(propertyName, new UIntPtr((uint)propertyName.Length), out propertyNameHandle));

            string notAPropertyName = "bar";
            JavaScriptValueSafeHandle notAPropertyNameHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(notAPropertyName, new UIntPtr((uint)notAPropertyName.Length), out notAPropertyNameHandle));

            string propertyValue = "Some people choose to see the ugliness in this world. The disarray. I choose to see the beauty.";
            JavaScriptValueSafeHandle propertyValueHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(propertyValue, new UIntPtr((uint)propertyValue.Length), out propertyValueHandle));

            Errors.ThrowIfError(Jsrt.JsSetProperty(objectHandle, propertyIdHandle, propertyValueHandle, true));

            bool hasObjectIndex;
            Errors.ThrowIfError(Jsrt.JsHasIndexedProperty(objectHandle, propertyNameHandle, out hasObjectIndex));

            Assert.True(hasObjectIndex);

            Errors.ThrowIfError(Jsrt.JsHasIndexedProperty(objectHandle, notAPropertyNameHandle, out hasObjectIndex));

            Assert.False(hasObjectIndex);

            propertyIdHandle.Dispose();
            propertyNameHandle.Dispose();
            notAPropertyNameHandle.Dispose();
            propertyValueHandle.Dispose();
            objectHandle.Dispose();


            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanRetrieveIndexedProperty()
        {
            var script = @"(() => {
var arr = ['Arnold', 'Bernard', 'Charlotte', 'Delores', 'Elsie', 'Felix', 'Hector', 'Lee', 'Maeve', 'Peter', 'Robert', 'Sylvester', 'Teddy', 'Wyatt'];
return arr;
})();
";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle arrayHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);

            JavaScriptValueSafeHandle arrayIndexHandle;
            Errors.ThrowIfError(Jsrt.JsIntToNumber(10, out arrayIndexHandle));

            JavaScriptValueSafeHandle valueHandle;
            Errors.ThrowIfError(Jsrt.JsGetIndexedProperty(arrayHandle, arrayIndexHandle, out valueHandle));

            Assert.True(valueHandle != JavaScriptValueSafeHandle.Invalid);

            var result = Extensions.IJavaScriptRuntimeExtensions.GetStringUtf8(Jsrt, valueHandle);

            Assert.Equal("Robert", result);

            valueHandle.Dispose();
            arrayIndexHandle.Dispose();
            arrayHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanSetIndexedProperty()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle arrayHandle;
            Errors.ThrowIfError(Jsrt.JsCreateArray(50, out arrayHandle));

            var value = "The Bicameral Mind";
            JavaScriptValueSafeHandle valueHandle;
            Errors.ThrowIfError(Jsrt.JsCreateStringUtf8(value, new UIntPtr((uint)value.Length), out valueHandle));

            JavaScriptValueSafeHandle arrayIndexHandle;
            Errors.ThrowIfError(Jsrt.JsIntToNumber(42, out arrayIndexHandle));

            Errors.ThrowIfError(Jsrt.JsSetIndexedProperty(arrayHandle, arrayIndexHandle, valueHandle));

            JavaScriptValueSafeHandle resultHandle;
            Errors.ThrowIfError(Jsrt.JsGetIndexedProperty(arrayHandle, arrayIndexHandle, out resultHandle));

            Assert.True(valueHandle != JavaScriptValueSafeHandle.Invalid);

            var result = Extensions.IJavaScriptRuntimeExtensions.GetStringUtf8(Jsrt, valueHandle);

            Assert.Equal("The Bicameral Mind", result);

            resultHandle.Dispose();
            valueHandle.Dispose();
            arrayIndexHandle.Dispose();
            arrayHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsCanDeleteIndexedProperty()
        {
            var script = @"(() => {
var arr = ['Arnold', 'Bernard', 'Charlotte', 'Delores', 'Elsie', 'Felix', 'Hector', 'Lee', 'Maeve', 'Peter', 'Robert', 'Sylvester', 'Teddy', 'Wyatt'];
return arr;
})();
";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle arrayHandle = Extensions.IJavaScriptRuntimeExtensions.JsRunScript(Jsrt, script);

            JavaScriptValueSafeHandle arrayIndexHandle;
            Errors.ThrowIfError(Jsrt.JsIntToNumber(12, out arrayIndexHandle));

            Errors.ThrowIfError(Jsrt.JsDeleteIndexedProperty(arrayHandle, arrayIndexHandle));

            JavaScriptValueSafeHandle valueHandle;
            Errors.ThrowIfError(Jsrt.JsGetIndexedProperty(arrayHandle, arrayIndexHandle, out valueHandle));
            
            Assert.True(valueHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueSafeHandle undefinedHandle;
            Errors.ThrowIfError(Jsrt.JsGetUndefinedValue(out undefinedHandle));

            Assert.Equal(undefinedHandle, valueHandle);

            undefinedHandle.Dispose();
            valueHandle.Dispose();
            arrayIndexHandle.Dispose();
            arrayHandle.Dispose();

            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsArrayCanBeCreated()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle arrayHandle;
            Errors.ThrowIfError(Jsrt.JsCreateArray(50, out arrayHandle));

            Assert.True(arrayHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(arrayHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.Array);

            arrayHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsArrayBufferCanBeCreated()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle arrayBufferHandle;
            Errors.ThrowIfError(Jsrt.JsCreateArrayBuffer(50, out arrayBufferHandle));

            Assert.True(arrayBufferHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(arrayBufferHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.ArrayBuffer);

            arrayBufferHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsExternalArrayBufferCanBeCreated()
        {
            var data = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            IntPtr ptrScript = Marshal.StringToHGlobalAnsi(data);
            try
            {
                JavaScriptValueSafeHandle externalArrayBufferHandle;
                Errors.ThrowIfError(Jsrt.JsCreateExternalArrayBuffer(ptrScript, (uint)data.Length, null, IntPtr.Zero, out externalArrayBufferHandle));
                Assert.True(externalArrayBufferHandle != JavaScriptValueSafeHandle.Invalid);

                JavaScriptValueType handleType;
                Errors.ThrowIfError(Jsrt.JsGetValueType(externalArrayBufferHandle, out handleType));

                Assert.True(handleType == JavaScriptValueType.ArrayBuffer);

                externalArrayBufferHandle.Dispose();
            }
            finally
            {
                contextHandle.Dispose();
                runtimeHandle.Dispose();
            }
        }

        [Fact]
        public void JsTypedArrayCanBeCreated()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle typedArrayHandle;
            Errors.ThrowIfError(Jsrt.JsCreateTypedArray(JavaScriptTypedArrayType.Int8, JavaScriptValueSafeHandle.Invalid, 0, 50, out typedArrayHandle));

            Assert.True(typedArrayHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(typedArrayHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.TypedArray);

            typedArrayHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsDataViewCanBeCreated()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle arrayBufferHandle;
            Errors.ThrowIfError(Jsrt.JsCreateArrayBuffer(50, out arrayBufferHandle));

            JavaScriptValueSafeHandle dataViewHandle;
            Errors.ThrowIfError(Jsrt.JsCreateDataView(arrayBufferHandle, 0, 50, out dataViewHandle));

            Assert.True(dataViewHandle != JavaScriptValueSafeHandle.Invalid);

            JavaScriptValueType handleType;
            Errors.ThrowIfError(Jsrt.JsGetValueType(dataViewHandle, out handleType));

            Assert.True(handleType == JavaScriptValueType.DataView);

            arrayBufferHandle.Dispose();
            dataViewHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsTypedArrayInfoCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle typedArrayHandle;
            Errors.ThrowIfError(Jsrt.JsCreateTypedArray(JavaScriptTypedArrayType.Int8, JavaScriptValueSafeHandle.Invalid, 0, 50, out typedArrayHandle));


            JavaScriptTypedArrayType typedArrayType;
            JavaScriptValueSafeHandle arrayBufferHandle;
            uint byteOffset, byteLength;
            Errors.ThrowIfError(Jsrt.JsGetTypedArrayInfo(typedArrayHandle, out typedArrayType, out arrayBufferHandle, out byteOffset, out byteLength));

            Assert.True(typedArrayType == JavaScriptTypedArrayType.Int8);
            Assert.True(arrayBufferHandle != JavaScriptValueSafeHandle.Invalid);
            Assert.True(byteOffset == 0);
            Assert.True(byteLength == 50);

            arrayBufferHandle.Dispose();
            typedArrayHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsArrayBufferStorageCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle arrayBufferHandle;
            Errors.ThrowIfError(Jsrt.JsCreateArrayBuffer(50, out arrayBufferHandle));

            IntPtr ptrBuffer;
            uint bufferLength;
            Errors.ThrowIfError(Jsrt.JsGetArrayBufferStorage(arrayBufferHandle, out ptrBuffer, out bufferLength));

            byte[] buffer = new byte[bufferLength];
            Marshal.Copy(ptrBuffer, buffer, 0, (int)bufferLength);

            Assert.True(bufferLength == 50);
            Assert.True(buffer.Length == 50);

            arrayBufferHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsTypedArrayStorageCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle typedArrayHandle;
            Errors.ThrowIfError(Jsrt.JsCreateTypedArray(JavaScriptTypedArrayType.Int8, JavaScriptValueSafeHandle.Invalid, 0, 50, out typedArrayHandle));

            IntPtr ptrBuffer;
            uint bufferLength;
            JavaScriptTypedArrayType typedArrayType;
            int elementSize;
            Errors.ThrowIfError(Jsrt.JsGetTypedArrayStorage(typedArrayHandle, out ptrBuffer, out bufferLength, out typedArrayType, out elementSize));

            //Normally, we'd create an appropriately typed buffer based on elementsize.
            Assert.True(elementSize == 1); //byte

            byte[] buffer = new byte[bufferLength];
            Marshal.Copy(ptrBuffer, buffer, 0, (int)bufferLength);

            Assert.True(bufferLength == 50);
            Assert.True(buffer.Length == 50);
            Assert.True(typedArrayType == JavaScriptTypedArrayType.Int8);
            

            typedArrayHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }

        [Fact]
        public void JsDataViewStorageCanBeRetrieved()
        {
            JavaScriptRuntimeSafeHandle runtimeHandle;
            Errors.ThrowIfError(Jsrt.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtimeHandle));

            JavaScriptContextSafeHandle contextHandle;
            Errors.ThrowIfError(Jsrt.JsCreateContext(runtimeHandle, out contextHandle));
            Errors.ThrowIfError(Jsrt.JsSetCurrentContext(contextHandle));

            JavaScriptValueSafeHandle arrayBufferHandle;
            Errors.ThrowIfError(Jsrt.JsCreateArrayBuffer(50, out arrayBufferHandle));

            JavaScriptValueSafeHandle dataViewHandle;
            Errors.ThrowIfError(Jsrt.JsCreateDataView(arrayBufferHandle, 0, 50, out dataViewHandle));

            IntPtr ptrBuffer;
            uint bufferLength;
            Errors.ThrowIfError(Jsrt.JsGetDataViewStorage(dataViewHandle, out ptrBuffer, out bufferLength));

            byte[] buffer = new byte[bufferLength];
            Marshal.Copy(ptrBuffer, buffer, 0, (int)bufferLength);

            Assert.True(bufferLength == 50);
            Assert.True(buffer.Length == 50);

            dataViewHandle.Dispose();
            arrayBufferHandle.Dispose();
            contextHandle.Dispose();
            runtimeHandle.Dispose();
        }
    }
}
