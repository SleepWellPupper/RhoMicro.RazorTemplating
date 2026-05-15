// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

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
    }
}
