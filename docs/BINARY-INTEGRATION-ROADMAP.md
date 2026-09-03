# 未覆盖命令优先级排序与补全计划

> 关联：[BINARY-PROTOCOL.md](BINARY-PROTOCOL.md) 与 [NEXT-STAGE.md](NEXT-STAGE.md) 的 `BIN-INTEGRATION-3`。
> 数字以 `PacketCommand` 枚举为准：总命令值 **85** = 寄存器已覆盖 **11** + 未覆盖 **74**。
> `ResumeRequest`（enum=6）经网关注释确认**非真实 wire 命令**（Resume 走 `ClientHello.ResumeToken`，`GetMaxPayload=-1`），不入任何批次，寄存器永不覆盖——故本计划覆盖全部可传输命令后，寄存器理论最大覆盖为 **84/85**。
> 本计划是"按真实业务价值逐批补齐"的执行顺序；在命令目录完整之前，连接级保持 JSON，不做双 codec 切换。

## 排序原则

批次的先后由四个维度综合决定，越靠前越应先补：

1. **协议健全／保活**：连接级一次握手后固定一种格式，缺了这些命令，二进制连接在长时间空闲或异常时无法维持/恢复。
2. **业务体量与收益**：体积越大、频率越高、编码复杂（嵌套/列表）的命令，JSON→binary 收益越明显，越值得先做短测基准。
3. **主链路前置**：已闭环的关系、语音消息、通话主链路用到的命令优先，避免滚动开发时二进制与功能并行冲突。
4. **依赖下游**：被其它 schema 引用的底层 DTO（如 `TcpAttachmentRef` 的语音字段、消息回执状态）先冻结，避免反工。

## 批次概览

累计覆盖 = 寄存器已覆盖 **16**（截至 P1，含 P0 的 4 个认证/心跳命令）+ 截至该批已补的命令数（理论最大 84/85，`ResumeRequest` 非 wire 命令除外）。

| 批次 | 主题 | 命令数 | 累计覆盖 |
|------|------|-------:|--------:|
| P0 | 保活与认证 | 4 | 11/85 ✅ |
| P1 | 消息读写信道 | 5 | 16/85 ✅ |
| P2 | 语音/附件/通话 | 8 | 24/85 ✅ |
| P3 | 消息事务（编辑/撤回/反应） | 12 | 36/85 ✅ |
| P4 | 会话列表与同步 + 已读回执 | 13 | 49/85 ✅ |
| P5 | 关系主链路 | 5 | 54/85 ✅ |
| P6 | 输入状态 / 在线 / 推送 | 10 | 64/85 ✅ |
| P7 | 群组 | 20 | 84/85 ✅ |

每批补 schema 时都必须满足 BIN-SCHEMA-2 验收：普通源码 encoder、generated decoder、golden/hash、分段 round-trip、malformed/limits 覆盖；反工时不修改已冻结字段号。

---

## P0：保活与认证（4 命令）— ✅ 已落地（2026-08-19）

**命令**：`Heartbeat` / `HeartbeatAcknowledgement`（空载荷帧）；`AuthenticationRequest` / `AuthenticationResponse`

**理由**：连接级固定格式后，两侧在空闲期用 Heartbeat 保活，连接建立用 Authentication 鉴权。它们决定二进制连接能否**维持与恢复**，是最小的连接级健全集。心跳两帧在 wire 上是空载荷，按"空帧"在寄存器中显式处理（非空载荷 fail-closed 为 `TrailingData`），不需要零字段 schema。

**前置**：`ServerHello/PayloadFormat` 协商语义稳定（已离线验证）；认证 canonical 契约已入 Shared `ChatApp.Protocol.Tcp`。
**验收**：心跳帧仅空载荷，非空即 `TrailingData` 失败；Authentication 凭证字段按字段缺失表示 null（`DeviceIdHash`/`ErrorMessage` 等），与 JSON 侧字段语义一致。`ResumeRequest` 非真实 wire 命令，不注册。
**落地**：canonical DTO 入 `ConnectionAuthContracts.cs`；字段号与 encoder/generated decoder 入 `CoreCommandSchema.cs`；仅空帧心跳 fail-closed 注册入 `TcpBinaryWireCodec`。回归 41→43 通过，Release 构建 0 警告 0 错误。

---

## P1：消息读写信道（5 命令）— ✅ 已落地（2026-08-19）

**命令**：`ChatMessage` / `MessageAcknowledgement` / `MessageReceipt` / `MessageReceiptAcknowledgement` / `MessageReceiptUpdated`

