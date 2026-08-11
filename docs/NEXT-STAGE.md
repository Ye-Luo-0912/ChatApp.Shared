# 下一阶段与接手状态

## 职责

Shared 只拥有跨进程、可版本化且至少有两个真实消费者的契约；实现、连接和运行策略留在各业务仓库。

## 下一步执行与交接

Shared 有两个顺序明确的接手批次，不得合并发布：

1. **`PROTO-FEED-1`（代码与测试已收口，feed 发布已解锁）。** 帧级畸形/超限 fuzz 与旧附件/关系字段 old-new fixture 已落地并通过（`TcpProtocolFrameFuzzTests.cs`，ArchitectureTests `68/68` 通过，Release 0 warning/error）。包确定性核查结论是 `.nupkg` SHA-256 由随机 `core-properties/*.psmdcp` 文件名与随机关系 `Id` 所致；版本策略已决策为**确定性归一化后重记录**（`tools/Normalize-NupkgDeterministic.ps1` 固定 psmdcp 文件名/Id/时间戳/排序与压缩级别，CI 已接入），连续两次独立 pack+归一化逐包 hash 一致。六包 `0.4.1` 确定字节 SHA-256 已重记录于迁移文档，可复现。把不可变 feed、包清单和 locked-restore 指令交给 Gateway/Client。
2. **`REL-WIRE-2`（schema 已收口，包/hash 与双端接入待下批）。** `REL-GATE-1` 已于 2026-08-11 通过（manifest、两轮 reconcile report、故障矩阵和稳定错误码清单齐备，见 RealtimeServices `docs/NEXT-STAGE.md`）。关系只读 list wire 已定义并在 Shared 收口：`TcpRelationshipListRequest/Response/Item` 与稳定错误码落在 `ChatApp.Protocol.Tcp`（`RelationshipListContracts.cs`），JSON metadata 已注册，golden/old-new 兼容矩阵/预算/畸形游标测试 `14/14` 通过（Architecture `82/82`）。字段/预算（PageSize 1–200、单响应 ≤ 80 KiB、ResourceId/Status/Message 上限）/opaque 游标/reset 语义表已固化在契约头部注释。剩余：打包新版本并记录 hash，Producer(Gateway)→Consumer(Client) 双端编译消费与 JSON 短联调，随后才允许开 capability。
3. **交付 Gateway/Client。** 发布新包版本和 SHA-256，附 reserved field 清单、兼容窗口、部署顺序与回滚顺序；Gateway/Client 只消费包，不复制源码或重新声明 DTO。Realtime 内部 snapshot/checkpoint/hash 不进入外部 wire。
4. **暂不并线。** `chatapp-tagged-v1` 可继续做离线 schema/基准，但生产协商保持关闭；关系首轮、二进制和媒体契约不得同批发布。

如果上游门禁证据缺失，下一位 Agent 只完成 `PROTO-FEED-1`，然后停在 `REL-WIRE-2` 前，不根据现有内部 DTO 猜客户端契约。

## 接手状态

- P0（已收口）：`ChatApp.Protocol.Tcp 0.4.1` 已成为 Client↔Gateway 历史、同步、附件和会话水位的唯一 wire schema；Gateway/Client 已升级同一不可变包并删除本地同义 DTO，`ConversationId`、`ClientMessageId`、`ChangedAtMs`、reset、游标方向和 null 语义由 Shared golden 固定。
- P0（兼容矩阵已完成第一版，发布仍是 TODO）：Shared 已覆盖 old-reader/new-writer、new-reader/old-writer、未知可选字段、未知枚举、Unix 毫秒、双向游标与截断输入；Gateway producer 和 Client consumer 各有 `8/8` 交叉 fixture。六个 nupkg、四份 fixture 和本地消费端二进制 SHA-256 已记录在迁移文档。
  - 发布前帧级超限/畸形输入 fuzz 与旧附件/关系字段组合已补齐并通过（见「下一步执行与交接」`PROTO-FEED-1`）；包确定性核查结论是 `.nupkg` 由随机 `core-properties/*.psmdcp` 文件名与随机关系 `Id` 导致不可复现。**版本策略已决策为确定性归一化后重记录**：`tools/Normalize-NupkgDeterministic.ps1` 固定 psmdcp 文件名/Id/时间戳/排序与压缩级别，CI 已接入；六包 `0.4.1` 确定字节 hash 已重记录，可复现。`0.4.1` 版本不变，入选作为权威发布工件。
  - feed 发布完成后让 Gateway/Client 仅从 feed 做一次 locked restore + 短时 TCP 联调；失败回滚到上一不可变包，不改 SQLite 数据，也不恢复本地重复 DTO。
  - breaking wire 变更必须使用新协议/包主版本和明确升级顺序，不能靠 namespace 或反序列化猜测兼容。
