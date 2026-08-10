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

## 0.4.1 本地发布候选证据（2026-08-10）

六个候选包由同一次 Release pack 生成，尚未发布到共享 feed；Client 与 Gateway 已用 locked restore 消费同一份 `ChatApp.Protocol.Tcp`/`.Json` 包并完成 Release 构建。相同版本不得用后续构建覆盖，若源码、README 或包元数据发生变化，必须重新升版本并生成新清单。

| 候选包 | SHA-256 |
| --- | --- |
| `ChatApp.Auth.Contracts.0.4.1.nupkg` | `543268ECC0764ED6FFF1DD98EB25CDA5D0B2C2F9C9EF48B45A663E0490CE951F` |
| `ChatApp.Contracts.Http.0.4.1.nupkg` | `30840595E295DFFBAAB4D7E3BE2C4940E74475C4EB0BD8F256C31468878F7F16` |
| `ChatApp.Protocol.Tcp.0.4.1.nupkg` | `D8CB84F024A82D92BF33BA51DB6F596375CE767DF4B734A802DF07083239F9DF` |
| `ChatApp.Protocol.Tcp.Json.0.4.1.nupkg` | `1A99C47A51DFC42C72460FE7F995E13EDB758E96CFCEF8C171056C79CA6A08CE` |
| `ChatApp.Protocol.Tcp.Binary.0.4.1.nupkg` | `0F8436483217B38E5641EA9CE6E862675BF12A6571C2EDC8788031BE89E4A643` |
| `ChatApp.Protocol.Tcp.Binary.Generator.0.4.1.nupkg` | `543BB4EE3C1F96ACD637784100A920D36EEFEDC9F356E370673D3ABE22AB5EE5` |

当前跨仓 fixture SHA-256：Shared JSON golden `DBACEE52E578C786DF3EED753F5B4DADB51EEE3F82F0AA4E04A308E061334D1E`，Shared old/new matrix `974B6AE15B1A9BA5060C1F5A8531210F8CFECEE5FB17C86641363A5F31968B25`，Gateway producer/compatibility fixture `DB0288078E293240F69B243BDB924AA719E11A533B35C77D3C45A812949C28CA`，Client consumer/compatibility fixture `D05BAE10DEADF4542071B1B77C0247F797161953ED8110FB67F2213BDFAF3E5F`。最终 locked build 的 `ChatApp.TcpGateway.dll`/`Chat_App.dll` SHA-256 分别为 `2B90FD4FC467780C58DD0364ECEA50EA6CC7B86CD7D6F4C38095739E08B60F89` 与 `6CDF4A0ED9CF1B523491764F6758058FB9B4B141BE0DECEFF4D36676F284CA4D`；这些二进制 hash 仅绑定本次本地候选，任何重新构建都应重新记录。

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
