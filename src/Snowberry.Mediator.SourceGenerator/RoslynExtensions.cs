using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Snowberry.Mediator.SourceGenerator;

/// <summary>Symbol helpers used during handler discovery and code emission.</summary>
internal static class RoslynExtensions
{
    /// <summary>The display format used to render fully-qualified, <c>global::</c>-prefixed type names.</summary>
    public static readonly SymbolDisplayFormat s_FqnFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>Renders the fully-qualified name of a type for emission.</summary>
    /// <param name="type">The type to render.</param>
    /// <returns>The fully-qualified, <c>global::</c>-prefixed type name.</returns>
    public static string Fqn(this ITypeSymbol type) => type.ToDisplayString(s_FqnFormat);

    /// <summary>Renders the unbound open-generic form, for example <c>global::App.LogBehavior&lt;,&gt;</c>.</summary>
    /// <param name="type">The open-generic type definition.</param>
    /// <returns>The unbound open-generic type-of expression.</returns>
    public static string OpenTypeOf(this INamedTypeSymbol type) =>
        type.ConstructUnboundGenericType().ToDisplayString(s_FqnFormat);

    /// <summary>Determines whether a type has type parameters (is an open-generic definition).</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> if the type has type parameters; otherwise <see langword="false"/>.</returns>
    public static bool IsOpenGeneric(this INamedTypeSymbol type) => type.TypeParameters.Length > 0;

