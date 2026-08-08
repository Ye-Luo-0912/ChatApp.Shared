# 共享组件边界

## 可以进入共享仓库

一个类型只有在满足以下条件时才应进入 `ChatApp.Shared`：

1. 它是两个或更多进程共同遵守的序列化、协议或缓存 schema；或者它是含义稳定且跨域一致的基础值对象。
2. 它不包含数据库、消息中间件、网络连接、UI、日志或依赖注入实现。
3. 它的 wire 字节、字段名称、枚举数值、大小写、null 语义和时间单位可以由兼容性测试固定。
4. 它的变更可以按语义化版本管理，并支持消费者滚动升级。

## 不进入共享仓库

- Server 领域实体、EF 映射、仓储和业务服务。
- Client UI 状态、本地 SQLite 模型和平台类型。
- Gateway 的限流、lane、路由与连接生命周期实现。
- Redis/NATS 的具体 client、key 扫描、订阅循环和重试策略。
- 最大载荷、超时、限流等可由端点独立调整的运行策略。
- 仅仅同名但所有权、生命周期或安全语义不同的工具类。
- 在不同传输边界上恰好字段相似的 DTO。

## 依赖方向

```text
Auth.Contracts
Contracts.Http
Protocol.Tcp <- Protocol.Tcp.Json
```

箭头表示“可按真实类型需要依赖”，不是必须提前添加引用。任何 adapter/实现项目都只能依赖 contracts，contracts 不反向依赖 adapter。

当前无 `Shared.Primitives` 项目。无两个以上语义稳定消费者时，不创建 speculative primitives 包或空 marker；字段相同也不能替代语义和所有权验证。

## 兼容性与 namespace 迁移

契约迁移必须先固定并保持跨进程兼容面：wire bytes、JSON 属性、枚举/命令数值、缓存键值 schema、null 语义与时间单位。共享包中的 golden tests 是这些兼容面的基线。

namespace 和程序集身份属于源码/二进制 API，而不是 wire contract。首批抽取将部分类型有意迁移到 `ChatApp.Shared.*` namespace，因此是明确的源码 breaking migration：所有直接源码消费者必须在同一批次改用新 namespace，或在消费者侧提供临时 alias/facade。不能把 wire 兼容误写成旧 namespace 或旧程序集 ABI 兼容。

后续公共 API 的重命名仍需独立评审和版本策略；wire contract 的任何不兼容修改必须建立新 fixture、明确版本协商，并提供消费者升级路径。

## Realtime 包所有权

`ChatApp.Realtime.Contracts` 已存在 2.x 包谱系和真实消费者。本仓不新建同名 0.x 包，也不发布空的 `ChatApp.Realtime.Client.Abstractions`。实时契约后续如迁入统一仓库，必须接续现有 2.x `PackageId`、版本历史与兼容策略，并先确认原包发布责任的交接，避免两个仓库同时产出同一 PackageId。

## 迁移门槛

迁入真实类型前，应先在原消费者之间建立：

- TCP 帧、命令号与协议版本 golden bytes/value tests；
- HTTP JSON/OpenAPI fixtures；
- NATS envelope JSON fixtures；
- Redis key/value writer-reader 交叉测试；
- 包身份、版本和 namespace 消费者迁移清单。

没有真实契约与 fixtures 的候选包不创建项目；已经存在的包必须以 golden/交叉兼容测试守卫后才可发布。

## 自动守卫

`ChatApp.Shared.ArchitectureTests` 会拒绝：

- `src` 项目中的外部 NuGet、Framework 或手工程序集引用；
- 指向 `src` 目录之外的项目引用；
- 非 `net10.0` 项目或重复/缺失的包与程序集身份；
- 编译输出中对非 BCL、非 ChatApp 共享程序集的依赖。

当未来需要 NATS、EF 或 Redis adapter 时，应新建独立实现项目，并建立单独的架构规则；不要放宽 contracts 的 BCL-only 守卫。
