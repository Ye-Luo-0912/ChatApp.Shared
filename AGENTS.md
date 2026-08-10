# ChatApp.Shared agent 约定

修改契约前先核对生产者、消费者、字段语义、时间单位、null 行为和兼容窗口，不因字段相似就合并类型。

这里只共享稳定 wire/schema、枚举和 source-generated codec metadata；不放连接、缓存 client、数据库、重试、UI 或业务实现。变更必须带 golden/交叉兼容测试并按语义化版本发布。

以正确/安全、可维护、可测量的性能和真实复用为顺序。验证按聚焦契约/golden 测试 → Release 构建 → 消费者短时联调推进；阶段长测和发布 soak 由消费者在功能冻结后执行。下一阶段与接手状态见 `docs/NEXT-STAGE.md`。
