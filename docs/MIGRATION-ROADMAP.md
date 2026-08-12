# 共享契约迁移状态与历史记录

## 已完成状态（Shared 0.4.1 / Realtime 2.x）

| 边界 | 唯一契约源 | 消费者 | 完成状态 |
| --- | --- | --- | --- |
| AccessToken Redis key/value | `ChatApp.Auth.Contracts` `0.4.1` | Server writer、TCP Gateway reader | 两端直接使用同一缓存 schema；Server 的领域投影通过显式 mapper 转为共享记录 |
| Auth、好友、附件、会话 HTTP DTO | `ChatApp.Contracts.Http` `0.4.1` | Client、Server Host | 两端直接使用共享 wire DTO；Server Core 保持 BCL-only，并在 Host 边界显式映射 |
| TCP 帧头、控制 DTO、历史/同步/附件业务 DTO | `ChatApp.Protocol.Tcp` `0.4.1` | Client、TCP Gateway | 两端已删除历史/同步同义 DTO；Gateway 显式映射 Realtime owner，Client 直接消费唯一 wire schema |
| TCP JSON metadata | `ChatApp.Protocol.Tcp.Json` `0.4.1` | Client、TCP Gateway、协议兼容测试 | 提供统一 source-generated JSON 入口；golden 固定 null、时间单位、reset 与游标语义 |
| TCP binary 历史实验包 | `ChatApp.Protocol.Tcp.Binary` / `.Generator` `0.4.1` | 无生产消费者 | 从未启用；其 API/格式不属于 `chatapp-bin-v1` 的兼容面，生产继续 JSON |
| Realtime DTO 与集成接口 | `ChatApp.Realtime.Contracts` `2.5.2`、`ChatApp.Realtime.Integration` `3.1.3` | RealtimeServices、Server、TCP Gateway | Contracts 延续既有 2.x 谱系；Integration 因公开 Outbox 类型迁出而按 breaking change 升至 3.x；消费者使用包引用，不依赖 sibling 源码目录 |
| Realtime EF Outbox 模型与映射 | `ChatApp.Realtime.Outbox.EntityFrameworkCore` `1.0.0` | Server | 从 NATS Integration 包剥离；只有需要 EF Outbox 的宿主引用，Gateway/PushWorker 不再传递依赖 EF Core |
| 通用基础类型 | 暂不建立项目 | 暂无两个以上语义稳定消费者 | 已删除空 `Primitives` marker，避免无实际类型的预设依赖 |

四个业务仓库现在都能仅依赖版本化 NuGet 包独立 restore/build，不需要相邻仓库源码。Shared、Client、Server、Gateway 与 RealtimeServices 均生成并校验 `packages.lock.json`；CI 和服务容器使用 locked restore。相同 PackageId/Version 的 `.nupkg` 与锁文件 content hash 在所有消费者中保持一致。

Client 与 Gateway 已删除本地 `PacketCommand` 枚举。源码中的 alias 只用于缩短名称，公开签名与编解码实际类型都是 `ChatApp.Shared.Protocol.Tcp.PacketCommand`。历史短名称没有进入共享枚举。

## 0.4.1 本地发布候选证据（2026-08-10；hash 于 2026-08-11 归一化后重记录）

六个候选包由同一次 Release pack 生成，尚未发布到共享 feed；Client 与 Gateway 已用 locked restore 消费同一份 `ChatApp.Protocol.Tcp`/`.Json` 包并完成 Release 构建。相同版本不得用后续构建覆盖，若源码、README 或包元数据发生变化，必须重新升版本并生成新清单。

以下 SHA-256 为 `tools/Normalize-NupkgDeterministic.ps1` 归一化后的确定字节（连续两次独立 pack + 归一化复现一致），可在干净 CI 中复现。

