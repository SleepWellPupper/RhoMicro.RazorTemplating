// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Renders razor source text templates to HTML.
/// </summary>
public static class RazorTemplateRenderer
{
    /// <summary>
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </summary>
    /// <param name="text">
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </param>
    /// <param name="parameters">
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </param>
    /// <param name="ct">
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </param>
    /// <returns>
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </returns>
    public static ValueTask<String> Render(
        String text,
        IEnumerable<KeyValuePair<String, Object?>> parameters,
        CancellationToken ct = default)
        => Render(text, parameters, static s => { }, ct);

    /// <summary>
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </summary>
    /// <param name="text">
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </param>
    /// <param name="ct">
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </param>
    /// <returns>
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </returns>
    public static ValueTask<String> Render(
        String text,
        CancellationToken ct = default)
        => Render(text, [], static s => { }, ct);

    /// <summary>
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </summary>
    /// <param name="text">
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </param>
    /// <param name="configureServices">
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </param>
    /// <param name="ct">
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </param>
    /// <returns>
    /// <inheritdoc cref="Render(String, IEnumerable{KeyValuePair{String, Object}}, Action{IServiceCollection}, CancellationToken)"/>
    /// </returns>
    public static ValueTask<String> Render(
        String text,
        Action<IServiceCollection> configureServices,
        CancellationToken ct = default)
        => Render(text, [], configureServices, ct);

    /// <summary>
    /// Renders a razor template source text to HTML source.
    /// </summary>
    /// <param name="text">
    /// The template source text to render.
    /// </param>
    /// <param name="parameters">
    /// The parameters to provide to the rendered component.
    /// </param>
    /// <param name="configureServices">
    /// A callback invoked to configure the used services used when rendering.
    /// </param>
    /// <param name="ct">
    /// The cancellation token used to request rendering to be canceled.
    /// </param>
    /// <returns>
    /// A task that, upon completion, will return the rendered HTML source.
    /// </returns>
    public static async ValueTask<String> Render(
        String text,
        IEnumerable<KeyValuePair<String, Object?>> parameters,
        Action<IServiceCollection> configureServices,
        CancellationToken ct = default)
    {
        const String name = "Template";

        var services = new ServiceCollection()
            .AddRazorTemplating()
            .AddSingleton<IRazorTemplateProvider>(
                new InMemoryRazorTemplateProvider(
                    RazorTemplate.Create(name, text)))
            .AddLogging(l => l.ClearProviders());

        configureServices.Invoke(services);

        var result = await services
            .BuildServiceProvider()
            .GetRequiredService<IRazorTemplateRenderer>()
            .Render(name, parameters, ct);

        return result;
    }
}
