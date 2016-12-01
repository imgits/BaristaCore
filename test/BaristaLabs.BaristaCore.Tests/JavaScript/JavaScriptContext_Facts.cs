﻿namespace BaristaLabs.BaristaCore.JavaScript.Tests
{
    using BaristaCore.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using Xunit;

    public class JavaScriptContext_Facts
    {
        private IServiceProvider Provider;

        public JavaScriptContext_Facts()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBaristaCore();

            Provider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void JavaScriptContextCanBeCreated()
        {
            using (var rt = JavaScriptRuntime.CreateRuntime(Provider))
            {
                using (var ctx = rt.CreateContext())
                {

                }
            }
            Assert.True(true);
        }

        [Fact]
        public void JavaScriptContextShouldParseAndInvokeScriptText()
        {
            using (var rt = JavaScriptRuntime.CreateRuntime(Provider))
            {
                using (var ctx = rt.CreateContext())
                {
                    using (ctx.Scope())
                    {
                        var script = "41+1";
                        var fn = ctx.ParseScriptText(script);

                        //Assert that the function is the script we passed in.
                        var fnText = fn.ToString();
                        Assert.Equal(script, fnText);

                        //Invoke it.
                        dynamic result = fn.Invoke();
                        Assert.True((int)result == 42);
                    }
                }
            }
        }

        [Fact]
        public void JsPropertyCanBeRetrievedByName()
        {
            //            var script = @"( () => { return {
            //    foo: 'bar',
            //    baz: 'qix'
            //  };
            //})()";
            //            string result;

            using (var rt = JavaScriptRuntime.CreateRuntime(Provider))
            {
                using (var ctx = rt.CreateContext())
                {
            //        using (var xc = ctx.AcquireExecutionContext())
            //        {
            //            JavaScriptValueSafeHandle obj;
            //            Errors.ThrowIfIs(ChakraApi.Instance.JsRunScript(script, JavaScriptSourceContext.None, null, JsParseScriptAttributes.JsParseScriptAttributeNone, out obj));

            //            JavaScriptValueSafeHandle foo;
            //            Errors.ThrowIfIs(ChakraApi.Instance.JsCreateStringUtf16("foo", new UIntPtr((uint)"foo".Length), out foo));

            //            JavaScriptValueSafeHandle propertyHandle;
            //            Errors.ThrowIfIs(ChakraApi.Instance.JsGetIndexedProperty(obj, foo,out propertyHandle));
            //            UIntPtr size;
            //            Errors.ThrowIfIs(ChakraApi.Instance.JsCopyStringUtf16(propertyHandle, 0, -1, null, out size));

            //            if ((int)size * 2 > int.MaxValue)
            //                throw new OutOfMemoryException("Exceeded maximum string length.");

            //            byte[] propertyValue = new byte[(int)size * 2];
            //            UIntPtr written;
            //            Errors.ThrowIfIs(ChakraApi.Instance.JsCopyStringUtf16(propertyHandle, 0, -1, propertyValue, out written));
            //            result = Encoding.Unicode.GetString(propertyValue, 0, propertyValue.Length);
            //        }
                }
            }

            //Assert.True("bar" == result);
        }

        [Fact]
        public void JsPropertyDescriptorCanBeRetrieved()
        {
            //            var script = @"( () => { return {
            //    foo: 'bar',
            //    baz: 'qix'
            //  };
            //})()";
            //            string result;

            //            using (var rt = new JavaScriptRuntime())
            //            {
            //                using (var ctx = rt.CreateContext())
            //                {
            //                    using (var xc = ctx.AcquireExecutionContext())
            //                    {
            //                        JavaScriptValueSafeHandle obj;
            //                        Errors.ThrowIfIs(ChakraApi.Instance.JsRunScript(script, JavaScriptSourceContext.None, null, JsParseScriptAttributes.JsParseScriptAttributeNone, out obj));

            //                        JavaScriptValueSafeHandle propertyDescriptor;
            //                        var propertyId = JavaScriptPropertyIdSafeHandle.FromString("foo");
            //                        Errors.ThrowIfIs(ChakraApi.Instance.JsGetOwnPropertyDescriptor(obj, propertyId, out propertyDescriptor));

            //                        dynamic desc = ctx.CreateValueFromHandle(propertyDescriptor) as JavaScriptObject;
            //                        result = (string)desc.value;
            //                    }
            //                }
            //            }

            //            Assert.True("bar" == result);
        }
    }
}
