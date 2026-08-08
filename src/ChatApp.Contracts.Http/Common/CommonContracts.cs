namespace ChatApp.Contracts.Http.Common;

/// <summary>Realtime gateway endpoint returned by the HTTP authentication API.</summary>
public struct ServerEndpoint
{
    public string Host { get; set; }
    public string Name { get; set; }
    public ushort Port { get; set; }
}

/// <summary>Stable cursor envelope used by HTTP collection endpoints.</summary>
public sealed class CursorPage<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
}

/// <summary>The success envelope currently emitted by several HTTP mutation endpoints.</summary>
public sealed class ApiEnvelope<T>
{
    public T? Data { get; init; }
}
