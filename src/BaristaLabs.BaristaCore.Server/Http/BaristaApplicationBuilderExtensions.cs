﻿namespace BaristaLabs.BaristaCore.Http
{
    using JavaScript;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public static class BaristaApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseBarista(this IApplicationBuilder app)
        {
            var evalRouteHandler = new RouteHandler(context =>
            {
                //var jsr = context.RequestServices.GetRequiredService<JavaScriptRuntime>();
                //using (var ctx = jsr.CreateContext())
                //{
                //    using (var jsContext = ctx.AcquireExecutionContext())
                //    {
                //        if (context.Request.Body != null && context.Request.Body.CanRead)
                //        {
                //            //Attempt to locate the code from the body.
                //        }
                //        var code = context.GetRouteValue("c");

                //        if (code == null)
                //            code = "1+41";

                //        var codeToExecute = code as string;

                //        JavaScriptValue jsValue;
                //        try
                //        {
                //            var fn = ctx.Evaluate(new ScriptSource("[eval code]", codeToExecute));

                //            jsValue = fn.Invoke(Enumerable.Empty<JavaScriptValue>());
                //        }
                //        catch(Exception)
                //        {
                //            //Catch the exception but don't do any additional processing.
                //            //ExecuteResultAsync should pick the exception up.
                //            jsValue = ctx.UndefinedValue;
                //        }

                //        var result = new JavaScriptValueResult(jsValue);
                //        return result.ExecuteResultAsync(context);
                //    }
                //}

                return Task.FromResult(42);
            });

            var routeBuilder = new RouteBuilder(app, evalRouteHandler);

            routeBuilder.MapRoute(
                "BaristaCore Eval Route",
                "api/eval/{c?}");

            var routes = routeBuilder.Build();
            app.UseRouter(routes);

            return app;
        }
    }
}