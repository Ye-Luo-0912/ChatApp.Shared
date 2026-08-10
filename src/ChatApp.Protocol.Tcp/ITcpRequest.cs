namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Correlates a request with its response. Producers generate one identifier and
/// consumers echo it unchanged.
/// </summary>
public interface ITcpRequest
{
    string? RequestId { get; set; }
}
