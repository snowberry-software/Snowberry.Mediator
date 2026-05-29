# Snowberry.Mediator source-generator sample

A minimal app that uses `Snowberry.Mediator.SourceGenerator` for reflection-free registration, exercised as a
Native AOT smoke test.

- `Snowberry.Mediator.SourceGenerator.Sample.Handlers` — a **separate** assembly with a request handler, a
  notification handler, an open-generic pipeline behavior and an open-generic notification handler. Proves
  cross-assembly discovery.
- `Snowberry.Mediator.SourceGenerator.Sample` — the composition root: `[assembly: SnowberryMediator]` +
  `services.AddSnowberryMediator()`. No `options.Assemblies`, no hand-listed handler types.

## Run (JIT)

```bash
dotnet run --project samples/Snowberry.Mediator.SourceGenerator.Sample
```

## Publish (Native AOT)

```bash
dotnet publish samples/Snowberry.Mediator.SourceGenerator.Sample -c Release -r <rid>
```

The generated registration path runs with **zero dynamic code** — both the build-time trim/AOT analyzers and
the publish-time ILLink/ILCompiler report no `IL2026`/`IL2055`/`IL3050` warnings.

### Native AOT prerequisites

Native AOT compiles + links native code, so it needs a C/C++ toolchain:

- **Linux** (CI): `clang` and `zlib1g-dev` (`sudo apt-get install -y clang zlib1g-dev`).
- **Windows**: the **Desktop development with C++** workload (MSVC linker + Windows SDK). Run the publish from
  a **Developer Command Prompt / Developer PowerShell**, or ensure `vswhere.exe` is resolvable — otherwise the
  toolset-discovery script (`vcvarsall.bat`) can invoke a bare `vswhere` that isn't on `PATH`, corrupting the
  linker path. Adding the installer directory to `PATH` fixes it:

  ```powershell
  $env:PATH = "C:\Program Files (x86)\Microsoft Visual Studio\Installer;$env:PATH"
  dotnet publish samples/Snowberry.Mediator.SourceGenerator.Sample -c Release -r win-x64
  ```
