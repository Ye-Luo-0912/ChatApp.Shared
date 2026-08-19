# ChatApp Binary V1

> 状态：首个格式 ID 为 `chatapp-bin-v1`。旧实验实现从未被 Client、Gateway、Server 或 Realtime 消费，已直接废弃，不提供兼容层。运行时协商仍关闭，当前路径继续使用 JSON。

## 决策与边界

二进制底座分为两层：

```text
ChatApp.Binary.Core
        ^
        |
ChatApp.Protocol.Tcp.Binary <--- ChatApp.Protocol.Tcp.Binary.Generator
                                      (build-time decode only)
```

- Core 只拥有 limits、status、wire primitives、连续/分段 cursor 和 static-generic 编解码入口；不认识 TCP、DTO、Roslyn、DI、池或网络。
- TCP Binary 只拥有格式身份和 schema attributes；真实 DTO 的字段表与手写 encoder 留在明确的 schema/消费程序集，不在底座预先猜测。
- Generator 只生成 decoder 和 schema diagnostics，不生成 encoder，也不进入运行时依赖图。
- Client/Gateway 拥有协商、session format、frame pool、fanout 分组、回退和 buffer 生命周期。

## Wire V1

- 外层仍使用既有 10-byte TCP frame；payload 不重复写格式版本。
- key 为 `(fieldNumber << 3) | wireType` 的最短 unsigned varint。
- wire type 首版仅支持 varint `0`、fixed64 `1`、length-delimited `2`、fixed32 `5`。
- signed integer 使用 ZigZag；unsigned、bool 和整数底层 enum 使用 unsigned varint。
- fixed32/fixed64 为 little-endian，`float`/`double` 保留原始 IEEE bits。
- string 使用无 BOM 的严格 UTF-8；bytes/string 共用 length-delimited envelope。
- null 由字段缺失表示；present 的默认值、空 string 与空 bytes 仍可编码。
- encoder 必须按字段号严格递增；decoder 同样要求严格递增，重复或倒序字段 fail-closed。
- 未知字段只在 limits 内跳过；未知 enum 保留其底层整数值。
- tag、value、length 必须 canonical；截断、溢出、错误 wire type、非法 UTF-8、超限和尾随非法数据都返回稳定 status，不以热路径异常表示普通输入错误。
- nested/list 已按真实 `MessageHistoryItem` 与 `SyncBootstrapRequest` 冻结：nested message 使用 length-delimited wire type，repeated scalar/nested item 使用同一 field number 的连续 unpacked occurrences；packed collection 本批次仍未启用。
- nested/list 的当前预算为 `MaxNestingDepth=8`、每个集合字段 `MaxCollectionElements=256`、每个 owning decoder `MaxMaterializedBytes=512 KiB`；每个 nested body 复用 `MaxFieldBytes`，未知 nested 字段仍只在 child limits 内跳过。
- nullable list 的缺失与空集合都不发字段并按 `null` 解码；重复字段只对声明为 repeated 的字段开放，普通字段重复、倒序、部分 nested 截断和任一预算超限都 fail-closed。

## API 与内存所有权

编码器是普通手写 `readonly struct`，实现 Core 的 static-abstract encoder contract。主入口接收 caller-owned `Span<byte>`，在一个受控 `fixed` 生命周期内单遍写入；成功返回精确 `written`，失败返回 `written=0`，目标内容未定义且调用方不得发布。没有 reflection、dynamic、表达式树、运行时 type registry、codec 对象或生成 encoder。

Core 不把 `IBufferWriter<byte>` 作为首版热路径。旧探针中慢的不是该接口本身，而是“先 Measure、后 Write”的两遍 schema 遍历；小 payload 下二次属性读取、UTF-8 长度扫描和 adapter 状态检查超过了减少 `GetSpan/Advance` 的收益。若后续增加 writer adapter，必须显式命名为 measured/convenience 路径，与单遍 Span 数据分开报告。

decoder 由 generator 生成静态代码：连续输入使用 native-pointer bounded cursor；`ReadOnlySequence<byte>` 使用 Core 原生逐段 cursor，不合并整包、不回退旧 reader。owning decode 允许 DTO、string、byte[] 这些最终对象分配，但不得构建临时对象图；未来 borrowed view 必须用 `ref struct`/`scoped` 限制生命周期，不得跨 `await` 或在输入 owner 归还后使用。

原生指针只允许出现在 Core 审批的极小文件中：pin 前完成容量/overflow 检查，指针不得离开 `fixed`、进入字段、跨线程/回调/异步边界或指向已归还的池内存。公共 API 不暴露 `byte*`，不使用 `System.Runtime.CompilerServices.Unsafe`。portable/native 必须逐字节、逐值、逐 status 一致；没有稳定收益的 pointer 分支应删除。

## TODO 与接手顺序

### `BIN-BASE-1`：原子替换底座（代码已完成）

