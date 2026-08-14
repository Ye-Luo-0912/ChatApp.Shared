namespace ChatApp.Shared.Protocol.Tcp.Binary.Generation;

/// <summary>Declares an unpacked repeated scalar field.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class TcpBinaryRepeatedFieldAttribute : Attribute
{
    public TcpBinaryRepeatedFieldAttribute(int fieldNumber, string propertyName)
    {
        FieldNumber = fieldNumber;
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
    }

    public int FieldNumber { get; }

    public string PropertyName { get; }
}
