using UnityEngine;

namespace JunkineeringTest.Runtime.Addressables
{
    public interface IAddressableResource
    {
        object Key { get; }
        Object AssetObject { get; }
        bool IsReleased { get; }
    }
}
