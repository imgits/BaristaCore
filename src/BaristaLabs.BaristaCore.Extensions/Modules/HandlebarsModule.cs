﻿namespace BaristaLabs.BaristaCore.Modules
{
    using System.Threading.Tasks;

    //https://cdnjs.cloudflare.com/ajax/libs/handlebars.js/4.0.11/handlebars.js
    [BaristaModule("handlebars", "Handlebars provides the power necessary to let you build semantic templates effectively with no frustration", Version = "4.0.11")]
    public class HandlebarsModule : INodeModule
    {
        public async Task<object> ExportDefault(BaristaContext context, BaristaModuleRecord referencingModule)
        {
            return await EmbeddedResourceHelper.LoadResourceAsync(this, "BaristaLabs.BaristaCore.Scripts.handlebars.min.js");
        }
    }
}
