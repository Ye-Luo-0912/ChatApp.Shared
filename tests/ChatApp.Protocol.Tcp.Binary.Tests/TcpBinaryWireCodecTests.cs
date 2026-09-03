using System.Buffers;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;
using ChatApp.Shared.Protocol.Tcp.Binary.Schemas;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class TcpBinaryWireCodecTests
{
    private static readonly BinaryLimits Limits = BinaryLimits.Default;

    [Fact]
    public void NegotiationSwitchRecognizesOnlyTheExactBinaryV1FormatId()
    {
        Assert.Equal("chatapp-bin-v1", TcpBinaryWireCodec.NegotiatedFormatId);
        Assert.True(TcpBinaryWireCodec.IsNegotiatedPayloadFormat("chatapp-bin-v1"));
        Assert.False(TcpBinaryWireCodec.IsNegotiatedPayloadFormat("json"));
        Assert.False(TcpBinaryWireCodec.IsNegotiatedPayloadFormat("pb"));
        Assert.False(TcpBinaryWireCodec.IsNegotiatedPayloadFormat(null));
        Assert.False(TcpBinaryWireCodec.IsNegotiatedPayloadFormat("CHATAPP-BIN-V1"));
    }

    [Fact]
    public void ControlFramesRoundTripThroughTheRegistryDispatch()
    {
        var serverHello = new ServerHello
        {
            ProtocolVersion = 1,
            FeatureBits = 0x8000_0001,
            ServerDeviceId = "reg-gateway",
            ServerTimeMs = 1_700_000_000_999,
            HeartbeatIntervalMs = 15_000,
            MaxPayloadBytes = 64 * 1024,
            ResumeSupported = true,
            PayloadFormat = TcpBinaryWireCodec.NegotiatedFormatId
        };
        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ServerHello,
            Encode(in serverHello, ServerHelloSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        Assert.Equal(BinaryStatus.Done, result.DecodeStatus);
        var actual = Assert.IsType<ServerHello>(result.Value);
        Assert.Equal(
            (serverHello.ProtocolVersion, serverHello.FeatureBits, serverHello.ServerDeviceId,
                serverHello.ServerTimeMs, serverHello.HeartbeatIntervalMs, serverHello.MaxPayloadBytes,
                serverHello.ResumeSupported, serverHello.PayloadFormat),
            (actual.ProtocolVersion, actual.FeatureBits, actual.ServerDeviceId,
                actual.ServerTimeMs, actual.HeartbeatIntervalMs, actual.MaxPayloadBytes,
                actual.ResumeSupported, actual.PayloadFormat));
    }

    [Fact]
    public void HistoryAndErrorFramesRoundTripThroughTheRegistryDispatch()
    {
        var request = new MessageHistoryRequest
        {
            RequestId = "reg-history",
            ConversationId = "conv-1",
            BeforeReceivedAtMs = 1_700_000_000_100,
            BeforeMessageId = "before",
            AfterReceivedAtMs = 1_700_000_000_200,
            AfterMessageId = "after",
            Limit = 40
        };
        TcpBinaryWireDecode requestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageHistoryRequest,
            Encode(in request, MessageHistoryRequestSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, requestResult.Status);
        var requestActual = Assert.IsType<MessageHistoryRequest>(requestResult.Value);
        Assert.Equal(
            (request.RequestId, request.ConversationId, request.BeforeReceivedAtMs,
                request.BeforeMessageId, request.AfterReceivedAtMs, request.AfterMessageId,
                request.Limit),
            (requestActual.RequestId, requestActual.ConversationId, requestActual.BeforeReceivedAtMs,
                requestActual.BeforeMessageId, requestActual.AfterReceivedAtMs, requestActual.AfterMessageId,
                requestActual.Limit));

        var error = new ProtocolErrorFrame
        {
            Code = ProtocolErrorCode.UnsupportedCommand,
            Fatal = true,
            RetryAfterMs = 500,
            Message = "binary not negotiated",
            OriginCommand = (ushort)PacketCommand.GoAway
        };
        TcpBinaryWireDecode errorResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.Error,
            Encode(in error, ProtocolErrorFrameSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, errorResult.Status);
        var errorActual = Assert.IsType<ProtocolErrorFrame>(errorResult.Value);
        Assert.Equal(
            (error.Code, error.Fatal, error.RetryAfterMs, error.Message, error.OriginCommand),
            (errorActual.Code, errorActual.Fatal, errorActual.RetryAfterMs, errorActual.Message, errorActual.OriginCommand));
    }

    [Fact]
    public void SegmentedPayloadDecodesThroughTheRegistryDispatch()
    {
        var goAway = new GoAway
        {
            RetryAfterMs = 5_000,
            Reason = "maintenance",
            ServerHint = "retry-01"
        };
        byte[] payload = Encode(in goAway, GoAwaySchema.TryEncode, Limits);
        ReadOnlySequence<byte> segmented = Segmented(payload, 1);

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(PacketCommand.GoAway, in segmented, Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        var actual = Assert.IsType<GoAway>(result.Value);
        Assert.Equal(
            (goAway.RetryAfterMs, goAway.Reason, goAway.ServerHint),
            (actual.RetryAfterMs, actual.Reason, actual.ServerHint));
    }

    [Fact]
    public void HistoryPageRoundTripsThroughTheRegistryDispatch()
    {
        var response = new MessageHistoryResponse
        {
            RequestId = "reg-page",
            ConversationId = "conv-1",
            Succeeded = true,
            ErrorCode = null,
            ErrorMessage = null,
            Items =
            [
                new MessageHistoryItem
                {
                    MessageId = "m-1",
                    ClientMessageId = "cm-1",
                    SenderUserId = 7,
                    ReceiverUserId = 8,
                    ConversationId = "conv-1",
                    Content = "hello",
                    ReceivedAtMs = 1_700_000_000_100,
                    ChangedAtMs = 1_700_000_000_100,
                    EditVersion = 1,
                    MentionedUserIds = [9],
                    MentionedRoles = ["role-a"]
                }
            ],
            NextCursor = new MessageHistoryCursor
            {
                ReceivedAtMs = 1_700_000_000_100,
                ChangedAtMs = 1_700_000_000_100,
                MessageId = "m-1"
            },
            HasMore = true
        };

        TcpBinaryWireDecode spanResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageHistoryPage,
            Encode(in response, MessageHistoryResponseSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, spanResult.Status);
        var spanActual = Assert.IsType<MessageHistoryResponse>(spanResult.Value);
        Assert.Equal(response.RequestId, spanActual.RequestId);
        Assert.Equal(response.Succeeded, spanActual.Succeeded);
        Assert.Single(spanActual.Items);
        Assert.Equal(response.Items[0].MessageId, spanActual.Items[0].MessageId);
        Assert.Equal(response.Items[0].MentionedUserIds, spanActual.Items[0].MentionedUserIds);
        Assert.NotNull(spanActual.NextCursor);
        Assert.Equal(response.NextCursor.MessageId, spanActual.NextCursor.MessageId);
        Assert.Equal(response.HasMore, spanActual.HasMore);

        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in response, MessageHistoryResponseSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode sequenceResult = TcpBinaryWireCodec.TryDecode(PacketCommand.MessageHistoryPage, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, sequenceResult.Status);
        var sequenceActual = Assert.IsType<MessageHistoryResponse>(sequenceResult.Value);
        Assert.Single(sequenceActual.Items);
        Assert.Equal(response.Items[0].Content, sequenceActual.Items[0].Content);
    }

    [Fact]
    public void ConversationListCommandPairRoundTripsThroughTheRegistryDispatch()
    {
        var request = new ConversationListRequest
        {
            RequestId = "reg-conv-list",
            BeforeIsPinned = true,
            BeforePinnedAtMs = 1_700_000_000_100,
            BeforeLastMessageAtMs = 1_700_000_000_200,
            BeforeConversationId = "conv-before",
            Limit = 30
        };
        TcpBinaryWireDecode requestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ConversationListRequest,
            Encode(in request, ConversationListRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, requestResult.Status);
        var requestActual = Assert.IsType<ConversationListRequest>(requestResult.Value);
        Assert.Equal(
            (request.RequestId, request.BeforeIsPinned, request.BeforePinnedAtMs,
                request.BeforeLastMessageAtMs, request.BeforeConversationId, request.Limit),
            (requestActual.RequestId, requestActual.BeforeIsPinned, requestActual.BeforePinnedAtMs,
                requestActual.BeforeLastMessageAtMs, requestActual.BeforeConversationId, requestActual.Limit));

        var page = new ConversationListPage
        {
            RequestId = "reg-conv-list",
            Succeeded = true,
            Items =
            [
                new TcpConversationListItem
                {
                    ConversationId = "conv-1",
                    Type = TcpConversationType.Group,
                    Title = "team",
                    LastMessageId = "m-1",
                    LastMessagePreview = "hello",
                    LastMessageAtMs = 1_700_000_000_100,
                    LastSenderUserId = 42,
                    UnreadCount = 3,
                    IsPinned = true,
                    PinnedAtMs = 1_600_000_000_000,
                    IsMuted = true,
                    MutedUntilMs = 1_800_000_000_000
                }
            ],
            NextCursor = new TcpConversationListCursor
            {
                IsPinned = true,
                PinnedAtMs = 1_600_000_000_000,
                LastMessageAtMs = 1_700_000_000_100,
                ConversationId = "conv-1"
            },
            HasMore = true
        };
        TcpBinaryWireDecode pageSpan = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ConversationListPage,
            Encode(in page, ConversationListPageSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, pageSpan.Status);
        var pageActual = Assert.IsType<ConversationListPage>(pageSpan.Value);
        Assert.Single(pageActual.Items);

        ReadOnlySequence<byte> pageSegmented = Segmented(
            Encode(in page, ConversationListPageSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode pageSequence = TcpBinaryWireCodec.TryDecode(PacketCommand.ConversationListPage, in pageSegmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, pageSequence.Status);
        var pageSequenceActual = Assert.IsType<ConversationListPage>(pageSequence.Value);
        Assert.Equal(page.Succeeded, pageSequenceActual.Succeeded);
        Assert.Equal(page.HasMore, pageSequenceActual.HasMore);
    }

    [Fact]
    public void MarkReadPairRoundTripsThroughTheRegistryDispatch()
    {
        var request = new ConversationMarkReadRequest
        {
            RequestId = "reg-mark-read",
            ConversationId = "conv-1",
            ReadAtMs = 1_700_000_000_100,
            ReadMessageId = "m-9"
        };
        TcpBinaryWireDecode requestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ConversationMarkReadRequest,
            Encode(in request, ConversationMarkReadRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, requestResult.Status);
        var requestActual = Assert.IsType<ConversationMarkReadRequest>(requestResult.Value);
        Assert.Equal(
            (request.RequestId, request.ConversationId, request.ReadAtMs, request.ReadMessageId),
            (requestActual.RequestId, requestActual.ConversationId, requestActual.ReadAtMs, requestActual.ReadMessageId));

        var response = new ConversationMarkReadResponse
        {
            RequestId = "reg-mark-read",
            Succeeded = true,
            ConversationId = "conv-1",
            UnreadCount = 0,
            LastReadMessageId = "m-9",
            LastReadAtMs = 1_700_000_000_100,
            Changed = true
        };
        TcpBinaryWireDecode responseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ConversationMarkReadResponse,
            Encode(in response, ConversationMarkReadResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, responseResult.Status);
        var responseActual = Assert.IsType<ConversationMarkReadResponse>(responseResult.Value);
        Assert.Equal(response.UnreadCount, responseActual.UnreadCount);
        Assert.Equal(response.Changed, responseActual.Changed);
    }

    [Fact]
    public void ConversationEventsRoundTripThroughTheRegistryDispatch()
    {
        var changed = new ConversationChangedUpdate
        {
            ConversationId = "conv-1",
            Type = TcpConversationType.Direct,
            PeerUserId = 7,
            LastMessageId = "m-5",
            LastMessagePreview = "hi",
            LastMessageAtMs = 1_700_000_000_100,
            LastSenderUserId = 7,
            IsPinned = false,
            IsMuted = true,
            MutedUntilMs = 1_800_000_000_000
        };
        TcpBinaryWireDecode changedResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ConversationChanged,
            Encode(in changed, ConversationChangedUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, changedResult.Status);
        var changedActual = Assert.IsType<ConversationChangedUpdate>(changedResult.Value);
        Assert.Equal(changed.Type, changedActual.Type);
        Assert.Equal(changed.MutedUntilMs, changedActual.MutedUntilMs);

        var unread = new UnreadCountChanged
        {
            ConversationId = "conv-1",
            UnreadCount = 12,
            LastReadMessageId = "m-4",
            LastReadAtMs = 1_700_000_000_050
        };
        TcpBinaryWireDecode unreadResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.UnreadCountChanged,
            Encode(in unread, UnreadCountChangedSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, unreadResult.Status);
        var unreadActual = Assert.IsType<UnreadCountChanged>(unreadResult.Value);
        Assert.Equal(unread.UnreadCount, unreadActual.UnreadCount);
        Assert.Equal(unread.LastReadAtMs, unreadActual.LastReadAtMs);

        var read = new ConversationReadUpdate
        {
            ConversationId = "conv-1",
            ReaderUserId = 7,
            LastReadMessageId = "m-6",
            LastReadAtMs = 1_700_000_000_100
        };
        TcpBinaryWireDecode readResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ConversationRead,
            Encode(in read, ConversationReadUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, readResult.Status);
        var readActual = Assert.IsType<ConversationReadUpdate>(readResult.Value);
        Assert.Equal(
            (read.ReaderUserId, read.LastReadMessageId, read.LastReadAtMs),
            (readActual.ReaderUserId, readActual.LastReadMessageId, readActual.LastReadAtMs));
    }

    [Fact]
    public void ConversationSetPrefsPairRoundTripsThroughTheRegistryDispatch()
    {
        var request = new ConversationSetPrefsRequest
        {
            RequestId = "reg-set-prefs",
            ConversationId = "conv-1",
            Pinned = true,
            Muted = true
        };
        TcpBinaryWireDecode requestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ConversationSetPrefsRequest,
            Encode(in request, ConversationSetPrefsRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, requestResult.Status);
        var requestActual = Assert.IsType<ConversationSetPrefsRequest>(requestResult.Value);
        Assert.Equal(
            (request.RequestId, request.ConversationId, request.Pinned, request.Muted),
            (requestActual.RequestId, requestActual.ConversationId, requestActual.Pinned, requestActual.Muted));

        var response = new ConversationSetPrefsResponse
        {
            RequestId = "reg-set-prefs",
            Succeeded = true,
            ConversationId = "conv-1",
            IsPinned = true,
            IsMuted = true,
            MutedUntilMs = 1_800_000_000_000,
            Changed = true
        };
        TcpBinaryWireDecode responseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ConversationSetPrefsResponse,
            Encode(in response, ConversationSetPrefsResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, responseResult.Status);
        var responseActual = Assert.IsType<ConversationSetPrefsResponse>(responseResult.Value);
        Assert.Equal(response.IsPinned, responseActual.IsPinned);
        Assert.Equal(response.MutedUntilMs, responseActual.MutedUntilMs);
    }

    [Fact]
    public void ReadReceiptQueryPairRoundTripsThroughTheRegistryDispatch()
    {
        var request = new MessageReadReceiptQueryRequest
        {
            RequestId = "reg-receipt-query",
            ConversationId = "conv-1",
            MessageId = "m-7",
            Cursor = 42,
            PageSize = 20
        };
        TcpBinaryWireDecode requestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageReadReceiptQueryRequest,
            Encode(in request, MessageReadReceiptQueryRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, requestResult.Status);
        var requestActual = Assert.IsType<MessageReadReceiptQueryRequest>(requestResult.Value);
        Assert.Equal(
            (request.RequestId, request.ConversationId, request.MessageId, request.Cursor, request.PageSize),
            (requestActual.RequestId, requestActual.ConversationId, requestActual.MessageId, requestActual.Cursor, requestActual.PageSize));

        var response = new MessageReadReceiptQueryResponse
        {
            RequestId = "reg-receipt-query",
            Succeeded = true,
            ConversationId = "conv-1",
            ReadCount = 2,
            TotalMemberCount = 5,
            IsSmallGroup = true,
            Readers =
            [
                new MessageReadReceiptItem { UserId = 7, ReadAtMs = 1_700_000_000_100 },
                new MessageReadReceiptItem { UserId = 8, ReadAtMs = 1_700_000_000_200 }
            ],
            NextCursor = 8,
            HasMore = false
        };
        TcpBinaryWireDecode responseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageReadReceiptQueryResponse,
            Encode(in response, MessageReadReceiptQueryResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, responseResult.Status);
        var responseActual = Assert.IsType<MessageReadReceiptQueryResponse>(responseResult.Value);
        Assert.Equal(response.ReadCount, responseActual.ReadCount);
        Assert.Equal(2, responseActual.Readers?.Count);
        Assert.Equal(response.NextCursor, responseActual.NextCursor);
    }

    [Fact]
    public void SyncBootstrapPairRoundTripsThroughTheRegistryDispatch()
    {
        var request = new SyncBootstrapRequest
        {
            RequestId = "reg-sync",
            ListLimit = 50,
            HistoryLimitPerConversation = 20,
            MaxConversationsWithHistory = 10
        };
        TcpBinaryWireDecode requestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.SyncBootstrapRequest,
            Encode(in request, SyncBootstrapRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, requestResult.Status);
        var requestActual = Assert.IsType<SyncBootstrapRequest>(requestResult.Value);
        Assert.Equal(request.ListLimit, requestActual.ListLimit);

        var response = new SyncBootstrapResponse
        {
            RequestId = "reg-sync",
            Succeeded = true,
            ServerTimeMs = 1_700_000_000_999,
            Conversations =
            [
                new TcpConversationListItem
                {
                    ConversationId = "conv-1",
                    Type = TcpConversationType.Direct,
                    PeerUserId = 7,
                    UnreadCount = 1
                }
            ],
            ConversationsHasMore = false
        };
        TcpBinaryWireDecode responseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.SyncBootstrapResponse,
            Encode(in response, SyncBootstrapResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, responseResult.Status);
        var responseActual = Assert.IsType<SyncBootstrapResponse>(responseResult.Value);
        Assert.Single(responseActual.Conversations);
        Assert.Equal(response.ServerTimeMs, responseActual.ServerTimeMs);

        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in response, SyncBootstrapResponseSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode sequenceResult = TcpBinaryWireCodec.TryDecode(PacketCommand.SyncBootstrapResponse, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, sequenceResult.Status);
        var sequenceActual = Assert.IsType<SyncBootstrapResponse>(sequenceResult.Value);
        Assert.Equal(response.Succeeded, sequenceActual.Succeeded);
    }

    [Fact]
    public void RelationshipListPairRoundTripsThroughTheRegistryDispatch()
    {
        var request = new TcpRelationshipListRequest
        {
            RequestId = "reg-rel-list",
            ListType = TcpRelationshipListType.Friends,
            PageSize = 30,
            Cursor = null
        };
        TcpBinaryWireDecode requestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RelationshipListRequest,
            Encode(in request, TcpRelationshipListRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, requestResult.Status);
        var requestActual = Assert.IsType<TcpRelationshipListRequest>(requestResult.Value);
        Assert.Equal(
            (request.RequestId, request.ListType, request.PageSize, request.Cursor),
            (requestActual.RequestId, requestActual.ListType, requestActual.PageSize, requestActual.Cursor));

        var response = new TcpRelationshipListResponse
        {
            RequestId = "reg-rel-list",
            ListType = TcpRelationshipListType.Friends,
            Succeeded = true,
            ResetRequired = false,
            Items =
            [
                new TcpRelationshipListItem
                {
                    UserId = 7,
                    ResourceId = "friend-1",
                    Status = "Accepted",
                    Message = null,
                    CreatedAtMs = 1_700_000_000_100
                },
                new TcpRelationshipListItem
                {
                    UserId = 8,
                    ResourceId = "friend-2",
                    Status = "Accepted",
                    CreatedAtMs = 1_700_000_000_200
                }
            ],
            NextCursor = "cursor-2",
            HasMore = true
        };
        TcpBinaryWireDecode spanResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RelationshipListResponse,
            Encode(in response, TcpRelationshipListResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, spanResult.Status);
        var spanActual = Assert.IsType<TcpRelationshipListResponse>(spanResult.Value);
        Assert.Equal(2, spanActual.Items.Count);
        Assert.Equal(response.Items[1].ResourceId, spanActual.Items[1].ResourceId);
        Assert.Equal(response.NextCursor, spanActual.NextCursor);
        Assert.Equal(response.HasMore, spanActual.HasMore);

        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in response, TcpRelationshipListResponseSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode sequenceResult = TcpBinaryWireCodec.TryDecode(PacketCommand.RelationshipListResponse, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, sequenceResult.Status);
        var sequenceActual = Assert.IsType<TcpRelationshipListResponse>(sequenceResult.Value);
        Assert.Equal(response.Succeeded, sequenceActual.Succeeded);
        Assert.Equal(response.Items.Count, sequenceActual.Items.Count);
    }

    [Fact]
    public void RelationshipCommandPairRoundTripsThroughTheRegistryDispatch()
    {
        var request = new TcpRelationshipCommandRequest
        {
            RequestId = "reg-rel-cmd",
            Operation = TcpRelationshipOperation.SendFriendRequest,
            TargetUserId = 42,
            Message = "hi from bob"
        };
        TcpBinaryWireDecode requestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RelationshipCommandRequest,
            Encode(in request, TcpRelationshipCommandRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, requestResult.Status);
        var requestActual = Assert.IsType<TcpRelationshipCommandRequest>(requestResult.Value);
        Assert.Equal(
            (request.RequestId, request.Operation, request.TargetUserId, request.Message, request.RequestIdToRespond),
            (requestActual.RequestId, requestActual.Operation, requestActual.TargetUserId, requestActual.Message, requestActual.RequestIdToRespond));

        var response = new TcpRelationshipCommandResponse
        {
            RequestId = "reg-rel-cmd",
            Succeeded = true,
            Operation = TcpRelationshipOperation.SendFriendRequest,
            TargetUserId = 42,
            ResourceId = "req-99"
        };
        TcpBinaryWireDecode responseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RelationshipCommandResponse,
            Encode(in response, TcpRelationshipCommandResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, responseResult.Status);
        var responseActual = Assert.IsType<TcpRelationshipCommandResponse>(responseResult.Value);
        Assert.Equal(
            (response.RequestId, response.Succeeded, response.Operation, response.TargetUserId, response.ResourceId),
            (responseActual.RequestId, responseActual.Succeeded, responseActual.Operation, responseActual.TargetUserId, responseActual.ResourceId));

        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in response, TcpRelationshipCommandResponseSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode sequenceResult = TcpBinaryWireCodec.TryDecode(PacketCommand.RelationshipCommandResponse, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, sequenceResult.Status);
        var sequenceActual = Assert.IsType<TcpRelationshipCommandResponse>(sequenceResult.Value);
        Assert.Equal(response.ResourceId, sequenceActual.ResourceId);
    }

    [Fact]
    public void RelationshipListChangedRoundTripsThroughTheRegistryDispatch()
    {
        var update = new TcpRelationshipListChangedUpdate
        {
            Resource = "friend-request",
            Action = "Pending",
            ResourceId = "req-88",
            ActorUserId = 42,
            Message = "please add me",
            OccurredAtMs = 1_700_000_000_100
        };
        TcpBinaryWireDecode spanResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RelationshipListChanged,
            Encode(in update, TcpRelationshipListChangedUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, spanResult.Status);
        var spanActual = Assert.IsType<TcpRelationshipListChangedUpdate>(spanResult.Value);
        Assert.Equal(
            (update.Resource, update.Action, update.ResourceId, update.ActorUserId, update.Message, update.OccurredAtMs),
            (spanActual.Resource, spanActual.Action, spanActual.ResourceId, spanActual.ActorUserId, spanActual.Message, spanActual.OccurredAtMs));

        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in update, TcpRelationshipListChangedUpdateSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode sequenceResult = TcpBinaryWireCodec.TryDecode(PacketCommand.RelationshipListChanged, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, sequenceResult.Status);
        var sequenceActual = Assert.IsType<TcpRelationshipListChangedUpdate>(sequenceResult.Value);
        Assert.Equal(update.OccurredAtMs, sequenceActual.OccurredAtMs);
    }

    [Fact]
    public void TypingPairRoundTripsThroughTheRegistryDispatch()
    {
        var notify = new TcpTypingNotify
        {
            TargetUserId = 42,
            ConversationId = "conv-1",
            IsTyping = true
        };
        TcpBinaryWireDecode notifyResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.TypingNotify,
            Encode(in notify, TcpTypingNotifySchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, notifyResult.Status);
        var notifyActual = Assert.IsType<TcpTypingNotify>(notifyResult.Value);
        Assert.Equal(
            (notify.TargetUserId, notify.ConversationId, notify.IsTyping),
            (notifyActual.TargetUserId, notifyActual.ConversationId, notifyActual.IsTyping));

        var update = new TcpTypingUpdate
        {
            SenderUserId = 7,
            ConversationId = "conv-1",
            IsTyping = false
        };
        TcpBinaryWireDecode updateResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.TypingUpdate,
            Encode(in update, TcpTypingUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, updateResult.Status);
        var updateActual = Assert.IsType<TcpTypingUpdate>(updateResult.Value);
        Assert.Equal((update.SenderUserId, update.IsTyping), (updateActual.SenderUserId, updateActual.IsTyping));
    }

    [Fact]
    public void PresenceCommandsRoundTripThroughTheRegistryDispatch()
    {
        var query = new TcpPresenceQueryRequest
        {
            RequestId = "reg-presence",
            UserIds = [7, 8, 9]
        };
        TcpBinaryWireDecode querySpan = TcpBinaryWireCodec.TryDecode(
            PacketCommand.PresenceQuery,
            Encode(in query, TcpPresenceQueryRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, querySpan.Status);
        var queryActual = Assert.IsType<TcpPresenceQueryRequest>(querySpan.Value);
        Assert.Equal(3, queryActual.UserIds!.Count);
        Assert.Equal(new long[] { 7, 8, 9 }, queryActual.UserIds);

        ReadOnlySequence<byte> querySegmented = Segmented(
            Encode(in query, TcpPresenceQueryRequestSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode querySequence = TcpBinaryWireCodec.TryDecode(PacketCommand.PresenceQuery, in querySegmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, querySequence.Status);
        var querySequenceActual = Assert.IsType<TcpPresenceQueryRequest>(querySequence.Value);
        Assert.Equal(3, querySequenceActual.UserIds!.Count);

        var unwatch = new TcpPresenceUnwatchRequest { UserIds = [42] };
        TcpBinaryWireDecode unwatchResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.PresenceUnwatch,
            Encode(in unwatch, TcpPresenceUnwatchRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, unwatchResult.Status);
        var unwatchActual = Assert.IsType<TcpPresenceUnwatchRequest>(unwatchResult.Value);
        Assert.Single(unwatchActual.UserIds!);
        Assert.Equal(42L, unwatchActual.UserIds![0]);

        var snapshot = new TcpPresenceSnapshotResponse
        {
            RequestId = "reg-presence",
            Items = [new TcpPresenceSnapshotItem { UserId = 7, IsOnline = true }]
        };
        TcpBinaryWireDecode snapshotResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.PresenceSnapshot,
            Encode(in snapshot, TcpPresenceSnapshotResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, snapshotResult.Status);
        var snapshotActual = Assert.IsType<TcpPresenceSnapshotResponse>(snapshotResult.Value);
        Assert.Single(snapshotActual.Items);
        Assert.True(snapshotActual.Items[0].IsOnline);

        var changed = new TcpPresenceChanged { UserId = 9, IsOnline = false };
        TcpBinaryWireDecode changedSpan = TcpBinaryWireCodec.TryDecode(
            PacketCommand.PresenceChanged,
            Encode(in changed, TcpPresenceChangedSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, changedSpan.Status);
        var changedActual = Assert.IsType<TcpPresenceChanged>(changedSpan.Value);
        Assert.Equal((changed.UserId, changed.IsOnline), (changedActual.UserId, changedActual.IsOnline));
    }

    [Fact]
    public void PushTokenPairsRoundTripThroughTheRegistryDispatch()
    {
        var register = new TcpRegisterPushTokenRequest
        {
            RequestId = "reg-push",
            Platform = TcpPushPlatform.Fcm,
            Token = "fcm-token-123",
            AppDeviceLabel = "my-device"
        };
        TcpBinaryWireDecode registerResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RegisterPushTokenRequest,
            Encode(in register, TcpRegisterPushTokenRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, registerResult.Status);
        var registerActual = Assert.IsType<TcpRegisterPushTokenRequest>(registerResult.Value);
        Assert.Equal(
            (register.RequestId, register.Platform, register.Token, register.AppDeviceLabel),
            (registerActual.RequestId, registerActual.Platform, registerActual.Token, registerActual.AppDeviceLabel));

        var registerResponse = new TcpRegisterPushTokenResponse
        {
            RequestId = "reg-push",
            Succeeded = true,
            ActiveTokenCount = 2
        };
        TcpBinaryWireDecode registerResponseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RegisterPushTokenResponse,
            Encode(in registerResponse, TcpRegisterPushTokenResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, registerResponseResult.Status);
        var registerResponseActual = Assert.IsType<TcpRegisterPushTokenResponse>(registerResponseResult.Value);
        Assert.Equal(registerResponse.ActiveTokenCount, registerResponseActual.ActiveTokenCount);

        var unregister = new TcpUnregisterPushTokenRequest
        {
            RequestId = "reg-unpush",
            Token = "fcm-token-123"
        };
        TcpBinaryWireDecode unregisterResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.UnregisterPushTokenRequest,
            Encode(in unregister, TcpUnregisterPushTokenRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, unregisterResult.Status);
        var unregisterActual = Assert.IsType<TcpUnregisterPushTokenRequest>(unregisterResult.Value);
        Assert.Equal((unregister.RequestId, unregister.Token), (unregisterActual.RequestId, unregisterActual.Token));

        var unregisterResponse = new TcpUnregisterPushTokenResponse
        {
            RequestId = "reg-unpush",
            Succeeded = false,
            ErrorCode = "not_found",
            ActiveTokenCount = 0
        };
        ReadOnlySequence<byte> unregisterResponseSegmented = Segmented(
            Encode(in unregisterResponse, TcpUnregisterPushTokenResponseSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode unregisterResponseSequence = TcpBinaryWireCodec.TryDecode(PacketCommand.UnregisterPushTokenResponse, in unregisterResponseSegmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, unregisterResponseSequence.Status);
        var unregisterResponseActual = Assert.IsType<TcpUnregisterPushTokenResponse>(unregisterResponseSequence.Value);
        Assert.Equal(unregisterResponse.ErrorCode, unregisterResponseActual.ErrorCode);
        Assert.Equal(unregisterResponse.Succeeded, unregisterResponseActual.Succeeded);
    }

    [Fact]
    public void GroupCommandsRoundTripThroughTheRegistryDispatch()
    {
        // Create: nested member list + repeated member user ids (Span path).
        var createRequest = new TcpCreateGroupRequest
        {
            RequestId = "grp-create",
            Title = "dev team",
            MemberUserIds = [101, 102, 103]
        };
        TcpBinaryWireDecode createRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CreateGroupRequest,
            Encode(in createRequest, TcpCreateGroupRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, createRequestResult.Status);
        var createRequestActual = Assert.IsType<TcpCreateGroupRequest>(createRequestResult.Value);
        Assert.Equal("dev team", createRequestActual.Title);
        Assert.Equal(new long[] { 101, 102, 103 }, createRequestActual.MemberUserIds);

        var createResponse = new TcpCreateGroupResponse
        {
            RequestId = "grp-create",
            Succeeded = true,
            ConversationId = "conv-g1",
            Title = "dev team",
            Members =
            [
                new TcpConversationMemberItem { UserId = 101, Role = TcpGroupMemberRole.Owner, JoinedAtMs = 10 },
                new TcpConversationMemberItem { UserId = 102, Role = TcpGroupMemberRole.Member, JoinedAtMs = 11 }
            ]
        };
        ReadOnlySequence<byte> createResponseSegmented = Segmented(
            Encode(in createResponse, TcpCreateGroupResponseSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode createResponseSequence = TcpBinaryWireCodec.TryDecode(PacketCommand.CreateGroupResponse, in createResponseSegmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, createResponseSequence.Status);
        var createResponseActual = Assert.IsType<TcpCreateGroupResponse>(createResponseSequence.Value);
        Assert.Equal("conv-g1", createResponseActual.ConversationId);
        Assert.Equal(TcpGroupMemberRole.Owner, createResponseActual.Members![0].Role);
        Assert.Equal(2, createResponseActual.Members!.Count);

        // Add members: repeated member user ids (Span) + nested member list (segmented).
        var addRequest = new TcpAddGroupMembersRequest { RequestId = "grp-add", ConversationId = "conv-g1", MemberUserIds = [201] };
        TcpBinaryWireDecode addRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AddGroupMembersRequest,
            Encode(in addRequest, TcpAddGroupMembersRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, addRequestResult.Status);
        var addRequestActual = Assert.IsType<TcpAddGroupMembersRequest>(addRequestResult.Value);
        Assert.Equal(new long[] { 201 }, addRequestActual.MemberUserIds);

        var addResponse = new TcpAddGroupMembersResponse
        {
            RequestId = "grp-add",
            Succeeded = true,
            ConversationId = "conv-g1",
            Members = [new TcpConversationMemberItem { UserId = 201, Role = TcpGroupMemberRole.Member, JoinedAtMs = 12 }]
        };
        ReadOnlySequence<byte> addResponseSegmented = Segmented(Encode(in addResponse, TcpAddGroupMembersResponseSchema.TryEncode, Limits), 2);
        TcpBinaryWireDecode addResponseSequence = TcpBinaryWireCodec.TryDecode(PacketCommand.AddGroupMembersResponse, in addResponseSegmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, addResponseSequence.Status);
        Assert.Equal(201, Assert.IsType<TcpAddGroupMembersResponse>(addResponseSequence.Value).Members![0].UserId);

        // Remove member.
        var removeRequest = new TcpRemoveGroupMemberRequest { RequestId = "grp-rm", ConversationId = "conv-g1", TargetUserId = 102 };
        TcpBinaryWireDecode removeResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RemoveGroupMemberRequest,
            Encode(in removeRequest, TcpRemoveGroupMemberRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, removeResult.Status);
        Assert.Equal(102, Assert.IsType<TcpRemoveGroupMemberRequest>(removeResult.Value).TargetUserId);

        var removeResponse = new TcpRemoveGroupMemberResponse { RequestId = "grp-rm", Succeeded = true, ConversationId = "conv-g1" };
        TcpBinaryWireDecode removeResponseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RemoveGroupMemberResponse,
            Encode(in removeResponse, TcpRemoveGroupMemberResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, removeResponseResult.Status);
        Assert.True(Assert.IsType<TcpRemoveGroupMemberResponse>(removeResponseResult.Value).Succeeded);

        // Leave group.
        var leaveRequest = new TcpLeaveGroupRequest { RequestId = "grp-leave", ConversationId = "conv-g1" };
        TcpBinaryWireDecode leaveResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.LeaveGroupRequest,
            Encode(in leaveRequest, TcpLeaveGroupRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, leaveResult.Status);
        Assert.Equal("conv-g1", Assert.IsType<TcpLeaveGroupRequest>(leaveResult.Value).ConversationId);

        var leaveResponse = new TcpLeaveGroupResponse { RequestId = "grp-leave", Succeeded = true, ConversationId = "conv-g1" };
        TcpBinaryWireDecode leaveResponseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.LeaveGroupResponse,
            Encode(in leaveResponse, TcpLeaveGroupResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, leaveResponseResult.Status);
        Assert.True(Assert.IsType<TcpLeaveGroupResponse>(leaveResponseResult.Value).Succeeded);

        // Change member role: enum field.
        var roleRequest = new TcpChangeMemberRoleRequest { RequestId = "grp-role", ConversationId = "conv-g1", TargetUserId = 201, NewRole = TcpGroupMemberRole.Admin };
        TcpBinaryWireDecode roleResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ChangeMemberRoleRequest,
            Encode(in roleRequest, TcpChangeMemberRoleRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, roleResult.Status);
        Assert.Equal(TcpGroupMemberRole.Admin, Assert.IsType<TcpChangeMemberRoleRequest>(roleResult.Value).NewRole);

        var roleResponse = new TcpChangeMemberRoleResponse { RequestId = "grp-role", Succeeded = true, ConversationId = "conv-g1" };
        TcpBinaryWireDecode roleResponseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ChangeMemberRoleResponse,
            Encode(in roleResponse, TcpChangeMemberRoleResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, roleResponseResult.Status);
        Assert.True(Assert.IsType<TcpChangeMemberRoleResponse>(roleResponseResult.Value).Succeeded);

        // List members: nullable page size/cursor + nested members + next cursor + has more (segmented).
        var listRequest = new TcpListGroupMembersRequest { RequestId = "grp-list", ConversationId = "conv-g1", PageSize = 50, Cursor = "cursor-a" };
        TcpBinaryWireDecode listRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ListGroupMembersRequest,
            Encode(in listRequest, TcpListGroupMembersRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, listRequestResult.Status);
        var listRequestActual = Assert.IsType<TcpListGroupMembersRequest>(listRequestResult.Value);
        Assert.Equal(50, listRequestActual.PageSize);
        Assert.Equal("cursor-a", listRequestActual.Cursor);

        var listResponse = new TcpListGroupMembersResponse
        {
            RequestId = "grp-list",
            Succeeded = true,
            ConversationId = "conv-g1",
            Members = [new TcpConversationMemberItem { UserId = 101, Role = TcpGroupMemberRole.Owner, JoinedAtMs = 10 }],
            NextCursor = "cursor-b",
            HasMore = true
        };
        ReadOnlySequence<byte> listResponseSegmented = Segmented(Encode(in listResponse, TcpListGroupMembersResponseSchema.TryEncode, Limits), 2);
        TcpBinaryWireDecode listResponseSequence = TcpBinaryWireCodec.TryDecode(PacketCommand.ListGroupMembersResponse, in listResponseSegmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, listResponseSequence.Status);
        var listResponseActual = Assert.IsType<TcpListGroupMembersResponse>(listResponseSequence.Value);
        Assert.Equal("cursor-b", listResponseActual.NextCursor);
        Assert.True(listResponseActual.HasMore);
        Assert.Equal(TcpGroupMemberRole.Owner, listResponseActual.Members![0].Role);

        // Member lifecycle updates: joined / left / removed.
        var joined = new TcpMemberJoinedUpdate { ConversationId = "conv-g1", UserId = 301, Role = TcpGroupMemberRole.Member, ActorUserId = 101, Title = "dev team", OccurredAtMs = 99 };
        TcpBinaryWireDecode joinedResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MemberJoined,
            Encode(in joined, TcpMemberJoinedUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, joinedResult.Status);
        var joinedActual = Assert.IsType<TcpMemberJoinedUpdate>(joinedResult.Value);
        Assert.Equal(301, joinedActual.UserId);
        Assert.Equal(TcpGroupMemberRole.Member, joinedActual.Role);

        var left = new TcpMemberLeftUpdate { ConversationId = "conv-g1", UserId = 301, OccurredAtMs = 100 };
        TcpBinaryWireDecode leftResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MemberLeft,
            Encode(in left, TcpMemberLeftUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, leftResult.Status);
        Assert.Equal(100, Assert.IsType<TcpMemberLeftUpdate>(leftResult.Value).OccurredAtMs);

        var removed = new TcpMemberRemovedUpdate { ConversationId = "conv-g1", UserId = 301, ActorUserId = 101, OccurredAtMs = 101 };
        TcpBinaryWireDecode removedResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MemberRemoved,
            Encode(in removed, TcpMemberRemovedUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, removedResult.Status);
        Assert.Equal(101, Assert.IsType<TcpMemberRemovedUpdate>(removedResult.Value).ActorUserId);

        // Role changed: nullable previous role populated.
        var roleChanged = new TcpRoleChangedUpdate { ConversationId = "conv-g1", UserId = 201, NewRole = TcpGroupMemberRole.Admin, PreviousRole = TcpGroupMemberRole.Member, ActorUserId = 101, OccurredAtMs = 102 };
        TcpBinaryWireDecode roleChangedResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RoleChanged,
            Encode(in roleChanged, TcpRoleChangedUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, roleChangedResult.Status);
        var roleChangedActual = Assert.IsType<TcpRoleChangedUpdate>(roleChangedResult.Value);
        Assert.Equal(TcpGroupMemberRole.Admin, roleChangedActual.NewRole);
        Assert.Equal(TcpGroupMemberRole.Member, roleChangedActual.PreviousRole);

        // Members added batch event: repeated user ids.
        var membersAdded = new TcpMembersAddedUpdate { ConversationId = "conv-g1", AddedUserIds = [401, 402], ActorUserId = 101, Title = "dev team", OccurredAtMs = 103 };
        TcpBinaryWireDecode membersAddedResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MembersAddedUpdate,
            Encode(in membersAdded, TcpMembersAddedUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, membersAddedResult.Status);
        Assert.Equal(new long[] { 401, 402 }, Assert.IsType<TcpMembersAddedUpdate>(membersAddedResult.Value).AddedUserIds);

        // Dissolve: request/response + dissolved update (segmented).
        var dissolveRequest = new TcpDissolveGroupRequest { RequestId = "grp-dissolve", ConversationId = "conv-g1" };
        TcpBinaryWireDecode dissolveRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.DissolveGroupRequest,
            Encode(in dissolveRequest, TcpDissolveGroupRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, dissolveRequestResult.Status);
        Assert.Equal("conv-g1", Assert.IsType<TcpDissolveGroupRequest>(dissolveRequestResult.Value).ConversationId);

        var dissolveResponse = new TcpDissolveGroupResponse { RequestId = "grp-dissolve", Succeeded = true, ConversationId = "conv-g1" };
        TcpBinaryWireDecode dissolveResponseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.DissolveGroupResponse,
            Encode(in dissolveResponse, TcpDissolveGroupResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, dissolveResponseResult.Status);
        Assert.True(Assert.IsType<TcpDissolveGroupResponse>(dissolveResponseResult.Value).Succeeded);

        var dissolved = new TcpConversationDissolvedUpdate { ConversationId = "conv-g1", ActorUserId = 101, OccurredAtMs = 104 };
        ReadOnlySequence<byte> dissolvedSegmented = Segmented(Encode(in dissolved, TcpConversationDissolvedUpdateSchema.TryEncode, Limits), 1);
        TcpBinaryWireDecode dissolvedSequence = TcpBinaryWireCodec.TryDecode(PacketCommand.ConversationDissolvedUpdate, in dissolvedSegmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, dissolvedSequence.Status);
        Assert.Equal(104, Assert.IsType<TcpConversationDissolvedUpdate>(dissolvedSequence.Value).OccurredAtMs);
    }

    [Fact]
    public void AuthenticationFramesRoundTripThroughTheRegistryDispatch()
    {
        var request = new AuthenticationRequest
        {
            AccessToken = "at-reg-1",
            DeviceIdHash = 0x0123_4567_89AB_CDEF
        };
        TcpBinaryWireDecode requestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AuthenticationRequest,
            Encode(in request, AuthenticationRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, requestResult.Status);
        var requestActual = Assert.IsType<AuthenticationRequest>(requestResult.Value);
        Assert.Equal(
            (request.AccessToken, request.DeviceIdHash),
            (requestActual.AccessToken, requestActual.DeviceIdHash));

        var response = new AuthenticationResponse
        {
            Success = true,
            UserId = 90001,
            SessionId = "session-01",
            DeviceIdHash = 0x0123_4567_89AB_CDEF,
            DeviceId = "device-01",
            ResumeToken = "resume-token-01"
        };
        TcpBinaryWireDecode responseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AuthenticationResponse,
            Encode(in response, AuthenticationResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, responseResult.Status);
        var responseActual = Assert.IsType<AuthenticationResponse>(responseResult.Value);
        Assert.Equal(
            (response.Success, response.UserId, response.SessionId,
                response.DeviceIdHash, response.DeviceId, response.ResumeToken),
            (responseActual.Success, responseActual.UserId, responseActual.SessionId,
                responseActual.DeviceIdHash, responseActual.DeviceId, responseActual.ResumeToken));

        // Optional/absent fields decode to null on both paths.
        var sparse = new AuthenticationResponse { Success = false, UserId = 0 };
        TcpBinaryWireDecode sparseSpan = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AuthenticationResponse,
            Encode(in sparse, AuthenticationResponseSchema.TryEncode, Limits),
            Limits);
        var sparseActual = Assert.IsType<AuthenticationResponse>(sparseSpan.Value);
        Assert.True(sparseActual.ErrorMessage is null && sparseActual.SessionId is null
                    && sparseActual.DeviceIdHash is null && sparseActual.DeviceId is null
                    && sparseActual.ResumeToken is null);

        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in request, AuthenticationRequestSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode segmentedResult = TcpBinaryWireCodec.TryDecode(PacketCommand.AuthenticationRequest, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, segmentedResult.Status);
        Assert.Equal(request.AccessToken, Assert.IsType<AuthenticationRequest>(segmentedResult.Value).AccessToken);
    }

    [Fact]
    public void MessageChannelFramesRoundTripThroughTheRegistryDispatch()
    {
        var message = new ChatMessage
        {
            ClientMessageId = "client-msg-1",
            MessageId = "msg-1",
            ConversationId = "conv-1",
            TargetUserId = 20002,
            SenderUserId = 20001,
            Content = "hello \u2713 世界",
            SentAtMs = 1_700_000_000_500,
            AttachmentIds = new[] { "att-1", "att-2" },
            Attachments = new[]
            {
                new TcpAttachmentRef
                {
                    AttachmentId = "att-1",
                    FileName = "a.txt",
                    ContentType = "text/plain",
                    SizeBytes = 12,
                    Status = 1
                }
            },
            ReplyToMessageId = "msg-0",
            ReplyToSenderUserId = 20001,
            ReplyToPreview = "hi",
            MentionedUserIds = new long[] { 20003, 20004 },
            MentionedRoles = new[] { "all" }
        };
        TcpBinaryWireDecode messageResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ChatMessage,
            Encode(in message, ChatMessageSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, messageResult.Status);
        var messageActual = Assert.IsType<ChatMessage>(messageResult.Value);
        Assert.Equal(message.Content, messageActual.Content);
        Assert.Equal(message.SentAtMs, messageActual.SentAtMs);
        Assert.Equal((message.ClientMessageId, message.MessageId, message.ConversationId,
            message.TargetUserId, message.SenderUserId), (messageActual.ClientMessageId, messageActual.MessageId,
            messageActual.ConversationId, messageActual.TargetUserId, messageActual.SenderUserId));
        Assert.Equal(message.AttachmentIds, messageActual.AttachmentIds);
        Assert.Single(messageActual.Attachments!);
        Assert.Equal("att-1", messageActual.Attachments![0].AttachmentId);
        Assert.Equal(message.ReplyToMessageId, messageActual.ReplyToMessageId);
        Assert.Equal(message.MentionedUserIds, messageActual.MentionedUserIds);
        Assert.Equal(message.MentionedRoles, messageActual.MentionedRoles);

        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in message, ChatMessageSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode segmentedResult = TcpBinaryWireCodec.TryDecode(PacketCommand.ChatMessage, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, segmentedResult.Status);
        Assert.Equal(message.Content, Assert.IsType<ChatMessage>(segmentedResult.Value).Content);

        var ack = new MessageAcknowledgement
        {
            ClientMessageId = "client-msg-1",
            CommandId = "cmd-1",
            Accepted = true,
            AcknowledgedAtMs = 1_700_000_001_000
        };
        TcpBinaryWireDecode ackResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageAcknowledgement,
            Encode(in ack, MessageAcknowledgementSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, ackResult.Status);
        var ackActual = Assert.IsType<MessageAcknowledgement>(ackResult.Value);
        Assert.Equal((ack.ClientMessageId, ack.CommandId, ack.Accepted, ack.AcknowledgedAtMs),
            (ackActual.ClientMessageId, ackActual.CommandId, ackActual.Accepted, ackActual.AcknowledgedAtMs));
    }

    [Fact]
    public void MessageReceiptFramesRoundTripThroughTheRegistryDispatch()
    {
        var receipt = new MessageReceipt
        {
            RequestId = "req-1",
            ConversationId = "conv-1",
            LastReadMessageId = "msg-5",
            LastReadAtMs = 1_700_000_010_000,
            ReaderUserId = 20002,
            ReceiverUserId = 20001
        };
        TcpBinaryWireDecode receiptResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageReceipt,
            Encode(in receipt, MessageReceiptSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, receiptResult.Status);
        var receiptActual = Assert.IsType<MessageReceipt>(receiptResult.Value);
        Assert.Equal((receipt.RequestId, receipt.ConversationId, receipt.LastReadMessageId,
            receipt.LastReadAtMs, receipt.ReaderUserId, receipt.ReceiverUserId),
            (receiptActual.RequestId, receiptActual.ConversationId, receiptActual.LastReadMessageId,
                receiptActual.LastReadAtMs, receiptActual.ReaderUserId, receiptActual.ReceiverUserId));

        var receiptAck = new MessageReceiptAcknowledgement { RequestId = "req-1", Accepted = true };
        TcpBinaryWireDecode ackResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageReceiptAcknowledgement,
            Encode(in receiptAck, MessageReceiptAcknowledgementSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, ackResult.Status);
        var receiptAckActual = Assert.IsType<MessageReceiptAcknowledgement>(ackResult.Value);
        Assert.Equal((receiptAck.RequestId, receiptAck.Accepted),
            (receiptAckActual.RequestId, receiptAckActual.Accepted));

        var updated = new MessageReceiptUpdated
        {
            ConversationId = "conv-1",
            LastReadMessageId = "msg-6",
            LastReadAtMs = 1_700_000_011_000,
            ReaderUserId = 20002
        };
        TcpBinaryWireDecode updatedResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageReceiptUpdated,
            Encode(in updated, MessageReceiptUpdatedSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, updatedResult.Status);
        var updatedActual = Assert.IsType<MessageReceiptUpdated>(updatedResult.Value);
        Assert.Equal((updated.ConversationId, updated.LastReadMessageId, updated.LastReadAtMs,
            updated.ReaderUserId), (updatedActual.ConversationId, updatedActual.LastReadMessageId,
            updatedActual.LastReadAtMs, updatedActual.ReaderUserId));

        // Sparse receipt decodes absent scalars to null on the span path.
        var sparse = new MessageReceipt();
        TcpBinaryWireDecode sparseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageReceipt,
            Encode(in sparse, MessageReceiptSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, sparseResult.Status);
        var sparseActual = Assert.IsType<MessageReceipt>(sparseResult.Value);
        Assert.True(sparseActual.ConversationId is null && sparseActual.LastReadMessageId is null
                    && sparseActual.LastReadAtMs is null && sparseActual.ReaderUserId is null
                    && sparseActual.ReceiverUserId is null);

        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in receipt, MessageReceiptSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode segmentedResult = TcpBinaryWireCodec.TryDecode(PacketCommand.MessageReceipt, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, segmentedResult.Status);
        Assert.Equal("msg-5", Assert.IsType<MessageReceipt>(segmentedResult.Value).LastReadMessageId);
    }

    [Fact]
    public void AttachmentAndCallFramesRoundTripThroughTheRegistryDispatch()
    {
        var lifecycle = new AttachmentLifecycleChanged
        {
            AttachmentId = "att-lifecycle-1",
            Status = 1,
            OccurredAtMs = 1_700_000_010_000,
            RejectReason = "virus",
            ThumbnailApiHint = "https://obj/hint/att-1",
            DownloadToken = "dl-token-1"
        };
        TcpBinaryWireDecode lifecycleResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AttachmentLifecycleChanged,
            Encode(in lifecycle, AttachmentLifecycleChangedSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, lifecycleResult.Status);
        var lifecycleActual = Assert.IsType<AttachmentLifecycleChanged>(lifecycleResult.Value);
        Assert.Equal((lifecycle.AttachmentId, lifecycle.Status, lifecycle.OccurredAtMs,
            lifecycle.RejectReason, lifecycle.ThumbnailApiHint, lifecycle.DownloadToken),
            (lifecycleActual.AttachmentId, lifecycleActual.Status, lifecycleActual.OccurredAtMs,
                lifecycleActual.RejectReason, lifecycleActual.ThumbnailApiHint, lifecycleActual.DownloadToken));

        var finalizeRequest = new AttachmentFinalizeRequest
        {
            RequestId = "req-finalize-1",
            AttachmentId = "att-f-1",
            SizeBytes = 4096,
            ContentHash = "sha256:abc"
        };
        TcpBinaryWireDecode finalizeRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AttachmentFinalizeRequest,
            Encode(in finalizeRequest, AttachmentFinalizeRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, finalizeRequestResult.Status);
        var finalizeRequestActual = Assert.IsType<AttachmentFinalizeRequest>(finalizeRequestResult.Value);
        Assert.Equal((finalizeRequest.RequestId, finalizeRequest.AttachmentId,
            finalizeRequest.SizeBytes, finalizeRequest.ContentHash),
            (finalizeRequestActual.RequestId, finalizeRequestActual.AttachmentId,
                finalizeRequestActual.SizeBytes, finalizeRequestActual.ContentHash));

        var finalizeResponse = new AttachmentFinalizeResponse
        {
            RequestId = "req-finalize-1",
            Succeeded = true,
            ErrorCode = null,
            ErrorMessage = null,
            AttachmentId = "att-f-1",
            Status = 2
        };
        TcpBinaryWireDecode finalizeResponseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AttachmentFinalizeResponse,
            Encode(in finalizeResponse, AttachmentFinalizeResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, finalizeResponseResult.Status);
        var finalizeResponseActual = Assert.IsType<AttachmentFinalizeResponse>(finalizeResponseResult.Value);
        Assert.Equal((finalizeResponse.RequestId, finalizeResponse.Succeeded, finalizeResponse.AttachmentId,
            finalizeResponse.Status), (finalizeResponseActual.RequestId, finalizeResponseActual.Succeeded,
            finalizeResponseActual.AttachmentId, finalizeResponseActual.Status));

        var downloadRequest = new AttachmentDownloadAuthorizeRequest
        {
            RequestId = "req-dl-1",
            AttachmentId = "att-dl-1",
            ConversationId = "conv-1"
        };
        TcpBinaryWireDecode downloadRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AttachmentDownloadAuthorizeRequest,
            Encode(in downloadRequest, AttachmentDownloadAuthorizeRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, downloadRequestResult.Status);
        var downloadRequestActual = Assert.IsType<AttachmentDownloadAuthorizeRequest>(downloadRequestResult.Value);
        Assert.Equal((downloadRequest.RequestId, downloadRequest.AttachmentId, downloadRequest.ConversationId),
            (downloadRequestActual.RequestId, downloadRequestActual.AttachmentId, downloadRequestActual.ConversationId));

        var downloadResponse = new AttachmentDownloadAuthorizeResponse
        {
            RequestId = "req-dl-1",
            Succeeded = true,
            ErrorCode = null,
            ErrorMessage = null,
            AttachmentId = "att-dl-1",
            DownloadUrl = "https://obj/dl/att-dl-1?x",
            DownloadToken = "dl-token-1",
            ExpiresAtMs = 1_700_000_020_000
        };
        TcpBinaryWireDecode downloadResponseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AttachmentDownloadAuthorizeResponse,
            Encode(in downloadResponse, AttachmentDownloadAuthorizeResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, downloadResponseResult.Status);
        var downloadResponseActual = Assert.IsType<AttachmentDownloadAuthorizeResponse>(downloadResponseResult.Value);
        Assert.Equal((downloadResponse.RequestId, downloadResponse.Succeeded, downloadResponse.AttachmentId,
            downloadResponse.DownloadUrl, downloadResponse.DownloadToken, downloadResponse.ExpiresAtMs),
            (downloadResponseActual.RequestId, downloadResponseActual.Succeeded, downloadResponseActual.AttachmentId,
                downloadResponseActual.DownloadUrl, downloadResponseActual.DownloadToken, downloadResponseActual.ExpiresAtMs));

        var signal = new TcpCallSignal
        {
            SignalId = "sig-1",
            CallId = "call-1",
            FromUserId = 20001,
            ToUserId = 20002,
            Kind = (TcpCallCommandType)1,
            Sdp = "v=0\r\nsdp-offer",
            Revision = 3,
            OccurredAtMs = 1_700_000_030_000
        };
        TcpBinaryWireDecode signalResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallSignal,
            Encode(in signal, TcpCallSignalSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, signalResult.Status);
        var signalActual = Assert.IsType<TcpCallSignal>(signalResult.Value);
        Assert.Equal((signal.SignalId, signal.CallId, signal.FromUserId, signal.ToUserId,
            signal.Kind, signal.Sdp, signal.Revision, signal.OccurredAtMs),
            (signalActual.SignalId, signalActual.CallId, signalActual.FromUserId, signalActual.ToUserId,
                signalActual.Kind, signalActual.Sdp, signalActual.Revision, signalActual.OccurredAtMs));

        var callRequest = new TcpCallCommandRequest
        {
            RequestId = "req-call-1",
            CommandId = "cmd-1",
            CallId = "call-1",
            Type = (TcpCallCommandType)1,
            ActorUserId = 20001,
            Revision = 2,
            Grant = new TcpCallGrant
            {
                CallId = "call-1",
                CallerUserId = 20001,
                CalleeUserId = 20002,
                ExpiresAtMs = 1_700_000_090_000,
                Nonce = "nonce-1",
                Signature = "sig"
            },
            Sdp = "v=0\r\nsdp-offer",
            ClientOccurredAtMs = 1_700_000_040_000
        };
        TcpBinaryWireDecode callRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallCommandRequest,
            Encode(in callRequest, TcpCallCommandRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, callRequestResult.Status);
        var callRequestActual = Assert.IsType<TcpCallCommandRequest>(callRequestResult.Value);
        Assert.Equal((callRequest.RequestId, callRequest.CommandId, callRequest.CallId, callRequest.Type,
            callRequest.ActorUserId, callRequest.Revision, callRequest.Sdp, callRequest.ClientOccurredAtMs),
            (callRequestActual.RequestId, callRequestActual.CommandId, callRequestActual.CallId, callRequestActual.Type,
                callRequestActual.ActorUserId, callRequestActual.Revision, callRequestActual.Sdp, callRequestActual.ClientOccurredAtMs));
        Assert.Equal(callRequest.Grant!.CallId, callRequestActual.Grant!.CallId);
        Assert.Equal(callRequest.Grant.CallerUserId, callRequestActual.Grant.CallerUserId);
        Assert.Equal(callRequest.Grant.CalleeUserId, callRequestActual.Grant.CalleeUserId);
        Assert.Equal(callRequest.Grant.ExpiresAtMs, callRequestActual.Grant.ExpiresAtMs);
        Assert.Equal(callRequest.Grant.Nonce, callRequestActual.Grant.Nonce);
        Assert.Equal(callRequest.Grant.Signature, callRequestActual.Grant.Signature);

        var callResponse = new TcpCallCommandResponse
        {
            RequestId = "req-call-1",
            CallId = "call-1",
            Succeeded = true,
            ErrorCode = null,
            ErrorMessage = null,
            State = (TcpCallState)2,
            EndReason = (TcpCallEndReason)0,
            Revision = 3,
            Replayed = false,
            SignalToForward = signal
        };
        TcpBinaryWireDecode callResponseResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallCommandResponse,
            Encode(in callResponse, TcpCallCommandResponseSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, callResponseResult.Status);
        var callResponseActual = Assert.IsType<TcpCallCommandResponse>(callResponseResult.Value);
        Assert.Equal((callResponse.RequestId, callResponse.CallId, callResponse.Succeeded, callResponse.State,
            callResponse.EndReason, callResponse.Revision, callResponse.Replayed),
            (callResponseActual.RequestId, callResponseActual.CallId, callResponseActual.Succeeded, callResponseActual.State,
                callResponseActual.EndReason, callResponseActual.Revision, callResponseActual.Replayed));
        Assert.NotNull(callResponseActual.SignalToForward);
        Assert.Equal(signal.SignalId, callResponseActual.SignalToForward.SignalId);

        // Segmented path for the nested-object call request.
        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in callRequest, TcpCallCommandRequestSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode segmentedResult = TcpBinaryWireCodec.TryDecode(PacketCommand.CallCommandRequest, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, segmentedResult.Status);
        Assert.Equal("call-1", Assert.IsType<TcpCallCommandRequest>(segmentedResult.Value).CallId);
    }

    [Fact]
    public void MessageTransactionFramesRoundTripThroughTheRegistryDispatch()
    {
        // Edit: request / ack / event.
        var editRequest = new MessageEditRequest { RequestId = "req-edit-1", MessageId = "msg-1", Content = "edited v1" };
        TcpBinaryWireDecode editRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageEditRequest,
            Encode(in editRequest, MessageEditRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, editRequestResult.Status);
        var editRequestActual = Assert.IsType<MessageEditRequest>(editRequestResult.Value);
        Assert.Equal((editRequest.RequestId, editRequest.MessageId, editRequest.Content),
            (editRequestActual.RequestId, editRequestActual.MessageId, editRequestActual.Content));

        var editAck = new MessageEditAcknowledgement
        {
            RequestId = "req-edit-1",
            MessageId = "msg-1",
            Succeeded = true,
            ErrorCode = null,
            ErrorMessage = null,
            ConversationId = "conv-1",
            Content = "edited v1",
            EditVersion = 2,
            EditedAtMs = 1_700_000_100_000
        };
        TcpBinaryWireDecode editAckResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageEditAck,
            Encode(in editAck, MessageEditAcknowledgementSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, editAckResult.Status);
        var editAckActual = Assert.IsType<MessageEditAcknowledgement>(editAckResult.Value);
        Assert.Equal((editAck.RequestId, editAck.MessageId, editAck.Succeeded, editAck.ConversationId,
            editAck.Content, editAck.EditVersion, editAck.EditedAtMs),
            (editAckActual.RequestId, editAckActual.MessageId, editAckActual.Succeeded, editAckActual.ConversationId,
                editAckActual.Content, editAckActual.EditVersion, editAckActual.EditedAtMs));

        var edited = new MessageEditedUpdate
        {
            MessageId = "msg-1",
            ConversationId = "conv-1",
            SenderUserId = 20001,
            ReceiverUserId = 20002,
            Content = "edited v1",
            EditVersion = 2,
            EditedAtMs = 1_700_000_100_000
        };
        TcpBinaryWireDecode editedResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageEdited,
            Encode(in edited, MessageEditedUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, editedResult.Status);
        var editedActual = Assert.IsType<MessageEditedUpdate>(editedResult.Value);
        Assert.Equal((edited.MessageId, edited.ConversationId, edited.SenderUserId, edited.ReceiverUserId,
            edited.Content, edited.EditVersion, edited.EditedAtMs),
            (editedActual.MessageId, editedActual.ConversationId, editedActual.SenderUserId, editedActual.ReceiverUserId,
                editedActual.Content, editedActual.EditVersion, editedActual.EditedAtMs));

        // Recall: request / ack / event.
        var recallRequest = new MessageRecallRequest { RequestId = "req-recall-1", MessageId = "msg-1" };
        TcpBinaryWireDecode recallRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageRecallRequest,
            Encode(in recallRequest, MessageRecallRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, recallRequestResult.Status);
        var recallRequestActual = Assert.IsType<MessageRecallRequest>(recallRequestResult.Value);
        Assert.Equal((recallRequest.RequestId, recallRequest.MessageId),
            (recallRequestActual.RequestId, recallRequestActual.MessageId));

        var recallAck = new MessageRecallAcknowledgement
        {
            RequestId = "req-recall-1",
            MessageId = "msg-1",
            Succeeded = true,
            ErrorCode = null,
            ErrorMessage = null,
            ConversationId = "conv-1",
            RecalledAtMs = 1_700_000_110_000
        };
        TcpBinaryWireDecode recallAckResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageRecallAck,
            Encode(in recallAck, MessageRecallAcknowledgementSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, recallAckResult.Status);
        var recallAckActual = Assert.IsType<MessageRecallAcknowledgement>(recallAckResult.Value);
        Assert.Equal((recallAck.RequestId, recallAck.MessageId, recallAck.Succeeded, recallAck.ConversationId,
            recallAck.RecalledAtMs),
            (recallAckActual.RequestId, recallAckActual.MessageId, recallAckActual.Succeeded, recallAckActual.ConversationId,
                recallAckActual.RecalledAtMs));

        var recalled = new MessageRecalledUpdate
        {
            MessageId = "msg-1",
            ConversationId = "conv-1",
            SenderUserId = 20001,
            ReceiverUserId = 20002,
            RecalledAtMs = 1_700_000_110_000
        };
        TcpBinaryWireDecode recalledResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageRecalled,
            Encode(in recalled, MessageRecalledUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, recalledResult.Status);
        var recalledActual = Assert.IsType<MessageRecalledUpdate>(recalledResult.Value);
        Assert.Equal((recalled.MessageId, recalled.ConversationId, recalled.SenderUserId, recalled.ReceiverUserId,
            recalled.RecalledAtMs),
            (recalledActual.MessageId, recalledActual.ConversationId, recalledActual.SenderUserId, recalledActual.ReceiverUserId,
                recalledActual.RecalledAtMs));

        // Reaction add: request / ack / event.
        var addRequest = new AddReactionRequest { RequestId = "req-add-1", MessageId = "msg-1", Emoji = "👍" };
        TcpBinaryWireDecode addRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AddReactionRequest,
            Encode(in addRequest, AddReactionRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, addRequestResult.Status);
        var addRequestActual = Assert.IsType<AddReactionRequest>(addRequestResult.Value);
        Assert.Equal((addRequest.RequestId, addRequest.MessageId, addRequest.Emoji),
            (addRequestActual.RequestId, addRequestActual.MessageId, addRequestActual.Emoji));

        var addAck = new AddReactionAcknowledgement
        {
            RequestId = "req-add-1",
            MessageId = "msg-1",
            Succeeded = true,
            ErrorCode = null,
            ErrorMessage = null,
            ConversationId = "conv-1",
            Emoji = "👍",
            OccurredAtMs = 1_700_000_120_000,
            EmojiCount = 3
        };
        TcpBinaryWireDecode addAckResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.AddReactionAck,
            Encode(in addAck, AddReactionAcknowledgementSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, addAckResult.Status);
        var addAckActual = Assert.IsType<AddReactionAcknowledgement>(addAckResult.Value);
        Assert.Equal((addAck.RequestId, addAck.MessageId, addAck.Succeeded, addAck.ConversationId,
            addAck.Emoji, addAck.OccurredAtMs, addAck.EmojiCount),
            (addAckActual.RequestId, addAckActual.MessageId, addAckActual.Succeeded, addAckActual.ConversationId,
                addAckActual.Emoji, addAckActual.OccurredAtMs, addAckActual.EmojiCount));

        var reactionAdded = new ReactionAddedUpdate
        {
            MessageId = "msg-1",
            ConversationId = "conv-1",
            ReactorUserId = 20003,
            MessageSenderUserId = 20001,
            MessageReceiverUserId = 20002,
            Emoji = "👍",
            EmojiCount = 3,
            OccurredAtMs = 1_700_000_120_000
        };
        TcpBinaryWireDecode reactionAddedResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ReactionAdded,
            Encode(in reactionAdded, ReactionAddedUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, reactionAddedResult.Status);
        var reactionAddedActual = Assert.IsType<ReactionAddedUpdate>(reactionAddedResult.Value);
        Assert.Equal((reactionAdded.MessageId, reactionAdded.ConversationId, reactionAdded.ReactorUserId,
            reactionAdded.MessageSenderUserId, reactionAdded.MessageReceiverUserId, reactionAdded.Emoji,
            reactionAdded.EmojiCount, reactionAdded.OccurredAtMs),
            (reactionAddedActual.MessageId, reactionAddedActual.ConversationId, reactionAddedActual.ReactorUserId,
                reactionAddedActual.MessageSenderUserId, reactionAddedActual.MessageReceiverUserId, reactionAddedActual.Emoji,
                reactionAddedActual.EmojiCount, reactionAddedActual.OccurredAtMs));

        // Reaction remove: request / ack / event.
        var removeRequest = new RemoveReactionRequest { RequestId = "req-rm-1", MessageId = "msg-1", Emoji = "👍" };
        TcpBinaryWireDecode removeRequestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RemoveReactionRequest,
            Encode(in removeRequest, RemoveReactionRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, removeRequestResult.Status);
        var removeRequestActual = Assert.IsType<RemoveReactionRequest>(removeRequestResult.Value);
        Assert.Equal((removeRequest.RequestId, removeRequest.MessageId, removeRequest.Emoji),
            (removeRequestActual.RequestId, removeRequestActual.MessageId, removeRequestActual.Emoji));

        var removeAck = new RemoveReactionAcknowledgement
        {
            RequestId = "req-rm-1",
            MessageId = "msg-1",
            Succeeded = true,
            ErrorCode = null,
            ErrorMessage = null,
            ConversationId = "conv-1",
            Emoji = "👍",
            OccurredAtMs = 1_700_000_130_000,
            EmojiCount = 2
        };
        TcpBinaryWireDecode removeAckResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.RemoveReactionAck,
            Encode(in removeAck, RemoveReactionAcknowledgementSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, removeAckResult.Status);
        var removeAckActual = Assert.IsType<RemoveReactionAcknowledgement>(removeAckResult.Value);
        Assert.Equal((removeAck.RequestId, removeAck.MessageId, removeAck.Succeeded, removeAck.ConversationId,
            removeAck.Emoji, removeAck.OccurredAtMs, removeAck.EmojiCount),
            (removeAckActual.RequestId, removeAckActual.MessageId, removeAckActual.Succeeded, removeAckActual.ConversationId,
                removeAckActual.Emoji, removeAckActual.OccurredAtMs, removeAckActual.EmojiCount));

        var reactionRemoved = new ReactionRemovedUpdate
        {
            MessageId = "msg-1",
            ConversationId = "conv-1",
            ReactorUserId = 20003,
            MessageSenderUserId = 20001,
            MessageReceiverUserId = 20002,
            Emoji = "👍",
            EmojiCount = 2,
            OccurredAtMs = 1_700_000_130_000
        };
        TcpBinaryWireDecode reactionRemovedResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ReactionRemoved,
            Encode(in reactionRemoved, ReactionRemovedUpdateSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, reactionRemovedResult.Status);
        var reactionRemovedActual = Assert.IsType<ReactionRemovedUpdate>(reactionRemovedResult.Value);
        Assert.Equal((reactionRemoved.MessageId, reactionRemoved.ConversationId, reactionRemoved.ReactorUserId,
            reactionRemoved.MessageSenderUserId, reactionRemoved.MessageReceiverUserId, reactionRemoved.Emoji,
            reactionRemoved.EmojiCount, reactionRemoved.OccurredAtMs),
            (reactionRemovedActual.MessageId, reactionRemovedActual.ConversationId, reactionRemovedActual.ReactorUserId,
                reactionRemovedActual.MessageSenderUserId, reactionRemovedActual.MessageReceiverUserId, reactionRemovedActual.Emoji,
                reactionRemovedActual.EmojiCount, reactionRemovedActual.OccurredAtMs));

        // Segmented path for one of the new commands.
        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in recalled, MessageRecalledUpdateSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode segmentedResult = TcpBinaryWireCodec.TryDecode(PacketCommand.MessageRecalled, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, segmentedResult.Status);
        Assert.Equal("msg-1", Assert.IsType<MessageRecalledUpdate>(segmentedResult.Value).MessageId);
    }

    [Fact]
    public void EmptyHeartbeatFramesDecodeThroughTheRegistryDispatch()
    {
        TcpBinaryWireDecode spanHeartbeat = TcpBinaryWireCodec.TryDecode(PacketCommand.Heartbeat, [], Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, spanHeartbeat.Status);
        Assert.IsType<Heartbeat>(spanHeartbeat.Value);

        TcpBinaryWireDecode spanAck = TcpBinaryWireCodec.TryDecode(PacketCommand.HeartbeatAcknowledgement, [], Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, spanAck.Status);
        Assert.IsType<HeartbeatAcknowledgement>(spanAck.Value);

        ReadOnlySequence<byte> segmented = Segmented([], 1);
        TcpBinaryWireDecode sequenceHeartbeat = TcpBinaryWireCodec.TryDecode(PacketCommand.Heartbeat, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, sequenceHeartbeat.Status);
        Assert.IsType<Heartbeat>(sequenceHeartbeat.Value);

        // Non-empty keep-alive frames are not legal on the wire and fail closed on both paths.
        TcpBinaryWireDecode nonEmptySpan = TcpBinaryWireCodec.TryDecode(PacketCommand.Heartbeat, new byte[] { 0x00 }, Limits);
        Assert.Equal(TcpBinaryWireStatus.DecodeFailure, nonEmptySpan.Status);
        Assert.Equal(BinaryStatus.TrailingData, nonEmptySpan.DecodeStatus);

        ReadOnlySequence<byte> nonEmptySeg = Segmented([0x00], 1);
        TcpBinaryWireDecode nonEmptySequence = TcpBinaryWireCodec.TryDecode(
            PacketCommand.HeartbeatAcknowledgement,
            in nonEmptySeg,
            Limits);
        Assert.Equal(TcpBinaryWireStatus.DecodeFailure, nonEmptySequence.Status);
        Assert.Equal(BinaryStatus.TrailingData, nonEmptySequence.DecodeStatus);
    }

    [Fact]
    public void UncoveredCommandsFailClosed()
    {
        foreach (PacketCommand command in new[]
                 {
                     PacketCommand.ResumeRequest
                 })
        {
            TcpBinaryWireDecode spanResult = TcpBinaryWireCodec.TryDecode(command, [], Limits);
            Assert.Equal(TcpBinaryWireStatus.SchemaNotCovered, spanResult.Status);
            Assert.Null(spanResult.Value);

            ReadOnlySequence<byte> segmented = Segmented([], 1);
            TcpBinaryWireDecode sequenceResult = TcpBinaryWireCodec.TryDecode(command, in segmented, Limits);
            Assert.Equal(TcpBinaryWireStatus.SchemaNotCovered, sequenceResult.Status);
            Assert.Null(sequenceResult.Value);
        }
    }

    [Fact]
    public void MalformedCoveredPayloadFailsClosedForBothDecoderPaths()
    {
        // Field 1 (varint) followed by a structurally valid but duplicated field for ClientHello.
        byte[] malformed = [0x08, 0x01, 0x08, 0x01];

        TcpBinaryWireDecode spanResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ClientHello,
            malformed.AsSpan(),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.DecodeFailure, spanResult.Status);
        Assert.Equal(BinaryStatus.DuplicateField, spanResult.DecodeStatus);
        Assert.Null(spanResult.Value);

        TcpBinaryWireDecode sequenceResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ClientHello,
            Segmented(malformed, 1),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.DecodeFailure, sequenceResult.Status);
        Assert.Equal(BinaryStatus.DuplicateField, sequenceResult.DecodeStatus);
        Assert.Null(sequenceResult.Value);
    }

    [Fact]
    public void OversizedCoveredPayloadFailsClosed()
    {
        var limited = new BinaryLimits(maxMessageBytes: 64, maxFieldBytes: 8, maxStringBytes: 4, maxByteArrayBytes: 8, maxFields: 8);
        var request = new MessageHistoryRequest { RequestId = "too-long-for-limits" };
        byte[] payload = Encode(in request, MessageHistoryRequestSchema.TryEncode, Limits);

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageHistoryRequest,
            payload.AsSpan(),
            limited);

        Assert.Equal(TcpBinaryWireStatus.DecodeFailure, result.Status);
        Assert.NotEqual(BinaryStatus.Done, result.DecodeStatus);
        Assert.Null(result.Value);
    }

    private delegate BinaryStatus EncodeDelegate<T>(in T value, Span<byte> destination, BinaryLimits limits, out int written);

    private static byte[] Encode<T>(in T value, EncodeDelegate<T> encode, BinaryLimits limits)
    {
        byte[] destination = new byte[limits.MaxMessageBytes];
        Assert.Equal(BinaryStatus.Done, encode(in value, destination, limits, out int written));
        return destination[..written].ToArray();
    }

    private static ReadOnlySequence<byte> Segmented(byte[] payload, int segmentSize) =>
        SequenceFactory.Segmented(payload, segmentSize);
}