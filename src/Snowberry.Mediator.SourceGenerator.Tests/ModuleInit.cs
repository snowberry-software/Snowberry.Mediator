using System.Runtime.CompilerServices;

namespace Snowberry.Mediator.SourceGenerator.Tests;

internal static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
