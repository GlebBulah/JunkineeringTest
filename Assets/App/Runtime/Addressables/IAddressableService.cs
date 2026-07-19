using System.Threading;
using System.Threading.Tasks;

namespace JunkineeringTest.Runtime.Addressables
{
    public interface IAddressableService
    {
        Task InitializeAsync(CancellationToken cancellationToken);
        Task<AddressableAsset<T>> LoadAsync<T>(object key, CancellationToken cancellationToken) where T : UnityEngine.Object;
        Task ReleaseAsync<T>(AddressableAsset<T> asset, CancellationToken cancellationToken) where T : UnityEngine.Object;
        Task ReleaseAllAsync(CancellationToken cancellationToken);
    }
}
