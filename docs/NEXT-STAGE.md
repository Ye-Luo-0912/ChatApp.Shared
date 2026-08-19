# 下一阶段与接手状态

## 职责与优先级

Shared 只拥有跨进程、可版本化且至少有两个真实消费者的契约；实现、连接和运行策略留在各业务仓库。

当前优先支持端到端产品功能：关系读取已完成契约收口，下一步是语音消息与通话控制面。二进制先把当前已展开的 schema 批次收成可验证状态，随后转为支撑轨，不要求在功能开发前覆盖所有命令。

## 当前工作：收口已展开的 Binary schema 批次

1. 保持已经冻结的 nested/list 规则：nested 使用 length-delimited，repeated 使用同字段号连续 unpacked occurrences；depth、元素数、materialized bytes 和 nested body 都有硬预算。
2. 完成当前工作树已经涉及的控制帧、会话列表、cursor reset、`MessageHistoryResponse` 与 `SyncBootstrapResponse` schema、普通源码 encoder、generated decoder、golden、分段输入和恶意输入测试。
3. 核对所有新 schema 只引用 canonical TCP DTO，没有复制 Gateway/Client/Realtime 类型；失败 status、required/presence、时间单位和 reserved field number 必须明确。
4. 完成后停止无边界扩张。剩余握手后目录按真实功能需要逐批补齐，完整目录与双端接入归入后续 `BIN-INTEGRATION-3`，不阻塞关系、语音或通话开发。

> 状态（2026-08-19）：**已收口**。当前展开批次完成测试级收口，Shared 全量回归全绿
> （Binary.Core 37 / EncoderOnly 1 / Protocol.Tcp.Binary 39 / Generator 15 / Architecture 110），
> `dotnet build -c Release` 0 警告 0 错误。生产 Schemas 程序集 + `TcpBinaryWireCodec` 寄存器本批
> 收口 7 个命令（`ClientHello/ServerHello/GoAway/ResumeResponse/ProtocolErrorFrame/MessageHistoryRequest/
> MessageHistoryPage`，其中 `MessageHistoryPage` 复用 `MessageHistoryResponse` schema 及其嵌套
> `MessageHistoryItem/MessageHistoryCursor/TcpAttachmentRef/MessageReactionSummary` 生产 schema），
> 对未覆盖命令与畸形/超限 payload 一律 fail-closed，不构成半套运行入口；会话列表与 `SyncBootstrapResponse`
> 等响应 schema 按钱包设计留在测试消费者侧离线验证（字段号由消费者侧持有，见 BINARY-PROTOCOL.md），不进寄存器。
> 未覆盖命令已给出按功能分组的明确清单（认证/心跳/消息正文/历史会话/在线/群组/关系/附件/推送/回执/通话），
> 详见 [BINARY-PROTOCOL.md](BINARY-PROTOCOL.md)。`ClientHello/ServerHello` 仍只在离线证据中验证，
> 生产握手继续 JSON。

完成标准：当前 diff 内只有一套 Core/Generator/schema 语义，Release/架构/golden/fuzz 全绿；生产格式仍为 JSON，未完成的命令目录有明确清单而不是半套运行入口。

## 当前 P0：`REL-E2E-4` 契约支持

关系 list/catch-up/reset、opaque cursor/watermark、错误与预算已经由 Shared 提供并完成双端 JSON 验证。Shared 只处理 Gateway/Client 真实联调复现出的语义缺口，不再新增关系内部字段，也不把 Realtime checkpoint、数据库 id 或审计信息放进外部 wire。

## 下一阶段功能契约

### P1：`VOICE-MSG-2` 语音消息

1. 从 Server、Realtime 和 Client 已有模型取交集，扩展统一的附件/消息 schema：codec、container、MIME、duration、sample rate、channels、size 与可选 waveform。
2. 为字段固定单位、null/default、最大长度/数量、未知值和版本演进；只表达稳定业务元数据，不包含本地路径、对象存储密钥、下载票或音频正文。
3. 覆盖 JSON golden、old/new optional field、未知 enum、超限/畸形输入，以及 Gateway mapper 与 Client consumer fixture；不得为二进制另建第二套语音 DTO。
4. 附件状态继续使用现有 Available/Scanning/Rejected/Expired 等语义；若业务需要新错误，先由 Server/Realtime 固定再进入 Shared。

**Shared 契约部分已完成（0.5.3）**：`TcpAttachmentRef` 增加 6 个有界语音字段
（`IsVoice`/`VoiceCodec`/`VoiceContainer`/`VoiceDurationMs`/`VoiceSampleRateHz`/`VoiceChannels`），
JSON golden/兼容/往返/畸形测试与二进制 fixture 已同步并全绿；二进制 field 10–15 已预留。
Gateway 侧 `HistoryWireMapper.MapAttachments` 已完成语音字段映射并新增映射测试。
剩余跨仓库工作：Client consumer fixture 与 Server/Client 端到端联调。

完成标准：Server/Realtime/Gateway/Client 使用同一语音元数据含义，文本和普通附件兼容不变，语音正文不进入 Shared payload。