- [x] 删除旧 reader/writer、对象型 codec、重复 TCP limits/error 映射、Measure 主契约、旧 format ID、兼容 golden 与 legacy probe；`src`/`generators` 已无可调用旧 runtime。
- [x] Core 完成单遍 Span encoder、连续 decoder 与原生 `ReadOnlySequence<byte>` decoder；跨段 varint/fixed/length/string/bytes 不复制整包。
- [x] Generator 只输出两个静态 decode 入口（Span/Sequence）和 private static-generic decoder；生成源码不包含 Encode、`IBufferWriter`、旧 reader 或对象型 codec。
- [x] 用覆盖 17 种字段形态的独立 fixture 建立新 golden；固定字段号、presence、raw IEEE、unknown enum、strict order、required 与 limits 行为。fixture 是底座验证，不代表真实业务 schema 已冻结。
- [x] 连续与逐字节分段的成功值/精确 status differential、每个 payload 前缀截断、non-canonical/overflow/wrong-wire/invalid-UTF8/unknown/duplicate/out-of-order 与 deterministic fuzz 已覆盖。
- [x] encoder-only 项目完全不引用 generator/analyzer；复用 caller buffer 时 encode 为零额外分配，失败不发布且 `written=0`。
- [x] locked restore、Release 0 warning/error、全套 `171/171` 与架构边界均已通过。

完成标准：仓内只有一套 binary runtime；新 ID/golden/API 一致；生产 format 仍不可协商；下一位 Agent 无需理解或维护旧实现。

当前 Windows x64/.NET 10 三进程方向性微探针（每进程 7 轮中位数）为：32 B body / 38 B payload 单遍 encode `21.2–23.8 ns`、`0 B/op`，连续 owning decode `29.2–31.0 ns`、`96 B/op`；1 KiB body / 1031 B payload encode `38.3–40.2 ns`、`0 B/op`，连续 owning decode `68.8–79.4 ns`、`1088 B/op`。分段 owning decode 分别为 `93.3–107.2 ns` 与 `149.8–161.2 ns`，不合并整包。该探针仅覆盖三字段平面 schema；owning 分配是 DTO/byte[] 最终对象，不能外推为真实 ChatMessage 或业务收益结论。

### `BIN-SCHEMA-2`：真实 DTO 接入

- [x] 首轮选择 canonical TCP `ClientHello` 与扁平 `MessageHistoryRequest`；字段号由 `tests/ChatApp.Protocol.Tcp.Binary.Tests` 和独立 Probe 的消费者侧 schema 持有，逐字段确认 null 以字段缺失表示、时间为 Unix milliseconds、默认值不隐式补发，编码器为普通源码，解码器由 generator 生成。`ClientHello/ServerHello` 仍只在离线证据中验证，生产握手继续 JSON。
- [x] 两个 schema 均保存 bytes/hash/payload size、连续/分段 decode corpus、ns/op/B/op 和 JSON 对照；禁止把相似的 Realtime/HTTP DTO 当成同一 wire 类型。
- [x] `RealTcpBinarySchemaTests` 已覆盖 string-heavy、全默认、最大合法和畸形输入；独立 Probe 已记录这些 corpus 的 payload/hash、连续/分段 decode、ns/op/B/op。当前 canonical TCP DTO 没有 bytes 字段，因此不人为扩展 schema；bytes-heavy 由 Core 的 1 KiB bytes workload 覆盖并单独标注。单遍编码保持 0 B/op，无需增加指针复杂度。
- [x] nested/list 已以 `MessageHistoryItem` 与 `SyncBootstrapRequest` 统一设计；Core/Generator 支持 unpacked repeated scalar/nested decode、单遍 nested encode、depth/element-count/materialized-byte budgets。packed collection 保留为后续独立 schema 变更。
- [x] 第一批握手后 payload 已补齐消费者侧 schema：`GoAway`、`ResumeResponse`、`ProtocolErrorFrame`、会话列表 flat DTO、cursor reset、`MessageHistoryResponse` 与 `SyncBootstrapResponse`；每项均有普通源码 encoder、generated decoder、golden/hash、分段 round-trip、malformed/limits 覆盖。response 还覆盖单个 nested cursor、repeated nested history/relationship catch-up 与三层同步拓扑。
- [ ] 先收口当前已展开的控制帧、会话、History/Sync response 批次；每个已纳入命令必须有普通源码 encoder、generated decoder、limits、golden 和恶意输入测试。其余命令按关系、语音消息和通话的真实需要分批补齐，不用“先覆盖全目录”阻塞产品功能。当前帧头没有逐帧格式位，因此连接级 binary 接入仍必须等待实际运行所需目录完整。

当前批次完成标准：nested/list 规则由真实模型驱动，已纳入 payload 的 bytes/golden/limits 完整，离线基准能说明体积与 CPU/分配；未覆盖命令保留明确清单，不能用部分覆盖宣称连接级格式可用。

