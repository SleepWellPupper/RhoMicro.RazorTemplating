// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

/// <summary>
/// Renders razor templates.
/// </summary>
public interface IRazorTemplateRenderer
{
    /// <summary>
    /// Renders a razor template with the given name.
    /// </summary>
    /// <param name="name">
    /// The name of the template to render.
    /// </param>
    /// <param name="parameters">
    /// The parameters to provide to the template when rendering.
    /// </param>
    /// <param name="ct">
    /// The cancellation token used to request rendering to be canceled.
    /// </param>
    /// <returns>
    /// A task that, upon completion, will contain the rendered HTML source
    /// obtained by rendering the specified component.
    /// </returns>
    ValueTask<String> Render(
        String name,
        IEnumerable<KeyValuePair<String, Object?>> parameters,
        CancellationToken ct = default);
}
