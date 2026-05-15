// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

/// <summary>
/// Provides razor templates from an in-memory collection.
/// </summary>
/// <param name="templates">
/// The templates to provide.
/// </param>
public sealed class InMemoryRazorTemplateProvider(params IEnumerable<RazorTemplate> templates) : IRazorTemplateProvider
{
    private readonly Dictionary<String, RazorTemplate> _templates = templates.ToDictionary(t => t.Name);

    /// <inheritdoc/>
    public async ValueTask<RazorTemplate> LoadTemplate(String name, CancellationToken ct = default)
        => _templates[name];
}
