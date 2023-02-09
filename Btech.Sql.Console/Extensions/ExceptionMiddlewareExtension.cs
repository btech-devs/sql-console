using Btech.Sql.Console.Models.Responses.Base;
using Btech.Sql.Console.Utils;
using Microsoft.AspNetCore.Diagnostics;

namespace Btech.Sql.Console.Extensions;

public static class ExceptionMiddlewareExtension
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app) =>
        app.UseExceptionHandler(
            appError =>
            {
                appError.Run(
                    async context =>
                    {
                        IExceptionHandlerFeature contextFeature = context.Features.Get<IExceptionHandlerFeature>();

                        if (contextFeature != null)
                        {
                            await AuditNotifier.ReportExceptionAsync(contextFeature.Error, $"{nameof(ExceptionMiddlewareExtension)}.{nameof(UseExceptionMiddleware)}");

                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode = 200;

                            Response response = new Response
                            {
                                ErrorMessage = contextFeature.Error.Message
                            };

                            await context.Response.WriteAsync(response.JsonSerialize());
                        }
                    });
            });
}