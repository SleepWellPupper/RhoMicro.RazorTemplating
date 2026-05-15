// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Basic.Reference.Assemblies;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

internal sealed partial class RazorTemplateCompiler(
    IRazorTemplateProvider templates,
    ILogger<RazorTemplateCompiler> logger,
    RazorTemplateCompilerOptions options)
{
    readonly struct RazorTemplateDependencyTreeBuilder(
        IRazorTemplateProvider templates,
        RazorTemplate root,
        CancellationToken ct)
    {
        private readonly Dictionary<String, RazorTemplate> _dependencies = [];

        public async ValueTask CollectDependencies()
        {
            ct.ThrowIfCancellationRequested();

            await CollectDependencies(root).ConfigureAwait(false);
        }

        private async ValueTask CollectDependencies(RazorTemplate template)
        {
            ct.ThrowIfCancellationRequested();

            if (!_dependencies.TryAdd(template.Name, template))
            {
                return;
            }

            foreach (var dependency in template.Dependencies)
            {
                ct.ThrowIfCancellationRequested();

                if (_dependencies.ContainsKey(dependency))
                {
                    continue;
                }

                var dependencyTemplate = await templates.LoadTemplate(dependency).ConfigureAwait(false);

                await CollectDependencies(dependencyTemplate).ConfigureAwait(false);
            }
        }

        public ImmutableArray<RazorTemplate> GetDependencySequence()
        {
            ct.ThrowIfCancellationRequested();

            var result = new RazorTemplate[_dependencies.Count - 1];
            var i = -1;

            SequentializeDependencies(
                root.Name,
                [],
                result,
                ref i);

            return ImmutableCollectionsMarshal.AsImmutableArray(result);
        }

        void SequentializeDependencies(
            String name,
            HashSet<String> visited,
            RazorTemplate[] result,
            ref Int32 i)
        {
            ct.ThrowIfCancellationRequested();

            if (visited.Contains(name))
            {
                return;
            }

            var template = _dependencies[name];

            foreach (var dependency in template.Dependencies)
            {
                SequentializeDependencies(
                    dependency,
                    visited,
                    result,
                    ref i);
            }

            visited.Add(name);
            if (++i < result.Length)
            {
                result[i] = template;
            }
        }

        public ImmutableArray<String> GetCycles()
        {
            ct.ThrowIfCancellationRequested();

            var resultBuilder = ImmutableArray.CreateBuilder<String>();
            GetCycles(root.Name, [], resultBuilder);
            return resultBuilder.DrainToImmutable();
        }

        private void GetCycles(String name, Stack<String> path, ImmutableArray<String>.Builder resultBuilder)
        {
            ct.ThrowIfCancellationRequested();

            if (path.Contains(name))
            {
                var cycle = String.Join("->", path.Reverse().Append(name));
                resultBuilder.Add(cycle);
                return;
            }

            path.Push(name);

            var template = _dependencies[name];
            foreach (var dependency in template.Dependencies)
            {
                ct.ThrowIfCancellationRequested();

                GetCycles(dependency, path, resultBuilder);
            }

            _ = path.Pop();
        }
    }

    public async ValueTask<ComponentTypeLifetime> Compile(RazorTemplate root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var now = Stopwatch.GetTimestamp();
        LogCompiling(logger, root);

        var dependencies = await GetDependencies(root, ct).ConfigureAwait(false);

        LogResolvedTemplateDependencies(logger, dependencies);

        var dependencyReferences = new Dictionary<String, MetadataReference>(dependencies.Length);
        var additionalPeStreams = new Stream[dependencies.Length];

        await CompileDependencies(
            dependencies,
            dependencyReferences,
            additionalPeStreams,
            ct).ConfigureAwait(false);

        ComponentTypeLifetime result;
        var peStream = new MemoryStream();
        await using (peStream.ConfigureAwait(false))
        {
            await Compile(root, dependencyReferences, peStream, ct).ConfigureAwait(false);
            result = ComponentTypeLifetime.Create(peStream, additionalPeStreams);
        }

        foreach (var additionalPeStream in additionalPeStreams)
        {
            ct.ThrowIfCancellationRequested();

            await additionalPeStream.DisposeAsync().ConfigureAwait(false);
        }

        var elapsed = Stopwatch.GetElapsedTime(now);
        LogDoneCompiling(logger, root, elapsed);

        return result;
    }

    private async ValueTask CompileDependencies(
        ImmutableArray<RazorTemplate> dependencies,
        Dictionary<String, MetadataReference> dependencyReferences,
        Stream[] additionalPeStreams,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        for (var i = 0; i < dependencies.Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            var dependency = dependencies[i];

            var now = Stopwatch.GetTimestamp();
            LogCompilingDependency(logger, dependency.Name);

            await CompileDependency(
                dependency,
                dependencyReferences,
                additionalPeStreams,
                i,
                ct).ConfigureAwait(false);

            var elapsedTime = Stopwatch.GetElapsedTime(now);
            LogDoneCompilingDependency(logger, dependency.Name, elapsedTime);
        }
    }

    private async ValueTask CompileDependency(
        RazorTemplate dependency,
        Dictionary<String, MetadataReference> dependencyReferences,
        Stream[] additionalPeStreams,
        Int32 i,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var additionalPeStream = new MemoryStream();
        await Compile(dependency, dependencyReferences, additionalPeStream, ct).ConfigureAwait(false);

        additionalPeStream.Seek(0, SeekOrigin.Begin);
        using (var copyStream = new MemoryStream())
        {
            await additionalPeStream.CopyToAsync(copyStream, ct).ConfigureAwait(false);
            copyStream.Seek(0, SeekOrigin.Begin);
            var reference = MetadataReference.CreateFromStream(copyStream);
            dependencyReferences.Add(dependency.Name, reference);
        }

        additionalPeStream.Seek(0, SeekOrigin.Begin);
        additionalPeStreams[i] = additionalPeStream;
    }

    private async ValueTask<ImmutableArray<RazorTemplate>> GetDependencies(RazorTemplate root, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var depTreeBuilder = new RazorTemplateDependencyTreeBuilder(
            templates,
            root,
            ct);

        await depTreeBuilder.CollectDependencies().ConfigureAwait(false);

        if (depTreeBuilder.GetCycles() is [_, ..] cycles)
        {
            LogDetectedCyclicalDependencies(logger, cycles);

            throw new InvalidOperationException(
                $"Unable to compile `{root.Name}`, as there are cyclical dependencies: {String.Join(", ", cycles)}");
        }

        var dependencies = depTreeBuilder.GetDependencySequence();

        return dependencies;
    }

    private async ValueTask Compile(
        RazorTemplate razorTemplate,
        Dictionary<String, MetadataReference> dependencyReferences,
        MemoryStream peStream,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var metadataReferences = GetMetadataReferences(razorTemplate, dependencyReferences, ct);
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest)
            .WithFeatures([new("use-roslyn-tokenizer", "true")]);
        var initialCompilation = CreateInitialCompilation(razorTemplate, parseOptions, metadataReferences, ct);
        var razorSourceText = await RazorSourceText.Create(razorTemplate, ct).ConfigureAwait(false);
        var generatorDriver = CreateGeneratorDriver(razorSourceText, parseOptions, ct);
        var compilation = CreateFinalCompilation(generatorDriver, initialCompilation, ct);
        EmitAssembly(compilation, peStream, ct);
    }

    private ImmutableArray<MetadataReference> GetMetadataReferences(
        RazorTemplate template,
        Dictionary<String, MetadataReference> dependencyReferences,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var resultBuilder = ImmutableArray.CreateBuilder<MetadataReference>();
        resultBuilder.Add(MetadataReference.CreateFromFile(typeof(ComponentBase).Assembly.Location));
        resultBuilder.AddRange(Net100.References.All);

        foreach (var dependency in template.Dependencies)
        {
            ct.ThrowIfCancellationRequested();

            resultBuilder.Add(dependencyReferences[dependency]);
        }

        foreach (var referenceAssembly in options.ReferenceAssemblies ?? [])
        {
            ct.ThrowIfCancellationRequested();

            resultBuilder.Add(MetadataReference.CreateFromFile(referenceAssembly.Location));
        }

        var result = resultBuilder.DrainToImmutable();

        return result;
    }

    private void EmitAssembly(
        Compilation compilation,
        MemoryStream peStream,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var emitResult = compilation.Emit(peStream, cancellationToken: ct);
        foreach (var diag in emitResult.Diagnostics)
        {
            ct.ThrowIfCancellationRequested();

            LogEmitDiagnosticReported(diag);
        }

        if (!emitResult.Success)
        {
            ThrowUnableToEmitAssembly();
        }
    }

    private static void ThrowUnableToEmitAssembly() => throw new InvalidOperationException("Unable to emit assembly.");

    private Compilation CreateFinalCompilation(
        CSharpGeneratorDriver generatorDriver,
        CSharpCompilation initialCompilation,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _ = generatorDriver.RunGeneratorsAndUpdateCompilation(initialCompilation,
            out var compilation,
            out var generatorDiagnostics,
            ct);

        var hasErrors = false;

        foreach (var generatorDiagnostic in generatorDiagnostics)
        {
            ct.ThrowIfCancellationRequested();

            LogGeneratorDiagnosticReported(generatorDiagnostic);

            if (generatorDiagnostic.Severity is DiagnosticSeverity.Error)
            {
                hasErrors = true;
            }
        }

        if (hasErrors)
        {
            ThrowUnableToEmitAssembly();
        }

        return compilation;
    }

    private CSharpGeneratorDriver CreateGeneratorDriver(
        RazorSourceText razorSourceText,
        CSharpParseOptions parseOptions,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var generatorDriver = CSharpGeneratorDriver.Create(
            generators:
            [
                new RazorSourceGenerator().AsSourceGenerator()
            ],
            additionalTexts:
            [
                razorSourceText
            ],
            optionsProvider: new InMemoryAnalyzerConfigOptionsProvider(
                additionalTextOptions:
                [
                    KeyValuePair.Create<AdditionalText, AnalyzerConfigOptions>(
                        razorSourceText,
                        InMemoryAnalyzerConfigOptions.Create(
                            KeyValuePair.Create(
                                "build_metadata.AdditionalFiles.TargetPath",
                                Convert.ToBase64String(
                                    Encoding.UTF8.GetBytes(razorSourceText.Template.Name))))),
                ],
                globalOptions: new InMemoryAnalyzerConfigOptions(
                [
                    KeyValuePair.Create("build_property.RazorLangVersion", "Latest"),
                    KeyValuePair.Create("build_property.RootNamespace", options.RootNamespace),
                ])),
            parseOptions: parseOptions);

        return generatorDriver;
    }

    private CSharpCompilation CreateInitialCompilation(
        RazorTemplate template,
        CSharpParseOptions parseOptions,
        ImmutableArray<MetadataReference> metadataReferences,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var usings = GetUsings(ct);
        var initialCompilation = CSharpCompilation.Create(
            assemblyName: $"RazorTemplateAssembly_{template.Name}",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(usings, options: parseOptions, cancellationToken: ct),
            ],
            references: metadataReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return initialCompilation;
    }

    private String GetUsings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var resultBuilder = new StringBuilder();

        foreach (var @using in options.GlobalUsings ?? [])
        {
            ct.ThrowIfCancellationRequested();

            resultBuilder.AppendLine(CultureInfo.InvariantCulture, $"global using {@using};");
        }

        var result = resultBuilder.ToString();

        return result;
    }

    private Boolean TryGetLogLevel(Diagnostic diagnostic, out LogLevel logLevel)
    {
        if (diagnostic.Severity < options.MinimumLoggingDiagnosticSeverity)
        {
            logLevel = default;
            return false;
        }

        logLevel = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => LogLevel.Error,
            DiagnosticSeverity.Warning => LogLevel.Warning,
            _ => LogLevel.Information
        };
        return true;
    }

    private void LogGeneratorDiagnosticReported(Diagnostic diagnostic)
    {
        if (!TryGetLogLevel(diagnostic, out var logLevel))
        {
            return;
        }

        LogGeneratorDiagnosticReported(logger, logLevel, diagnostic);
    }

    private void LogEmitDiagnosticReported(Diagnostic diagnostic)
    {
        if (!TryGetLogLevel(diagnostic, out var logLevel))
        {
            return;
        }

        LogEmitDiagnosticReported(logger, logLevel, diagnostic);
    }

    [LoggerMessage("Generator diagnostic reported: {Diagnostic}")]
    private static partial void LogGeneratorDiagnosticReported(
        ILogger logger,
        LogLevel logLevel,
        Diagnostic diagnostic);

    [LoggerMessage("Emit diagnostic reported: {Diagnostic}")]
    private static partial void LogEmitDiagnosticReported(
        ILogger logger,
        LogLevel logLevel,
        Diagnostic diagnostic);

    [LoggerMessage(LogLevel.Information, "Compiling root `{Template}`.")]
    private static partial void LogCompiling(
        ILogger logger,
        RazorTemplate template);

    [LoggerMessage(LogLevel.Information, "Done compiling root `{Template}`, elapsed: {elapsed}.")]
    private static partial void LogDoneCompiling(
        ILogger logger,
        RazorTemplate template,
        TimeSpan elapsed);

    [LoggerMessage(LogLevel.Information, "Resolved template dependencies: {dependencies}")]
    static partial void LogResolvedTemplateDependencies(
        ILogger<RazorTemplateCompiler> logger,
        ImmutableArray<RazorTemplate> dependencies);

    [LoggerMessage(LogLevel.Information, "Compiling dependency `{Name}`.")]
    static partial void LogCompilingDependency(ILogger<RazorTemplateCompiler> logger, String name);

    [LoggerMessage(LogLevel.Information, "Done compiling dependency `{name}`, elapsed: {elapsed}.")]
    static partial void LogDoneCompilingDependency(
        ILogger<RazorTemplateCompiler> logger,
        string name,
        TimeSpan elapsed);

    [LoggerMessage(LogLevel.Error, "Detected cyclical dependencies: {cycles}")]
    static partial void LogDetectedCyclicalDependencies(
        ILogger<RazorTemplateCompiler> logger,
        ImmutableArray<String> cycles);
}
