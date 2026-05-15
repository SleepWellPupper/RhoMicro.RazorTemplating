// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using System.Collections.Immutable;

/// <summary>
/// Provides extensions for <see cref="IRazorTemplateRenderer"/> instances.
/// </summary>
public static class RazorTemplateRendererExtensions
{
    extension(IRazorTemplateRenderer renderer)
    {
        /// <inheritdoc cref="IRazorTemplateRenderer.Render"/>
        public ValueTask<String> Render(String name, CancellationToken ct = default)
            => renderer.Render(name, [], ct);

        public ValueTask<String> Render(
            RazorTemplateDefinition definition,
            CancellationToken ct = default)
            => renderer.Render(
                definition.TemplateName,
                definition.GetTemplateParameters(),
                ct);
    }
}

public abstract class RazorTemplateDefinition(String templateName)
{
    public String TemplateName { get; } = templateName;
    public abstract IEnumerable<KeyValuePair<String, Object?>> GetTemplateParameters();
}