当前 BIN-SCHEMA-2 证据：`RealTcpBinarySchemaTests` `21/21` 通过，覆盖四组 golden/hash、null-by-absence、Unix milliseconds、strict order、limits、默认值、字符串密集、最大合法、畸形输入、连续 decode 和逐字节分段 decode。独立 Windows x64/.NET 10 Release Probe 的 canonical fixture 仍为：`ClientHello` binary `42 B`、JSON `151 B`；binary encode/decode `64.3/93.0 ns/op, 0/152 B/op`，JSON encode/decode `206.9/1118.8 ns/op, 176.3/152 B/op`。`MessageHistoryRequest` binary `66 B`、JSON `199 B`；binary encode/decode `138.2/157.4 ns/op, 0/272 B/op`，JSON encode/decode `306.0/556.7 ns/op, 224.2/272 B/op`。新增 corpus：ClientHello default `6/54 B`、string-heavy `1305/2184 B`、max-legal `2078/3217 B`；MessageHistoryRequest default `2/12 B`、string-heavy `2077/2974 B`、max-legal `3452/4613 B`（均为 binary/JSON）。Probe 同时记录了每个 corpus 的 binary SHA-256、连续/分段 decode 和 ns/op/B/op；畸形 corpus 覆盖 invalid UTF-8 与 truncation。当前 canonical TCP DTO 没有 bytes 字段，bytes-heavy 只由 Core 1,024-byte bytes workload 覆盖，不猜测业务字段。分段 decode 仍只作为安全/语义 coverage，不宣称性能收益。Binary golden SHA-256：`ClientHello AA2A83D7D5F61D3522FAEACF3091773D348325D816D4FBF03EC9E2EBD386B2AC`；`MessageHistoryRequest BCF897F4F71D3912D6159395FDBE0F1D783BB3DC4B03EA6482F00D86C4163AD0`。

### `BIN-INTEGRATION-3`：双端开发接入（支撑项）

- [ ] Client/Gateway 同时接入 JSON 与 binary codec；`ClientHello/ServerHello` 始终 JSON，完整握手后连接级固定一种格式，不 sniff、不在连接中途切换。
- [ ] Resume 首版保持 JSON；未来支持 binary resume 时必须先发 JSON `ServerHello` 再切换，不暗改旧顺序。
- [ ] Gateway session 保存不可变 negotiated format；fanout 按格式最多分组编码一次并共享 frame，禁止逐 session 序列化。
- [ ] 完成 malformed/oversize/fuzz、JSON fallback、GoAway/重连、混合格式 fanout 与 80/320/640 msg/s 的 5–20 分钟短测；比较 payload、Gateway CPU、allocation/msg、GC 与 p95/p99。
- [ ] 本项在关系、语音消息和通话主链路之后执行。只有 payload 体积或 codec CPU/分配有稳定收益、零漏投/重复且 p99 无不可解释回退时，才改变 JSON 默认路径。

`BIN-INTEGRATION-3` 的双端接入仍为支撑项，默认路径保持 JSON。本阶段已完成的是接入所需的共享契约层与寄存器（离线验证能力）：

- [x] 新增生产 schema 消费程序集 `ChatApp.Protocol.Tcp.Binary.Schemas`（`src/` 内第 7 个契约包，net10.0，packable）：`ClientHello`/`ServerHello`/`GoAway`/`ResumeResponse`/`ProtocolErrorFrame`/`MessageHistoryRequest`/`MessageHistoryCursor` 的真实字段号、手写 encoder，decoder 由 generator 构建期生成；`ClientHello/ServerHello` 仍只在离线证据中验证，生产握手继续 JSON。
- [x] 实现命令→schema 寄存器 `TcpBinaryWireCodec`：`TryDecode(PacketCommand, Span/ReadOnlySequence, BinaryLimits)` 按命令分发到对应 schema，未覆盖命令、畸形/超限覆盖命令一律 fail-closed（`SchemaNotCovered`/`DecodeFailure`）；`IsNegotiatedPayloadFormat` 仅识别精确 `chatapp-bin-v1`，用于协商识别。
- [x] 新增 `TcpBinaryWireCodecTests`（7 项）：协商识别、控制/历史/错误帧往返、分段解码、未覆盖 fail-closed、畸形 fail-closed、超限 fail-closed。Shared 全量回归：Binary.Core 37 / EncoderOnly 1 / Protocol.Tcp.Binary 38 / Generator 15 / Architecture 110 全部通过，`dotnet build ChatApp.Shared.slnx -c Release` 0 警告 0 错误。架构边界补充：契约包允许 `OutputItemType="Analyzer"` 的 ProjectReference 指向 generator。

完成标准（沿用）：关闭开关可让新连接恢复 JSON，已协商连接可排空/重连；未达门槛时只保留底座和离线验证能力。Client/Gateway 的实际双 codec 接入、混合格式 fanout 与 5–20 分钟短测仍待主链路推进。

## 格式演进规则

旧实验包与格式不构成 `chatapp-bin-v1` 的兼容承诺；format identity 独立于仓库或包版本。任何不兼容 wire 变更必须分配新的 exact format ID、保留被删除字段号，并重新完成 golden、双端 fallback 和短时故障验证。