**理由**：`ChatMessage` 是频率最高、载荷最大的命令，含附件引用与嵌套，JSON→binary 体积与 CPU 收益最显著，也是 80/320/640 msg/s 短测的基准载荷。语音消息（`Voice`）字段已通过 `TcpAttachmentRef` 冻结，可直接复用。

**前置**：`TcpAttachmentRef`/`MessageReactionSummary` 生产 schema 已就绪。
**验收**：消息含/不含附件两种形态、正文 UTF-8、时间戳、回执状态机（pending→delivered→read）全覆盖；漏投/重复计数纳入短测验收项。
**落地**：canonical DTO 入 `MessageChannelContracts.cs`（时间戳用 Unix ms）；字段号与 encoder/generated decoder 入 `CoreCommandSchema.cs`；`ChatMessage` 复用 `TcpAttachmentRef` 嵌套 + repeated 字符串附件 Id/提及；寄存器共 11→16 命令。协议测试 43/43 + 纯二进制测试通过，Release 构建 0 警告 0 错误。

## P2：语音/附件/通话（8 命令）— ✅ 已落地（2026-08-19）

**命令**：`AttachmentLifecycleChanged` / `AttachmentFinalizeRequest` / `AttachmentFinalizeResponse` / `AttachmentDownloadAuthorizeRequest` / `AttachmentDownloadAuthorizeResponse`；`CallCommandRequest` / `CallCommandResponse` / `CallSignal`

**理由**：语音消息（VOICE-MSG）与通话（CALL-E2E）主链路已闭环，把已稳定面先接入二进制滚动冲突最小。语音正文经附件引用（`TcpAttachmentRef` 已冻结），附件最终化/下载授权是语音内容的传输前置；通话命令体量小但属主链路。

**前置**：附件可用性校验与绑定规则当前约束已齐；通话状态机已在 Realtime 侧验证。
**验收**：附件 Available/Confirmed 绑定状态与错误码（`AttachmentBindErrorCode` 语义）映射进 schema，授权 token 按缺失表示空；通话 command/signal 的幂等与 revision 语义与 JSON 侧一致。
**落地**：canonical 附件 DTO 入 `AttachmentCommandContracts.cs`（附件 Id/状态/时间戳/token），通话复用 `CallCommandContracts.cs` 既有 DTO 与 `TcpCallGrant` 嵌套；字段号与 encoder/generated decoder 入 `CoreCommandSchema.cs`（`TcpCallCommandRequest`/`TcpCallCommandResponse` 含 nested `TcpCallGrant`/`TcpCallSignal`）；寄存器共 16→24 命令。协议二进制测试 44/44 + 纯二进制测试通过，Release 构建 0 警告 0 错误。

## P3：消息事务（12 命令）— ✅ 已落地（2026-08-19）

**命令**：`MessageEditRequest`/`MessageEditAck`/`MessageEdited`；`MessageRecallRequest`/`MessageRecallAck`/`MessageRecalled`；`AddReactionRequest`/`AddReactionAck`/`ReactionAdded`；`RemoveReactionRequest`/`RemoveReactionAck`/`ReactionRemoved`

**理由**：编辑/撤回/反应属于消息面较高频但载荷小；补全后消息侧命令基本闭环，可作为 640 msg/s 高吞吐 mix 的组成部分。

**前置**：`MessageReactionSummary` 生产 schema 已就绪；`ChatMessage`（P1）先定稿字段基线。
**验收**：命令体为 request/ack/event 三段式，幂等与乱序行为明确；repeated 反应字段用 unpacked 连续 occurrence（已冻结规则）。
**落地**：canonical DTO 入 `MessageTransactionContracts.cs`（request/ack/event 三段式，nullable 编辑版本/时间戳/收藏数按字段缺失表示空）；字段号与 encoder/generated decoder 入 `CoreCommandSchema.cs`；寄存器共 24→36 命令。协议二进制测试 45/45 + 全量回归通过，Release 构建 0 警告 0 错误。

## P4：会话列表与同步 + 已读回执（13 命令）— ✅ 已落地（2026-08-20）

**命令**：`ConversationListRequest`/`ConversationListPage`/`ConversationMarkReadRequest`/`ConversationMarkReadResponse`/`ConversationChanged`/`UnreadCountChanged`/`SyncBootstrapRequest`/`SyncBootstrapResponse`/`ConversationSetPrefsRequest`/`ConversationSetPrefsResponse`/`ConversationRead`；`MessageReadReceiptQueryRequest`/`MessageReadReceiptQueryResponse`

