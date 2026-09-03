using System.Text.Json;
using ChatApp.Contracts.Http;
using ChatApp.Contracts.Http.Auth;
using ChatApp.Contracts.Http.Common;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

/// <summary>
/// ENDPOINT-TLS-1 契约基线：端点描述 DTO + 校验策略的 golden、旧 endpoint 默认值兼容、
/// 不安全组合拒绝与未知枚举/可选字段演进行为。
/// </summary>
public sealed class EndpointSecurityContractTests
{
    private static EndpointDescriptor? Deserialize(string json) =>
        JsonSerializer.Deserialize(json, HttpContractsJsonSerializerContext.Default.EndpointDescriptor);

    private static string Serialize(EndpointDescriptor value) =>
        JsonSerializer.Serialize(value, HttpContractsJsonSerializerContext.Default.EndpointDescriptor);

    // ---- JSON golden 往返 ----

    [Fact]
    public void EndpointDescriptor_RoundTripsGoldenJson()
    {
        var value = new EndpointDescriptor
        {
            Scheme = EndpointScheme.TcpTls,
            Host = "gw.chatapp.internal",
            Port = 7443,
            SniTargetHost = "gw.example.com",
            MinimumTls = MinimumTlsPolicy.Tls12OrAbove
        };

        string json = Serialize(value);

        Assert.Equal(
            """{"scheme":4,"host":"gw.chatapp.internal","port":7443,"sniTargetHost":"gw.example.com","minimumTls":1}""",
            json);

        EndpointDescriptor? roundTripped = Deserialize(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(value.Scheme, roundTripped.Scheme);
        Assert.Equal(value.Host, roundTripped.Host);
        Assert.Equal(value.Port, roundTripped.Port);
        Assert.Equal(value.SniTargetHost, roundTripped.SniTargetHost);
        Assert.Equal(value.MinimumTls, roundTripped.MinimumTls);
    }

    [Fact]
    public void EndpointDescriptor_OptionalFields_AreOmittedOnWireAndAbsentOnRead()
    {
        // 省略可空字段 = 消费者沿用旧行为（端口取 scheme 默认、SNI 取 Host），wire 上不出现。
        var value = new EndpointDescriptor
        {
            Scheme = EndpointScheme.Https,
            Host = "api.example.com",
            MinimumTls = MinimumTlsPolicy.Tls13Only
        };

        Assert.Equal(
            """{"scheme":2,"host":"api.example.com","minimumTls":2}""",
            Serialize(value));

        EndpointDescriptor? roundTripped = Deserialize("""{"scheme":2,"host":"api.example.com","minimumTls":2}""");

        Assert.NotNull(roundTripped);
        Assert.Null(roundTripped.Port);
        Assert.Null(roundTripped.SniTargetHost);
    }

    // ---- 旧 endpoint 默认值兼容矩阵 ----

    [Theory]
    [InlineData(EndpointScheme.TcpTls, "10.0.0.8", 7000)]
    [InlineData(EndpointScheme.Tcp, "gw.local", 7000)]
    [InlineData(EndpointScheme.Https, "gw.example.com", 443)]
    public void LegacyServerEndpoint_MapsToDescriptor_WithoutTighteningLegacyBehavior(
        EndpointScheme consumerChosenScheme, string legacyHost, ushort legacyPort)
    {
        // 旧 wire 形状只有 host/name/port，TLS 与版本由客户端自行决定。
        string serverJson = $$"""{"host":"{{legacyHost}}","name":"cn-1","port":{{legacyPort}}}""";
        LoginResponse? login = JsonSerializer.Deserialize(
            $$"""{"isSuccess":true,"server":{{serverJson}}}""",
            HttpContractsJsonSerializerContext.Default.LoginResponse);

        Assert.NotNull(login);
        Assert.NotNull(login.Server);
        ServerEndpoint server = login.Server.Value;

        // 消费者映射规则（唯一被 Shared 认可的旧形状语义）：
        // scheme 由消费者按其传输真相填写；port 显式保留；sni 留空 = 回退 Host；
        // minimumTls = None = 未声明最低版本 → 消费者保持既有平台默认，不被契约静默变严。
        var descriptor = new EndpointDescriptor
        {
            Scheme = consumerChosenScheme,
            Host = server.Host,
            Port = server.Port,
            MinimumTls = MinimumTlsPolicy.None
        };

        Assert.True(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.None, violation);
        Assert.Equal(legacyHost, descriptor.Host);
        Assert.Equal(legacyPort, descriptor.Port);
        Assert.Null(descriptor.SniTargetHost);
        Assert.Equal(MinimumTlsPolicy.None, descriptor.MinimumTls);
    }

    [Fact]
    public void LegacyServerEndpoint_IgnoresNewEndpointSecurityFields()
    {
        // 老消费者（0.3.0 旧形状 DTO）跳过未知字段：新服务端多发的端点安全字段不得破坏旧客户端。
        LoginResponse? login = JsonSerializer.Deserialize(
            """
            {"isSuccess":true,"server":{"host":"gw","name":"cn-1","port":7000,"scheme":4,"minimumTls":2,"sniTargetHost":"sni","someFutureField":true}}
            """,
            HttpContractsJsonSerializerContext.Default.LoginResponse);

        Assert.NotNull(login);
        Assert.NotNull(login.Server);
        ServerEndpoint server = login.Server.Value;
        Assert.Equal("gw", server.Host);
        Assert.Equal("cn-1", server.Name);
        Assert.Equal((ushort)7000, server.Port);
    }

    // ---- ServerEndpoint 加性扩展（ENDPOINT-TLS-1 消费者接入）----

    [Fact]
    public void ServerEndpoint_LegacyJson_LeavesNewFieldsNull()
    {
        // 新消费者读旧服务端：wire 只有 host/name/port → 新字段缺省 = null = 保持旧行为。
        LoginResponse? login = JsonSerializer.Deserialize(
            """{"isSuccess":true,"server":{"host":"gw","name":"cn-1","port":7000}}""",
            HttpContractsJsonSerializerContext.Default.LoginResponse);

        Assert.NotNull(login);
        Assert.NotNull(login.Server);
        ServerEndpoint server = login.Server.Value;
        Assert.Null(server.Scheme);
        Assert.Null(server.MinimumTls);
        Assert.Null(server.SniTargetHost);
    }

    [Fact]
    public void ServerEndpoint_NewFields_DeserializeFromAdditiveWire()
    {
        // 新消费者读新服务端：加性字段与 EndpointDescriptor 语义一一对应，未知枚举保留数值。
        LoginResponse? login = JsonSerializer.Deserialize(
            """
            {"isSuccess":true,"server":{"host":"gw","name":"cn-1","port":7000,"scheme":4,"sniTargetHost":"gw.example.com","minimumTls":1,"someFutureField":true}}
            """,
            HttpContractsJsonSerializerContext.Default.LoginResponse);

        Assert.NotNull(login);
        Assert.NotNull(login.Server);
        ServerEndpoint server = login.Server.Value;
        Assert.Equal(EndpointScheme.TcpTls, server.Scheme);
        Assert.Equal("gw.example.com", server.SniTargetHost);
        Assert.Equal(MinimumTlsPolicy.Tls12OrAbove, server.MinimumTls);
    }

    [Fact]
    public void ServerEndpoint_WithoutMetadata_SerializesLegacyWireShape()
    {
        // 新服务端未配置端点元数据（明文部署缺省）→ wire 与旧形状逐字节一致，旧客户端零影响。
        var login = new LoginResponse
        {
            IsSuccess = true,
            Server = new ServerEndpoint { Host = "gw", Name = "cn-1", Port = 7000 }
        };

        string json = JsonSerializer.Serialize(login, HttpContractsJsonSerializerContext.Default.LoginResponse);

        Assert.Contains("\"server\":{\"host\":\"gw\",\"name\":\"cn-1\",\"port\":7000}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerEndpoint_WithMetadata_SerializesAdditiveFieldsOnlyWhenSet()
    {
        var login = new LoginResponse
        {
            IsSuccess = true,
            Server = new ServerEndpoint
            {
                Host = "gw",
                Name = "cn-1",
                Port = 7000,
                Scheme = EndpointScheme.TcpTls,
                SniTargetHost = "gw.example.com",
                MinimumTls = MinimumTlsPolicy.Tls12OrAbove
            }
        };

        string json = JsonSerializer.Serialize(login, HttpContractsJsonSerializerContext.Default.LoginResponse);

        Assert.Contains(
            "\"server\":{\"host\":\"gw\",\"name\":\"cn-1\",\"port\":7000,\"scheme\":4,\"sniTargetHost\":\"gw.example.com\",\"minimumTls\":1}",
            json,
            StringComparison.Ordinal);

        // 单独只配置 scheme 时，其余字段不出现在 wire 上。
        var schemeOnly = new ServerEndpoint { Host = "gw", Name = "cn-1", Port = 7000, Scheme = EndpointScheme.Tcp };
        string schemeOnlyJson = JsonSerializer.Serialize(
            new LoginResponse { IsSuccess = true, Server = schemeOnly },
            HttpContractsJsonSerializerContext.Default.LoginResponse);

        Assert.Contains("\"server\":{\"host\":\"gw\",\"name\":\"cn-1\",\"port\":7000,\"scheme\":3}", schemeOnlyJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerEndpoint_MetadataFeedsEndpointDescriptorValidation()
    {
        // 消费者把新字段 1:1 装入 EndpointDescriptor 后必须能通过 EndpointPolicy 校验；
        // 明文 scheme + TLS policy 的不安全组合在组合起点就被拒绝。
        LoginResponse? login = JsonSerializer.Deserialize(
            """
            {"isSuccess":true,"server":{"host":"10.0.0.8","name":"cn-1","port":7000,"scheme":4,"sniTargetHost":"gw.example.com","minimumTls":1}}
            """,
            HttpContractsJsonSerializerContext.Default.LoginResponse);

        ServerEndpoint server = login!.Server!.Value;
        var descriptor = new EndpointDescriptor
        {
            Scheme = server.Scheme!.Value,
            Host = server.Host,
            Port = server.Port,
            SniTargetHost = server.SniTargetHost,
            MinimumTls = server.MinimumTls ?? MinimumTlsPolicy.None
        };

        Assert.True(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.None, violation);
    }

    // ---- 不安全组合拒绝矩阵 ----

    [Theory]
    [InlineData(EndpointScheme.Http, MinimumTlsPolicy.Tls12OrAbove)]
    [InlineData(EndpointScheme.Http, MinimumTlsPolicy.Tls13Only)]
    [InlineData(EndpointScheme.Tcp, MinimumTlsPolicy.Tls12OrAbove)]
    [InlineData(EndpointScheme.Tcp, MinimumTlsPolicy.Tls13Only)]
    public void PlaintextScheme_DeclaringTlsPolicy_IsRejected(EndpointScheme scheme, MinimumTlsPolicy minimumTls)
    {
        var descriptor = new EndpointDescriptor { Scheme = scheme, Host = "gw", Port = 7000, MinimumTls = minimumTls };

        Assert.False(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.PlaintextSchemeWithTlsPolicy, violation);
    }

    [Theory]
    [InlineData(EndpointScheme.Https, MinimumTlsPolicy.None, null)]
    [InlineData(EndpointScheme.Https, MinimumTlsPolicy.Tls12OrAbove, null)]
    [InlineData(EndpointScheme.Https, MinimumTlsPolicy.Tls13Only, null)]
    [InlineData(EndpointScheme.TcpTls, MinimumTlsPolicy.None, (ushort)7000)]
    [InlineData(EndpointScheme.TcpTls, MinimumTlsPolicy.Tls12OrAbove, (ushort)7000)]
    [InlineData(EndpointScheme.TcpTls, MinimumTlsPolicy.Tls13Only, (ushort)7000)]
    public void TlsScheme_WithDeclaredOrUndeclaredPolicy_IsValid(EndpointScheme scheme, MinimumTlsPolicy minimumTls, ushort? port)
    {
        var descriptor = new EndpointDescriptor { Scheme = scheme, Host = "gw", Port = port, MinimumTls = minimumTls };

        // Https/TcpTls + None = 旧行为（消费者平台默认），必须保持合法以免静默变严。
        Assert.True(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.None, violation);
    }

    [Theory]
    [InlineData(EndpointScheme.Http, (ushort)8080)]
    [InlineData(EndpointScheme.Tcp, (ushort)7000)]
    public void PlaintextScheme_WithoutPolicy_RemainsValidForLegacyDeployments(EndpointScheme scheme, ushort port)
    {
        var descriptor = new EndpointDescriptor { Scheme = scheme, Host = "127.0.0.1", Port = port };

        Assert.True(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.None, violation);
    }

    // ---- 默认端口规则 ----

    [Theory]
    [InlineData(EndpointScheme.Http, (ushort)80)]
    [InlineData(EndpointScheme.Https, (ushort)443)]
    [InlineData(EndpointScheme.Tcp, null)]
    [InlineData(EndpointScheme.TcpTls, null)]
    public void DefaultPortRules_AreStable(EndpointScheme scheme, ushort? expected)
    {
        Assert.Equal(expected, EndpointPolicy.GetDefaultPort(scheme));
    }

    [Theory]
    [InlineData(EndpointScheme.Tcp)]
    [InlineData(EndpointScheme.TcpTls)]
    public void NullPort_OnSchemeWithoutDefault_IsRejected(EndpointScheme scheme)
    {
        var descriptor = new EndpointDescriptor { Scheme = scheme, Host = "gw", MinimumTls = MinimumTlsPolicy.Tls12OrAbove };

        Assert.False(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.MissingPort, violation);
    }

    // ---- host / SNI 结构规则 ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingHost_IsRejected(string? host)
    {
        var descriptor = new EndpointDescriptor { Scheme = EndpointScheme.Https, Host = host!, Port = 443 };

        Assert.False(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.MissingHost, violation);
    }

    [Fact]
    public void HostLength_IsBoundedAt253Characters()
    {
        string valid = new string('a', 253);
        string tooLong = new string('a', 254);

        Assert.True(EndpointPolicy.TryValidate(
            new EndpointDescriptor { Scheme = EndpointScheme.Https, Host = valid, Port = 443 },
            out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.None, violation);

        Assert.False(EndpointPolicy.TryValidate(
            new EndpointDescriptor { Scheme = EndpointScheme.Https, Host = tooLong, Port = 443 },
            out violation));
        Assert.Equal(EndpointPolicyViolation.HostTooLong, violation);
    }

    [Theory]
    [InlineData("gw.example.com/path")]
    [InlineData("10.0.0.8:7000 ")]
    [InlineData("gw chatapp")]
    [InlineData("gw\\escape")]
    [InlineData("主机名")]
    public void Host_WithInvalidCharacters_IsRejected(string host)
    {
        var descriptor = new EndpointDescriptor { Scheme = EndpointScheme.Https, Host = host, Port = 443 };

        Assert.False(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.HostInvalidCharacters, violation);
    }

    [Theory]
    [InlineData("gw.example.com")]
    [InlineData("10.0.0.8")]
    [InlineData("[::1]")]
    [InlineData("[fe80::1%25eth0]")]
    public void Host_FormsCovered_DnsIpv4BracketedIpv6_AreValid(string host)
    {
        Assert.True(EndpointPolicy.TryValidate(
            new EndpointDescriptor { Scheme = EndpointScheme.Https, Host = host, Port = 443 },
            out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.None, violation);
    }

    [Fact]
    public void SniTargetHost_WhitespaceMeansAbsent_InvalidValueIsRejected()
    {
        // 空白 = 未覆盖（回退 Host），与消费者既有 TlsServerName 语义一致。
        Assert.True(EndpointPolicy.TryValidate(
            new EndpointDescriptor { Scheme = EndpointScheme.TcpTls, Host = "10.0.0.8", Port = 7000, SniTargetHost = "  " },
            out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.None, violation);

        Assert.False(EndpointPolicy.TryValidate(
            new EndpointDescriptor { Scheme = EndpointScheme.TcpTls, Host = "10.0.0.8", Port = 7000, SniTargetHost = "gw example" },
            out violation));
        Assert.Equal(EndpointPolicyViolation.SniHostInvalid, violation);

        Assert.False(EndpointPolicy.TryValidate(
            new EndpointDescriptor { Scheme = EndpointScheme.TcpTls, Host = "10.0.0.8", Port = 7000, SniTargetHost = new string('a', 254) },
            out violation));
        Assert.Equal(EndpointPolicyViolation.SniHostInvalid, violation);
    }

    // ---- 未知枚举 / 演进行为 ----

    [Fact]
    public void UnknownSchemeValue_DeserializesWirePreserved_ButFailsValidation()
    {
        // 版本演进行为定义：未知枚举值反序列化保留数值（前向兼容），但校验 fail-closed，
        // 绝不解释成任何"安全默认"（如 Https）。
        EndpointDescriptor? descriptor = Deserialize("""{"scheme":99,"host":"gw","port":7000}""");

        Assert.NotNull(descriptor);
        Assert.Equal((EndpointScheme)99, descriptor.Scheme);
        Assert.False(Enum.IsDefined(descriptor.Scheme));

        Assert.False(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.UnknownScheme, violation);
    }

    [Fact]
    public void UnknownTlsPolicyValue_DeserializesWirePreserved_ButFailsValidation()
    {
        EndpointDescriptor? descriptor = Deserialize("""{"scheme":4,"host":"gw","port":7000,"minimumTls":9}""");

        Assert.NotNull(descriptor);
        Assert.Equal((MinimumTlsPolicy)9, descriptor.MinimumTls);
        Assert.False(Enum.IsDefined(descriptor.MinimumTls));

        Assert.False(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.UnknownTlsPolicy, violation);
    }

    [Fact]
    public void MissingScheme_FailsClosedInsteadOfDefaulting()
    {
        // scheme 缺失反序列化为保留值 0：不得静默当作任何具体 scheme。
        EndpointDescriptor? descriptor = Deserialize("""{"host":"gw","port":7000}""");

        Assert.NotNull(descriptor);
        Assert.Equal((EndpointScheme)0, descriptor.Scheme);

        Assert.False(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.UnknownScheme, violation);
    }

    [Fact]
    public void UnknownOptionalFields_AreSkippedByNewConsumer()
    {
        // 新消费者同样必须跳过未来新增的可选字段（滚动升级的双向兼容）。
        EndpointDescriptor? descriptor = Deserialize(
            """{"scheme":2,"host":"api.example.com","minimumTls":2,"futureField":"x"}""");

        Assert.NotNull(descriptor);
        Assert.True(EndpointPolicy.TryValidate(descriptor, out EndpointPolicyViolation violation));
        Assert.Equal(EndpointPolicyViolation.None, violation);
    }

    // ---- 枚举数值稳定性 ----

    [Fact]
    public void EndpointSchemeValues_AreStable()
    {
        // 显式 \n 连接，避免源文件/平台的换行符差异影响 golden 比对。
        Assert.Equal(
            "Http=1\nHttps=2\nTcp=3\nTcpTls=4",
            string.Join(
                "\n",
                Enum.GetValues<EndpointScheme>().Select(value => $"{value}={(byte)value}")));
    }

    [Fact]
    public void MinimumTlsPolicyAndViolationValues_AreStable()
    {
        Assert.Equal(
            "None=0\nTls12OrAbove=1\nTls13Only=2",
            string.Join(
                "\n",
                Enum.GetValues<MinimumTlsPolicy>().Select(value => $"{value}={(byte)value}")));

        Assert.Equal(
            "None=0\nUnknownScheme=1\nUnknownTlsPolicy=2\nMissingHost=3\nHostTooLong=4\nHostInvalidCharacters=5\nSniHostInvalid=6\nMissingPort=7\nPlaintextSchemeWithTlsPolicy=8",
            string.Join(
                "\n",
                Enum.GetValues<EndpointPolicyViolation>().Select(value => $"{value}={(byte)value}")));
    }
}
