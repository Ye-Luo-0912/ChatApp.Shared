# 下一阶段与接手状态

## 职责

Shared 只拥有跨进程、可版本化且至少有两个真实消费者的契约；实现、连接和运行策略留在各业务仓库。

## 下一步执行与交接

Shared 有两个顺序明确的接手批次，不得合并发布：

1. **`PROTO-FEED-1`（代码与测试已收口，feed 发布已解锁）。** 帧级畸形/超限 fuzz 与旧附件/关系字段 old-new fixture 已落地并通过（`TcpProtocolFrameFuzzTests.cs`，ArchitectureTests `68/68` 通过，Release 0 warning/error）。包确定性核查结论是 `.nupkg` SHA-256 由随机 `core-properties/*.psmdcp` 文件名与随机关系 `Id` 所致；版本策略已决策为**确定性归一化后重记录**（`tools/Normalize-NupkgDeterministic.ps1` 固定 psmdcp 文件名/Id/时间戳/排序与压缩级别，CI 已接入），连续两次独立 pack+归一化逐包 hash 一致。六包 `0.4.1` 确定字节 SHA-256 已重记录于迁移文档，可复现。把不可变 feed、包清单和 locked-restore 指令交给 Gateway/Client。
2. **`REL-WIRE-2`（schema 已收口，包/hash 与双端接入待下批）。** `REL-GATE-1` 已于 2026-08-11 通过（manifest、两轮 reconcile report、故障矩阵和稳定错误码清单齐备，见 RealtimeServices `docs/NEXT-STAGE.md`）。关系只读 list wire 已定义并在 Shared 收口：`TcpRelationshipListRequest/Response/Item` 与稳定错误码落在 `ChatApp.Protocol.Tcp`（`RelationshipListContracts.cs`），JSON metadata 已注册，golden/old-new 兼容矩阵/预算/畸形游标测试 `14/14` 通过。字段/预算（PageSize 1–200、单响应 ≤ 80 KiB、ResourceId/Status/Message 上限）/opaque 游标/reset 语义表已固化在契约头部注释。关系**增量同步（sync/catch-up）wire 也已收口（2026-08-11）**：`RelationshipSyncWatermark` / `RelationshipChangeLogEntry` / `RelationshipCatchUp` 与 `TcpRelationshipChangeOperation` 移入 `RelationshipSyncContracts.cs`，补齐语义表、稳定错误码（`TcpRelationshipSyncErrorCode`：unavailable / projection_changed / gap / invalid_cursor / retention_exceeded / request_too_large / bad_request）与预算常量（`TcpRelationshipSyncConstants`）；`ResetRequired` 改为可空以兼容旧生产端，并移除泄漏 Realtime 内部的 `ChangeSequence`/`RequestId`/`RetentionFloorSequence`/`ResetReason` 字段，水位 `AfterSequence`/`NextSequence` 明确为不透明位置令牌。新增 `TcpRelationshipSyncContractTests` `13/13`（golden / reset 语义 / 水位不透明 / 未知操作 fail-closed / 预算 / 内部编码隔离 / old-new 兼容 / 畸形截断），Architecture 基线涨至 `95/95`。**0.4.2 已打包并记录 hash（2026-08-12）**：list + sync/catch-up 两个新能力在 0.4.1 记录后进入 Tcp/Json 源码，六包统一升 patch 至 `0.4.2`，Release 构建 0 warning/error、测试 Architecture `95/95` / Binary `21/21` / Generator `7/7`，归一化后六包 SHA-256 已记录于 MIGRATION-ROADMAP。**双端联调已完成（2026-08-12）**：Gateway（ChatAppTCP_Server）与 Client（Chat_App）均从各自 `packages/` feed locked restore `0.4.2` 并 Release 构建 0 warning/error；Gateway `TcpProtocolContractCompatibilityTests` 15/15 与 `RealtimeContractConsolidationTests` 全部通过（内部→wire mapper 只保公开字段），全套 566 passed + 1 Redis skip（1 个 DirectSocket/Persistent Windows loopback 时序 flaky 隔离通过，与 0.4.2 无关）；Client Protocol `58/58`、Unit `40/40`、Integration `185/185`（含关系 fail-closed 与 `SyncBootstrapMultiPageTests` 适配精简 DTO）。**真实跨进程短时 TCP JSON 联调也已通过（2026-08-12，见 MIGRATION-ROADMAP「0.4.2 跨进程 TCP JSON 联调证据」）**：隔离进程 `.tmp-crossproc/CrossProcLink` 引用 `ChatApp.Protocol.Tcp 0.4.2`，经 Redis 引导 AccessToken（`ChatApp.Auth.Contracts` 写路径）后，完整走 ClientHello→ServerHello→AuthenticationRequest→AuthenticationResponse→RelationshipListRequest→RelationshipListResponse，鉴权成功且关系列表返回共享契约稳定错误码 `relationship_read_projection_unavailable`（capability 关闭时的正确 fail-closed）。剩余唯一拦项「真实跨进程短时 TCP JSON 联调」已清除；Realtime 关系投影 reconcile gate 两轮有界分页/状态不变/全量指纹校验也已通过（2026-08-12，见 MIGRATION-ROADMAP「0.4.2 关系投影 reconcile gate 两轮校验证据」，`gatePassed=true`、两轮指纹一致、6 条流无差异）。至此开 capability 的剩余拦项全部清除。
3. **交付 Gateway/Client。** 发布新包版本和 SHA-256，附 reserved field 清单、兼容窗口、部署顺序与回滚顺序；Gateway/Client 只消费包，不复制源码或重新声明 DTO。Realtime 内部 snapshot/checkpoint/hash 不进入外部 wire。
4. **暂不并线。** 全新 `chatapp-bin-v1` 只做底座/golden/基准，生产协商保持关闭；关系首轮、二进制和媒体契约不得同批发布。

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
- P1（全新 Binary V1 底座代码已收口，生产协商关闭）：旧实验实现从未被 Client/Gateway/Server/Realtime 引用，reader/writer、旧 format、codec facade 与兼容窗口已全部删除。首个候选格式为 `chatapp-bin-v1`；10-byte 帧头、JSON 默认路径和生产能力位均不变。
  - **已完成：** Core 单遍 caller-owned `Span` encoder、Core 原生连续/分段 decoder、decode-only generator、新 golden/strict-order/limits/fuzz/逐字节分段差分均已落地；旧 runtime 源码检索为零。Release 构建 0 warning/error，全套 `171/171`（Core 37、Binary 21、Generator 15、Encoder-only 1、Architecture 97）通过。
  - **所有权与安全：** encode 除 caller buffer 外目标为零额外分配；owning decode 允许最终 DTO/string/byte[]，但不允许临时对象图。native pointer 只在 Core 有界 `fixed` 内部，不公开裸指针、不调用 `Unsafe`；分段输入不合并整包。
  - **BIN-SCHEMA-2 corpus 已完成：** 消费者侧已接入 canonical `ClientHello` 与扁平 `MessageHistoryRequest` 的 field numbers/普通 encoder，generator 产出连续与分段 decoder；`RealTcpBinarySchemaTests` `21/21` 覆盖 golden/hash、null-by-absence、Unix milliseconds、strict order、limits、默认值、string-heavy、最大合法与畸形输入。独立 Release Probe 已记录二进制/JSON payload、hash、ns/op、B/op；当前真实 DTO 无 canonical bytes 字段，bytes-heavy 保留为 Core 1 KiB workload，不猜测业务字段。生产握手、Resume 和 capability 仍保持 JSON。
  - **下一批 TODO：** 进入 Client/Gateway 的 JSON fallback、Resume、混合格式 fanout 与 80/320/640 msg/s 短测前，先冻结消费者侧 schema owner/包来源和 binary-only test fixture；`MessageHistoryItem` 的 nested/list 尚未冻结，不得在底座里猜测。
  - **消费者前置检查（2026-08-12）：** Client（`Chat_App`）和 Gateway（`ChatAppTCP_Server`）当前均从各自 `packages/` feed 及 lock file 使用 `ChatApp.Protocol.Tcp 0.4.2` / `ChatApp.Protocol.Tcp.Json 0.4.2`；两端没有 `ChatApp.Protocol.Tcp.Binary` 0.5.0 或 generator 包，运行时分别固定 `JsonPacketBodySerializer` / JSON `IPayloadCodec<T>`，握手、Resume 和 `ServerHello.PayloadFormat` 均为 JSON。Shared 当前已有绑定不可变提交的 0.5.0 clean-pack candidate，但尚未发布为消费者 feed，因此本批不修改两个消费者仓库、不恢复 sibling `ProjectReference`、不开 capability。下一执行点是审核并发布/校验七包 feed，再由消费者各自切包、锁定 schema owner 后做 binary-only offline fixture 和短时 JSON smoke。
  - **发布状态：** 本候选不可变提交已在 detached clean checkout 完成 locked restore、Release build/test、两次 clean pack+normalize 和七包 SHA-256 记录；仍需审核后发布内部 feed，脏工作树包不发布，且包发布不自动打开生产协商。详细规则见 [`BINARY-PROTOCOL.md`](BINARY-PROTOCOL.md)。
- P2（传输能力契约）：只有 QUIC 隔离试验达到门槛后才共享 capability、版本和错误码；裸 UDP、WebRTC/ICE/TURN/SRTP、拥塞控制和 socket 实现始终留在端点/媒体服务。
- 当前验证：本候选不可变提交的 detached clean checkout 两次 pack+normalize 的七包集合与逐包 SHA-256 一致，且 BIN-SCHEMA-2 corpus 已补齐；下一执行点是审核后准备七包内部 feed，再进入 `BIN-INTEGRATION-3` 的消费者短时联调；不是重建底座。

## 功能路线

先共享语音附件的 MIME/codec/duration/waveform 等稳定元数据；随后仅共享 1:1 通话 capability、信令 DTO、错误码和版本。录音实现、WebRTC/ICE/TURN/SRTP 与 UDP/QUIC socket 均不进入 Shared。

## 发布顺序

聚焦 golden/兼容测试 → Release 构建与包版本 → 不可变 nupkg/hash → 消费者按需升级 → 跨进程短时联调；阶段长测和发布 soak 留到消费者功能冻结后。
