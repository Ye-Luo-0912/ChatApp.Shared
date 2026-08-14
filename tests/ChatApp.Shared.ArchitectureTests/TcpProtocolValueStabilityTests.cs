using System.Buffers.Binary;
using ChatApp.Shared.Protocol.Tcp;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

public sealed class TcpProtocolValueStabilityTests
{
    [Fact]
    public void FrameHeaderConstantsMatchGoldenWireLayout()
    {
        Assert.Equal(0x1A2B3C4Du, TcpFrameConstants.MagicNumber);
        Assert.Equal(0, TcpFrameConstants.MagicOffset);
        Assert.Equal(4, TcpFrameConstants.CommandOffset);
        Assert.Equal(6, TcpFrameConstants.LengthOffset);
        Assert.Equal(10, TcpFrameConstants.HeaderSize);
        Assert.Equal((ushort)1, TcpFrameConstants.CurrentProtocolVersion);

        Span<byte> header = stackalloc byte[TcpFrameConstants.HeaderSize];
        BinaryPrimitives.WriteUInt32LittleEndian(
            header[TcpFrameConstants.MagicOffset..],
            TcpFrameConstants.MagicNumber);
        BinaryPrimitives.WriteUInt16LittleEndian(
            header[TcpFrameConstants.CommandOffset..],
            0x1234);
        BinaryPrimitives.WriteInt32LittleEndian(
            header[TcpFrameConstants.LengthOffset..],
            0x12345678);

        Assert.Equal("4D3C2B1A341278563412", Convert.ToHexString(header));
    }

