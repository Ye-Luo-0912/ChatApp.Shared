namespace ChatApp.Shared.Protocol.Tcp.Binary.Generation;

/// <summary>Declares an unpacked repeated length-delimited nested message field.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class TcpBinaryNestedFieldAttribute : Attribute
{
    public TcpBinaryNestedFieldAttribute(int fieldNumber, string propertyName, Type descriptorType)
    {
        FieldNumber = fieldNumber;
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        DescriptorType = descriptorType ?? throw new ArgumentNullException(nameof(descriptorType));
    }

    public int FieldNumber { get; }

    public string PropertyName { get; }

    public Type DescriptorType { get; }
}
