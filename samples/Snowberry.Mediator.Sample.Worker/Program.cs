using Snowberry.Mediator.Extensions.OpenTelemetry;
using Snowberry.Mediator.Sample.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Aspire service defaults: OpenTelemetry (with the Snowberry.Mediator instrumentation), health checks,
// service discovery and resilient HTTP clients. Exports to the Aspire dashboard over OTLP.
builder.AddServiceDefaults();

// Reflection-free, source-generated mediator registration (opt-in via [assembly: SnowberryMediator]).
builder.Services.AddSnowberryMediator();

// Decorate IMediator with the OpenTelemetry tracing and metrics instrumentation, including per-step spans.
builder.Services.AddSnowberryMediatorOpenTelemetry(options =>
{
    options.EnablePipelineBehaviorSpans = true;
    options.EnableNotificationHandlerSpans = true;
});

builder.Services.AddHostedService<OrderWorker>();

var host = builder.Build();
host.Run();
