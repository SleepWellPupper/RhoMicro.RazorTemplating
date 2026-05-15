// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Provides integration for razor templating into service collections.
/// </summary>
public static class ServiceCollectionExtensions
{
    // ReSharper disable once UnusedType.Global
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds razor templating to the service collection.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Implement and register an implementation of <see cref="IRazorTemplateProvider"/> to define templates by name.
        /// Templates returned from the <see cref="IRazorTemplateProvider"/> are cached in an injected <see cref="IMemoryCache"/>.
        /// To configure cache entries, register an implementation of <see cref="IMemoryCacheEntryConfiguration"/>.
        /// </para>
        /// <para>
        /// Inject a <see cref="IRazorTemplateRenderer"/> instance to render razor templates by name.
        /// </para>
        /// </remarks>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public IServiceCollection AddRazorTemplating()
        {
            services.AddLogging();
            services.AddMemoryCache();
            services.TryAddScoped<RazorTemplateCompiler>();
            services.TryAddScoped<IRazorTemplateRenderer, DefaultRazorTemplateRenderer>();
            services.TryAddScoped<IRazorTemplateProvider, SampleRazorTemplateProvider>();
            services.TryAddScoped<IMemoryCacheEntryConfiguration, NullMemoryCacheEntryConfiguration>();
            services.TryAddScoped<RazorTemplateRendererContext>();
            services.TryAddSingleton(RazorTemplateCompilerOptions.Default);

            return services;
        }
    }
}