| 候选包 | SHA-256 |
| --- | --- |
| `ChatApp.Auth.Contracts.0.4.1.nupkg` | `D1F146BCD912F6032204D89F82D510453354932D97103BB451BC97A4E920E27B` |
| `ChatApp.Contracts.Http.0.4.1.nupkg` | `E95E87E6345C0C21D2C805F1ADFFCBEDD1719060402DF600EB9E475A8F722E0A` |
| `ChatApp.Protocol.Tcp.0.4.1.nupkg` | `F56E6662F958396136C46359B0538EEC5F024331617771DDE55CFD55BE0EA4D5` |
| `ChatApp.Protocol.Tcp.Json.0.4.1.nupkg` | `15BAAFEF4AAF2A14BEBEC89B6C2217B56C8C8FEFE80D8652826DC210B61ABAFC` |
| `ChatApp.Protocol.Tcp.Binary.0.4.1.nupkg` | `031B13FD20942D41E267CACD43093EA939B987D1EFE40612C4599527AC4D4FF6` |
| `ChatApp.Protocol.Tcp.Binary.Generator.0.4.1.nupkg` | `ABE61020AF257A07AAC7475FA09BBC670762B25A2BE444590F05658801E4628E` |

当前跨仓 fixture SHA-256：Shared JSON golden `DBACEE52E578C786DF3EED753F5B4DADB51EEE3F82F0AA4E04A308E061334D1E`，Shared old/new matrix `974B6AE15B1A9BA5060C1F5A8531210F8CFECEE5FB17C86641363A5F31968B25`，Gateway producer/compatibility fixture `DB0288078E293240F69B243BDB924AA719E11A533B35C77D3C45A812949C28CA`，Client consumer/compatibility fixture `D05BAE10DEADF4542071B1B77C0247F797161953ED8110FB67F2213BDFAF3E5F`。最终 locked build 的 `ChatApp.TcpGateway.dll`/`Chat_App.dll` SHA-256 分别为 `2B90FD4FC467780C58DD0364ECEA50EA6CC7B86CD7D6F4C38095739E08B60F89` 与 `6CDF4A0ED9CF1B523491764F6758058FB9B4B141BE0DECEFF4D36676F284CA4D`；这些二进制 hash 仅绑定本次本地候选，任何重新构建都应重新记录。

## PROTO-FEED-1 帧级 fuzz 与包确定性核查（2026-08-11）

发布前补的帧级畸形/超限 fuzz 与旧附件、关系字段组合 old/new fixture 已落地并通过：

- 新增 `tests/ChatApp.Shared.ArchitectureTests/TcpProtocolFrameFuzzTests.cs`，覆盖：
  - **帧级畸形输入**：截断 JSON、非 JSON 字节、错误值类型、空/顶层标量、深嵌套（超默认 MaxDepth）、尾部垃圾、重复字段（last-wins）、未知字段忽略、非法转义、超 range 数值——codec 必须干净抛 `JsonException`，不半物化或崩溃。
  - **帧级超限**：超大字符串字段、接近/超过 80 KiB 硬上限的多条 item 仍忠实往返；`Limit` 是端点策略而非 codec 职责（见 `TcpFrameConstants` 注释）。
  - **旧附件字段组合**：旧格式 `TcpAttachmentRef`（缺 `DownloadApiHint/DownloadToken/ThumbnailApiHint`）降级到默认值；全字段新格式原样往返；附件嵌在 `MessageHistoryItem` 内的旧组合可读。
  - **关系字段组合**：旧 `RelationshipCatchUp`（缺 `NextSequence/RetentionFloorSequence/ResetRequired`）降级到默认；新格式全字段往返；`RelationshipSyncWatermark` 与带 `RelationshipCatchUps` 的 `SyncBootstrapResponse` 往返。
  - 结果：`ChatApp.Shared.ArchitectureTests` Release 构建 `0 warning / 0 error`，测试 `68/68` 通过（原 41 + 新增 27）。该改动仅触及测试项目，不改变任何 `0.4.1` 包源码或元数据。
