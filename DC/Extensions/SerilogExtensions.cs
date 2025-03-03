namespace DC.Extensions;

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Settings.Configuration;
using Serilog.Sinks.Grafana.Loki;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder, string sectionName = "Serilog")
    {
        var serilogOptions = new SerilogOptions();
        builder.Configuration.GetSection(sectionName).Bind(serilogOptions);

        builder.Host.UseSerilog((context, loggerConfiguration) =>
        {
            var options = new ConfigurationReaderOptions { SectionName = sectionName };
            loggerConfiguration.ReadFrom.Configuration(context.Configuration, options);

            loggerConfiguration
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails();

            loggerConfiguration.MinimumLevel.Override("Microsoft", LogEventLevel.Information);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);

            if (serilogOptions.UseConsole)
            {
                loggerConfiguration.WriteTo.Async(writeTo =>
                {
                    writeTo.Console(outputTemplate: serilogOptions.LogTemplate);
                });
            }

            if (!string.IsNullOrEmpty(serilogOptions.SeqUrl))
            {
                loggerConfiguration.WriteTo.Seq(serilogOptions.SeqUrl);
            }

            if (!string.IsNullOrEmpty(serilogOptions.LokiUrl))
            {
                loggerConfiguration.WriteTo.GrafanaLoki(serilogOptions.LokiUrl,
                    labels: new List<LokiLabel>
                    {
                        new() { Key = "app", Value = "DC" },
                    });
            }
        });

        return builder;
    }

    public static WebApplication UseSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(opts =>
        {
            opts.EnrichDiagnosticContext = SerilogExtensions.EnrichFromRequest;
            opts.GetLevel = SerilogExtensions.ExcludeHealthChecks;
        });

        return app;
    }

    private static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        var request = httpContext.Request;

        // Set all the common properties available for every request
        diagnosticContext.Set("Host", request.Host);
        diagnosticContext.Set("Protocol", request.Protocol);
        diagnosticContext.Set("Scheme", request.Scheme);

        // Only set it if available. You're not sending sensitive data in a querystring right?!
        if (request.QueryString.HasValue)
        {
            diagnosticContext.Set("QueryString", request.QueryString.Value);
        }

        // Set the content-type of the Response at this point
        diagnosticContext.Set("ContentType", httpContext.Response.ContentType!);

        // Retrieve the IEndpointFeature selected for the request
        var endpoint = httpContext.GetEndpoint();
        if (endpoint is not null) // endpoint != null
        {
            diagnosticContext.Set("EndpointName", endpoint.DisplayName!);
        }
    }

    private static LogEventLevel ExcludeHealthChecks(HttpContext ctx, double _, Exception? ex)
    {
        if (ex != null)
        {
            return LogEventLevel.Error;
        }

        if (ctx.Response.StatusCode > 499)
        {
            return LogEventLevel.Error;
        }

        return LogEventLevel.Debug;
    }

    internal sealed class SerilogOptions
    {
        public bool UseConsole { get; set; } = true;
        public string? SeqUrl { get; set; }
        public string? LokiUrl { get; set; }

        public string LogTemplate { get; set; } =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3} - {Message:lj}{NewLine}{Exception}";
    }
}