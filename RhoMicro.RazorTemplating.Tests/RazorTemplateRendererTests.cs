// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating.Tests;

using Meziantou.Extensions.Logging.Xunit.v3;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class RazorTemplateRendererTests
{
    public static TheoryData<String, IEnumerable<KeyValuePair<String, Object?>>, String> RendersExpectedHtmlData =>
    [
        // Basic string interpolation
        ("""
         <div>@(Value)</div>
         @code
         {
            [Parameter] public string Value { get; set; } = "foo";
         }
         """,
         [],
         """
         <div>foo</div>
         """),

        // Parameter override
        ("""
         <div>@(Value)</div>
         @code
         {
            [Parameter] public string Value { get; set; } = "foo";
         }
         """,
         [new("Value", "bar")],
         """
         <div>bar</div>
         """),

        // Null value handling
        ("""
         <div>@(Value ?? "default")</div>
         @code
         {
            [Parameter] public string? Value { get; set; }
         }
         """,
         [],
         """
         <div>default</div>
         """),

        // Null value handling with value
        ("""
         <div>@(Value ?? "default")</div>
         @code
         {
            [Parameter] public string? Value { get; set; }
         }
         """,
         [new("Value", "value")],
         """
         <div>value</div>
         """),

        // Nested components
        ("""
         <div>@Component</div>

         @code
         {
            [Parameter] public string Title { get; set; } = "Nested";
            [Parameter] public string Content { get; set; } = "content";

            private RenderFragment Component =>
                @<span class="nested">@(Title): @(Content)</span>;
         }
         """,
         [],
         """
         <div><span class="nested">Nested: content</span></div>
         """),

        // Different data types
        ("""
         <div>
            <p>Name: @(Name)</p>
            <p>Age: @(Age)</p>
            <p>IsActive: @(IsActive)</p>
         </div>
         @code
         {
            [Parameter] public string Name { get; set; } = "John";
            [Parameter] public int Age { get; set; } = 30;
            [Parameter] public bool IsActive { get; set; } = true;
         }
         """,
         [new("Name", "Pedro"), new("Age", -1), new("IsActive", false)],
         "<div><p>Name: Pedro</p>\n   <p>Age: -1</p>\n   <p>IsActive: False</p></div>"),

        // Complex expressions
        ("""
         <div>
            @(Value * 2) items @(Value > 10 ? "(many)" : "(few)")
         </div>
         @code
         {
            [Parameter] public int Value { get; set; } = 5;
         }
         """,
         [],
         """
         <div>10 items (few)</div>
         """),

        // Conditional rendering
        ("""
         @if (ShowTitle)
         {
            <h1>@Title</h1>
         }
         <p>@Content</p>
         @code
         {
            [Parameter] public string Title { get; set; } = "Default";
            [Parameter] public string Content { get; set; } = "Content";
            [Parameter] public bool ShowTitle { get; set; } = true;
         }
         """,
         [new("ShowTitle", false)],
         """
         <p>Content</p>
         """),

        // Loops
        ("""
         <ul>
            @foreach (var item in Items)
            {
                <li>@item</li>
            }
         </ul>
         @code
         {
            [Parameter] public IEnumerable<string> Items { get; set; } = [];
         }
         """,
         [new("Items", new[] { "One", "Two", "Three" })],
         """
         <ul><li>One</li><li>Two</li><li>Three</li></ul>
         """),

        // Child content with interpolation
        ("""
         <div>
            <h2>@Title</h2>
            @Content
         </div>
         @code
         {
            [Parameter] public RenderFragment Content { get; set; }
            [Parameter] public string Title { get; set; } = "Section";
         }
         """,
         [
             new("Title", "Important Note"),
             new("Content", (RenderFragment)(b =>
             {
                 b.OpenElement(0, "p");
                 b.AddContent(1, "This is the child content");
                 b.CloseElement();
             }))
         ],
         "<div><h2>Important Note</h2>\n   <p>This is the child content</p></div>"),

        // Mixed scenarios with null checks
        ("""
         <div class="@(ShowBorder ? "border" : "")">
            @if (!string.IsNullOrEmpty(Title))
            {
                <h3>@Title</h3>
            }
            @foreach (var item in Items ?? new List<string>())
            {
                <p>@item</p>
            }
            @if (HasFooter)
            {
                <footer>© @CurrentYear</footer>
            }
         </div>
         @code
         {
            [Parameter] public string Title { get; set; }
            [Parameter] public IEnumerable<string> Items { get; set; }
            [Parameter] public bool ShowBorder { get; set; } = true;
            [Parameter] public bool HasFooter { get; set; } = false;
            [Parameter] public int CurrentYear { get; set; } = 2023;
         }
         """,
         [
             new("Title", "Mixed Test"),
             new("Items", new[] { "Item 1", "Item 2" }),
             new("HasFooter", true)
         ],
         """
         <div class="border"><h3>Mixed Test</h3><p>Item 1</p><p>Item 2</p><footer>© 2023</footer></div>
         """)
    ];

    [Theory]
    [MemberData(nameof(RendersExpectedHtmlData))]
    public async Task RendersExpectedHtml(
        String source,
        IEnumerable<KeyValuePair<String, Object?>> parameters,
        String expected)
    {
        // Act
        var actual = await RazorTemplateRenderer.Render(
            source,
            parameters,
            s => s.AddLogging(l => l
                .AddXunit(new XUnitLoggerOptions() { IncludeCategory = true })
                .SetMinimumLevel(LogLevel.Trace)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task RendersAdminCredentialsEmail()
    {
        const String expected =
"""
<!DOCTYPE html>
<html lang="en"><head><meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Your Lola account for RhoMicro is ready</title>
    <style>
        body { margin: 0; padding: 0; background-color: #f4f4f5; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
        .container { max-width: 600px; margin: 0 auto; padding: 40px 20px; }
        .card { background-color: #ffffff; border-radius: 8px; padding: 40px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
        .logo { margin-bottom: 32px; }
        h1 { font-size: 20px; color: #18181b; margin: 0 0 16px 0; }
        p { font-size: 15px; line-height: 1.6; color: #3f3f46; margin: 0 0 16px 0; }
        .cta { display: inline-block; background-color: #18181b; color: #ffffff; text-decoration: none; padding: 12px 32px; border-radius: 6px; font-size: 15px; font-weight: 500; margin: 16px 0; }
        .footer { text-align: center; margin-top: 32px; font-size: 13px; color: #a1a1aa; }
        .credentials { background-color: #f4f4f5; border-radius: 6px; padding: 20px; margin: 20px 0; }
.credentials p { margin: 0 0 8px 0; font-size: 14px; }
.credentials p:last-child { margin-bottom: 0; }
.credentials strong { color: #18181b; }
.credentials code { background-color: #e4e4e7; padding: 2px 8px; border-radius: 4px; font-size: 14px; font-family: 'SF Mono', Monaco, 'Cascadia Code', monospace; }
.warning { font-size: 13px; color: #71717a; margin-top: 24px; padding: 12px; background-color: #fefce8; border-radius: 6px; border-left: 3px solid #eab308; }</style></head>
<body><div class="container"><div class="card"><div class="logo"><img src="cid:lola-logo" alt="Lola" height="48" style="height:48px;width:auto;"></div>
            <h1>Your account for RhoMicro is ready</h1>
    <p>
        Your organization <strong>RhoMicro</strong> has been created on the Lola platform
        and is pending review. Use the credentials below to log in once your organization is approved.
    </p>

    <div class="credentials"><p><strong>Username:</strong> <code>SleepWellPupper</code></p>
    <p><strong>Temporary password:</strong> <code>Password1</code></p></div>

    <a href="https://localhost/" class="cta">Log in to Lola</a>

    <p class="warning">
        You will be asked to change your password on first login. Please keep your new password safe.
    </p></div>
        <div class="footer"><p>This email was sent by Lola. If you did not expect this, you can safely ignore it.</p></div></div></body></html>
""";
        var renderer = new ServiceCollection()
            .AddRazorTemplating()
            .AddSingleton<IRazorTemplateProvider>(
                new InMemoryRazorTemplateProvider(
                    RazorTemplate.Create(
                        "TransactionalEmailLayout",
                        """
                        @namespace Lola.Organizations.WebApi.Services.Emails.Layouts
                        <!DOCTYPE html>
                        <html lang="en">
                        <head>
                            <meta charset="UTF-8" />
                            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                            <title>@Title</title>
                            <style>
                                body { margin: 0; padding: 0; background-color: #f4f4f5; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
                                .container { max-width: 600px; margin: 0 auto; padding: 40px 20px; }
                                .card { background-color: #ffffff; border-radius: 8px; padding: 40px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
                                .logo { margin-bottom: 32px; }
                                h1 { font-size: 20px; color: #18181b; margin: 0 0 16px 0; }
                                p { font-size: 15px; line-height: 1.6; color: #3f3f46; margin: 0 0 16px 0; }
                                .cta { display: inline-block; background-color: #18181b; color: #ffffff; text-decoration: none; padding: 12px 32px; border-radius: 6px; font-size: 15px; font-weight: 500; margin: 16px 0; }
                                .footer { text-align: center; margin-top: 32px; font-size: 13px; color: #a1a1aa; }
                                @((MarkupString)ExtraStyles)
                            </style>
                        </head>
                        <body>
                            <div class="container">
                                <div class="card">
                                    <div class="logo"><img src="cid:lola-logo" alt="Lola" height="48" style="height:48px;width:auto;" /></div>
                                    @ChildContent
                                </div>
                                <div class="footer">
                                    <p>This email was sent by Lola. If you did not expect this, you can safely ignore it.</p>
                                </div>
                            </div>
                        </body>
                        </html>

                        @code {
                            [Parameter]
                            public string Title { get; set; } = "";

                            [Parameter]
                            public string ExtraStyles { get; set; } = "";

                            [Parameter]
                            public RenderFragment? ChildContent { get; set; }
                        }
                        """,
                        dependencies: "CredentialsBlock"),
                    RazorTemplate.Create(
                        "AdminCredentialsEmail",
                        """"
                        <Lola.Organizations.WebApi.Services.Emails.Layouts.TransactionalEmailLayout
                            Title="@($"Your Lola account for {OrganizationName} is ready")"
                            ExtraStyles="@CredentialsStyles">

                            <h1>Your account for @OrganizationName is ready</h1>
                            <p>
                                Your organization <strong>@OrganizationName</strong> has been created on the Lola platform
                                and is pending review. Use the credentials below to log in once your organization is approved.
                            </p>

                            <Lola.Organizations.WebApi.Services.Emails.Components.CredentialsBlock
                                Username="@Username"
                                TemporaryPassword="@TemporaryPassword" />

                            <a href="@ManageAppUrl" class="cta">Log in to Lola</a>

                            <p class="warning">
                                You will be asked to change your password on first login. Please keep your new password safe.
                            </p>

                        </Lola.Organizations.WebApi.Services.Emails.Layouts.TransactionalEmailLayout>

                        @code {
                            [Parameter]
                            public string OrganizationName { get; set; } = "";

                            [Parameter]
                            public Uri? ManageAppUrl { get; set; } = null;

                            [Parameter]
                            public string Username { get; set; } = "";

                            [Parameter]
                            public string TemporaryPassword { get; set; } = "";

                            private const string CredentialsStyles = 
                                """
                                .credentials { background-color: #f4f4f5; border-radius: 6px; padding: 20px; margin: 20px 0; }
                                .credentials p { margin: 0 0 8px 0; font-size: 14px; }
                                .credentials p:last-child { margin-bottom: 0; }
                                .credentials strong { color: #18181b; }
                                .credentials code { background-color: #e4e4e7; padding: 2px 8px; border-radius: 4px; font-size: 14px; font-family: 'SF Mono', Monaco, 'Cascadia Code', monospace; }
                                .warning { font-size: 13px; color: #71717a; margin-top: 24px; padding: 12px; background-color: #fefce8; border-radius: 6px; border-left: 3px solid #eab308; }
                                """;
                        }
                        """",
                        dependencies: ["TransactionalEmailLayout", "CredentialsBlock"]),
                    RazorTemplate.Create(
                        "CredentialsBlock",
                        """"
                        @namespace Lola.Organizations.WebApi.Services.Emails.Components

                        <div class="credentials">
                            <p><strong>Username:</strong> <code>@Username</code></p>
                            <p><strong>Temporary password:</strong> <code>@TemporaryPassword</code></p>
                        </div>

                        @code {
                            [Parameter]
                            public string Username { get; set; } = "";

                            [Parameter]
                            public string TemporaryPassword { get; set; } = "";

                            public const string Styles = """
                                .credentials { background-color: #f4f4f5; border-radius: 6px; padding: 20px; margin: 20px 0; }
                                .credentials p { margin: 0 0 8px 0; font-size: 14px; }
                                .credentials p:last-child { margin-bottom: 0; }
                                .credentials strong { color: #18181b; }
                                .credentials code { background-color: #e4e4e7; padding: 2px 8px; border-radius: 4px; font-size: 14px; font-family: 'SF Mono', Monaco, 'Cascadia Code', monospace; }
                                """;
                        }
                        """")))
            .AddLogging(l => l
                .AddXunit(new XUnitLoggerOptions() { IncludeCategory = true })
                .SetMinimumLevel(LogLevel.Trace))
            .BuildServiceProvider()
            .GetRequiredService<IRazorTemplateRenderer>();

        // Act
        var actual = await renderer.Render(
            "AdminCredentialsEmail",
            [
                new("OrganizationName", "RhoMicro"),
                new("ManageAppUrl", new Uri("https://localhost")),
                new("Username", "SleepWellPupper"),
                new("TemporaryPassword", "Password1")
            ],
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task RendersWithTemplateDependency()
    {
        // Arrange
        const String expected = "<outer><inner></inner></outer>";
        var renderer = new ServiceCollection()
            .AddRazorTemplating()
            .AddSingleton<IRazorTemplateProvider>(
                new InMemoryRazorTemplateProvider(
                    RazorTemplate.Create(
                        "Inner",
                        """
                        @namespace Foo
                        <Outer><inner></inner></Outer>
                        @code
                        {
                            private readonly Type _outerType = typeof(Outer);
                            [Inject] public ILogger<Inner> Logger { get; set; }
                            protected override void OnInitialized()
                                => Logger.LogInformation("Outer type: {Type}", _outerType);
                        }
                        """,
                        dependencies: "Outer"),
                    RazorTemplate.Create(
                        "Outer",
                        """
                        @namespace Foo
                        <outer>@ChildContent</outer>
                        @code
                        {
                            [Parameter] public RenderFragment? ChildContent { get; set; }
                        }
                        """)))
            .AddLogging(l => l
                .AddXunit(new XUnitLoggerOptions() { IncludeCategory = true })
                .SetMinimumLevel(LogLevel.Trace))
            .BuildServiceProvider()
            .GetRequiredService<IRazorTemplateRenderer>();

        // Act
        var actual = await renderer.Render("Inner", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, actual);
    }


    [Fact]
    public async Task RendersWithExternalDependency()
    {
        // Arrange
        const String expected = "<external></external>\n<external><inner></inner></external>";
        var renderer = new ServiceCollection()
            .AddRazorTemplating()
            .AddSingleton<IRazorTemplateProvider>(
                new InMemoryRazorTemplateProvider(
                    RazorTemplate.Create(
                        "Inner",
                        """
                        @using RhoMicro.RazorTemplating.Tests
                        @namespace Foo
                        <DynamicComponent Type="typeof(DependencyComponent)"/>
                        <DependencyComponent><inner></inner></DependencyComponent>
                        @code
                        {
                            private readonly Type _outerType = typeof(DependencyComponent);
                            [Inject] public ILogger<Inner> Logger { get; set; }
                            protected override void OnInitialized()
                                => Logger.LogInformation("Outer type: {Type}", _outerType);
                        }
                        """)))
            .AddSingleton(RazorTemplateCompilerOptions.Default with
            {
                ReferenceAssemblies =
                RazorTemplateCompilerOptions.Default.ReferenceAssemblies?.Add(typeof(DependencyComponent).Assembly)
             ?? [typeof(DependencyComponent).Assembly]
            })
            .AddLogging(l => l
                .AddXunit(new XUnitLoggerOptions() { IncludeCategory = true })
                .SetMinimumLevel(LogLevel.Trace))
            .BuildServiceProvider()
            .GetRequiredService<IRazorTemplateRenderer>();

        // Act
        var actual = await renderer.Render("Inner", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, actual);
    }

    public static TheoryData<String, IEnumerable<RazorTemplate>> ThrowsOnCircularDependenciesData =>
    [
        (
            "T1->T2->T1",
            [
                RazorTemplate.Create("T1", String.Empty, "T2"),
                RazorTemplate.Create("T2", String.Empty, "T1"),
            ]
        ),
        (
            "T1->T2->T3->T2",
            [
                RazorTemplate.Create("T1", String.Empty, "T2"),
                RazorTemplate.Create("T2", String.Empty, "T3"),
                RazorTemplate.Create("T3", String.Empty, "T2"),
            ]
        ),
        (
            "T1->T2->T3->T2",
            [
                RazorTemplate.Create("T1", String.Empty, "T2"),
                RazorTemplate.Create("T2", String.Empty, "T3", "T1"),
                RazorTemplate.Create("T3", String.Empty, "T2"),
            ]
        ),
        (
            "T1->T2->T1",
            [
                RazorTemplate.Create("T1", String.Empty, "T2"),
                RazorTemplate.Create("T2", String.Empty, "T3", "T1"),
                RazorTemplate.Create("T3", String.Empty, "T2"),
            ]
        ),
    ];

    [Theory]
    [MemberData(nameof(ThrowsOnCircularDependenciesData))]
    public async Task ThrowsOnCircularDependencies(String expectedTrace, IEnumerable<RazorTemplate> templates)
    {
        // Arrange
        var renderer = new ServiceCollection()
            .AddRazorTemplating()
            .AddSingleton<IRazorTemplateProvider>(
                new InMemoryRazorTemplateProvider(
                    templates))
            .AddLogging(l => l
                .AddXunit(new XUnitLoggerOptions() { IncludeCategory = true, IncludeLogLevel = true })
                .SetMinimumLevel(LogLevel.Trace))
            .BuildServiceProvider()
            .GetRequiredService<IRazorTemplateRenderer>();

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await renderer.Render("T1", TestContext.Current.CancellationToken));
        Assert.Contains(expectedTrace, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotThrowOnMultipleComponentReuse()
    {
        // Arrange
        var renderer = new ServiceCollection()
            .AddRazorTemplating()
            .AddSingleton<IRazorTemplateProvider>(
                new InMemoryRazorTemplateProvider(
                    RazorTemplate.Create("T1", "", "T2", "T3", "T4"),
                    RazorTemplate.Create("T2", "", "T5", "T6"),
                    RazorTemplate.Create("T3", "", "T7", "T4"),
                    RazorTemplate.Create("T4", "", "T8", "T9"),
                    RazorTemplate.Create("T5", ""),
                    RazorTemplate.Create("T6", ""),
                    RazorTemplate.Create("T7", ""),
                    RazorTemplate.Create("T8", "", "T6"),
                    RazorTemplate.Create("T9", "")))
            .AddLogging(l => l
                .AddXunit(new XUnitLoggerOptions() { IncludeCategory = true, IncludeLogLevel = true })
                .SetMinimumLevel(LogLevel.Trace))
            .BuildServiceProvider()
            .GetRequiredService<IRazorTemplateRenderer>();

        // Act
        _ = await renderer.Render("T1", TestContext.Current.CancellationToken);
    }
}

public sealed class DependencyComponent : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "external");
        builder.AddContent(1, ChildContent);
        builder.CloseElement();
    }
}
