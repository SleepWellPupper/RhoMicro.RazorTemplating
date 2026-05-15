// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides options to the razor template compiler.
/// </summary>
/// <param name="RootNamespace">
/// The root namespace to emit template components into.
/// </param>
/// <param name="MinimumLoggingDiagnosticSeverity">
/// The minimum severity that diagnostics emitted during the template compilation
/// process must have in order to be logged.
/// </param>
/// <param name="GlobalUsings">
/// A set of global usings to be emitted into the template context when compiling.
/// </param>
/// <param name="ReferenceAssemblies">
/// A set of assemblies to be referenced during compilation of templates.
/// </param>
public sealed record RazorTemplateCompilerOptions(
    String RootNamespace = "",
    DiagnosticSeverity MinimumLoggingDiagnosticSeverity = DiagnosticSeverity.Error,
    ImmutableHashSet<String>? GlobalUsings = null,
    ImmutableHashSet<Assembly>? ReferenceAssemblies = null)
{
    /// <summary>
    /// Gets the default options instance.
    /// </summary>
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
