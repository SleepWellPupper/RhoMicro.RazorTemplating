// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Represents a razor template.
/// </summary>
public abstract class RazorTemplate : IDisposable
{
    private sealed class StringImplementation(
        String name,
        String text)
        : RazorTemplate(name)
    {
        internal override async ValueTask<SourceText> GetText(CancellationToken ct) => SourceText.From(text);
    }

    private sealed class StreamImplementation(
        String name,
        Stream source)
        : RazorTemplate(name)
    {
        internal override async ValueTask<SourceText> GetText(CancellationToken ct)
        {
            using var reader = new StreamReader(source, leaveOpen: true);
            var text = await reader.ReadToEndAsync(ct);
            var result = SourceText.From(text);
            return result;
        }
    }

    private RazorTemplate(String name)
    {
        Name = name;
    }

    private ComponentTypeLifetime? _component;
    private readonly SemaphoreSlim _gate = new(1, 1);
    public String Name { get; }
    public ImmutableHashSet<String> Dependencies { get; private init; } = [];

    /// <summary>
    /// Creates a razor template.
    /// </summary>
    /// <param name="name">
    /// The name of the template.
    /// </param>
    /// <param name="text">
    /// The source text of the template.
    /// </param>
    /// <param name="dependencies">
    /// The names of templates this template depends on.
    /// </param>
    /// <returns>
    /// A new razor template with the specified name and source text.
    /// </returns>
    public static RazorTemplate Create(
        String name,
        String text,
        params ImmutableHashSet<String> dependencies) =>
        new StringImplementation(name, text) { Dependencies = dependencies };

    /// <summary>
    /// Creates a razor template.
    /// </summary>
    /// <param name="name">
    /// The name of the template.
    /// </param>
    /// <param name="textSource">
    /// The stream providing the source text of the template.
    /// </param>
    /// <param name="dependencies">
    /// The names of templates this template depends on.
    /// </param>
    /// <returns>
    /// A new razor template with the specified name and source text.
    /// </returns>
    public static RazorTemplate Create(
        String name,
        Stream textSource,
        params ImmutableHashSet<String> dependencies) =>
        new StreamImplementation(name, textSource) { Dependencies = dependencies };

    internal async ValueTask<ComponentTypeLifetime> GetComponentType(RazorTemplateCompiler compiler, CancellationToken ct = default)
    {
        if (_component is { } component)
        {
            return component;
        }

        var result = await CompileComponentType(compiler, ct);

        return result;
    }

    private async ValueTask<ComponentTypeLifetime> CompileComponentType(RazorTemplateCompiler compiler, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);

        try
        {
            if (_component is { } component)
            {
                return component;
            }

            _component = await compiler.Compile(this, ct);

            return _component;
        }
        finally
        {
            _gate.Release();
        }
    }

    internal abstract ValueTask<SourceText> GetText(CancellationToken ct);

    public override String ToString() => Name;

    void IDisposable.Dispose()
    {
        _gate.Dispose();
        _component?.Dispose();
    }
}
