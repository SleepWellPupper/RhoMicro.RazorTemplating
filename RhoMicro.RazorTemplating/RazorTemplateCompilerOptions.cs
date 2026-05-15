// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

public sealed record RazorTemplateCompilerOptions(
    String RootNamespace = "",
    DiagnosticSeverity MinimumLoggingDiagnosticSeverity = DiagnosticSeverity.Error,
    ImmutableHashSet<String>? GlobalUsings = null,
    ImmutableHashSet<Assembly>? ReferenceAssemblies = null)
{
    public static RazorTemplateCompilerOptions Default { get; } = new(
        GlobalUsings:
        [
            // "System.Net.Http.Json",
            // "Microsoft.AspNetCore.Builder",
            // "Microsoft.AspNetCore.Hosting",
            // "Microsoft.AspNetCore.Http",
            // "Microsoft.AspNetCore.Routing",
            // "Microsoft.Extensions.Configuration",
            // "Microsoft.Extensions.DependencyInjection",
            // "Microsoft.Extensions.Hosting",
            "Microsoft.Extensions.Logging",
        ],
        ReferenceAssemblies:
        [
            typeof(ILogger).Assembly
        ]);
}