- P0（边界已收口）：Gateway 通过显式 mapper 把 Realtime 的附件、Reaction、会话和关系投影转换成 TCP wire；Shared 不直接引用 Realtime 包，也不把数据库/事件所有权搬进 wire 层。后续新增嵌套类型仍须记录 canonical owner 与映射差异。
- P0（关系只读 wire schema 已在 Shared 收口，生产 capability 仍关闭）：Server→Realtime 的 `RelationshipProjectionDelta`、流级 snapshot/checkpoint 与 `RelationshipProjectionStreamDigest` 属于内部投影契约，不复制进 `ChatApp.Protocol.Tcp`。快照导出/原子导入、数据库时钟租约扫描、持久化 cursor、两轮稳定判定和 count/hash 单流自修复已经具备；Realtime 内部 snapshot-gated list processor 与不暴露列表内容的 status/streams/reconcile 视图也已实现，但生产开关仍关闭。隔离环境必须用 Realtime 的 reconcile gate 工具完成两轮有界分页、状态不变与全量指纹校验，并在故障恢复后再次通过；在开放能力之前，Shared 已按真实 Server/Client 语义定义并收口 Client list 只读 DTO（`TcpRelationshipListRequest/Response/Item`、稳定错误码、预算与 opaque 游标/reset 语义，见 `RelationshipListContracts.cs`），但这些 DTO 尚未打包发布、也未经 Gateway/Client 双端编译消费与 JSON 短联调，因此不为未验证投影开放 capability。
  - ✅ 字段清单（list type、稳定资源键、version/watermark、页面方向、partial/reset、unavailable/version-changed/gap、null 与未知枚举）已按真实 Server/Client 语义写出并固化于 `RelationshipListContracts.cs` 头部注释；old/new golden、最大列表/字符串/响应字节预算（PageSize 1–200、单响应 ≤ 80 KiB）与畸形游标行为已由 `TcpRelationshipListContractTests` 覆盖。opaque cursor 只承诺继续或明确失效，不暴露 Realtime 内部编码；inbox、snapshot checkpoint、对账 hash、actor 审计和数据库行形状均不得进入客户端 wire。剩余：首版只读包须先由 Gateway/Client 双端编译消费并完成 JSON 短联调，才允许发布 capability。
  - 首次发布只开放读取能力位，mutation 仍由 Server HTTP 拥有；回滚关闭 capability 并让新连接恢复 HTTP 路径，不删除客户端已有关系投影。
- P1：补 endpoint scheme/SNI/TLS policy 的明确契约，不把证书验证实现放进 Shared。
  - 契约只表达 scheme、host、port、SNI/target host 与最低 TLS policy；证书链、pinning、开发例外和平台 API 仍由消费者实现。需覆盖旧 endpoint 的升级默认值和拒绝不安全组合。
- P1（二进制基础层已完成，生产协商仍关闭）：已按明确的 Client+Gateway 双端候选授权新增独立 `ChatApp.Protocol.Tcp.Binary` runtime 和 build-time generator，保留 10-byte 帧头与唯一 DTO；格式 id 为 `chatapp-tagged-v1`，尚未加入可协商列表，也没有修改 Gateway/Client 默认 JSON。发布和启用仍以两个真实消费者完成接入为前提。
  - runtime 仅依赖 BCL 与 `ChatApp.Protocol.Tcp`，以 `IBufferWriter<byte>`/`ReadOnlySequence<byte>` 工作；编码器由 incremental generator 生成，无运行时反射、动态 registry 或第二套 DTO。长度、字段数、字符串和 bytes 在对应分配前检查上限；canonical varint、未知字段跳过、重复字段、required presence 与严格 UTF-8 均显式校验。
  - 原生指针仅在 x86/x64/Arm64 little-endian 且 span 已完成 4/8 字节边界检查时读写定宽值；不支持的架构回退到 `BinaryPrimitives`，分段定宽值先复制到精确的 4/8 字节栈缓冲再进入同一 helper，全部变长字段保留安全路径。运行时代码不调用 `System.Runtime.CompilerServices.Unsafe`，指针 helper 由架构测试限制为单文件。
  - 当前证据：代表性消息 87 B，对照 JSON 240 B；三轮短基准编码约 574–597 ns/op 且 0 B/op，解码约 743–783 ns/op。Release 通过 runtime `21/21`、generator-driver `7/7`、Architecture/compatibility `41/41`，并精确产出 6 个候选包；这些数据只证明基础层可行，不等于生产启用。
  - 接入 TODO：为首批 schema 建立永久 reserved field 清单、集合/嵌套类型、old/new golden 与 consumer fixture；随后覆盖全部握手后 payload 命令。只有 JSON 降级、Resume、混合格式 fanout、畸形输入和真实 80/320/640 msg/s 短测通过，才允许打开 feature；失败时新连接恢复 JSON。
  - 发布 TODO：公开 Binary/Generator 包前更新包清单与 README，确认 runtime 仍为 BCL-only、generator 仅以 analyzer 资产进入且不把 Roslyn 带到运行时；重新 pack 后记录 6 个不可变 nupkg/hash，并由 Client/Gateway 从 feed 做 locked restore。任何源码、生成器、README 或包元数据变化都升版本，不覆盖已有候选包。
- P2（传输能力契约）：只有 QUIC 隔离试验达到门槛后才共享 capability、版本和错误码；裸 UDP、WebRTC/ICE/TURN/SRTP、拥塞控制和 socket 实现始终留在端点/媒体服务。
- 当前验证：Release 构建 0 warning/error；Binary runtime `21/21`、Generator `7/7`、Architecture/compatibility `41/41` 通过。

## 功能路线

先共享语音附件的 MIME/codec/duration/waveform 等稳定元数据；随后仅共享 1:1 通话 capability、信令 DTO、错误码和版本。录音实现、WebRTC/ICE/TURN/SRTP 与 UDP/QUIC socket 均不进入 Shared。

## 发布顺序

聚焦 golden/兼容测试 → Release 构建与包版本 → 不可变 nupkg/hash → 消费者按需升级 → 跨进程短时联调；阶段长测和发布 soak 留到消费者功能冻结后。
