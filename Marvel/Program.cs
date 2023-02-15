var builder = WebApplication.CreateBuilder(args);

builder.AddSerilog();

builder.Services.AddDaprClient(null);

builder.Services.AddHealthChecks();

builder.Services.AddFastEndpoints(o =>
{
    o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
});
builder.Services.AddSwaggerDoc(shortSchemaNames: true);

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
    c.Endpoints.ShortNames = true;
});


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseOpenApi();
    app.UseSwaggerUi3(s => s.ConfigureDefaults());
}

app.MapHealthChecks("/health/startup");
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = _ => false });

app.UseSerilogRequestLogging();

app.Run();