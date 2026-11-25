using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EfCoreCompatibility.Api.Diagnostics;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DevelopmentOnlyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}
