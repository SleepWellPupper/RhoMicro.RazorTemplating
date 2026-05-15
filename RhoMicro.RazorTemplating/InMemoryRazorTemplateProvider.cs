// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

/// <summary>
/// Provides razor templates from an in-memory collection.
/// Upon disposal, all templates provided will be disposed.
/// </summary>
/// <param name="templates">
/// The templates to provide.
/// </param>
public sealed class InMemoryRazorTemplateProvider(params IEnumerable<RazorTemplate> templates)
    : IRazorTemplateProvider, IDisposable
{
    private readonly Dictionary<String, RazorTemplate> _templates = templates.ToDictionary(t => t.Name);

    /// <inheritdoc/>
    public async ValueTask<RazorTemplate> LoadTemplate(String name, CancellationToken ct = default)
        => _templates[name];

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var template in _templates.Values.OfType<IDisposable>())
        {
            template.Dispose();
        }
    }
}