- **包确定性核查（重要工程发现）+ 修复（已收口）**：
  - 对同一源码连续两次 `dotnet pack` 产出不同的 `.nupkg` SHA-256，根因是 NuGet pack 生成的 core-properties `.psmdcp` 文件**文件名是每次随机 GUID**，且 core-properties 关系的 `Id` 也是每次随机 16 位（`R` + 15 位十六进制）；内容、`nuspec`、`README` 与编译产出 DLL 均逐字节一致。
  - **版本策略决策（2026-08-11）：采用「确定性归一化后重记录」**。新增 `tools/Normalize-NupkgDeterministic.ps1`，对每个 nupkg ①把 `core-properties/*.psmdcp` 重命名为固定名 `package.psmdcp`，②把 core-properties 关系 `Id` 归一化为固定值 `R0000000000000002`，③按名称排序、固定时间戳（2026-01-01T00:00:00Z）、固定压缩级别重写 zip。CI（`contracts.yml`）已在 Pack 之后、写 SHA256SUMS 之前插入该归一化步骤。
  - **可复现验证**：对六包连续两次独立 pack + 归一化，逐包 SHA-256 完全一致（`IDENTICAL`）。因此 `0.4.1` 六包现可在本地与干净 CI 复现同一组 hash，作为权威发布工件。
  - 版本处理：源码在 c06dccc 与 `0.4.1` 记录同提交，未在记录后变化；`0.4.1` 保持版本不变，仅以归一化后的确定字节重新记录六包 hash（下表）。

## REL-WIRE-2 关系只读 list wire schema 收口（2026-08-11）

关系只读列表的 Client↔Gateway wire 已在 Shared 按真实 Server/Client 语义定义并收口：

- 新增唯一 DTO `TcpRelationshipListRequest` / `TcpRelationshipListResponse` / `TcpRelationshipListItem` 与稳定错误码常量（`TcpRelationshipListErrorCode`），位于 `src/ChatApp.Protocol.Tcp/RelationshipListContracts.cs`。列表类型复用既有 `TcpRelationshipListType`（Friends=1 / FriendRequests=2 / BlockedUsers=3，与 Realtime 数值一致）。
- 字段/预算/游标/reset 语义表固化在契约头部注释：PageSize 1–200（默认 50）、单响应 ≤ 80 KiB、`ResourceId ≤ 64` / `Status ≤ 32` / `Message ≤ 512`（UTF-8 字节）；opaque cursor 只承诺继续或明确失效，畸形游标返回 `invalid_cursor`；unavailable / version-changed / gap 以稳定错误码表达，其中 version-changed / gap 触发 `ResetRequired`，消费者必须丢弃游标从第一页重建。
- JSON metadata 已注册于 `ChatApp.Protocol.Tcp.Json`（`TcpProtocolJsonSerializerContext`）。
- 测试 `tests/ChatApp.Shared.ArchitectureTests/TcpRelationshipListContractTests.cs` 覆盖 golden、old/new 兼容矩阵（legacy 无 `ResetRequired` 字段）、未知列表类型/未知 Status、畸形/截断输入与字节预算。结果：该批 `14/14` 通过，Architecture 全套 `82/82` 通过，Release 构建 `0 warning / 0 error`。
- 尚未打包发布、也未经 Gateway/Client 双端编译消费与 JSON 短联调；能力位保持关闭，mutation 仍走 Server HTTP。**2026-08-12 更新**：六包已升 `0.4.2` 并记录归一化 SHA-256；Gateway/Client 已从 feed locked restore `0.4.2` 双端编译消费（Release 0 warning/error，wire 兼容测试全过，见「0.4.2 本地发布候选证据」）。**真实跨进程短时 TCP JSON 联调已于 2026-08-12 通过**（见「0.4.2 跨进程 TCP JSON 联调证据」）。

## REL-WIRE-2 关系增量同步（sync/catch-up）wire 收口（2026-08-11）

关系列表的增量同步 wire 已在 Shared 按与 list 只读 wire 同等的严谨度收口：

