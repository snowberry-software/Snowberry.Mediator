; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 2.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SBMED001 | SnowberryMediator | Error | Duplicate request handler for the same request/response.
SBMED002 | SnowberryMediator | Error | Duplicate stream request handler for the same request/response.
SBMED101 | SnowberryMediator | Info | Handler type is inaccessible to the consuming assembly and was skipped.
SBMED102 | SnowberryMediator | Warning | A handler's generic argument is inaccessible and the handler was skipped.
SBMED103 | SnowberryMediator | Info | A non-instantiable type implements a handler interface and was skipped.
SBMED201 | SnowberryMediator | Warning | Multiple [SnowberryMediator] assembly attributes; the first one is used.
SBMED202 | SnowberryMediator | Info | The generator was triggered but discovered no handlers.
