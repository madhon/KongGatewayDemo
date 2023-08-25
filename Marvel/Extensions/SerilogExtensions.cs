namespace Marvel.Extensions
{
    using Serilog.Sinks.Grafana.Loki;

    public static class SerilogExtensions
    {
        public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder, string sectionName = "Serilog")
        {
            var serilogOptions = new SerilogOptions();
            builder.Configuration.GetSection(sectionName).Bind(serilogOptions);

            _ = builder.Host.UseSerilog((context, loggerConfiguration) =>
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
                            new LokiLabel { Key = "app", Value = "Marvell" }
                        });
                }
            });

            return builder;
        }

        private sealed class SerilogOptions
        {
            public bool UseConsole { get; set; } = true;
            public string? SeqUrl { get; set; }
            public string? LokiUrl { get; set; }

            public string LogTemplate { get; set; } =
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3} - {Message:lj}{NewLine}{Exception}";
        }

        public static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
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
            diagnosticContext.Set("ContentType", httpContext.Response.ContentType);

            // Retrieve the IEndpointFeature selected for the request
            var endpoint = httpContext.GetEndpoint();
            if (endpoint is object) // endpoint != null
            {
                diagnosticContext.Set("EndpointName", endpoint.DisplayName);
            }
        }

        public static LogEventLevel ExcludeHealthChecks(HttpContext ctx, double _, Exception? ex) =>
            ex != null
                ? LogEventLevel.Error
                : ctx.Response.StatusCode > 499
                    ? LogEventLevel.Error
                    : LogEventLevel.Debug;
    }
}