- 独立契约文件 `src/ChatApp.Protocol.Tcp/RelationshipSyncContracts.cs`：汇聚 `RelationshipSyncWatermark` / `RelationshipChangeLogEntry` / `RelationshipCatchUp` 与 `TcpRelationshipChangeOperation`（从 `SyncBootstrapContracts.cs` 迁入，同 namespace 无引用改动），并新增稳定错误码 `TcpRelationshipSyncErrorCode`（`relationship_read_projection_unavailable` / `relationship_projection_changed` / `relationships_gap` / `invalid_cursor` / `relationships_retention_exceeded` / `request_too_large` / `bad_request`）与预算常量 `TcpRelationshipSyncConstants`（`ResourceId ≤ 64` / `Status ≤ 32` / `Message ≤ 512`、单页 ≤ 200 条、单响应 ≤ 80 KiB）。
- 语义表固化在契约头部注释：Upsert/Delete 按 `ResourceId` 应用（未知操作 fail-closed）；opaque 水位 `AfterSequence`/`NextSequence` 客户端只持久化并原样回传、不解释不自增；中间页 `HasMore=true` 用 `NextCursor` 续页且不推进水位，尾页才持久化 `NextSequence`；水位低于保留水位 / 分页版本变化 / 缺口时 `ResetRequired=true` 并返回稳定错误码，客户端丢弃本地状态经全量 list 重建。
- **内部编码隔离**：移除泄漏 Realtime 内部的 `ChangeSequence` / `RequestId` / `RetentionFloorSequence` / `ResetReason` 字段；`ResetRequired` 改为可空以兼容旧生产端省略该字段。
- JSON metadata 仍注册于 `TcpProtocolJsonSerializerContext`（类型名/namespace 未变）。
- 测试 `tests/ChatApp.Shared.ArchitectureTests/TcpRelationshipSyncContractTests.cs` 覆盖 golden、reset 语义、水位不透明往返、未知枚举 fail-closed、预算、内部编码不序列化、old-new 兼容和畸形截断。结果：该批 `13/13` 通过，Architecture 全套 `95/95` 通过，Release 构建 `0 warning / 0 error`（原 `TcpProtocolFrameFuzzTests` 中关系 sync 字段组合用例已同步适配精简后的 DTO）。
- 尚未打包发布、也未经 Gateway/Client 双端编译消费与 JSON 短联调；能力位保持关闭，mutation 仍走 Server HTTP。**2026-08-12 更新**：六包已升 `0.4.2` 并记录归一化 SHA-256；Gateway/Client 已从 feed locked restore `0.4.2` 双端编译消费（Release 0 warning/error，wire 兼容测试全过，见「0.4.2 本地发布候选证据」）。**真实跨进程短时 TCP JSON 联调已于 2026-08-12 通过**（见「0.4.2 跨进程 TCP JSON 联调证据」）。

## 0.4.2 本地发布候选证据（2026-08-12）

REL-WIRE-2 的 list 只读 + sync/catch-up 两个 wire 能力在 `0.4.1` 记录之后进入 `ChatApp.Protocol.Tcp`/`.Json` 源码，`0.4.1` 的 hash 已不能代表当前源码。按迁移规则「新能力或可选字段升 minor / 0.x breaking 至少升 minor」，本批视为对 `0.4.1` 候选的内部泄漏字段清理 + 新能力，**六包统一升 patch 至 `0.4.2`**（`Directory.Build.props` `VersionPrefix`、`ContractBoundaryTests` 断言、`contracts.yml` 包清单、`README` 版本表已同步）。

六个候选包由同一次 Release pack 生成，尚未发布到共享 feed。以下 SHA-256 为 `tools/Normalize-NupkgDeterministic.ps1` 归一化后的确定字节：

| 候选包 | SHA-256 |
| --- | --- |
| `ChatApp.Auth.Contracts.0.4.2.nupkg` | `1D3FA0B3DA97FE943EA4A0195B0D9A20BBE3407BB85F0A78C81CF3DA1FC67C42` |
| `ChatApp.Contracts.Http.0.4.2.nupkg` | `BB0097C0AC81483B5ABE63546673945F1CDFE96B6E56D531698428B1E0867F7E` |
| `ChatApp.Protocol.Tcp.0.4.2.nupkg` | `7AD665AD311A799BBA946D71F9D7F96347A8236074A879504E5CAA72A4A33E75` |
| `ChatApp.Protocol.Tcp.Binary.0.4.2.nupkg` | `8546A0C469CEC15C5AA117BF48AC37FFBCF678D666DC71A8B3FFB4163ED6C095` |
| `ChatApp.Protocol.Tcp.Binary.Generator.0.4.2.nupkg` | `505FAA6B9C76AB213BC32222C30CB37BA658C2D971E2B42637BB22C1799B4E0F` |
| `ChatApp.Protocol.Tcp.Json.0.4.2.nupkg` | `08280BB404C6A29D6FBF70FB4A152698E7831445D0CD9B9C8DDD0808F199D19F` |

