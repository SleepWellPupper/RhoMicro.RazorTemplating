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
            var text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
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
    /// <summary>
    /// Gets the name of this template.
    /// </summary>
    public String Name { get; }
    /// <summary>
    /// Gets the names of templates that this template depends on.
    /// </summary>
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

    internal async ValueTask<ComponentTypeLifetime> GetComponentType(
        IRazorTemplateCompiler compiler,
        CancellationToken ct = default)
    {
        if (_component is { } component)
        {
            return component;
        }

        var result = await CompileComponentType(compiler, ct).ConfigureAwait(false);

        return result;
    }

    private async ValueTask<ComponentTypeLifetime> CompileComponentType(
        IRazorTemplateCompiler compiler,
        CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (_component is { } component)
            {
                return component;
            }

            _component = await compiler.Compile(this, ct).ConfigureAwait(false);

            return _component;
        }
        finally
        {
            _gate.Release();
        }
    }

    internal abstract ValueTask<SourceText> GetText(CancellationToken ct);

    /// <inheritdoc/>
    public override String ToString() => Name;

    /// <summary>
    /// <inheritdoc cref="IDisposable.Dispose"/>
    /// </summary>
    /// <param name="disposing">
    /// Indicates whether the disposal was initiated from the <see cref="Dispose"/> method.
    /// </param>
    protected virtual void Dispose(Boolean disposing)
    {
        if (!disposing)
        {
            return;
        }

        _gate.Dispose();
        _component?.Dispose();
    }

#pragma warning disable CA1063
    void IDisposable.Dispose()
#pragma warning restore CA1063
    {
        GC.SuppressFinalize(this);
        Dispose(disposing: true);
    }
}
