// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using Microsoft.Extensions.Caching.Memory;

internal sealed class NullMemoryCacheEntryConfiguration : IMemoryCacheEntryConfiguration
{
    public async ValueTask Configure(ICacheEntry entry, RazorTemplate template)
    {
        entry.AbsoluteExpiration = null;
        entry.SlidingExpiration = null;
        entry.AbsoluteExpirationRelativeToNow = null;
    }
}