`0.4.2` 已在本地完成 Release 构建（`0 warning / 0 error`）与全套测试（Architecture `95/95`、Binary `21/21`、Generator `7/7`）。**双端联调已完成（2026-08-12）**：Gateway（ChatAppTCP_Server）与 Client（Chat_App）均从各自 `packages/` feed locked restore `0.4.2` 并 Release 构建 0 warning/error；Gateway wire 兼容测试 `TcpProtocolContractCompatibilityTests` 15/15 与 `RealtimeContractConsolidationTests` 全过，全套 566 passed + 1 Redis skip（1 个 DirectSocket/Persistent Windows loopback 时序 flaky 隔离通过，与 0.4.2 无关）；Client Protocol `58/58`、Unit `40/40`、Integration `185/185`（含关系 fail-closed 与 `SyncBootstrapMultiPageTests` 适配精简 DTO）。**真实跨进程短时 TCP JSON 联调也已通过（2026-08-12）**，见「0.4.2 跨进程 TCP JSON 联调证据」。

## 0.4.2 跨进程 TCP JSON 联调证据（2026-08-12）

隔离进程联调客户端 `.tmp-crossproc/CrossProcLink`（`net10.0`，仅引用 `ChatApp.Protocol.Tcp 0.4.2`、`ChatApp.Auth.Contracts 0.4.2`、`StackExchange.Redis`）在真实环境（Garnet 6379 / NATS 4222 / Postgres 5432 均运行，Gateway=ChatApp.TcpGateway 监听 `127.0.0.1:8888`，Realtime 服务运行中）完成短时 TCP JSON 联调：

- **认证**：用 `ChatApp.Auth.Contracts` 的 Server 写路径在 Redis 写入 `AccessTokenCacheRecord`（key=`cache:AT:{sha256hex(token)}`），Gateway 经 `RedisAccessTokenStore` 校验通过。
- **链路**：`ClientHello(version=1) → ServerHello(version=1, format=json, device=a57180056e9b4a689156fc953866c246) → AuthenticationRequest → AuthenticationResponse(success=true, userId=90001, sessionId=cp-90001) → RelationshipListRequest(listType=Friends, pageSize=10) → RelationshipListResponse`。
- **wire 大小写**：确认 wire JSON 为 camelCase（Gateway `GatewayJsonSerializerContext` 与 Client `ChatJsonContext` 均 `PropertyNamingPolicy=CamelCase`）；错误的 PascalCase 请求被 Gateway 判 `AccessToken 为空`，改 camelCase 后鉴权成功。
- **fail-closed 语义**：关系列表响应 `Succeeded=false`、`ErrorCode=relationship_read_projection_unavailable`（即共享契约 `TcpRelationshipListErrorCode.ProjectionUnavailable`），`HasMore=false`、`Items=0`、`ResetRequired=null`——capability 关闭时的正确 fail-closed，且共享 `TcpRelationshipListResponse` 可完整反序列化，证明 RequestId/ListType/PageSize/Cursor 请求与 RequestId/Succeeded/ErrorCode/Items/NextCursor/HasMore 响应跨进程双向成立。
- **清理**：联调结束后删除 Redis 引导 token。

联调客户端构建 Release `0 warning / 0 error`，运行终止码 0（PASSED）。剩余开 capability 门禁：Realtime 关系投影 reconcile gate 两轮有界分页/状态不变/全量指纹校验通过。

## 0.4.2 关系投影 reconcile gate 两轮校验证据（2026-08-12）

