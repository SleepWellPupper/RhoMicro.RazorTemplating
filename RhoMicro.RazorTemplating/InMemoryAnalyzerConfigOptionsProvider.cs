// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

internal sealed class InMemoryAnalyzerConfigOptionsProvider(
    ImmutableDictionary<SyntaxTree, AnalyzerConfigOptions> syntaxTreeOptions,
    ImmutableDictionary<AdditionalText, AnalyzerConfigOptions> additionalTextOptions,
    AnalyzerConfigOptions globalOptions)
    : AnalyzerConfigOptionsProvider
{
    public InMemoryAnalyzerConfigOptionsProvider(
        ImmutableDictionary<AdditionalText, AnalyzerConfigOptions> additionalTextOptions,
        AnalyzerConfigOptions globalOptions)
        : this([], additionalTextOptions, globalOptions)
    {
    }

    public override AnalyzerConfigOptions GlobalOptions => globalOptions;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        => syntaxTreeOptions.TryGetValue(tree, out var options)
            ? options
            : GlobalOptions;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        => additionalTextOptions.TryGetValue(textFile, out var options)
            ? options
            : GlobalOptions;
}
