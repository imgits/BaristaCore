﻿namespace BaristaLabs.BaristaCore.Extensions.Tests
{
    using BaristaLabs.BaristaCore.Extensions;
    using BaristaLabs.BaristaCore.ModuleLoaders;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class WebResourceModuleLoader_Facts
    {
        public IBaristaRuntimeFactory GetRuntimeFactory()
        {
            var webResourceModuleLoader = new WebResourceModuleLoader("https://raw.githubusercontent.com/BaristaLabs/curly-octo-umbrella/master/");

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBaristaCore(moduleLoader: webResourceModuleLoader);

            var provider = serviceCollection.BuildServiceProvider();
            return provider.GetRequiredService<IBaristaRuntimeFactory>();
        }

        [Fact]
        public void CanLoadWebResource()
        {
            var script = @"
        import test1 from 'test1.js';

        export default test1;
        ";

            var baristaRuntime = GetRuntimeFactory();

            using (var rt = baristaRuntime.CreateRuntime())
            {
                using (var ctx = rt.CreateContext())
                {
                    using (ctx.Scope())
                    {
                        var result = ctx.EvaluateModule<JsObject>(script);
                        Assert.Equal("hello, world!", result.ToString());
                    }
                }
            }
        }

        [Fact]
        public void CanLoadTypeScriptResource()
        {
            var script = @"
        import greeter from 'Greeter.ts';

        export default greeter.greet();
        ";

            var baristaRuntime = GetRuntimeFactory();

            using (var rt = baristaRuntime.CreateRuntime())
            {
                using (var ctx = rt.CreateContext())
                {
                    using (ctx.Scope())
                    {
                        var result = ctx.EvaluateModule<JsObject>(script);
                        Assert.Equal("Hello, world", result.ToString());
                    }
                }
            }
        }

        [Fact]
        public void CanLoadTextResources()
        {
            var script = @"
        import txt from 'my-text-file.txt';

        export default txt;
        ";

            var baristaRuntime = GetRuntimeFactory();

            using (var rt = baristaRuntime.CreateRuntime())
            {
                using (var ctx = rt.CreateContext())
                {
                    using (ctx.Scope())
                    {
                        var result = ctx.EvaluateModule<JsObject>(script);
                        Assert.Equal("Some text goes here.\n", result.ToString());
                    }
                }
            }
        }

        [Fact]
        public void CanLoadJsonResources()
        {
            var script = @"
        import jsObj from 'test.json';

        export default jsObj;
        ";

            var baristaRuntime = GetRuntimeFactory();

            using (var rt = baristaRuntime.CreateRuntime())
            {
                using (var ctx = rt.CreateContext())
                {
                    using (ctx.Scope())
                    {
                        var result = ctx.EvaluateModule<JsObject>(script);
                        Assert.Equal("bar", result["foo"].ToString());
                    }
                }
            }
        }

        [Fact]
        public void CanLoadBinaryResources()
        {
            var script = @"
        import logo from 'BaristaLabs.png';

        export default logo;
        ";

            var baristaRuntime = GetRuntimeFactory();

            using (var rt = baristaRuntime.CreateRuntime())
            {
                using (var ctx = rt.CreateContext())
                {
                    using (ctx.Scope())
                    {
                        var result = ctx.EvaluateModule<JsObject>(script);
                        Assert.NotNull(result);
                        Assert.True(result.HasBean);
                        result.TryGetBean(out JsExternalObject exObj);

                        Assert.IsType<Blob>(exObj.Target);
                    }
                }
            }
        }

        [Fact]
        public void NestedImportsAreRootedAtInitialPath()
        {
            var script = @"
        import index from 'index.js';

        export default index;
        ";

            var baristaRuntime = GetRuntimeFactory();

            using (var rt = baristaRuntime.CreateRuntime())
            {
                using (var ctx = rt.CreateContext())
                {
                    using (ctx.Scope())
                    {
                        var result = ctx.EvaluateModule<JsObject>(script);
                        Assert.Equal("hello, world!", result["test1"].ToString());
                        Assert.Equal("Another Hello!", result["test2"].ToString());
                    }
                }
            }
        }
    }
}
