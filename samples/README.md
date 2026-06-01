# Snowberry.Mediator samples

This folder contains runnable samples for Snowberry.Mediator. Open
[`Snowberry.Mediator.Samples.slnx`](Snowberry.Mediator.Samples.slnx) to load every sample together with the
library projects they reference.

| Sample | What it shows |
| --- | --- |
| Source-generator (Native AOT) | Reflection-free registration discovered across assemblies, exercised as a Native AOT smoke test. |
| Aspire worker (OpenTelemetry) | A hosted service that dispatches through the mediator with full OpenTelemetry tracing and metrics, observed live in the .NET Aspire dashboard. |

## Source-generator sample (Native AOT)

A minimal console app that uses `Snowberry.Mediator.SourceGenerator` for reflection-free registration.

- `Snowberry.Mediator.SourceGenerator.Sample.Handlers` is a **separate** assembly with a request handler, a
  notification handler, an open-generic pipeline behavior and an open-generic notification handler, which proves
  cross-assembly discovery.
- `Snowberry.Mediator.SourceGenerator.Sample` is the composition root with `[assembly: SnowberryMediator]` plus
  `services.AddSnowberryMediator()`. There is no `options.Assemblies` and no hand-listed handler types.

### Run (JIT)

```bash
dotnet run --project samples/Snowberry.Mediator.SourceGenerator.Sample
```

### Publish (Native AOT)

```bash
dotnet publish samples/Snowberry.Mediator.SourceGenerator.Sample -c Release -r <rid>
```

The generated registration path runs with **zero dynamic code**: both the build-time trim/AOT analyzers and
the publish-time ILLink/ILCompiler report no `IL2026`/`IL2055`/`IL3050` warnings.

#### Native AOT prerequisites

Native AOT compiles and links native code, so it needs a C/C++ toolchain:

- **Linux** (CI): `clang` and `zlib1g-dev` (`sudo apt-get install -y clang zlib1g-dev`).
- **Windows**: the **Desktop development with C++** workload (MSVC linker and Windows SDK). Run the publish from
  a **Developer Command Prompt / Developer PowerShell**, or ensure `vswhere.exe` is resolvable. Otherwise the
  toolset-discovery script (`vcvarsall.bat`) can invoke a bare `vswhere` that is not on `PATH`, corrupting the
  linker path. Adding the installer directory to `PATH` fixes it:

  ```powershell
  $env:PATH = "C:\Program Files (x86)\Microsoft Visual Studio\Installer;$env:PATH"
  dotnet publish samples/Snowberry.Mediator.SourceGenerator.Sample -c Release -r win-x64
  ```

## Aspire worker sample (OpenTelemetry)

A hosted service (`BackgroundService`) that dispatches orders through the mediator on a timer, wired into
[.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) so the mediator traces and metrics are visible live in
the Aspire dashboard.

- `Snowberry.Mediator.Sample.Worker` is the hosted service. It registers the mediator with the source generator
  (`services.AddSnowberryMediator()`), decorates it with `services.AddSnowberryMediatorOpenTelemetry(...)`
  (turning on per-pipeline-behavior and per-notification-handler spans), and dispatches a `ProcessOrder` request
  followed by an `OrderProcessed` notification every two seconds. A share of orders fail on purpose so error
  spans appear in the dashboard.
- `Snowberry.Mediator.Sample.ServiceDefaults` holds the shared Aspire defaults. Its `ConfigureOpenTelemetry`
  subscribes the OpenTelemetry tracer and meter providers to the Snowberry.Mediator sources through
  `AddSnowberryMediatorInstrumentation()`, then exports everything over OTLP.
- `Snowberry.Mediator.Sample.AppHost` is the Aspire orchestrator. It launches the worker and the dashboard, and
  points the worker at the dashboard's OTLP endpoint.

### Run

```bash
dotnet run --project samples/Snowberry.Mediator.Sample.AppHost
```

The console prints a dashboard URL (for example `http://localhost:15170`). Open it, select the `worker` resource,
and watch the **Traces** and **Metrics** tabs. Each dispatch produces a `Mediator.Send ProcessOrder` span with
child spans for the `LoggingBehavior` pipeline step, plus a `Mediator.Publish OrderProcessed` span with a child
span per notification handler. The `snowberry.mediator.*` counters and duration histograms appear under Metrics.

The `https` launch profile is also available if you have a trusted local development certificate
(`dotnet dev-certs https --trust`):

```bash
dotnet run --project samples/Snowberry.Mediator.Sample.AppHost --launch-profile https
```

### Run the worker standalone (without the dashboard)

The worker runs on its own and exports OTLP to whatever `OTEL_EXPORTER_OTLP_ENDPOINT` points at. With no
endpoint configured it simply runs and logs:

```bash
dotnet run --project samples/Snowberry.Mediator.Sample.Worker
```
