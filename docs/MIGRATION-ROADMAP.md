# 共享契约迁移状态与发布路线

## 已完成状态（Shared 0.4.1 / Realtime 2.x）

| 边界 | 唯一契约源 | 消费者 | 完成状态 |
| --- | --- | --- | --- |
| AccessToken Redis key/value | `ChatApp.Auth.Contracts` `0.4.1` | Server writer、TCP Gateway reader | 两端直接使用同一缓存 schema；Server 的领域投影通过显式 mapper 转为共享记录 |
| Auth、好友、附件、会话 HTTP DTO | `ChatApp.Contracts.Http` `0.4.1` | Client、Server Host | 两端直接使用共享 wire DTO；Server Core 保持 BCL-only，并在 Host 边界显式映射 |
| TCP 帧头、控制 DTO、历史/同步/附件业务 DTO | `ChatApp.Protocol.Tcp` `0.4.1` | Client、TCP Gateway | 两端已删除历史/同步同义 DTO；Gateway 显式映射 Realtime owner，Client 直接消费唯一 wire schema |
| TCP JSON metadata | `ChatApp.Protocol.Tcp.Json` `0.4.1` | Client、TCP Gateway、协议兼容测试 | 提供统一 source-generated JSON 入口；golden 固定 null、时间单位、reset 与游标语义 |
| TCP tagged-binary 候选 | `ChatApp.Protocol.Tcp.Binary` / `.Generator` `0.4.1` | Client、TCP Gateway（待接入） | runtime BCL-only、generator 仅构建期；格式默认不可协商，生产继续 JSON |
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
- 尚未打包发布、也未经 Gateway/Client 双端编译消费与 JSON 短联调；能力位保持关闭，mutation 仍走 Server HTTP。

## 独立构建验证快照（2026-08-06，历史基线）

| 仓库 | 锁定还原 / Release 构建 | 测试结果 |
| --- | --- | ---: |
| `ChatApp.Shared` | PASS，0 warning / 0 error | 25 passed |
| `Chat_App` | PASS，0 warning / 0 error | 245 passed |
| `ChatApp.Server` | PASS，0 warning / 0 error | 238 passed / 3 external failure-injection skipped |
| `ChatAppTCP_Server` | PASS，0 warning / 0 error | 516 passed / 1 Redis integration skipped |
| `ChatApp.RealtimeServices` | PASS，0 warning / 0 error | 285 passed；ACK timing focused 4 passed |

跳过项必须在需要发布相应适配器时由带真实依赖的 CI/预发布环境补跑，不能把 skipped 当作已覆盖。

## 发布与部署顺序

1. Shared CI 对六个 `0.4.1` 包执行一次构建、测试、打包并产出 SHA-256 清单；Realtime 发布流水线分别产出 `Contracts 2.5.2`、`Integration 3.1.3` 与 `Outbox.EntityFrameworkCore 1.0.0`。审核后只把这些不可变候选发布到内部 NuGet feed；仓库内 `packages/` 只作为离线、CI 和迁移期的可复现来源。
2. 在发布流水线中验证包 hash 后，再部署 Server、Gateway 与 Client；禁止恢复 sibling `ProjectReference` 或源码路径 fallback。
3. Client 升级时同时应用 SQLite `deviceCredential` 迁移；Server 与 Gateway 应作为同一认证缓存契约批次部署。
4. Gateway watcher 在过渡期同时读取 canonical `watchers:*` 与旧 `pw:*`，并双写两套结构。全部旧实例下线后至少等待旧 key 最大 TTL（当前 30 分钟）及观测缓冲，再删除 `pw:*` 兼容路径。

## 后续演进规则

1. 任何 TCP 命令先修改共享 `PacketCommand` 与 golden contract，再更新 Client/Gateway；消费者不得重新声明本地 wire enum。
2. HTTP、Auth 缓存或 Realtime wire 变更必须先在唯一契约包中完成，并以兼容性测试和版本升级表达；业务项目只保留领域模型与显式 adapter。
3. Realtime 契约只在既有 2.x PackageId 上演进，不创建同名 0.x 包。
4. 只有出现两个以上真实消费者、语义稳定且版本节奏一致的值对象时，才评审新增 primitives 包。
5. 每次升级必须通过独立 locked restore、Release build、契约/golden 测试与容器构建，并记录 PackageId、版本和 hash，确保可回滚。

## 后续改动的标准路径

1. **先确定所有权。** TCP wire、HTTP wire、Auth 缓存 schema 和 Realtime/NATS wire 分别只改其唯一契约包；只被一个服务使用的领域 DTO、数据库实体和业务策略留在该服务内。
2. **先提交契约，再提交消费者。** 契约 PR 必须包含 golden bytes/JSON、枚举数值或缓存 schema 兼容测试，并产出不可变 `.nupkg` 与 SHA-256；禁止由消费者复制源码、链接文件或增加 sibling `ProjectReference`。
3. **按兼容性升版本。** 无 wire/API 变化的修复升 patch；可向后兼容的可选字段或新能力升 minor；删除/改名、字段类型或语义变化、枚举数值变化和帧布局变化升 major。0.x 包发生 breaking change 时至少升 minor，并按 breaking migration 管理；稳定后应进入 1.x。
4. **消费者按需升级。** 只在实际需要新能力的 Client、Server、Gateway 或 Realtime 仓库中更新 `Directory.Packages.props`，随后重新生成锁文件并核对 content hash；没有需求的消费者继续锁定旧版本。
5. **兼容窗口内分阶段部署。** 生产者先做到旧/新消费者都可读，消费者再逐个升级；认证缓存、命令能力或事件 schema 的旧路径必须等所有实例升级并超过最大 TTL/保留窗口后才能删除。
6. **合并门禁。** 每个受影响仓库必须独立执行 locked restore、Release build、契约测试和自身行为测试；跨进程变更再补一条真实 Redis/NATS/HTTP/TCP 联调。发布记录保留包版本、hash、消费者版本和回滚顺序。
