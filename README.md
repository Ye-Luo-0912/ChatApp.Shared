# ChatApp.Shared

`ChatApp.Shared` 是 ChatApp 各应用之间共享的跨进程契约仓库。它只承载可版本化的 wire、JSON、缓存 schema 与稳定基础类型，不承载 Redis、NATS、Entity Framework Core、ASP.NET Core、UI 或业务实现。

## 当前源码模块

| 模块 | 职责 | 内部依赖 |
| --- | --- | --- |
| `ChatApp.Auth.Contracts` | AccessToken Redis 键、缓存值 schema、账户状态与 JSON metadata | 无 |
| `ChatApp.Binary.Core` | 与传输无关的单遍编码、有界连续/分段解码、limits/status 与受限 native-pointer fast path | 无 |
| `ChatApp.Contracts.Http` | Auth、好友、附件、会话 HTTP wire DTO 与 JSON metadata | 无 |
| `ChatApp.Protocol.Tcp` | TCP 帧/命令/能力/错误，以及 Client↔Gateway 的历史、同步和附件 wire DTO | 无 |
| `ChatApp.Protocol.Tcp.Json` | TCP DTO 的 source-generated JSON metadata 与统一序列化策略 | `ChatApp.Protocol.Tcp` |
| `ChatApp.Protocol.Tcp.Binary` | `chatapp-bin-v1` 格式身份与 TCP binary schema attributes | `ChatApp.Binary.Core` |
| `ChatApp.Protocol.Tcp.Binary.Generator` | 只生成 decoder/diagnostics 的 build-time incremental generator | 无运行时依赖 |

六个 runtime 模块保持 .NET 10、BCL-only；Generator 使用 netstandard2.0，Roslyn 只在构建期使用且不进入消费者运行时。二进制底座以 [`docs/BINARY-PROTOCOL.md`](docs/BINARY-PROTOCOL.md) 为唯一规范：encoder 是单遍普通源码，generator 只生成连续/分段 decoder。旧实验实现已直接废弃；`chatapp-bin-v1` 仍处于开发验证，运行路径继续使用 JSON。

当前没有 `ChatApp.Shared.Primitives` 项目：尚无两个以上语义稳定的消费者，暂不建立 speculative primitives 包。未来满足共享门槛时再以真实类型、消费者矩阵和兼容性测试共同引入，不保留空 marker。

## Realtime 包谱系

`ChatApp.Realtime.Contracts` 已在现有 Realtime 工程中形成独立谱系，本仓库不创建同名替代模块。当前 NATS 集成与 EF Outbox 模型继续由 Realtime 仓库拥有；后续提取实时契约时必须沿用既有类型所有权，不能从本仓另起重名实现。

## 兼容性承诺

本次迁移保持的是跨进程契约兼容性：

- TCP Magic、帧头布局、命令号、协议版本和枚举数值；
- JSON 字段名、大小写、null 行为和数值表示；
- Auth 刷新凭据、过期时间，好友分页/envelope，以及附件上传 header；
- AccessToken Redis 键和值 schema。

类型迁移到 `ChatApp.Shared.*` 命名空间是有意的源码 breaking migration，并不承诺旧程序集或旧 namespace 的二进制/源码 ABI。所有直接引用这些类型的源码消费者应在同一迁移批次更新；如需错峰开发，由消费者临时提供 alias/facade，同时仍以 Shared wire contract 为唯一事实源。

## 依赖边界

- `src` 下所有项目必须保持 BCL-only：不得添加外部 `PackageReference`、`FrameworkReference` 或手工程序集 `Reference`。
- 共享项目之间只在真实类型使用发生时添加 `ProjectReference`，不得为未来可能的需要预先耦合。
- Redis/NATS/EF/ASP.NET/日志等实现放在独立 adapter 包或原业务仓库中，不进入 contracts。
- TCP DTO、HTTP DTO、NATS payload 分开维护；字段相似不代表它们是同一 wire contract。
- 每个项目拥有独立的 `PackageId`、`AssemblyName` 与 `RootNamespace`。
- 最大帧长、超时、限流等端点策略不进入固定帧布局常量。

更详细的判定和演进规则见 [docs/BOUNDARIES.md](docs/BOUNDARIES.md)，当前开发路线见 [docs/NEXT-STAGE.md](docs/NEXT-STAGE.md)，已完成迁移记录见 [docs/MIGRATION-ROADMAP.md](docs/MIGRATION-ROADMAP.md)。

## 本地验证

```powershell
dotnet restore .\ChatApp.Shared.slnx
dotnet build .\ChatApp.Shared.slnx -c Release --no-restore
dotnet test .\ChatApp.Shared.slnx -c Release --no-build
```

架构测试会检查源项目依赖、唯一项目身份、目标框架和编译后程序集引用。当前阶段以源码、契约测试和真实消费者联调为准。