**理由**：首屏会话列表与增量同步（`SyncBootstrapResponse` 已在测试消费者侧有 schema，提升到生产寄存器成本最低）、已读水位，是客户端最高频的"批量读"路径。

**前置**：`MessageHistoryPage`/`MessageHistoryResponse` 覆盖为先例，栈已通。
**验收**：列表分页 + 双层 nested（conversation→lastMessage）、cursor/watermark 语义，与 history 双向 spot-round-trip。
**落地**：canonical DTO 入 `ConversationCommandContracts.cs`（列表分页/标记已读/偏好/已读回执查询，复用 `SyncBootstrapContracts` 的 `TcpConversationListItem`/`TcpConversationListCursor`/`ConversationSyncWatermark` 与 `MessageHistoryContracts` 的 `MessageHistoryItem`）；字段号与 encoder/generated decoder 入 `CoreCommandSchema.cs`（`TcpConversationListItemSchema`/`TcpConversationListCursorSchema`/`SyncBootstrapResponseSchema` 等 nested 子类型，repeated 用 unpacked 连续 occurrence）；寄存器共 36→49 命令。新增 6 条寄存器 round-trip 测试（列表对、mark-read 对、会话事件、偏好对、已读回执对、bootstrap 对），寄存器测试 51/51 通过，Release 构建 0 警告 0 错误。

## P5：关系主链路（5 命令）— ✅ 已落地（2026-08-20）

**命令**：`RelationshipListChanged`/`RelationshipCommandRequest`/`RelationshipCommandResponse`/`RelationshipListRequest`/`RelationshipListResponse`

**理由**：关系读取（REL-E2E）主链路已闭环，此批将已稳定验证的关系 DTO 接入二进制，滚动冲突最小。`RelationshipListResponse` 含 friend/request/blacklist 三个子列表，是复杂 repeated-nested 载荷基准。

**前置**：REL-E2E 回归在 JSON 路径持续全绿。
**验收**：三列表分页、status 一致性、pagination cursor 全覆盖；增量/重置/离线恢复语义与 JSON 侧一致。
**落地**：canonical 命令/事件 DTO 入 `RelationshipCommandContracts.cs`（`TcpRelationshipCommandRequest`/`TcpRelationshipCommandResponse`/`TcpRelationshipListChangedUpdate` + `TcpRelationshipOperation`，数值与 Realtime `RelationshipOperation` 一致），复用既有 `TcpRelationshipListRequest`/`TcpRelationshipListResponse`/`TcpRelationshipListItem`；字段号与 encoder/generated decoder 入 `CoreCommandSchema.cs`（含 `TcpRelationshipListItemSchema` nested repeated）；寄存器共 49→54 命令。新增 3 条寄存器 round-trip 测试（列表对、命令对、列表变更事件），寄存器测试 54/54 通过，Release 构建 0 警告 0 错误。

## P6：输入状态 / 在线 / 推送（10 命令）— ✅ 已落地（2026-08-20）

**命令**：`TypingNotify`/`TypingUpdate`；`PresenceQuery`/`PresenceSnapshot`/`PresenceChanged`/`PresenceUnwatch`；`RegisterPushTokenRequest`/`RegisterPushTokenResponse`/`UnregisterPushTokenRequest`/`UnregisterPushTokenResponse`

**理由**：实时状态提示与推送注册，频次高但载荷极小，JSON 已足够快；binary 收益有限，放后。

**前置**：暂无。
**验收**：静态字段+无嵌套，golden 与 malformed 覆盖即可。
**落地**：canonical DTO 入 `RealtimeStatusContracts.cs`（Typing 2/Presence 4/Push 4 共 10 个 DTO + `TcpPushPlatform` 枚举，Payload 镜像客户端 `Core.Models.DTO` 形状）；字段号与 encoder/generated decoder 入 `CoreCommandSchema.cs`（repeated int64 用 `[TcpBinaryRepeatedField]` + `TryAddCollectionElement`/`WriteRepeatedInt64`，PresenceSnapshot 的 Item 列表用 nested repeated）；寄存器共 54→64 命令。新增 3 条寄存器 round-trip 测试（Typing 对、Presence 组、Push 对，覆盖 Span 与分段双路径），寄存器测试 57/57 通过，Release 构建 0 警告 0 错误。

