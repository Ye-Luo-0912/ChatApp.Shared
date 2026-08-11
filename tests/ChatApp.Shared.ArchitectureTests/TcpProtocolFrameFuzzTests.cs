using System.Text;
using System.Text.Json;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Json;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

/// <summary>
/// PROTO-FEED-1 帧级畸形/超限 fuzz 与旧附件、关系字段组合 old/new fixture。
/// 目标是证明 source-generated JSON codec 对畸形/超限/旧字段输入要么干净拒绝
/// （JsonException/降级），要么忠实往返，绝不崩溃、挂起或产出损坏结果。
/// </summary>
public sealed class TcpProtocolFrameFuzzTests
{
    private static TcpProtocolJsonSerializerContext Json =>
        TcpProtocolJsonSerializerContext.Default;

    // ---- 帧级畸形输入：必须干净拒绝（JsonException），不得半物化或崩溃 ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("12345")]
    [InlineData("[]")]
    [InlineData("true")]
    [InlineData("{\"requestId\":\"sync-01\",\"succeeded\":true,\"catchUps\":[{")]
    [InlineData("{\"items\":\"not-an-array\"}")]
    [InlineData("{\"requestId\":123}")]
    [InlineData("{\"items\":[{\"receivedAtMs\":1.5}]}")]
    [InlineData("{\"items\":[{\"receivedAtMs\":99999999999999999999999999}]}")]
    [InlineData("{\"requestId\":\"\\q\"}")]
    [InlineData("{\"succeeded\":true} trailing")]
    [InlineData("{\"succeeded\":true}{\"succeeded\":false}")]
    public void MalformedPayloadIsRejectedWithJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(
            json,
            Json.MessageHistoryResponse));
    }

    [Fact]
    public void DeeplyNestedPayloadIsRejectedBeyondDefaultDepth()
    {
        var sb = new StringBuilder();
        sb.Append(' ', 64);
        for (var i = 0; i < 200; i++)
        {
            sb.Append('[');
        }

        var json = sb.ToString();

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(
            json,
            Json.MessageHistoryResponse));
    }

    [Fact]
    public void OversizedStringFieldIsToleratedAndRoundTrips()
    {
        var value = new MessageHistoryResponse
        {
            RequestId = "oversized",
            Succeeded = true,
            Items =
            [
                new MessageHistoryItem
                {
                    MessageId = new string('m', 512_000),
                    Content = new string('c', 512_000)
                }
            ]
        };

        var json = JsonSerializer.Serialize(value, Json.MessageHistoryResponse);
        var roundTrip = JsonSerializer.Deserialize(json, Json.MessageHistoryResponse);

        Assert.NotNull(roundTrip);
        Assert.Equal(512_000, roundTrip.Items[0].MessageId.Length);
        Assert.Equal(512_000, roundTrip.Items[0].Content.Length);
    }

    [Fact]
    public void DuplicateFieldsLastWinsWithoutCorruption()
    {
        const string json = """
            {"requestId":"first","requestId":"second","succeeded":true,"succeeded":false,"items":[]}
            """;

        var value = JsonSerializer.Deserialize(json, Json.MessageHistoryResponse);

        Assert.NotNull(value);
        Assert.Equal("second", value.RequestId);
        Assert.False(value.Succeeded);
    }

    [Fact]
    public void UnknownFieldsAreIgnoredAndKnownFieldsPreserved()
    {
        const string json = """
            {"requestId":"known","succeeded":true,"items":[],"futureField":{"nested":[1,2,3]},"anotherUnknown":42}
            """;

        var value = JsonSerializer.Deserialize(json, Json.MessageHistoryResponse);

        Assert.NotNull(value);
        Assert.Equal("known", value.RequestId);
        Assert.True(value.Succeeded);
    }

    // ---- 帧级超限：codec 必须仍能忠实往返，Limit 是端点策略而非 codec 职责 ----

    [Fact]
    public void OversizedPayloadNearHardLimitRoundTripsExactly()
    {
        // 硬上限 MaxPayloadSize = 80 KiB 是端点策略（见 TcpFrameConstants 注释）；
        // codec 层对接近/超过限制的 payload 必须仍能忠实往返而不损坏。
        var items = new List<MessageHistoryItem>(1_000);
        for (var i = 0; i < 1_000; i++)
        {
            items.Add(new MessageHistoryItem
            {
                MessageId = $"message-{i}",
                ClientMessageId = $"client-{i}",
                SenderUserId = i,
                ConversationId = "conversation-01",
                Content = new string('x', 200),
                ReceivedAtMs = 1_735_689_600_000 + i,
                ChangedAtMs = 1_735_689_600_100 + i
            });
        }

        var value = new MessageHistoryResponse
        {
            RequestId = "history-oversized",
            Succeeded = true,
            Items = items,
            HasMore = true
        };

        var json = JsonSerializer.Serialize(value, Json.MessageHistoryResponse);
        Assert.True(json.Length > 100_000, $"expected payload near/above hard limit, got {json.Length}");

        var roundTrip = JsonSerializer.Deserialize(json, Json.MessageHistoryResponse);

        Assert.NotNull(roundTrip);
        Assert.Equal(1_000, roundTrip.Items.Count);
        for (var i = 0; i < 1_000; i++)
        {
            Assert.Equal($"message-{i}", roundTrip.Items[i].MessageId);
            Assert.Equal($"client-{i}", roundTrip.Items[i].ClientMessageId);
            Assert.Equal(200, roundTrip.Items[i].Content.Length);
        }
    }

    // ---- 旧附件字段组合：旧格式缺新字段，新 reader 必须降级到默认值 ----

    [Theory]
    [InlineData(
        "{\"attachmentId\":\"attachment-01\",\"fileName\":\"voice.opus\",\"contentType\":\"audio/opus\",\"sizeBytes\":1234,\"status\":1}")]
    [InlineData(
        "{\"attachmentId\":\"attachment-01\",\"fileName\":\"voice.opus\",\"contentType\":\"audio/opus\",\"sizeBytes\":1234,\"status\":1,\"downloadToken\":null}")]
    public void OldAttachmentRefReadsWithDefaults(string json)
    {
        var attachment = JsonSerializer.Deserialize(json, Json.TcpAttachmentRef);

        Assert.NotNull(attachment);
        Assert.Equal(1, attachment.RefVersion);
        Assert.Equal("attachment-01", attachment.AttachmentId);
        Assert.Equal("audio/opus", attachment.ContentType);
        Assert.Null(attachment.DownloadApiHint);
        Assert.Null(attachment.DownloadToken);
        Assert.Null(attachment.ThumbnailApiHint);
    }

    [Fact]
    public void NewAttachmentRefRoundTripsAllFields()
    {
        var value = new TcpAttachmentRef
        {
            RefVersion = 2,
            AttachmentId = "attachment-09",
            FileName = "photo.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 2048,
            Status = 4,
            DownloadApiHint = "/api/attachments/attachment-09",
            DownloadToken = "token-xyz",
            ThumbnailApiHint = "/api/attachments/attachment-09/thumbnail"
        };

        var json = JsonSerializer.Serialize(value, Json.TcpAttachmentRef);
        var roundTrip = JsonSerializer.Deserialize(json, Json.TcpAttachmentRef);

        Assert.NotNull(roundTrip);
        Assert.Equal(2, roundTrip.RefVersion);
        Assert.Equal("token-xyz", roundTrip.DownloadToken);
        Assert.Equal("/api/attachments/attachment-09/thumbnail", roundTrip.ThumbnailApiHint);
    }

    [Fact]
    public void AttachmentInsideHistoryItemReadsLegacyCombination()
    {
        const string json = """
            {"messageId":"message-01","content":"hello","receivedAtMs":1735689600000,"attachments":[{"attachmentId":"attachment-01","fileName":"voice.opus","contentType":"audio/opus","sizeBytes":1234,"status":1}]}
            """;

        var item = JsonSerializer.Deserialize(json, Json.MessageHistoryItem);

        Assert.NotNull(item);
        Assert.Equal("hello", item.Content);
        Assert.Single(item.Attachments!);
        Assert.Equal(1, item.Attachments![0].RefVersion);
        Assert.Null(item.Attachments![0].DownloadToken);
    }

    // ---- 关系字段组合：旧/新 list/catch-up 字段的 old/new fixture ----

    [Fact]
    public void OldRelationshipCatchUpReadsWithNewFieldDefaults()
    {
        const string json = """
            {"listType":1,"changes":[{"changeSequence":7,"operation":0,"resourceId":"user-1","userId":42,"createdAtMs":1735689600100,"occurredAtMs":1735689600150}],"hasMore":true}
            """;

        var catchUp = JsonSerializer.Deserialize(json, Json.RelationshipCatchUp);

        Assert.NotNull(catchUp);
        Assert.Equal(TcpRelationshipListType.Friends, catchUp.ListType);
        Assert.Single(catchUp.Changes);
        Assert.True(catchUp.HasMore);
        Assert.Equal(0, catchUp.NextSequence);
        Assert.Null(catchUp.ResetRequired);
        Assert.Null(catchUp.NextCursor);
    }

    [Fact]
    public void NewRelationshipCatchUpRoundTripsAllFields()
    {
        var value = new RelationshipCatchUp
        {
            ListType = TcpRelationshipListType.BlockedUsers,
            Changes =
            [
                new RelationshipChangeLogEntry
                {
                    Operation = TcpRelationshipChangeOperation.Delete,
                    ResourceId = "user-3",
                    UserId = 33,
                    Status = "done",
                    Message = "blocked",
                    CreatedAtMs = 1_735_689_600_200,
                    OccurredAtMs = 1_735_689_600_250
                }
            ],
            HasMore = false,
            NextCursor = "opaque-cursor",
            NextSequence = 12,
            ResetRequired = false
        };

        var json = JsonSerializer.Serialize(value, Json.RelationshipCatchUp);
        var roundTrip = JsonSerializer.Deserialize(json, Json.RelationshipCatchUp);

        Assert.NotNull(roundTrip);
        Assert.Equal(TcpRelationshipListType.BlockedUsers, roundTrip.ListType);
        Assert.Single(roundTrip.Changes);
        Assert.Equal(TcpRelationshipChangeOperation.Delete, roundTrip.Changes[0].Operation);
        Assert.Equal("opaque-cursor", roundTrip.NextCursor);
        Assert.Equal(12, roundTrip.NextSequence);
    }

    [Fact]
    public void RelationshipSyncWatermarkRoundTrips()
    {
        var value = new RelationshipSyncWatermark
        {
            ListType = TcpRelationshipListType.FriendRequests,
            AfterSequence = 99
        };

        var json = JsonSerializer.Serialize(value, Json.RelationshipSyncWatermark);
        var roundTrip = JsonSerializer.Deserialize(json, Json.RelationshipSyncWatermark);

        Assert.NotNull(roundTrip);
        Assert.Equal(TcpRelationshipListType.FriendRequests, roundTrip.ListType);
        Assert.Equal(99, roundTrip.AfterSequence);
    }

    [Fact]
    public void SyncBootstrapResponseWithRelationshipCatchUpsRoundTrips()
    {
        var value = new SyncBootstrapResponse
        {
            RequestId = "sync-rel-01",
            Succeeded = true,
            ServerTimeMs = 1_735_689_600_300,
            RelationshipCatchUps =
            [
                new RelationshipCatchUp
                {
                    ListType = TcpRelationshipListType.Friends,
                    Changes =
                    [
                        new RelationshipChangeLogEntry
                        {
                            Operation = TcpRelationshipChangeOperation.Upsert,
                            ResourceId = "user-5",
                            UserId = 55,
                            CreatedAtMs = 1_735_689_600_000,
                            OccurredAtMs = 1_735_689_600_000
                        }
                    ],
                    NextSequence = 1
                }
            ]
        };

        var json = JsonSerializer.Serialize(value, Json.SyncBootstrapResponse);
        var roundTrip = JsonSerializer.Deserialize(json, Json.SyncBootstrapResponse);

        Assert.NotNull(roundTrip);
        Assert.Single(roundTrip.RelationshipCatchUps!);
        Assert.Equal(TcpRelationshipListType.Friends, roundTrip.RelationshipCatchUps![0].ListType);
        Assert.Equal(1, roundTrip.RelationshipCatchUps![0].NextSequence);
    }
}