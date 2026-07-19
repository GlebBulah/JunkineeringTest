using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace JunkineeringTest.Runtime.Addressables
{
    public sealed class AddressableService : IAddressableService, IDisposable
    {
        private readonly object _gate = new object();
        private readonly HashSet<ITrackedAddressableAsset> _loadedAssets = new();
        private readonly SemaphoreSlim _initializeGate = new(1, 1);

        private AsyncOperationHandle<IResourceLocator> _initializeHandle;
        private bool _hasInitializeHandle;
        private bool _isInitialized;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized)
            {
                return;
            }

            await _initializeGate.WaitAsync(cancellationToken);

            try
            {
                if (_isInitialized)
                {
                    return;
                }

                _initializeHandle = UnityAddressables.InitializeAsync(false);
                _hasInitializeHandle = true;

                try
                {
                    await _initializeHandle.AwaitAsync(cancellationToken);
                    EnsureSucceeded(_initializeHandle, "Addressables initialization", null, typeof(IResourceLocator));
                    _isInitialized = true;
                }
                catch
                {
                    SafeRelease(_initializeHandle);
                    _hasInitializeHandle = false;
                    throw;
                }
            }
            finally
            {
                _initializeGate.Release();
            }
        }

        public async Task<AddressableAsset<T>> LoadAsync<T>(object key, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            ValidateKey(key);
            await InitializeAsync(cancellationToken);

            var handle = UnityAddressables.LoadAssetAsync<T>(key);

            try
            {
                await handle.AwaitAsync(cancellationToken);
                EnsureSucceeded(handle, "Load asset", key, typeof(T));

                if (handle.Result == null)
                {
                    throw new AddressableOperationException($"Addressable asset '{FormatKey(key)}' loaded as null.", key, typeof(T));
                }

                var asset = new AddressableAsset<T>(key, handle.Result, handle);
                Track(asset);
                return asset;
            }
            catch (OperationCanceledException)
            {
                SafeRelease(handle);
                throw;
            }
            catch (AddressableOperationException)
            {
                SafeRelease(handle);
                throw;
            }
            catch (Exception exception)
            {
                SafeRelease(handle);
                throw new AddressableOperationException($"Failed to load addressable asset '{FormatKey(key)}' as {typeof(T).Name}.", key, typeof(T), exception);
            }
        }

        public Task ReleaseAsync<T>(AddressableAsset<T> asset, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            if (asset == null)
            {
                return Task.CompletedTask;
            }

            lock (_gate)
            {
                _loadedAssets.Remove(asset);
            }

            asset.TryRelease();
            return Task.CompletedTask;
        }

        public Task ReleaseAllAsync(CancellationToken cancellationToken)
        {
            List<ITrackedAddressableAsset> assets;

            lock (_gate)
            {
                assets = new List<ITrackedAddressableAsset>(_loadedAssets);
                _loadedAssets.Clear();
            }

            for (var index = assets.Count - 1; index >= 0; index--)
            {
                assets[index].Release();
            }

            if (_hasInitializeHandle)
            {
                SafeRelease(_initializeHandle);
                _hasInitializeHandle = false;
                _isInitialized = false;
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            ReleaseAllAsync(CancellationToken.None).GetAwaiter().GetResult();
            _initializeGate.Dispose();
        }

        private void Track(ITrackedAddressableAsset asset)
        {
            lock (_gate)
            {
                _loadedAssets.Add(asset);
            }
        }

        private static void ValidateKey(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), "Addressable key cannot be null.");
            }

            if (key is string textKey && string.IsNullOrWhiteSpace(textKey))
            {
                throw new ArgumentException("Addressable key cannot be empty.", nameof(key));
            }
        }

        private static void EnsureSucceeded(AsyncOperationHandle handle, string operationName, object key, Type assetType)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return;
            }

            var message = key == null
                ? $"{operationName} failed."
                : $"{operationName} failed for addressable key '{FormatKey(key)}'.";

            throw new AddressableOperationException(message, key, assetType, handle.OperationException);
        }

        private static string FormatKey(object key)
        {
            return key?.ToString() ?? "<null>";
        }

        private static void SafeRelease<T>(AsyncOperationHandle<T> handle)
        {
            if (handle.IsValid())
            {
                UnityAddressables.Release(handle);
            }
        }

        private static void SafeRelease(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
            {
                UnityAddressables.Release(handle);
            }
        }
    }
}