## P7：群组（20 命令）

**命令**：`CreateGroupRequest`/`CreateGroupResponse`/`AddGroupMembersRequest`/`AddGroupMembersResponse`/`RemoveGroupMemberRequest`/`RemoveGroupMemberResponse`/`LeaveGroupRequest`/`LeaveGroupResponse`/`ChangeMemberRoleRequest`/`ChangeMemberRoleResponse`/`ListGroupMembersRequest`/`ListGroupMembersResponse`/`MemberJoined`/`MemberLeft`/`MemberRemoved`/`RoleChanged`/`MembersAddedUpdate`/`ConversationDissolvedUpdate`/`DissolveGroupRequest`/`DissolveGroupResponse`

**理由**：当前无群组主链路强制推进需求，属功能完整补齐，收益最低，最后批。

**前置**：成员/角色/解散语义未来由功能主链路定稿后再冻结更稳。
**验收**：request/response/update 多形态字段，repeated 成员 id 与角色枚举全覆盖。
**落地**：canonical 群组契约入 `GroupCommandContracts.cs`（19 个 DTO + 嵌套 `TcpConversationMemberItem` + `TcpGroupMemberRole` 枚举，Payload 镜像客户端 `Core.Models.DTO.GroupConversationDtos` 形状）；字段号与 encoder/generated decoder 入 `CoreCommandSchema.cs`（repeated int64 用 `[TcpBinaryRepeatedField]` + `TryAddCollectionElement`/`WriteRepeatedInt64`，成员列表用 nested repeated `WriteNested<TcpConversationMemberItemSchemaEncoder,...>`，`TcpAddGroupMembersRequest.MemberUserIds`/`TcpMembersAddedUpdate.AddedUserIds` 复用 repeated int64，角色枚举以 UInt32 写 byte，`RoleChangedUpdate.PreviousRole` 可空枚举按存在性写入）；寄存器共 64→84 命令（覆盖 20/20，唯一余量 `ResumeRequest` 未覆盖）。新增 1 条寄存器 round-trip 测试覆盖全部 20 命令（Span 与分段双路径、嵌套成员列表、repeated 角色枚举、可空枚举、分页游标），寄存器测试 58/58 通过，Architecture 110/110 通过，Release 构建 0 警告 0 错误。

---

## 关键路径（最短解除门控的批次链）

解除 `BIN-INTEGRATION-3` 第一个硬门控（"实际运行命令目录完整"）的最短路径是：

**P0（保活/认证）→ P1（消息读写信道）→ P4（会话同步）→ P5（关系）→ P2（语音/附件/通话）**

这条链覆盖"连接可维持 + 消息读写信道 + 会话/同步 + 关系 + 语音/通话"，足以支撑一次真实链路级的 5–20 分钟短测；`P3/P6/P7` 可在短测期间并行补齐，无需阻塞第一轮收益验证。

## 每批完成即更新

1. 把该批命令从 [BINARY-PROTOCOL.md](BINARY-PROTOCOL.md) 的"未覆盖命令明确清单"移入寄存器覆盖统计（11→…→84；理论最大 84/85）。
2. 该批 schema 计入共享全量回归，跑 `dotnet build ChatApp.Shared.slnx -c Release` 0 警告 0 错误。
3. 更新本计划"批次概览"的累计覆盖列。

## 计数核对

原始未覆盖 78 个（含 `ResumeRequest`）；经核实 `ResumeRequest` 非真实 wire 命令，已从批次中剔除，故本计划覆盖 **77 个可传输命令**（P0 已落地 4 个）。命令全集不含 `ResumeRequest` 的传输语义，其他命令无遗漏：

| 原始分组（数量） | 归入批次 |
|------------------|----------|
| 心跳 2 / 认证 2（`AuthenticationRequest`/`AuthenticationResponse`） | P0（4） |
| ~~认证握手续传 3~~ → 已剔除 `ResumeRequest`（非 wire 命令） | — |
| 消息正文 17 | P1（5）+ P3（12） |
| 附件 5 / 通话 3 | P2（8） |
| 会话 11 / 已读回执 2 | P4（13） |
| 关系 5 | P5（5） |
| 输入状态 2 / 在线 4 / 推送 4 | P6（10） |
| 群组 20 | P7（20） |

合计（可传输命令）4+5+8+12+13+5+10+20 = **77**；寄存器理论最大覆盖 = 7 + 77 = **84/85**（余下 `ResumeRequest` 非 wire 命令，永不覆盖）。