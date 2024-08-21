﻿var builder = WebApplication.CreateBuilder(args);

builder.AddSerilog();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient();

builder.Services.AddHealthChecks();

builder.Services.AddProblemDetails();

/*builder.Services.AddRateLimiter(rlo => {
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

});*/

var app = builder.Build();

app.UseForwardedHeaders();

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

//app.UseRateLimiter();

app.MapHealthChecks("/health/startup");
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = _ => false });

app.MapHealthChecks("/hc", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapStrongestEndpoint();

app.UseSerilogRequestLogging();

app.Run();