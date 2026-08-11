using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Json;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

public sealed class TcpRelationshipListContractTests
{
    private static TcpProtocolJsonSerializerContext JsonContext =>
        TcpProtocolJsonSerializerContext.Default;

    // ---- golden ----

    [Fact]
    public void RelationshipListRequestMatchesGoldenJson()
    {
        var value = new TcpRelationshipListRequest
        {
            RequestId = "rel-01",
            ListType = TcpRelationshipListType.FriendRequests,
            PageSize = 50,
            Cursor = "abc123"
        };

        const string expected =
            """{"requestId":"rel-01","listType":2,"pageSize":50,"cursor":"abc123"}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.TcpRelationshipListRequest));
    }

    [Fact]
    public void RelationshipListRequestFirstPageOmitsCursorAndPageSize()
    {
        var value = new TcpRelationshipListRequest
        {
            RequestId = "rel-01",
            ListType = TcpRelationshipListType.Friends
        };

        const string expected =
            """{"requestId":"rel-01","listType":1}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.TcpRelationshipListRequest));
    }

    [Fact]
    public void RelationshipListResponseMatchesGoldenJson()
    {
        var value = new TcpRelationshipListResponse
        {
            RequestId = "rel-01",
            ListType = TcpRelationshipListType.Friends,
            Succeeded = true,
            Items =
            [
                new TcpRelationshipListItem
                {
                    UserId = 42,
                    ResourceId = "friendship-1",
                    Status = "Accepted",
                    CreatedAtMs = 1_735_689_600_000
                }
            ],
            NextCursor = "cursor-next",
            HasMore = true
        };

        const string expected =
            """{"requestId":"rel-01","listType":1,"succeeded":true,"items":[{"userId":42,"resourceId":"friendship-1","status":"Accepted","createdAtMs":1735689600000}],"nextCursor":"cursor-next","hasMore":true}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.TcpRelationshipListResponse));
    }

    [Fact]
    public void RelationshipListResponseOmitsNullOptionalFields()
    {
        var value = new TcpRelationshipListResponse
        {
            RequestId = "rel-01",
            ListType = TcpRelationshipListType.BlockedUsers,
            Succeeded = false,
            ErrorCode = TcpRelationshipListErrorCode.ProjectionUnavailable,
            ErrorMessage = "not ready",
            Items = []
        };

        const string expected =
            """{"requestId":"rel-01","listType":3,"succeeded":false,"errorCode":"relationship_read_projection_unavailable","errorMessage":"not ready","items":[],"hasMore":false}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.TcpRelationshipListResponse));
    }

    // ---- old / new compatibility ----

    [Fact]
    public void LegacyReaderAcceptsNewListResponseAndIgnoresResetRequired()
    {
        var current = new TcpRelationshipListResponse
        {
            RequestId = "rel-01",
            ListType = TcpRelationshipListType.Friends,
            Succeeded = false,
            ErrorCode = TcpRelationshipListErrorCode.ProjectionChanged,
            ErrorMessage = "changed",
            ResetRequired = true,
            Items = []
        };

        var json = JsonSerializer.Serialize(current, JsonContext.TcpRelationshipListResponse);
        var legacy = JsonSerializer.Deserialize(json, LegacyRelJsonContext.Default.LegacyRelationshipListResponse);

        Assert.NotNull(legacy);
        Assert.False(legacy.Succeeded);
        Assert.Equal(TcpRelationshipListErrorCode.ProjectionChanged, legacy.ErrorCode);
        Assert.Equal(TcpRelationshipListType.Friends, legacy.ListType);
    }

    [Fact]
    public void CurrentReaderAcceptsLegacyListResponseWithoutResetRequired()
    {
        var legacy = new LegacyRelationshipListResponse
        {
            RequestId = "rel-01",
            ListType = TcpRelationshipListType.Friends,
            Succeeded = true,
            Items =
            [
                new LegacyRelationshipListItem { UserId = 7, ResourceId = "friendship-7", CreatedAtMs = 1_735_689_600_000 }
            ],
            HasMore = false
        };

        var json = JsonSerializer.Serialize(legacy, LegacyRelJsonContext.Default.LegacyRelationshipListResponse);
        var current = JsonSerializer.Deserialize(json, JsonContext.TcpRelationshipListResponse);

        Assert.NotNull(current);
        Assert.True(current.Succeeded);
        Assert.Null(current.ResetRequired);
        Assert.Equal(7, current.Items.Single().UserId);
        Assert.Equal("friendship-7", current.Items.Single().ResourceId);
    }

    [Fact]
    public void CurrentReaderIgnoresUnknownOptionalFields()
    {
        const string json =
            """{"requestId":"rel-01","listType":1,"succeeded":true,"items":[],"hasMore":false,"futureOptional":{"enabled":true}}""";

        var current = JsonSerializer.Deserialize(json, JsonContext.TcpRelationshipListResponse);

        Assert.NotNull(current);
        Assert.True(current.Succeeded);
    }

    [Fact]
    public void CurrentReaderPreservesUnknownListTypeEnumValue()
    {
        const string json =
            """{"requestId":"rel-01","listType":255,"succeeded":true,"items":[],"hasMore":false}""";

        var current = JsonSerializer.Deserialize(json, JsonContext.TcpRelationshipListResponse);

        Assert.NotNull(current);
        Assert.Equal((TcpRelationshipListType)255, current.ListType);
    }

    [Fact]
    public void CurrentReaderPreservesUnknownStatusString()
    {
        const string json =
            """{"requestId":"rel-01","listType":1,"succeeded":true,"items":[{"userId":1,"resourceId":"r-1","status":"future-status","createdAtMs":1}],"hasMore":false}""";

        var current = JsonSerializer.Deserialize(json, JsonContext.TcpRelationshipListResponse);

        Assert.NotNull(current);
        Assert.Equal("future-status", current.Items.Single().Status);
    }

    // ---- opaque cursor semantics ----

    [Fact]
    public void MalformedCursorStringIsRoundTrippedNotDecoded()
    {
        var request = new TcpRelationshipListRequest
        {
            RequestId = "rel-01",
            ListType = TcpRelationshipListType.Friends,
            Cursor = "not-a-real-cursor@@"
        };

        var json = JsonSerializer.Serialize(request, JsonContext.TcpRelationshipListRequest);
        var roundTrip = JsonSerializer.Deserialize(json, JsonContext.TcpRelationshipListRequest);

        Assert.NotNull(roundTrip);
        Assert.Equal("not-a-real-cursor@@", roundTrip.Cursor);
    }

    // ---- budget ----

    [Fact]
    public void ResponseItemFieldBudgetsAreStable()
    {
        Assert.Equal(50, TcpRelationshipListConstants.DefaultPageSize);
        Assert.Equal(1, TcpRelationshipListConstants.MinPageSize);
        Assert.Equal(200, TcpRelationshipListConstants.MaxPageSize);
        Assert.Equal(64, TcpRelationshipListConstants.MaxResourceIdBytes);
        Assert.Equal(32, TcpRelationshipListConstants.MaxStatusBytes);
        Assert.Equal(512, TcpRelationshipListConstants.MaxMessageBytes);
        Assert.Equal(80 * 1024, TcpRelationshipListConstants.MaxResponseBytes);
    }

    [Fact]
    public void SingleMaxFieldItemRoundTripsWithinResponseBudget()
    {
        var item = new TcpRelationshipListItem
        {
            UserId = 1,
            ResourceId = new string('r', TcpRelationshipListConstants.MaxResourceIdBytes),
            Status = new string('s', TcpRelationshipListConstants.MaxStatusBytes),
            Message = new string('m', TcpRelationshipListConstants.MaxMessageBytes),
            CreatedAtMs = 1
        };

        var response = new TcpRelationshipListResponse
        {
            RequestId = "rel-big",
            ListType = TcpRelationshipListType.Friends,
            Succeeded = true,
            Items = [item],
            HasMore = true,
            NextCursor = "cursor"
        };

        var json = JsonSerializer.Serialize(response, JsonContext.TcpRelationshipListResponse);
        var bytes = Encoding.UTF8.GetByteCount(json);

        Assert.True(bytes <= TcpRelationshipListConstants.MaxResponseBytes,
            $"single max-field item wire payload {bytes} exceeds budget {TcpRelationshipListConstants.MaxResponseBytes}");

        var roundTrip = JsonSerializer.Deserialize(json, JsonContext.TcpRelationshipListResponse);
        Assert.NotNull(roundTrip);
        Assert.Equal(new string('r', 64), roundTrip.Items.Single().ResourceId);
        Assert.Equal(new string('m', 512), roundTrip.Items.Single().Message);
    }

    [Fact]
    public void FullPageMustBeBoundedByProducerToStayWithinResponseBudget()
    {
        // 200 条最大字段长度的 item 必然超过 80 KiB 响应预算；语义要求生产者按字节预算收紧单页，
        // 而不是静默截断。此测试固定该约束：200 条最大 item 的 wire 字节数必须超过预算，
        // 从而迫使 producer 在返回前按预算裁剪页大小。
        var items = new TcpRelationshipListItem[200];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = new TcpRelationshipListItem
            {
                UserId = i,
                ResourceId = new string('r', TcpRelationshipListConstants.MaxResourceIdBytes),
                Status = new string('s', TcpRelationshipListConstants.MaxStatusBytes),
                Message = new string('m', TcpRelationshipListConstants.MaxMessageBytes),
                CreatedAtMs = i
            };
        }

        var response = new TcpRelationshipListResponse
        {
            RequestId = "rel-big",
            ListType = TcpRelationshipListType.Friends,
            Succeeded = true,
            Items = items,
            HasMore = true,
            NextCursor = "cursor"
        };

        var json = JsonSerializer.Serialize(response, JsonContext.TcpRelationshipListResponse);
        var bytes = Encoding.UTF8.GetByteCount(json);

        Assert.True(bytes > TcpRelationshipListConstants.MaxResponseBytes,
            $"expected 200 max items to exceed {TcpRelationshipListConstants.MaxResponseBytes} bytes, got {bytes}");
    }

    // ---- truncated / malformed codec ----

    [Fact]
    public void TruncatedRelationshipPayloadIsRejectedInsteadOfPartiallyMaterialized()
    {
        const string truncated = """{"requestId":"rel-01","listType":1,"succeeded":true,"items":[{""";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(
            truncated,
            JsonContext.TcpRelationshipListResponse));
    }
}

// Legacy (v1) reader/writer shapes: same JSON shape minus the optional ResetRequired flag.
internal sealed class LegacyRelationshipListItem
{
    public long UserId { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Message { get; set; }
    public long CreatedAtMs { get; set; }
}

internal sealed class LegacyRelationshipListResponse
{
    public string? RequestId { get; set; }
    public TcpRelationshipListType ListType { get; set; }
    public bool Succeeded { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public LegacyRelationshipListItem[] Items { get; set; } = [];
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LegacyRelationshipListResponse))]
internal sealed partial class LegacyRelJsonContext : JsonSerializerContext;