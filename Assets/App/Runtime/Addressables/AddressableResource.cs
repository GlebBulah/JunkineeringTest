using System.Threading;
using UnityEngine;

namespace JunkineeringTest.Runtime.Addressables
{
    public abstract class AddressableResource : IAddressableResource
    {
        private int _releaseState;

        public abstract object Key { get; }
        public abstract Object AssetObject { get; }
        public bool IsReleased => Volatile.Read(ref _releaseState) != 0;

        internal bool TryMarkReleased()
        {
            return Interlocked.Exchange(ref _releaseState, 1) == 0;
        }

        internal abstract void ReleaseFromAddressables();
    }
}
