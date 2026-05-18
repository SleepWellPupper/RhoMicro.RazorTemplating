// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

/// <summary>
/// Compiles razor templates and loads the compiled type into memory.
/// </summary>
public interface IRazorTemplateCompiler
{
    /// <summary>
    /// Compiles a razor template.
    /// </summary>
    ValueTask<ComponentTypeLifetime> Compile(RazorTemplate root, CancellationToken ct = default);
}
