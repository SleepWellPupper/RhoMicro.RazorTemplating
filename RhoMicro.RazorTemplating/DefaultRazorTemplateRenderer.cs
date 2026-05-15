// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

internal sealed partial class DefaultRazorTemplateRenderer(
    RazorTemplateRendererContext context,
    IServiceProvider services,
    ILoggerFactory loggers,
    ILogger<DefaultRazorTemplateRenderer> logger)
    : IRazorTemplateRenderer
{
    public async ValueTask<String> Render(
        String name,
        IEnumerable<KeyValuePair<String, Object?>> parameters,
        CancellationToken ct = default)
    {
        var now = Stopwatch.GetTimestamp();
        LogRendering(logger, name);

        var componentTypeLifetime = await context.GetComponentType(name, ct).ConfigureAwait(false);

        if (componentTypeLifetime is null)
        {
            throw new InvalidOperationException($"Unable to determine component type for `{name}`.");
        }

        using var _ = componentTypeLifetime.PreventDisposal();

        var parametersMap = parameters.ToDictionary();

        String html;
        var htmlRenderer = new HtmlRenderer(services, loggers);
        await using (htmlRenderer.ConfigureAwait(false))
        {
            html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameterView = ParameterView.FromDictionary(parametersMap);
                var output = await htmlRenderer.RenderComponentAsync(componentTypeLifetime.Type, parameterView)
                    .ConfigureAwait(false);

                return output.ToHtmlString();
            }).ConfigureAwait(false);
        }

        var elapsed = Stopwatch.GetElapsedTime(now);
        LogDoneRendering(
            logger,
            name,
            elapsed);

        return html;
    }

    [LoggerMessage(LogLevel.Information, "Rendering `{name}`.")]
    private static partial void LogRendering(ILogger<DefaultRazorTemplateRenderer> logger, String name);

    [LoggerMessage(LogLevel.Information, "Done rendering `{name}`, elapsed: {elapsed}.")]
    private static partial void LogDoneRendering(
        ILogger<DefaultRazorTemplateRenderer> logger,
        String name,
        TimeSpan elapsed);
}
