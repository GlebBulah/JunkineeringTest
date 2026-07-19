using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace JunkineeringTest.Runtime.Addressables
{
    public interface IAddressableService
    {
        Task InitializeAsync(CancellationToken cancellationToken);
        Task<AddressableAsset<T>> LoadAssetAsync<T>(object key, CancellationToken cancellationToken) where T : UnityEngine.Object;
        Task<IReadOnlyList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type assetType, CancellationToken cancellationToken);
        Task<long> GetDownloadSizeAsync(object key, CancellationToken cancellationToken);
        Task DownloadDependenciesAsync(object key, CancellationToken cancellationToken);
        Task<AddressableInstance> InstantiateAsync(object key, Vector3 position, Quaternion rotation, Transform parent, CancellationToken cancellationToken);
        Task ReleaseAsync<T>(AddressableAsset<T> asset, CancellationToken cancellationToken) where T : UnityEngine.Object;
        Task ReleaseInstanceAsync(AddressableInstance instance, CancellationToken cancellationToken);
        Task ReleaseAllAsync(CancellationToken cancellationToken);
    }
}
