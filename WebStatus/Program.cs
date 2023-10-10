var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services
    .AddHealthChecksUI()
    .AddInMemoryStorage();

builder.Logging.AddJsonConsole();


var app = builder.Build();

app.UseForwardedHeaders();

var pathBase = builder.Configuration["PATH_BASE"];
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseHealthChecksUI(config =>
{
    config.ResourcesPath = string.IsNullOrEmpty(pathBase)
        ? "/ui/resources"
        : $"{pathBase}/ui/resources";
});

app.MapGet(string.IsNullOrEmpty(pathBase)
    ? "/"
    : pathBase, () => Results.LocalRedirect("~/healthchecks-ui"));

app.MapHealthChecksUI();

app.Run();
