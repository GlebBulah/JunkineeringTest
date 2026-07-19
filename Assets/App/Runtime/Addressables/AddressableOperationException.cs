using System;

namespace JunkineeringTest.Runtime.Addressables
{
    public sealed class AddressableOperationException : Exception
    {
        public AddressableOperationException(string message, object key, Type assetType, Exception innerException = null)
            : base(message, innerException)
        {
            Key = key;
            AssetType = assetType;
        }

        public object Key { get; }
        public Type AssetType { get; }
    }
}
