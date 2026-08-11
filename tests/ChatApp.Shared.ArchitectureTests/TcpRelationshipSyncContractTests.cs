using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Json;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

/// <summary>
/// REL-WIRE-2：关系增量同步（sync / catch-up）wire 收口测试。与 list 只读 wire
/// （<see cref="TcpRelationshipListContractTests"/>）同等的 golden / 兼容 / 水位 /
/// reset / 预算 / 畸形覆盖，并固定「不泄漏 Realtime 内部编码」的字段边界。
/// </summary>
public sealed class TcpRelationshipSyncContractTests
{
    private static TcpProtocolJsonSerializerContext JsonContext =>
        TcpProtocolJsonSerializerContext.Default;

    // ---- golden ----

    [Fact]
    public void CatchUpGoldenJson_UpsertTailPage()
    {
        var value = new RelationshipCatchUp
        {
            ListType = TcpRelationshipListType.Friends,
            Changes =
            [
                new RelationshipChangeLogEntry
                {
                    Operation = TcpRelationshipChangeOperation.Upsert,
                    ResourceId = "friendship-1",
                    UserId = 42,
                    Status = "Accepted",
                    CreatedAtMs = 1_735_689_600_000,
                    OccurredAtMs = 1_735_689_600_050
                }
            ],
            HasMore = false,
            NextSequence = 7
        };

        const string expected =
            """{"listType":1,"changes":[{"operation":0,"resourceId":"friendship-1","userId":42,"status":"Accepted","createdAtMs":1735689600000,"occurredAtMs":1735689600050}],"hasMore":false,"nextSequence":7}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.RelationshipCatchUp));
    }

    [Fact]
    public void CatchUpGoldenJson_DeleteEntryOmitsNullableFields()
    {
        var value = new RelationshipCatchUp
        {
            ListType = TcpRelationshipListType.BlockedUsers,
            Changes =
            [
                new RelationshipChangeLogEntry
                {
                    Operation = TcpRelationshipChangeOperation.Delete,
                    ResourceId = "user-9",
                    UserId = 91,
                    CreatedAtMs = 1_735_689_600_100,
                    OccurredAtMs = 1_735_689_600_150
                }
            ],
            HasMore = true,
            NextCursor = "cursor-next"
        };

        const string expected =
            """{"listType":3,"changes":[{"operation":1,"resourceId":"user-9","userId":91,"createdAtMs":1735689600100,"occurredAtMs":1735689600150}],"hasMore":true,"nextCursor":"cursor-next","nextSequence":0}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.RelationshipCatchUp));
    }

    [Fact]
    public void CatchUpGoldenJson_ResetCaseCarriesStableErrorCode()
    {
        var value = new RelationshipCatchUp
        {
            ListType = TcpRelationshipListType.FriendRequests,
            HasMore = false,
            ResetRequired = true,
            ErrorCode = TcpRelationshipSyncErrorCode.RetentionExceeded,
            ErrorMessage = "watermark too old"
        };

        const string expected =
            """{"listType":2,"changes":[],"hasMore":false,"nextSequence":0,"resetRequired":true,"errorCode":"relationships_retention_exceeded","errorMessage":"watermark too old"}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.RelationshipCatchUp));
    }

    [Fact]
    public void SyncWatermarkGoldenJson()
    {
        var value = new RelationshipSyncWatermark
        {
            ListType = TcpRelationshipListType.FriendRequests,
            AfterSequence = 99
        };

        const string expected =
            """{"listType":2,"afterSequence":99}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.RelationshipSyncWatermark));
    }

    // ---- opaque watermark semantics ----

    [Fact]
    public void AfterSequenceIsOpaque_RoundTripsUninterpreted()
    {
        var value = new RelationshipSyncWatermark
        {
            ListType = TcpRelationshipListType.Friends,
            AfterSequence = 1_234_567_890_123
        };

        var json = JsonSerializer.Serialize(value, JsonContext.RelationshipSyncWatermark);
        var roundTrip = JsonSerializer.Deserialize(json, JsonContext.RelationshipSyncWatermark);

        Assert.NotNull(roundTrip);
        Assert.Equal(1_234_567_890_123, roundTrip.AfterSequence);
    }

    // ---- pagination semantics ----

    [Fact]
    public void IntermediatePage_CarriesNextCursorAndDoesNotAdvanceWatermark()
    {
        // 中间页 HasMore=true + NextCursor：客户端续页，但按语义表不得推进持久化水位，
        // 因此 NextSequence 在此页不承载「已消费完毕」的语义（具体值由生产者控制）。
        var value = new RelationshipCatchUp
        {
            ListType = TcpRelationshipListType.Friends,
            Changes =
            [
                new RelationshipChangeLogEntry
                {
                    Operation = TcpRelationshipChangeOperation.Upsert,
                    ResourceId = "friendship-2",
                    UserId = 43,
                    CreatedAtMs = 1,
                    OccurredAtMs = 2
                }
            ],
            HasMore = true,
            NextCursor = "opaque-next"
        };

        var json = JsonSerializer.Serialize(value, JsonContext.RelationshipCatchUp);
        var roundTrip = JsonSerializer.Deserialize(json, JsonContext.RelationshipCatchUp);

        Assert.NotNull(roundTrip);
        Assert.True(roundTrip.HasMore);
        Assert.Equal("opaque-next", roundTrip.NextCursor);
        Assert.Single(roundTrip.Changes);
    }

    // ---- unknown enum / fail-closed ----

    [Fact]
    public void UnknownChangeOperation_IsPreservedOnWire()
    {
        // 未知操作值无法安全应用，语义要求消费端 fail-closed（视本次同步失败并走全量重建）。
        // 本测试固定 wire 会原样保留该数值，不静默改写。
        const string json =
            """{"listType":1,"changes":[{"operation":255,"resourceId":"r-1","userId":1,"createdAtMs":1,"occurredAtMs":2}],"hasMore":false}""";

        var catchUp = JsonSerializer.Deserialize(json, JsonContext.RelationshipCatchUp);

        Assert.NotNull(catchUp);
        Assert.Equal((TcpRelationshipChangeOperation)255, catchUp.Changes.Single().Operation);
    }

    // ---- budget ----

    [Fact]
    public void SyncBudgetsAreStable()
    {
        Assert.Equal(64, TcpRelationshipSyncConstants.MaxResourceIdBytes);
        Assert.Equal(32, TcpRelationshipSyncConstants.MaxStatusBytes);
        Assert.Equal(512, TcpRelationshipSyncConstants.MaxMessageBytes);
        Assert.Equal(200, TcpRelationshipSyncConstants.MaxChangesPerPage);
        Assert.Equal(80 * 1024, TcpRelationshipSyncConstants.MaxResponseBytes);
    }

    [Fact]
    public void SingleMaxFieldEntry_StaysWithinResponseBudget()
    {
        var catchUp = new RelationshipCatchUp
        {
            ListType = TcpRelationshipListType.Friends,
            HasMore = true,
            NextCursor = "cursor",
            NextSequence = 1,
            Changes =
            [
                new RelationshipChangeLogEntry
                {
                    Operation = TcpRelationshipChangeOperation.Upsert,
                    ResourceId = new string('r', TcpRelationshipSyncConstants.MaxResourceIdBytes),
                    Status = new string('s', TcpRelationshipSyncConstants.MaxStatusBytes),
                    Message = new string('m', TcpRelationshipSyncConstants.MaxMessageBytes),
                    CreatedAtMs = 1,
                    OccurredAtMs = 2
                }
            ]
        };

        var json = JsonSerializer.Serialize(catchUp, JsonContext.RelationshipCatchUp);
        var bytes = Encoding.UTF8.GetByteCount(json);

        Assert.True(bytes <= TcpRelationshipSyncConstants.MaxResponseBytes,
            $"single max-field catch-up wire payload {bytes} exceeds budget {TcpRelationshipSyncConstants.MaxResponseBytes}");
    }

    // ---- internal-encoding isolation ----

    [Fact]
    public void InternalSequenceAndRequestIdFields_AreNotSerialized()
    {
        // Realtime 内部的 change_sequence / requestId / retention floor / resetReason 不得进入外部 wire。
        var value = new RelationshipCatchUp
        {
            ListType = TcpRelationshipListType.Friends,
            HasMore = false,
            NextSequence = 7,
            Changes =
            [
                new RelationshipChangeLogEntry
                {
                    Operation = TcpRelationshipChangeOperation.Upsert,
                    ResourceId = "r-1",
                    UserId = 1,
                    CreatedAtMs = 1,
                    OccurredAtMs = 2
                }
            ]
        };

        var json = JsonSerializer.Serialize(value, JsonContext.RelationshipCatchUp);

        Assert.DoesNotContain("changeSequence", json);
        Assert.DoesNotContain("requestId", json);
        Assert.DoesNotContain("retentionFloorSequence", json);
        Assert.DoesNotContain("resetReason", json);
    }

    // ---- old / new compatibility ----

    [Fact]
    public void LegacyCatchUpJson_ReadsWithNewReader_DropsInternalFields()
    {
        // 旧生产端携带内部字段；新读取端忽略未知字段，仅读公开契约字段。
        const string json =
            """{"listType":1,"changes":[{"changeSequence":7,"operation":0,"resourceId":"user-1","userId":42,"createdAtMs":1000,"occurredAtMs":2000,"requestId":"req-1"}],"hasMore":true,"nextCursor":"c","nextSequence":7,"retentionFloorSequence":3,"resetRequired":false,"resetReason":"old"}""";

        var current = JsonSerializer.Deserialize(json, JsonContext.RelationshipCatchUp);

        Assert.NotNull(current);
        Assert.Equal(TcpRelationshipListType.Friends, current.ListType);
        Assert.True(current.HasMore);
        Assert.Equal("c", current.NextCursor);
        Assert.Equal(7, current.NextSequence);
        Assert.False(current.ResetRequired);
        var entry = Assert.Single(current.Changes);
        Assert.Equal(TcpRelationshipChangeOperation.Upsert, entry.Operation);
        Assert.Equal("user-1", entry.ResourceId);
        Assert.Equal(42, entry.UserId);
    }

    [Fact]
    public void NewCatchUpJson_ReadsWithLegacyReader_InternalFieldsDefault()
    {
        var value = new RelationshipCatchUp
        {
            ListType = TcpRelationshipListType.BlockedUsers,
            HasMore = false,
            NextSequence = 5,
            Changes =
            [
                new RelationshipChangeLogEntry
                {
                    Operation = TcpRelationshipChangeOperation.Delete,
                    ResourceId = "user-3",
                    UserId = 33,
                    CreatedAtMs = 1_000,
                    OccurredAtMs = 2_000
                }
            ]
        };

        var json = JsonSerializer.Serialize(value, JsonContext.RelationshipCatchUp);
        var legacy = JsonSerializer.Deserialize(json, LegacyRelSyncJsonContext.Default.LegacyRelationshipCatchUp);

        Assert.NotNull(legacy);
        Assert.Equal(TcpRelationshipListType.BlockedUsers, legacy.ListType);
        Assert.False(legacy.HasMore);
        Assert.Equal(5, legacy.NextSequence);
        Assert.Equal(0, legacy.RetentionFloorSequence);
        Assert.Null(legacy.ResetReason);
        var entry = Assert.Single(legacy.Changes);
        Assert.Equal(TcpRelationshipChangeOperation.Delete, entry.Operation);
        Assert.Equal(0, entry.ChangeSequence);
        Assert.Null(entry.RequestId);
    }

    // ---- truncated / malformed codec ----

    [Fact]
    public void TruncatedCatchUpPayload_IsRejectedInsteadOfPartiallyMaterialized()
    {
        const string truncated = """{"listType":1,"changes":[{""";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(
            truncated,
            JsonContext.RelationshipCatchUp));
    }
}

// Legacy (pre-收口) reader shapes: retain the internal fields that the closed wire no longer carries.
internal sealed class LegacyRelationshipChangeLogEntry
{
    public long ChangeSequence { get; set; }
    public TcpRelationshipChangeOperation Operation { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public long CreatedAtMs { get; set; }
    public long OccurredAtMs { get; set; }
    public string? RequestId { get; set; }
}

internal sealed class LegacyRelationshipCatchUp
{
    public TcpRelationshipListType ListType { get; set; }
    public LegacyRelationshipChangeLogEntry[] Changes { get; set; } = [];
    public bool HasMore { get; set; }
    public string? NextCursor { get; set; }
    public long NextSequence { get; set; }
    public long RetentionFloorSequence { get; set; }
    public bool ResetRequired { get; set; }
    public string? ResetReason { get; set; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LegacyRelationshipCatchUp))]
internal sealed partial class LegacyRelSyncJsonContext : JsonSerializerContext;