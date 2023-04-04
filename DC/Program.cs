﻿var builder = WebApplication.CreateBuilder(args);

builder.AddSerilog();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient(null);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRouting();
//app.UseAuthorization();



if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health/startup");
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = _ => false });

app.MapStrongestEndpoint();

app.UseSerilogRequestLogging();

app.Run();