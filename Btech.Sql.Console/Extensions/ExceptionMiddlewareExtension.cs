using Btech.Sql.Console.Models.Responses.Base;
using Btech.Sql.Console.Utils;
using Microsoft.AspNetCore.Diagnostics;

namespace Btech.Sql.Console.Extensions;

/// <summary>
/// Extension method to add exception handling middleware to the ASP.NET Core pipeline.
/// </summary>
/// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
/// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
public static class ExceptionMiddlewareExtension
{
    /// <summary>
    /// The method adds exception handling middleware to the pipeline.
    /// If an exception occurs during request processing,
    /// the middleware catches the exception, reports it to an audit service,
    /// and returns an error message to the client in a JSON format.
    /// </summary>
    /// <param name="app">The application.</param>
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