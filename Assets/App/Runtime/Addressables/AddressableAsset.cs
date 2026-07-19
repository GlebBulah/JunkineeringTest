using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace JunkineeringTest.Runtime.Addressables
{
    internal interface ITrackedAddressableAsset
    {
        void Release();
    }

    public sealed class AddressableAsset<T> : ITrackedAddressableAsset where T : Object
    {
        private int _isReleased;

        internal AddressableAsset(object key, T asset, AsyncOperationHandle<T> handle)
        {
            Key = key;
            Asset = asset;
            Handle = handle;
        }

        public object Key { get; }
        public T Asset { get; }
        public bool IsReleased => Volatile.Read(ref _isReleased) != 0;

        internal AsyncOperationHandle<T> Handle { get; }

        internal bool TryRelease()
        {
            if (Interlocked.Exchange(ref _isReleased, 1) != 0)
            {
                return false;
            }

            if (Handle.IsValid())
            {
                UnityAddressables.Release(Handle);
            }

            return true;
        }

        void ITrackedAddressableAsset.Release()
        {
            TryRelease();
        }
    }
}
