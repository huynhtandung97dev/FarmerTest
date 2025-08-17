using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace FarmerTest.Helpers
{
    public static class ControllerExtensions
    {
        public static async Task<string> RenderPartialViewAsync(
            this Controller controller, string viewName, object model)
        {
            controller.ViewData.Model = model;

            await using var writer = new StringWriter();
            var sp = controller.HttpContext.RequestServices;
            var engine = sp.GetRequiredService<ICompositeViewEngine>();

            var viewResult = engine.FindView(controller.ControllerContext, viewName, isMainPage: false);
            if (!viewResult.Success)
                throw new InvalidOperationException($"Partial view '{viewName}' not found.");

            var viewContext = new ViewContext(
                controller.ControllerContext,
                viewResult.View,
                controller.ViewData,
                controller.TempData,
                writer,
                new HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);
            return writer.ToString();
        }
    }
}
