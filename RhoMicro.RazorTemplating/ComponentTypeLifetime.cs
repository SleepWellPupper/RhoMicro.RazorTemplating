// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.RazorTemplating;

using System.Runtime.Loader;
using Microsoft.AspNetCore.Components;

internal sealed class ComponentTypeLifetime : IDisposable
{
    private sealed class Scope(ComponentTypeLifetime lifetime) : IDisposable
    {
        private Boolean _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true))
            {
                return;
            }

            lifetime._lock.ExitReadLock();
        }
    }

    private ComponentTypeLifetime(
        Type type,
        AssemblyLoadContext alc)
    {
        _type = type;
        _alc = alc;
    }

    public Type Type
    {
        get
        {
            ObjectDisposedException.ThrowIf(_type is null, this);

            return _type;
        }
    }

    private readonly ReaderWriterLockSlim _lock = new();
    private AssemblyLoadContext? _alc;
    private Type? _type;

    public IDisposable PreventDisposal()
    {
        _lock.EnterReadLock();

        return new Scope(this);
    }

    public static ComponentTypeLifetime Create(Stream componentPeStream, params Stream[] additionalPeStreams)
    {
        var alc = new AssemblyLoadContext(name: null, isCollectible: true);
        componentPeStream.Seek(0, SeekOrigin.Begin);
        alc.LoadFromStream(componentPeStream);
        var type = alc.Assemblies
            .SelectMany(a => a.ExportedTypes)
            .Single(t => t.IsAssignableTo(typeof(ComponentBase)));

        foreach (var additionalPeStream in additionalPeStreams)
        {
            alc.LoadFromStream(additionalPeStream);
        }

        var result = new ComponentTypeLifetime(type, alc);

        return result;
    }

    public void Dispose()
    {
        _lock.EnterWriteLock();

        try
        {
            _alc?.Unload();
            _type = null;
            _alc = null;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        
        _lock.Dispose();
    }
}
