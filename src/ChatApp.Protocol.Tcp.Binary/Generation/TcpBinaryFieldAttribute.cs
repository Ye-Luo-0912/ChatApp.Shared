namespace ChatApp.Shared.Protocol.Tcp.Binary.Generation;

/// <summary>
/// Declares a stable binary-v1 field number. The same ordinary-source declaration is consumed by
/// handwritten encoders and the decode-only source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class TcpBinaryFieldAttribute : Attribute
{
    public TcpBinaryFieldAttribute(int fieldNumber, string propertyName)
    {
        FieldNumber = fieldNumber;
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
    }

    public int FieldNumber { get; }

    public string PropertyName { get; }
}
