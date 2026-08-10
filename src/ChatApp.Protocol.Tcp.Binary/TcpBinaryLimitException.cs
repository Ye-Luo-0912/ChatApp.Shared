namespace ChatApp.Shared.Protocol.Tcp.Binary;

public sealed class TcpBinaryLimitException : Exception
{
    public TcpBinaryLimitException(TcpBinaryDecodeError error, string message)
        : base(message)
    {
        Error = error;
    }

    public TcpBinaryDecodeError Error { get; }
}
