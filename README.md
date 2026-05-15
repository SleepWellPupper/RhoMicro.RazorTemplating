# RhoMicro.RazorTemplating

This is a library for compiling razor templates at runtime.

> [!WARNING]  
> This package uses an unsupported and inofficial dependency for obtaining 
> the `RazorSourceGenerator` used when compiling razor templates.
> This means that language features past net8 are not necessarily supported and
> may not be downstreamed from the razor toolchain in the future.

## Licensing

This work is licensed to you under the MPL-2.0 license.

## Features

- dynamically compile razor source texts at runtime
- manage interdependent razor templates (`Layout`, `MyReusableComponent` etc.)
- provide your own template providers for flexible retrieval from any data source
- cached compilation of templates (using `IMemoryCache`) for fast (amortized) rendering

## Installation

CLI:
```
dotnet add package RhoMicro.RazorTemplating
```

## How To Use

### Simple Use Cases

Use the rendering façade:
```cs
using RhoMicro.RazorTemplating;

var html = await RazorTemplateRenderer.Render(
    // Razor source text to render:
    """
    <div>
        Hello, @(Name)!
    </div>

    @code
    {
        [Parameter] public string Name { get; set; } = "World";
    }
    """,
    // Collection of parameters to pass to the component:
    [new("Name", "SleepWellPupper")]);

// html contains:
// <div>
//     Hello, SleepWellPupper!
// </div>
```

### Custom Use Cases

Integrate into your DI-container:
```cs
using RhoMicro.RazorTemplating;

services.AddRazorTemplating();
```

Provide an implementation of `IRazorTemplateProvider` to allow for template discovery:
```cs
using RhoMicro.RazorTemplating;

services.AddScoped<IRazorTemplateProvider, MyRazorTemplateProvider>();

public class MyRazorTemplateProvider(MyDbContext db)
    : IRazorTemplateProvider
{
    public async ValueTask<RazorTemplate> LoadTemplate(
        String name,
        CancellationToken ct = default)
    {
        var template = await db.MailTemplates.FindAsync(name);
        var result = RazorTemplate.Create(
            name,
            template.Text);
        
        return result;
    }    
}
```

An implementation for in-memory definition of templates is provided by the library:
```cs
using RhoMicro.RazorTemplating;

services.AddSingleton<IRazorTemplateProvider>(
    new InMemoryRazorTemplateProvider(
        RazorTemplate.Create("Template1", "<div>source text here</div>"),
        RazorTemplate.Create("Template2", "<div>source text here</div>")));
```

Define options for compiling templates:
```cs
using RhoMicro.RazorTemplating;

services.AddSingleton(new RazorTemplateCompilerOptions(
    // Root namespace used when compiling templates:
    RootNamespace: "MyTemplates",
    // When compiling, diagnostics with this severity or higher are logged:
    MinimumLoggingDiagnosticSeverity: DiagnosticSeverity.Error,
    // Using statement parts used in `gloabl using {0};` statements:
    GlobalUsings: 
    [
        "MyTemplates.Utilities",
        "static MyTemplates.StaticHelperClass"
    ],
    // Assemblies to be referenced when compiling templates:
    ReferenceAssemblies:
    [
        typeof(MySharedComponentLibrary.SharedComponent).Assembly
    ]));
```

Configure caching of compiled templates by registering an implementation of `IMemoryCacheEntryConfiguration`:
```cs
using RhoMicro.RazorTemplating;

services.AddSingleton<IMemoryCacheEntryConfiguration, MyMemoryCacheEntryConfiguration>();

public class MyMemoryCacheEntryConfiguration : IMemoryCacheEntryConfiguration
{
    public async ValueTask Configure(ICacheEntry entry, RazorTemplate template)
    {
        entry.SlidingExpiration = TimeSpan.FromHours(1);
    }
}
```

Inject an instance of `IRazorTemplateRenderer` to render templates:
```cs
using RhoMicro.RazorTemplating;

public class MyEmailService(
    IEmailClient client,
    IRazorTemplateRenderer renderer)
{
    public async Task SendEmail(CancellationToken ct)
    {
        var text = await renderer.Render(
            // The name of the template to render:
            "MyEmailTemplate",
            // Parameters to pass to the razor component:
            [
                new("Name", "John Doe"),
                new("Date", DateTime.Now)
            ],
            ct);
        
        await client.SendEmail(text, "john.doe@aol.com");
    }
}
```

### Lifetimes And Caching

`RazorTemplate` objects own and manage the compiled template type, but they are managed by a cache.
Consumer code should not dispose them manually.
For example, the `InMemoryRazorTemplateProvider` will dispose all registered templates upon disposal.
Likewise, the cache does not need to register a disposal callback to cache entries.

By default, cache entries do not expire.
