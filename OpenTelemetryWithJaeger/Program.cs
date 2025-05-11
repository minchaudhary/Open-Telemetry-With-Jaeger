using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace OpenTelemetryWithJaeger;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .AddSource("MyApp") // Your application's source name
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName: "MyService", serviceVersion: "1.0.0"))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddJaegerExporter(jaegerOptions =>
                    {
                        jaegerOptions.AgentHost = "localhost"; // Jaeger agent host
                        jaegerOptions.AgentPort = 6831; // Default Jaeger agent UDP port
                                                        // Optional: If using HTTP endpoint directly
                                                        // jaegerOptions.Endpoint = new Uri("http://localhost:14268/api/traces");
                    });
            });

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapGet("/", () => "Hello World!");

        // Example traced endpoint
        app.MapGet("/trace-me", async (ILogger<Program> logger) =>
        {
            using var activity = Activity.Current?.Source.StartActivity("TraceMe");
            logger.LogInformation("This is a traced endpoint");

            // Simulate some work
            await Task.Delay(100);

            return Results.Ok(new { Message = "Traced successfully!" });
        });
        app.Run();
    }
}
