// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

internal sealed class InMemoryAnalyzerConfigOptions(ImmutableDictionary<String, String> values) : AnalyzerConfigOptions
{
    public static AnalyzerConfigOptions Create(params ImmutableDictionary<String, String> values)
        => new InMemoryAnalyzerConfigOptions(values);

    public override Boolean TryGetValue(
        String key,
        [NotNullWhen(true)] out String? value)
        => values.TryGetValue(key, out value);

    public override IEnumerable<String> Keys => values.Keys;
}
