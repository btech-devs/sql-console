namespace Btech.Sql.Console.Extensions;

public static class Extensions
{
    /// <summary>
    /// Wraps <see cref="string.IsNullOrEmpty"/> method.
    /// </summary>
    public static bool IsNullOrEmpty(this string value) => string.IsNullOrEmpty(value);

    // ReSharper disable once InconsistentNaming
    public static IApplicationBuilder UseAPIConfigurations(this IApplicationBuilder applicationBuilder)
    {
        return applicationBuilder
            .UseHttpsRedirection()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(
                endpoints =>
                {
                    endpoints
                        .MapControllers();
                })
            .UseStaticFiles();
    }
}