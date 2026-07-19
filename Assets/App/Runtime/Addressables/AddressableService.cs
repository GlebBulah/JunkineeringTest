using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace JunkineeringTest.Runtime.Addressables
{
    public sealed class AddressableService : IAddressableService, IDisposable
    {
        private readonly object _resourceGate = new object();
        private readonly HashSet<AddressableResource> _activeResources = new HashSet<AddressableResource>();
        private readonly SemaphoreSlim _initializationGate = new SemaphoreSlim(1, 1);

        private AsyncOperationHandle<IResourceLocator> _initializeHandle;
        private bool _hasInitializeHandle;
        private bool _isInitialized;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized)
            {
                return;
            }

            await _initializationGate.WaitAsync(cancellationToken);

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
                _initializationGate.Release();
            }
        }

        public async Task<AddressableAsset<T>> LoadAssetAsync<T>(object key, CancellationToken cancellationToken) where T : UnityEngine.Object
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

                var resource = new AddressableAsset<T>(key, handle.Result, handle);
                Track(resource);
                return resource;
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

        public async Task<IReadOnlyList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type assetType, CancellationToken cancellationToken)
        {
            ValidateKey(key);
            assetType = assetType ?? typeof(UnityEngine.Object);
            await InitializeAsync(cancellationToken);

            var handle = UnityAddressables.LoadResourceLocationsAsync(key, assetType);

            try
            {
                await handle.AwaitAsync(cancellationToken);
                EnsureSucceeded(handle, "Load resource locations", key, assetType);
                return new List<IResourceLocation>(handle.Result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AddressableOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new AddressableOperationException($"Failed to load addressable locations for '{FormatKey(key)}'.", key, assetType, exception);
            }
            finally
            {
                SafeRelease(handle);
            }
        }

        public async Task<long> GetDownloadSizeAsync(object key, CancellationToken cancellationToken)
        {
            ValidateKey(key);
            await InitializeAsync(cancellationToken);

            var handle = UnityAddressables.GetDownloadSizeAsync(key);

            try
            {
                await handle.AwaitAsync(cancellationToken);
                EnsureSucceeded(handle, "Get download size", key, typeof(long));
                return handle.Result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AddressableOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new AddressableOperationException($"Failed to get download size for '{FormatKey(key)}'.", key, typeof(long), exception);
            }
            finally
            {
                SafeRelease(handle);
            }
        }

        public async Task DownloadDependenciesAsync(object key, CancellationToken cancellationToken)
        {
            ValidateKey(key);
            await InitializeAsync(cancellationToken);

            var handle = UnityAddressables.DownloadDependenciesAsync(key, false);

            try
            {
                await handle.AwaitAsync(cancellationToken);
                EnsureSucceeded(handle, "Download dependencies", key, typeof(object));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AddressableOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new AddressableOperationException($"Failed to download dependencies for '{FormatKey(key)}'.", key, typeof(object), exception);
            }
            finally
            {
                SafeRelease(handle);
            }
        }

        public async Task<AddressableInstance> InstantiateAsync(object key, Vector3 position, Quaternion rotation, Transform parent, CancellationToken cancellationToken)
        {
            ValidateKey(key);
            await InitializeAsync(cancellationToken);

            var handle = UnityAddressables.InstantiateAsync(key, position, rotation, parent);

            try
            {
                await handle.AwaitAsync(cancellationToken);
                EnsureSucceeded(handle, "Instantiate", key, typeof(GameObject));

                if (handle.Result == null)
                {
                    throw new AddressableOperationException($"Addressable instance '{FormatKey(key)}' loaded as null.", key, typeof(GameObject));
                }

                var instance = new AddressableInstance(key, handle.Result, handle);
                Track(instance);
                return instance;
            }
            catch (OperationCanceledException)
            {
                SafeReleaseInstance(handle);
                throw;
            }
            catch (AddressableOperationException)
            {
                SafeReleaseInstance(handle);
                throw;
            }
            catch (Exception exception)
            {
                SafeReleaseInstance(handle);
                throw new AddressableOperationException($"Failed to instantiate addressable prefab '{FormatKey(key)}'.", key, typeof(GameObject), exception);
            }
        }

        public Task ReleaseAsync<T>(AddressableAsset<T> asset, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            ReleaseResource(asset);
            return Task.CompletedTask;
        }

        public Task ReleaseInstanceAsync(AddressableInstance instance, CancellationToken cancellationToken)
        {
            ReleaseResource(instance);
            return Task.CompletedTask;
        }

        public Task ReleaseAllAsync(CancellationToken cancellationToken)
        {
            List<AddressableResource> resources;

            lock (_resourceGate)
            {
                resources = new List<AddressableResource>(_activeResources);
                _activeResources.Clear();
            }

            for (var index = resources.Count - 1; index >= 0; index--)
            {
                ReleaseResource(resources[index]);
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
            _initializationGate.Dispose();
        }

        private void Track(AddressableResource resource)
        {
            lock (_resourceGate)
            {
                _activeResources.Add(resource);
            }
        }

        private void ReleaseResource(AddressableResource resource)
        {
            if (resource == null || !resource.TryMarkReleased())
            {
                return;
            }

            lock (_resourceGate)
            {
                _activeResources.Remove(resource);
            }

            resource.ReleaseFromAddressables();
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

        private static void SafeReleaseInstance(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.IsValid())
            {
                UnityAddressables.ReleaseInstance(handle);
            }
        }
    }
}
