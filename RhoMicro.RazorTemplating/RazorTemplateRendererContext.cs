// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using Microsoft.Extensions.Caching.Memory;

internal sealed class RazorTemplateRendererContext(
    RazorTemplateCompiler compiler,
    IRazorTemplateProvider provider,
    IMemoryCacheEntryConfiguration configuration,
    IMemoryCache cache)
{
    public async ValueTask<ComponentTypeLifetime> GetComponentType(String name, CancellationToken ct)
    {
        var key = new RazorTemplateCacheKey(name);
        var template = await cache.GetOrCreateAsync(
            key,
            async e =>
            {
                var result = await provider.LoadTemplate(name, ct).ConfigureAwait(false);

                await configuration.Configure(e, result).ConfigureAwait(false);

                e.RegisterPostEvictionCallback((_, value, _, _) =>
                {
                    if (value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                });

                return result;
            }).ConfigureAwait(false);

        if (template is null)
        {
            throw new InvalidOperationException("Unable to retrieve razor template from cache.");
        }

        var type = await template.GetComponentType(compiler, ct).ConfigureAwait(false);

        return type;
    }
}
