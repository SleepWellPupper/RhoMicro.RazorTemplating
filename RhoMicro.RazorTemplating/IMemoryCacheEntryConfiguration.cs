// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Configures razor template cache entries.
/// </summary>
public interface IMemoryCacheEntryConfiguration
{
    /// <summary>
    /// Configures the cache entry for a razor template.
    /// </summary>
    /// <param name="entry">
    /// The cache entry to configure.
    /// </param>
    /// <param name="template">
    /// The template to be cached using <paramref name="entry"/>.
    /// </param>
    /// <returns>
    /// A task representing the configuration work.
    /// </returns>
    ValueTask Configure(ICacheEntry entry, RazorTemplate template);
}
