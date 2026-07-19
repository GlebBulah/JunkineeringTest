using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace JunkineeringTest.Runtime.Addressables
{
    public sealed class AddressableAsset<T> : AddressableResource where T : Object
    {
        internal AddressableAsset(object key, T asset, AsyncOperationHandle<T> handle)
        {
            Key = key;
            Asset = asset;
            Handle = handle;
        }

        public override object Key { get; }
        public T Asset { get; }
        public override Object AssetObject => Asset;

        internal AsyncOperationHandle<T> Handle { get; }

        internal override void ReleaseFromAddressables()
        {
            if (Handle.IsValid())
            {
                UnityAddressables.Release(Handle);
            }
        }
    }
}
