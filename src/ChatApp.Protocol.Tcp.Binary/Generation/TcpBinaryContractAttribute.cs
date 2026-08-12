namespace ChatApp.Shared.Protocol.Tcp.Binary.Generation;

/// <summary>
/// Associates a handwritten binary schema/encoder descriptor with its canonical contract type.
/// The decoder generator consumes this metadata at build time; encoding never depends on generated code.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TcpBinaryContractAttribute : Attribute
{
    public TcpBinaryContractAttribute(Type contractType) =>
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));

    public Type ContractType { get; }
}