### P1：`CALL-E2E-2` 通话控制面

1. 在 Server call grant 与 Realtime 状态机语义稳定后，定义 invite/ringing/accept/reject/cancel/end/reconnect、结果和稳定错误。
2. 字段至少包括 call id、command id、actor/participants、revision、issued/expire time、capability 与有界 SDP/ICE envelope；明确幂等、乱序、过期和未知命令行为。
3. Shared 只拥有 capability、DTO、错误和预算，不拥有 WebRTC、ICE/TURN/SRTP、UDP/QUIC socket、Redis 状态、NATS subject 或 SFU 策略。
4. 增加 Gateway producer/Client consumer 交叉 fixture 和 truncation/oversize/fuzz，确保旧客户端可忽略未启用 capability。

完成标准：外部 wire 能完整表达 Realtime 已验证的状态机而不泄漏内部实现，媒体数据始终留在独立媒体面。

### P1：`ENDPOINT-TLS-1` 端点安全契约

补齐 scheme、host、port、SNI/target host 与最低 TLS policy，并覆盖旧 endpoint 默认值和不安全组合拒绝。证书链、pinning、开发例外和平台 API 留给消费者。

## 支撑项：`BIN-INTEGRATION-3`

功能命令目录稳定后，再由 Shared 补齐对应 schema，Gateway/Client 接入同一 encoder/decoder。`ClientHello/ServerHello` 和首版 Resume 保持 JSON；协商后 session 固定 exact format，混合连接按格式共享编码。只做 5–20 分钟正确性与收益短测，收益不足时继续使用 JSON。

> 状态（2026-08-19）：双端接入前的共享契约层与寄存器已完成——
> 新增生产 schema 程序集 `ChatApp.Protocol.Tcp.Binary.Schemas`（`src/` 内第 7 个契约包，
> 覆盖 `ClientHello/ServerHello/GoAway/ResumeResponse/ProtocolErrorFrame/MessageHistoryRequest/
> MessageHistoryCursor` 真实字段号 + 手写 encoder + generator 生成 decoder，并已纳入 History response：
> `MessageHistoryResponse` 及其嵌套 `MessageHistoryItem/TcpAttachmentRef/MessageReactionSummary` 生产 schema）
> + 命令→schema 寄存器 `TcpBinaryWireCodec`（已纳入 `MessageHistoryPage`→`MessageHistoryResponse`；
> 未覆盖命令、畸形/超限一律 fail-closed）。新增 `TcpBinaryWireCodecTests` 8 项；Shared 全量回归
> Binary.Core 37 / EncoderOnly 1 / Protocol.Tcp.Binary 39 / Generator 15 / Architecture 110 通过，
> `dotnet build -c Release` 0 警告 0 错误。`ClientHello/ServerHello` 仅在离线证据中验证，生产握手继续 JSON。
>
> **门控现状**：连接级双 codec 切换受硬条件门控，当前**未达门槛，保持 JSON**，不得开启切换——
> - **[未满足] 实际运行命令目录完整**：寄存器仅覆盖 7/85 命令值（`ClientHello/ServerHello/GoAway/
>   ResumeResponse/ProtocolErrorFrame/MessageHistoryRequest/MessageHistoryPage/Error`），主链路实际用到的
>   `ChatMessage/Heartbeat`、关系、通话等大量命令仍无 schema；帧头无逐帧格式位，残缺目录下无法安全启用。
> - **[已满足] 主链路前置**：关系/语音消息/通话主链路均已关闭，不再阻塞此支撑项。
> - **[未满足] 收益与稳定性证据**：缺 80/320/640 msg/s 5–20 分钟短测；须证明稳定收益、零漏投/重复、p99 无不可解释回退。
> - **[未满足] 双端实现与验收**：双 codec 接入、协商后固定格式、fanout 分组共享、JSON fallback、GoAway/重连、混合 fanout 与短测均未开始。
>
> 完整未覆盖命令按功能分组的可度量清单（含各组命令数与总体 7 覆盖/78 未覆盖统计）见
> [`BINARY-PROTOCOL.md`](BINARY-PROTOCOL.md)。`ClientHello/ServerHello` 仅在离线证据中验证，生产握手继续 JSON。
> 实际双 codec 接入、混合格式 fanout 与 5–20 分钟短测仍待命令目录与收益证据达标后推进。

详细 wire、内存与 pointer 约束唯一维护在 [`BINARY-PROTOCOL.md`](BINARY-PROTOCOL.md)，不要复制到业务路线。

## 接手约束

- 先确认至少两个真实消费者和字段所有者，再增加 Shared 类型；不从数据库表或某一端私有 DTO 反推公共协议。
- 修改代码前理解现有语义、兼容窗口和 buffer 所有权。保持少约束但明确上限、生命周期和失败行为。
- 优先普通源码、source generation 和显式 mapper；禁止运行时反射、动态 schema registry、网络/DI/池进入 Shared。

## 验证顺序

聚焦 golden/架构/恶意输入测试 → Release 构建 → 消费者 5–20 分钟跨进程联调。当前阶段到功能联调验收为止。
