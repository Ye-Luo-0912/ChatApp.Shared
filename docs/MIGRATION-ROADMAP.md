# 共享契约迁移状态与发布路线

## 已完成状态（Shared 0.2.0 / Realtime 2.x）

| 边界 | 唯一契约源 | 消费者 | 完成状态 |
| --- | --- | --- | --- |
| AccessToken Redis key/value | `ChatApp.Auth.Contracts` `0.2.0` | Server writer、TCP Gateway reader | 两端直接使用同一缓存 schema；Server 的领域投影通过显式 mapper 转为共享记录 |
| Auth、好友、附件、会话 HTTP DTO | `ChatApp.Contracts.Http` `0.2.0` | Client、Server Host | 两端直接使用共享 wire DTO；Server Core 保持 BCL-only，并在 Host 边界显式映射 |
| TCP 帧头、`PacketCommand`、握手/恢复 DTO、能力位、错误码 | `ChatApp.Protocol.Tcp` `0.2.0` | Client、TCP Gateway | 本地重复命令枚举已删除；golden bytes、帧编解码和握手契约测试守卫兼容性 |
| TCP JSON metadata | `ChatApp.Protocol.Tcp.Json` `0.2.0` | TCP Gateway、协议兼容测试 | 提供统一 source-generated JSON 入口；消费者自有 context 通过交叉序列化测试校验 |
| Realtime DTO 与集成接口 | `ChatApp.Realtime.Contracts` `2.3.0`、`ChatApp.Realtime.Integration` `3.0.0` | RealtimeServices、Server、TCP Gateway | Contracts 延续既有 2.x 谱系；Integration 因公开 Outbox 类型迁出而按 breaking change 升至 3.x；消费者使用包引用，不依赖 sibling 源码目录 |
| Realtime EF Outbox 模型与映射 | `ChatApp.Realtime.Outbox.EntityFrameworkCore` `1.0.0` | Server | 从 NATS Integration 包剥离；只有需要 EF Outbox 的宿主引用，Gateway/PushWorker 不再传递依赖 EF Core |
| 通用基础类型 | 暂不建立项目 | 暂无两个以上语义稳定消费者 | 已删除空 `Primitives` marker，避免无实际类型的预设依赖 |

四个业务仓库现在都能仅依赖版本化 NuGet 包独立 restore/build，不需要相邻仓库源码。Shared、Client、Server、Gateway 与 RealtimeServices 均生成并校验 `packages.lock.json`；CI 和服务容器使用 locked restore。相同 PackageId/Version 的 `.nupkg` 与锁文件 content hash 在所有消费者中保持一致。

Client 与 Gateway 已删除本地 `PacketCommand` 枚举。源码中的 alias 只用于缩短名称，公开签名与编解码实际类型都是 `ChatApp.Shared.Protocol.Tcp.PacketCommand`。历史短名称没有进入共享枚举。

## 独立构建验证快照（2026-08-06）

| 仓库 | 锁定还原 / Release 构建 | 测试结果 |
| --- | --- | ---: |
| `ChatApp.Shared` | PASS，0 warning / 0 error | 25 passed |
| `Chat_App` | PASS，0 warning / 0 error | 245 passed |
| `ChatApp.Server` | PASS，0 warning / 0 error | 238 passed / 3 external failure-injection skipped |
| `ChatAppTCP_Server` | PASS，0 warning / 0 error | 516 passed / 1 Redis integration skipped |
| `ChatApp.RealtimeServices` | PASS，0 warning / 0 error | 285 passed；ACK timing focused 4 passed |

跳过项必须在需要发布相应适配器时由带真实依赖的 CI/预发布环境补跑，不能把 skipped 当作已覆盖。

## 发布与部署顺序

1. Shared CI 对四个 `0.2.0` 包执行一次构建、测试、打包并产出 SHA-256 清单；Realtime 发布流水线分别产出 `Contracts 2.3.0`、`Integration 3.0.0` 与 `Outbox.EntityFrameworkCore 1.0.0`。审核后只把这些不可变候选发布到内部 NuGet feed；仓库内 `packages/` 只作为离线、CI 和迁移期的可复现来源。
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
