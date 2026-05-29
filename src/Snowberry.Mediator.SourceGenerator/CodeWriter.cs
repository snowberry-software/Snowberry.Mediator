using System.Text;

namespace Snowberry.Mediator.SourceGenerator;

/// <summary>Minimal indentation-aware text writer for emitting generated C#.</summary>
internal sealed class CodeWriter
{
    private readonly StringBuilder _builder = new();
    private int _indent;

    /// <summary>Writes a line at the current indentation, or a blank line when <paramref name="text"/> is empty.</summary>
    /// <param name="text">The line text.</param>
    /// <returns>The same writer, for chaining.</returns>
    public CodeWriter Line(string text = "")
    {
        if (text.Length == 0)
        {
            _builder.Append('\n');
            return this;
        }

        for (int i = 0; i < _indent; i++)
            _builder.Append("    ");

        _builder.Append(text).Append('\n');
        return this;
    }

    /// <summary>Writes a header line followed by an opening brace, then increases indentation.</summary>
    /// <param name="text">The header line preceding the brace (for example, a namespace or method declaration).</param>
    /// <returns>The same writer, for chaining.</returns>
    public CodeWriter Open(string text)
    {
        Line(text);
        Line("{");
        _indent++;
        return this;
    }

    /// <summary>Emits a single opening brace and increases indentation (for object initializers and switch bodies).</summary>
    /// <returns>The same writer, for chaining.</returns>
    public CodeWriter OpenBrace()
    {
        Line("{");
        _indent++;
        return this;
    }

    /// <summary>Decreases indentation and writes a closing brace followed by <paramref name="suffix"/>.</summary>
    /// <param name="suffix">Text appended after the closing brace (for example, <c>);</c> or <c>;</c>).</param>
    /// <returns>The same writer, for chaining.</returns>
    public CodeWriter Close(string suffix = "")
    {
        _indent--;
        Line("}" + suffix);
        return this;
    }

    /// <inheritdoc/>
    public override string ToString() => _builder.ToString();
}
