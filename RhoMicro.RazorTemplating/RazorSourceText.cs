// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

internal sealed class RazorSourceText : AdditionalText
{
    private RazorSourceText(RazorTemplate template, SourceText sourceText)
    {
        // Id = Guid.NewGuid().ToString("N");
        // Directory = $"Template_{Id}";
        Template = template;
        // Path = $"{Directory}/{Template.Name}.razor";
        Path = $"{Template.Name}.razor";
        _sourceText = sourceText/*.WithChanges(
            new TextChange(
                TextSpan.FromBounds(0, 0),
                $"@attribute [global::{RazorTemplateAttribute.FullName}(\"{Id}\")]\n"))*/;
    }

    private readonly SourceText _sourceText;
    public RazorTemplate Template { get; }
    // public String Directory { get; }
    // public String Id { get; }

    public static async ValueTask<RazorSourceText> Create(
        RazorTemplate template,
        CancellationToken ct)
    {
        var sourceText = await template.GetText(ct).ConfigureAwait(false);
        var result = new RazorSourceText(template, sourceText);
        return result;
    }

    public override SourceText GetText(CancellationToken cancellationToken = default) => _sourceText;
    public override String Path { get; }

    public override String ToString() => Template.Name;
}