开 capability 前的最后一道门禁已通过。Realtime（Development，8080）从事实源 Server（`http://127.0.0.1:5200`，导出端点 `X-Relationship-Projection-Key: reconcile-test-key`）启用 `RelationshipProjectionRebuild` 拉取投影快照，Rebuilder 稳定后（`stablePasses=3`、6 条流 baseline 一致、投影项 8 条），用 `scripts/Invoke-RelationshipProjectionReconcile.ps1`（`CHATAPP_OPS_API_KEY=reconcile-ops-key`，`-RequiredCleanPasses 2`）在 Realtime `/ops/relationship-projection/reconcile` 上完成两轮有界分页/状态不变/全量指纹校验：

- **Pass 1**：状态 token 前后一致（`passNumber=4;stablePasses=3;passChanged=False;versionStreamCount=6;snapshotBaselineStreamCount=6;streamsWithoutSnapshotBaselineCount=0;projectionItemCount=8;inboxEventCount=0`），`mismatchCount=0`，单页 200，指纹 `97E522BB719D6ADE7BEE351F99A60B9686390FBAB0DAAF6D83AB5F25AFE049DE`。
- **Pass 2**：状态 token 前后一致，`mismatchCount=0`，指纹与 Pass 1 完全一致。
- 报告 `gatePassed=true`，6 条流全部匹配、无差异。报告落盘 `ChatApp.RealtimeServices/.artifacts/relationship-projection-reconcile/20260811T201857Z/report.json`。

至此 REL-WIRE-2 开 capability 的剩余拦项（真实跨进程短时 TCP JSON 联调 + `0.4.2` 关系投影 reconcile gate 两轮校验）全部清除。

## REL-READ-3 关系投影同源对照读取端证据（2026-08-12）

reconcile gate 覆盖「Server 权威快照 → 投影」的写入端一致；本批补充「投影 → list/catch-up 读取」的读取端同源对照，两段合起来构成 Server 权威为唯一事实源、客户端从 snapshot 后仅靠增量收敛的完整闭环。

- 新增 `ChatApp.Realtime.IntegrationTests.RelationshipProjectionSourceParityTests`（6/6 通过）：以内存 Server authority 驱动真实 Postgres 投影（`NpgsqlRelationshipProjectionStore` + `NpgsqlRelationshipProjectionQueryStore`），逐项验证读取与权威一致。每个用例使用独立 owner，避免共享 Postgres 中 `(owner,list)` 流版本在用例间互相污染。
- 覆盖场景：快照后仅靠增量收敛（好友/申请/黑名单逐项对照）、分页期间并发 mutation → `VersionChanged` 禁止静默返回过期页、重复 cursor 同页稳定延续（断线续页语义）、无 snapshot checkpoint 时 fail-closed 返回 `Unavailable`、删除项从列表消失且 history 仍推进、重复 delta 幂等重放仍收敛。
- 与 catch-up 语义闭环一致：gap/保留期越界由 `DefaultSyncBootstrapQueryProcessor` fail-closed 置 `ResetRequired`（`beyond_retention` / `projection_unavailable`）而非返回伪空成功；关闭读取开关后 `UnavailableRelationshipProjectionStore` 仍 fail-closed；旧关系写表不参与任何在线结果。
- 结果：`ChatApp.Realtime.IntegrationTests` 全套 `94/94` 通过（含本批 6 个同源对照用例）。仅触及 Realtime 测试项目，不改变任何 `0.4.2`/`0.5.0` 包源码或元数据。

## 0.5.0 全新二进制底座候选（2026-08-12）

旧实验二进制从未被 Client、Gateway、Server 或 Realtime 引用，因此本批直接废弃其 reader/writer、format ID、codec facade 与兼容语义。首个候选格式改为 `chatapp-bin-v1`：BCL-only Core 提供单遍 Span encoder 和有界连续/分段 decoder，Generator 只生成 decoder；生产协商仍关闭。先前工作树的兼容包/hash 全部作废；`0.4.2` 历史表只作为历史记录保持不变。

breaking 重构代码已完成：新 fixture 覆盖 17 种字段形态，golden SHA-256 为 `9CAF417B6B6CE9D79DE722D94C8609C23AD2AFA5CF8DF5058F09081C6CFC1DB4`；strict order、unknown/required/limits、每前缀截断、deterministic fuzz，以及连续 Core 与逐字节分段 Core 的成功值/精确 status 差分均已通过。locked restore 和 Release 构建 0 warning/error，全套 `171/171`（Core 37、Binary 21、Generator 15、Encoder-only 1、Architecture 97）通过。

