using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilog();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient(null);

builder.Services.AddHealthChecks();

builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(rlo => {
    rlo.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    rlo.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Request.Headers.Host.ToString(), 
            factory: partition =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 4,
                AutoReplenishment = true,
                Window = TimeSpan.FromSeconds(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
    });

    
    rlo.OnRejected = async (context, token) =>
    {
        context.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("Microsoft.AspNetCore.RateLimitingMiddleware").LogWarning("OnRejected: {RequestPath}", context.HttpContext.Request.Path);
    };

});


var app = builder.Build();

app.UseRouting();
//app.UseAuthorization();



app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/health/startup");
    endpoints.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
    endpoints.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = _ => false });

    endpoints.MapHealthChecks("/hc", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    endpoints.MapStrongestEndpoint();


});

//app.MapHealthChecks("/health/startup");
//app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
//app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = _ => false });

//app.MapHealthChecks("/hc", new HealthCheckOptions
//{
//    Predicate = _ => true,
//    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
//});

//app.MapStrongestEndpoint();



app.UseSerilogRequestLogging();

app.Run();