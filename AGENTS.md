# ChatApp.Shared agent 约定

修改契约前先核对生产者、消费者、字段语义、时间单位、null 行为和兼容窗口，不因字段相似就合并类型。

这里只共享稳定 wire/schema、枚举和公共编解码契约；不放连接、缓存 client、数据库、重试、UI 或业务实现。Binary V1 的 encoder 必须是无生成器依赖的普通源码，generator 仅生成 decoder，native pointer 只留在 Core 的有界内部 fast path；旧实验实现无兼容承诺。详细边界见 `docs/BINARY-PROTOCOL.md`。变更必须带 golden/消费者测试并按语义化版本演进。

以正确/安全、功能链路完整、可维护、可测量的性能和真实复用为顺序。验证按聚焦契约/golden 测试 → Release 构建 → 消费者短时联调推进。下一阶段与接手状态见 `docs/NEXT-STAGE.md`。