先前微探针说明 measured `IBufferWriter` 的两遍 schema 遍历会抵消 writer 调用减少，因此新热路径只保留 caller-owned Span 单遍编码。最终底座在 Windows x64/.NET 10 的三进程方向性探针中，32 B/1 KiB body encode 分别为 `21.2–23.8 ns` / `38.3–40.2 ns` 且均为 `0 B/op`；连续 owning decode 为 `29.2–31.0 ns` / `68.8–79.4 ns`，分配只包含最终 DTO/byte[]。这些三字段结果不能替代真实业务结论；接入前仍需真实 Chat/History/Sync schema、Gateway/Client 隔离消费、Linux x64/Arm64 与端到端短测，失败时继续使用 JSON。

BIN-SCHEMA-2 corpus 证据已补齐：消费者侧 schema 选择 canonical TCP `ClientHello` 和扁平 `MessageHistoryRequest`，`RealTcpBinarySchemaTests` `21/21` 通过。Binary golden SHA-256 分别为 `ClientHello AA2A83D7D5F61D3522FAEACF3091773D348325D816D4FBF03EC9E2EBD386B2AC`、`MessageHistoryRequest BCF897F4F71D3912D6159395FDBE0F1D783BB3DC4B03EA6482F00D86C4163AD0`；canonical payload 分别为 `42 B` 和 `66 B`，同一 JSON fixture 为 `151 B` 和 `199 B`。新增 corpus 的 binary/JSON payload 分别为：ClientHello default `6/54 B`、string-heavy `1305/2184 B`、max-legal `2078/3217 B`；MessageHistoryRequest default `2/12 B`、string-heavy `2077/2974 B`、max-legal `3452/4613 B`。Windows x64/.NET 10 Release Probe 的 canonical span binary encode/decode 为 `64.3/93.0 ns/op, 0/152 B/op` 与 `138.2/157.4 ns/op, 0/272 B/op`；对应 JSON encode/decode 为 `206.9/1118.8 ns/op, 176.3/152 B/op` 与 `306.0/556.7 ns/op, 224.2/272 B/op`。Probe 已记录各 corpus 的 binary hash、连续/分段 decode、ns/op/B/op 和 invalid UTF-8/truncation malformed case；当前真实 TCP DTO 没有 canonical bytes 字段，bytes-heavy 只由 Core 1,024-byte workload 覆盖，不猜测业务字段。生产握手、Resume 和 capability 继续关闭 binary；`MessageHistoryItem` nested/list 延后到真实模型冻结。

消费者接入验证（2026-08-12）：`Chat_App` 与 `ChatAppTCP_Server` 已仅将 `ChatApp.Protocol.Tcp` / `ChatApp.Protocol.Tcp.Json` 升级至 `0.5.0`，并在各自 `packages/` 离线 feed 与 lock file 下通过 locked restore、Release build。Client 全套 `283/283` 通过；Gateway 串行测试 `566/566` 通过，Redis 互操作 1 项因未设置 `CHATAPP_TEST_REDIS` 跳过。Gateway 默认并行执行稳定只有既有的 `DirectSocket + PersistentSendLoop` 参数化用例失败，单独执行该方法 `6/6` 通过，暂按测试并发隔离问题记录。两端均没有 `ChatApp.Protocol.Tcp.Binary`、`ChatApp.Binary.Core` 或 generator，Client 仍使用 `JsonPacketBodySerializer`，Gateway 仍使用 JSON `IPayloadCodec<T>`，握手、Resume 和 `ServerHello.PayloadFormat` 仍为 `json`；生产 binary capability 继续关闭。

本候选不可变提交的 detached clean checkout 已完成 locked restore、Release build/test、两次 clean pack+normalize，七包集合与逐包 SHA-256 一致，且 Core/Binary/Generator 包拓扑符合边界；0.5.0 JSON 包已完成 Client/Gateway 本地离线消费验证。该段只记录已完成证据，不是下一阶段前置条件；生产 binary capability 继续关闭。

