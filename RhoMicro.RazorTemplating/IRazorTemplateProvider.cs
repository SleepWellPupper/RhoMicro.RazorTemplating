// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

/// <summary>
/// Provides razor templates for rendering.
/// </summary>
public interface IRazorTemplateProvider
{
    /// <summary>
    /// Loads a template with the given name.
    /// </summary>
    /// <remarks>
    /// If no template with the provided name could be loaded, the specific provider implementation
    /// <i>may</i> throw an exception.
    /// </remarks>
    /// <param name="name">
    /// The name of the template.
    /// </param>
    /// <param name="ct">
    /// The cancellation token used to request loading to be canceled.
    /// </param>
    /// <returns>
    /// A task that, upon completion, will return the loaded razor template.
    /// </returns>
    ValueTask<RazorTemplate> LoadTemplate(String name, CancellationToken ct = default);
}
