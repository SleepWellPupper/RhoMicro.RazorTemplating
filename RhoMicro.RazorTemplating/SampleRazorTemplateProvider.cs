// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

internal sealed class SampleRazorTemplateProvider : IRazorTemplateProvider
{
    public async ValueTask<RazorTemplate> LoadTemplate(String name, CancellationToken ct = default)
        => RazorTemplate.Create(
            name,
            """
            <div>
                Hello, @(Name)!
                Make sure to register an implementation for IRazorTemplateProvider to provide your own razor templates.
            </div>

            @code
            {
                [Parameter] public string Name { get; set; } = "World";
            }
            """);
}