两次 clean pack+normalize 的七包逐项一致，相关结果作为本次迁移历史保留；不把自引用 hash 表继续写入源码文档。

## 独立构建验证快照（2026-08-06，历史基线）

| 仓库 | 锁定还原 / Release 构建 | 测试结果 |
| --- | --- | ---: |
| `ChatApp.Shared` | PASS，0 warning / 0 error | 25 passed |
| `Chat_App` | PASS，0 warning / 0 error | 245 passed |
| `ChatApp.Server` | PASS，0 warning / 0 error | 238 passed / 3 external failure-injection skipped |
| `ChatAppTCP_Server` | PASS，0 warning / 0 error | 516 passed / 1 Redis integration skipped |
| `ChatApp.RealtimeServices` | PASS，0 warning / 0 error | 285 passed；ACK timing focused 4 passed |

跳过项在对应能力进入集成验证时由带真实依赖的环境补跑，不能把 skipped 当作已覆盖。

## 下一阶段开发衔接

1. Client 执行 `REL-READ-3`：建立关系 SQLite 投影与每 list 水位，完成 list/catch-up/reset 的事务应用、故障恢复和 HTTP 权威对照；Shared 只修复能够复现的契约缺口。
2. Shared Binary nested/list 已完成首批：`MessageHistoryItem` 与 `SyncBootstrapRequest` 已冻结字段号、unpacked repeated/nested 语义、depth/element/materialized-byte limits，并有 Core/Generator 与真实 corpus；随后已补控制帧、会话列表 flat DTO、cursor reset、`MessageHistoryResponse` 与 `SyncBootstrapResponse`（含单个 nested cursor 和 repeated nested catch-up/items）的消费者侧 schema/golden/limits。关系 list/sync 与其余握手后 payload 仍留待后续批次，packed collection 也暂不启用。
3. Client/Gateway 执行 `BIN-INTEGRATION-3`：保持 JSON 握手和 Resume，连接级固定 exact format，按格式分组 fanout；用 5–20 分钟短测验证 fallback、重连、畸形/超限输入和 80/320/640 msg/s 下的 CPU、分配与 p95/p99。
4. 独立补 endpoint scheme/SNI/TLS policy 契约；媒体方向先完成语音附件元数据，再评估 WebRTC 信令，二者均不与关系或 binary 同批开发。

## 后续演进规则

1. 任何 TCP 命令先修改共享 `PacketCommand` 与 golden contract，再更新 Client/Gateway；消费者不得重新声明本地 wire enum。
2. HTTP、Auth 缓存或 Realtime wire 变更必须先在唯一契约源中完成，并用兼容性测试表达；业务项目只保留领域模型与显式 adapter。
3. Realtime 契约只在既有 2.x PackageId 上演进，不创建同名 0.x 包。
4. 只有出现两个以上真实消费者、语义稳定且版本节奏一致的值对象时，才评审新增 primitives 包。
5. 每次跨仓变更必须通过独立 Release build、契约/golden 测试与受影响消费者短时联调；失败时不得推进能力位或本地水位。

## 后续改动的标准路径

1. **先确定所有权。** TCP wire、HTTP wire、Auth 缓存 schema 和 Realtime/NATS wire 分别只改其唯一契约包；只被一个服务使用的领域 DTO、数据库实体和业务策略留在该服务内。
2. **先改契约，再改消费者。** 契约变更必须包含 golden bytes/JSON、枚举数值或缓存 schema 兼容测试；禁止消费者复制源码、链接文件或增加 sibling `ProjectReference`。
3. **显式处理兼容性。** 新字段必须有默认/null/未知行为；删除或改义必须保留 reserved 标识并采用新的 exact format/capability，不能靠 namespace 或反序列化猜测。
4. **消费者按需接入。** 只改实际需要新能力的 Client、Server、Gateway 或 Realtime；未接入者保持现有稳定路径，不提前复制 schema 或开启 capability。
5. **短反馈验证。** 每个受影响仓库独立执行 Release build、契约测试和自身行为测试；跨进程变更补真实 Redis/NATS/HTTP/TCP 联调，长时测试留到功能冻结后。