    [Fact]
    public void PacketCommandNamesAndNumbersMatchGoldenContract()
    {
        string actual = string.Join(
            Environment.NewLine,
            Enum.GetValues<PacketCommand>()
                .OrderBy(value => (ushort)value)
                .Select(value => $"{value}={(ushort)value}"));

        string expected = string.Join(
            Environment.NewLine,
            """
            Heartbeat=0
            AuthenticationRequest=1
            AuthenticationResponse=2
            ClientHello=3
            ServerHello=4
            GoAway=5
            ResumeRequest=6
            ResumeResponse=7
            ChatMessage=101
            MessageAcknowledgement=102
            MessageReceipt=103
            MessageReceiptAcknowledgement=104
            MessageReceiptUpdated=105
            MessageHistoryRequest=106
            MessageHistoryPage=107
            ConversationListRequest=108
            ConversationListPage=109
            ConversationMarkReadRequest=110
            ConversationMarkReadResponse=111
            ConversationChanged=112
            UnreadCountChanged=113
            SyncBootstrapRequest=114
            SyncBootstrapResponse=115
            ConversationSetPrefsRequest=116
            ConversationSetPrefsResponse=117
            MessageRecallRequest=118
            MessageRecallAck=119
            MessageRecalled=120
            TypingNotify=121
            TypingUpdate=122
            PresenceQuery=123
            PresenceSnapshot=124
            PresenceChanged=125
            PresenceUnwatch=126
            MessageEditRequest=127
            MessageEditAck=128
            MessageEdited=129
            AddReactionRequest=130
            AddReactionAck=131
            ReactionAdded=132
            RemoveReactionRequest=133
            RemoveReactionAck=134
            ReactionRemoved=135
            CreateGroupRequest=136
            CreateGroupResponse=137
            AddGroupMembersRequest=138
            AddGroupMembersResponse=139
            RemoveGroupMemberRequest=140
            RemoveGroupMemberResponse=141
            LeaveGroupRequest=142
            LeaveGroupResponse=143
            ChangeMemberRoleRequest=144
            ChangeMemberRoleResponse=145
            ListGroupMembersRequest=146
            ListGroupMembersResponse=147
            MemberJoined=148
            MemberLeft=149
            MemberRemoved=150
            RoleChanged=151
            ConversationRead=152
            RelationshipListChanged=153
            AttachmentLifecycleChanged=154
            RegisterPushTokenRequest=155
            RegisterPushTokenResponse=156
            UnregisterPushTokenRequest=157
            UnregisterPushTokenResponse=158
            AttachmentFinalizeRequest=159
            AttachmentFinalizeResponse=160
            RelationshipCommandRequest=161
            RelationshipCommandResponse=162
            RelationshipListRequest=163
            RelationshipListResponse=164
            MembersAddedUpdate=165
            ConversationDissolvedUpdate=166
            AttachmentDownloadAuthorizeRequest=167
            AttachmentDownloadAuthorizeResponse=168
            MessageReadReceiptQueryRequest=169
            MessageReadReceiptQueryResponse=170
            DissolveGroupRequest=171
            DissolveGroupResponse=172
            CallCommandRequest=173
            CallCommandResponse=174
            CallSignal=175
            Error=500
            HeartbeatAcknowledgement=1000
            """.Split('\n').Select(line => line.TrimEnd('\r')));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FeatureAndErrorNumbersMatchGoldenContract()
    {
        Assert.Equal(0u, (uint)GatewayFeature.None);
        Assert.Equal(1u << 0, (uint)GatewayFeature.BinaryPayload);
        Assert.Equal(1u << 1, (uint)GatewayFeature.Compression);
        Assert.Equal(1u << 2, (uint)GatewayFeature.StreamingChat);
        Assert.Equal(1u << 3, (uint)GatewayFeature.CommandCapabilities);
        Assert.Equal(1u << 4, (uint)GatewayFeature.SessionResume);
        Assert.Equal(1u << 5, (uint)GatewayFeature.ConversationSync);
        Assert.Equal(1u << 6, (uint)GatewayFeature.ConversationPreferences);
        Assert.Equal(1u << 7, (uint)GatewayFeature.MessageMutation);
        Assert.Equal(1u << 8, (uint)GatewayFeature.PresenceAndTyping);
        Assert.Equal(1u << 9, (uint)GatewayFeature.MessageReactions);
        Assert.Equal(1u << 10, (uint)GatewayFeature.GroupManagement);
        Assert.Equal(1u << 11, (uint)GatewayFeature.PushTokenManagement);
        Assert.Equal(1u << 12, (uint)GatewayFeature.CallSignaling);

        Assert.Equal((ushort)0, (ushort)ProtocolErrorCode.None);
        Assert.Equal((ushort)1, (ushort)ProtocolErrorCode.ProtocolViolation);
        Assert.Equal((ushort)2, (ushort)ProtocolErrorCode.UnsupportedCommand);
        Assert.Equal((ushort)3, (ushort)ProtocolErrorCode.UnsupportedVersion);
        Assert.Equal((ushort)4, (ushort)ProtocolErrorCode.InvalidPayload);
        Assert.Equal((ushort)10, (ushort)ProtocolErrorCode.AuthRequired);
        Assert.Equal((ushort)11, (ushort)ProtocolErrorCode.AuthRejected);
        Assert.Equal((ushort)12, (ushort)ProtocolErrorCode.SessionRevoked);
        Assert.Equal((ushort)13, (ushort)ProtocolErrorCode.ResumeFailed);
        Assert.Equal((ushort)14, (ushort)ProtocolErrorCode.DependencyUnavailable);
        Assert.Equal((ushort)15, (ushort)ProtocolErrorCode.AccountSuspended);
        Assert.Equal((ushort)20, (ushort)ProtocolErrorCode.RateLimited);
        Assert.Equal((ushort)21, (ushort)ProtocolErrorCode.PayloadTooLarge);
        Assert.Equal((ushort)22, (ushort)ProtocolErrorCode.FeatureNotNegotiated);
        Assert.Equal((ushort)30, (ushort)ProtocolErrorCode.ServerOverloaded);
        Assert.Equal((ushort)31, (ushort)ProtocolErrorCode.Shutdown);
        Assert.Equal((ushort)32, (ushort)ProtocolErrorCode.OutboundQueueFull);
        Assert.Equal((ushort)99, (ushort)ProtocolErrorCode.InternalError);
    }
}
