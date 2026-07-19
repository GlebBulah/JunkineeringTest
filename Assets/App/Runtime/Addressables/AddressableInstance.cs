using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace JunkineeringTest.Runtime.Addressables
{
    public sealed class AddressableInstance : AddressableResource
    {
        internal AddressableInstance(object key, GameObject instance, AsyncOperationHandle<GameObject> handle)
        {
            Key = key;
            Instance = instance;
            Handle = handle;
        }

        public override object Key { get; }
        public GameObject Instance { get; }
        public override Object AssetObject => Instance;

        internal AsyncOperationHandle<GameObject> Handle { get; }

        internal override void ReleaseFromAddressables()
        {
            if (Handle.IsValid())
            {
                UnityAddressables.ReleaseInstance(Handle);
            }
            else if (Instance != null)
            {
                Object.Destroy(Instance);
            }
        }
    }
}
