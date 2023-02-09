using System.Reflection;
using Btech.Sql.Console.Extensions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);

webApplicationBuilder.Services
    .AddAPILayer(webApplicationBuilder.Configuration);

webApplicationBuilder.WebHost
    .UseSerilog(
        (_, loggerConfiguration) =>
        {
            LoggingLevelSwitch loggingLevelSwitch = new();

            loggerConfiguration
                .Enrich.WithProperty(
                    name: "assembly-version",
                    value: Assembly.GetExecutingAssembly().GetName().Version)
                .MinimumLevel.ControlledBy(loggingLevelSwitch)
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] - {SourceContext} - {Message:lj}{NewLine}{Exception}",
                    levelSwitch: loggingLevelSwitch);
        });

WebApplication webApplication = webApplicationBuilder.Build();

webApplication
    .MapFallbackToFile("index.html");

if (webApplicationBuilder.Environment.IsDevelopment())
    webApplication
        .UseDeveloperExceptionPage();

webApplication
    .UseExceptionMiddleware()
    .UseSwagger()
    .UseSwaggerUI(
        options =>
        {
            options
                .SwaggerEndpoint("/swagger/v1/swagger.json", "Btech.Sql.Console v1");
        })
    .UseAPIConfigurations();

webApplication
    .Run();