    /// <summary>Enumerates every named type reachable from a namespace (child namespaces and nested types).</summary>
    /// <param name="root">The root namespace to walk.</param>
    /// <param name="ct">A token used to cancel the walk.</param>
    /// <returns>The named types in the namespace tree.</returns>
    public static IEnumerable<INamedTypeSymbol> EnumerateAllTypes(this INamespaceSymbol root, CancellationToken ct)
    {
        var stack = new Stack<INamespaceOrTypeSymbol>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var current = stack.Pop();

            foreach (var member in current.GetMembers())
            {
                switch (member)
                {
                    case INamespaceSymbol ns:
                        stack.Push(ns);
                        break;

                    case INamedTypeSymbol type:
                        yield return type;
                        // Nested types.
                        foreach (var nested in type.GetTypeMembers())
                            stack.Push(nested);
                        break;
                }
            }
        }
    }

    /// <summary>Determines whether the assembly directly references Snowberry.Mediator.Abstractions.</summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns><see langword="true"/> if it references the abstractions assembly; otherwise <see langword="false"/>.</returns>
    public static bool ReferencesAbstractions(this IAssemblySymbol assembly)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var referenced in module.ReferencedAssemblies)
            {
                if (referenced.Name == WellKnown.c_AbstractionsAssemblyName)
                    return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a type and all of its (recursive) generic arguments are accessible to the compilation.</summary>
    /// <param name="type">The type to check.</param>
    /// <param name="compilation">The consuming compilation.</param>
    /// <returns><see langword="true"/> if the type and every type argument are accessible; otherwise <see langword="false"/>.</returns>
    public static bool IsFullyAccessible(this ITypeSymbol type, Compilation compilation)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                return true;

            case IArrayTypeSymbol array:
                return array.ElementType.IsFullyAccessible(compilation);

            case INamedTypeSymbol named:
                if (!compilation.IsSymbolAccessibleWithin(named, compilation.Assembly))
                    return false;

                foreach (var arg in named.TypeArguments)
                {
                    if (!arg.IsFullyAccessible(compilation))
                        return false;
                }

                return true;

            default:
                return compilation.IsSymbolAccessibleWithin(type, compilation.Assembly);
        }
    }

    /// <summary>
    /// Determines whether closing <paramref name="openType"/> with <paramref name="args"/> satisfies every
    /// generic constraint (special constraints and constraint types, with type-parameter substitution).
    /// </summary>
    /// <param name="openType">The open-generic type definition being closed.</param>
    /// <param name="args">The candidate type arguments, in type-parameter order.</param>
    /// <param name="compilation">The compilation used to classify conversions.</param>
    /// <returns><see langword="true"/> if all constraints are satisfied; otherwise <see langword="false"/>.</returns>
    public static bool SatisfiesConstraints(this INamedTypeSymbol openType, ImmutableArray<ITypeSymbol> args, Compilation compilation)
    {
        var typeParameters = openType.TypeParameters;
        if (typeParameters.Length != args.Length)
            return false;

        for (int i = 0; i < typeParameters.Length; i++)
        {
            if (!SatisfiesTypeParameter(typeParameters[i], args[i], typeParameters, args, compilation))
                return false;
        }

        return true;
    }

    private static bool SatisfiesTypeParameter(
        ITypeParameterSymbol parameter,
        ITypeSymbol arg,
        ImmutableArray<ITypeParameterSymbol> parameters,
        ImmutableArray<ITypeSymbol> args,
        Compilation compilation)
    {
        if (parameter.HasReferenceTypeConstraint && !arg.IsReferenceType)
            return false;

        if (parameter.HasValueTypeConstraint && (!arg.IsValueType || IsNullableValueType(arg)))
            return false;

        if (parameter.HasUnmanagedTypeConstraint && !(arg.IsUnmanagedType && arg.IsValueType))
            return false;

        if (parameter.HasConstructorConstraint && !HasAccessibleParameterlessConstructor(arg))
            return false;

        foreach (var constraintType in parameter.ConstraintTypes)
        {
            var substituted = Substitute(constraintType, parameters, args, compilation);
            if (substituted is null)
                return false;

            if (SymbolEqualityComparer.Default.Equals(arg, substituted))
                continue;

            if (compilation is not CSharpCompilation csharp)
                return false;

            var conversion = csharp.ClassifyConversion(arg, substituted);
            if (conversion.IsIdentity)
                continue;

            if (conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing))
                continue;

            return false;
        }

        return true;
    }

    private static bool IsNullableValueType(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };

    private static bool HasAccessibleParameterlessConstructor(ITypeSymbol type)
    {
        if (type.IsValueType)
            return true;

        if (type is not INamedTypeSymbol named || named.IsAbstract)
            return false;

        foreach (var ctor in named.InstanceConstructors)
        {
            if (ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public)
                return true;
        }

        return false;
    }

    /// <summary>Substitutes the given type parameters with the given arguments inside a (constraint) type.</summary>
    /// <param name="type">The type to rewrite.</param>
    /// <param name="parameters">The type parameters to replace.</param>
    /// <param name="args">The replacement arguments, aligned with <paramref name="parameters"/>.</param>
    /// <param name="compilation">The compilation used to construct array types.</param>
    /// <returns>The substituted type, or <see langword="null"/> if a component could not be substituted.</returns>
    private static ITypeSymbol? Substitute(
        ITypeSymbol type,
        ImmutableArray<ITypeParameterSymbol> parameters,
        ImmutableArray<ITypeSymbol> args,
        Compilation compilation)
    {
        switch (type)
        {
            case ITypeParameterSymbol tp:
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (SymbolEqualityComparer.Default.Equals(tp, parameters[i]))
                        return args[i];
                }

                return type;

            case IArrayTypeSymbol array:
                var element = Substitute(array.ElementType, parameters, args, compilation);
                return element is null ? null : compilation.CreateArrayTypeSymbol(element, array.Rank);

            case INamedTypeSymbol named when named.IsGenericType:
                var typeArgs = named.TypeArguments;
                var newArgs = new ITypeSymbol[typeArgs.Length];
                for (int i = 0; i < typeArgs.Length; i++)
                {
                    var substituted = Substitute(typeArgs[i], parameters, args, compilation);
                    if (substituted is null)
                        return null;

                    newArgs[i] = substituted;
                }

                return named.ConstructedFrom.Construct(newArgs);

            default:
                return type;
        }
    }
}
