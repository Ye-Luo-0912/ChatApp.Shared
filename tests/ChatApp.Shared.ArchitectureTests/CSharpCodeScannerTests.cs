using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

/// <summary>
/// <see cref="CSharpCodeScanner"/> 的正反用例：注释/字符串里出现 "unsafe" 字样不得误报，
/// 真正的 unsafe 修饰符、unsafe 块与指针类型必须仍被检出。这取代了旧的
/// <c>string.Contains("unsafe")</c> 子串扫描（注释含该英文单词即误报）。
/// </summary>
public sealed class CSharpCodeScannerTests
{
    // ---- 反例：注释 / 字符串 / 非关键字位置里的 "unsafe" 不算数 ----

    [Theory]
    [InlineData("// 这里引用了英文单词 unsafe，属于注释。")]
    [InlineData("/* block comment mentioning unsafe code */")]
    [InlineData("/// <summary>Doc comment: never use unsafe here.</summary>")]
    [InlineData("#region unsafe pointer helpers")]
    [InlineData("#pragma warning disable CS8500 // unsafe 相关告警")]
    [InlineData("var message = \"do not write unsafe code\";")]
    [InlineData("var verbatim = @\"unsafe text inside verbatim\";")]
    [InlineData("var raw = \"\"\"\n    unsafe text inside raw string\n    \"\"\";")]
    [InlineData("var interpolated = $\"unsafe {Environment.Version} inside string\";")]
    [InlineData("var nested = $\"outer {\"unsafe\"} inner\";")]
    [InlineData("var holeWithBraces = $\"values {new[] { 1, 2, \"unsafe\" }} end\";")]
    [InlineData("var escaped = \"\\tunsafe after escape\";")]
    [InlineData("var quoted = @\"he said \"\"unsafe\"\" aloud\";")]
    [InlineData("char quote = '\''; string s = \"unsafe\";")]
    [InlineData("string identifierOnly = unsafeIdentifier;")]
    [InlineData("string typeReference = System.Runtime.CompilerServices.Unsafe.Accessors;")]
    public void UnsafeWord_InCommentsStringsOrIdentifiers_IsNotDetected(string source)
    {
        Assert.False(CSharpCodeScanner.ContainsUnsafeKeyword(source));
    }

    [Fact]
    public void UnsafeAssemblyReference_InCode_IsDetectedByStrippedScan()
    {
        Assert.Contains(
            "System.Runtime.CompilerServices.Unsafe",
            CSharpCodeScanner.StripCommentsAndLiterals(
                "var x = System.Runtime.CompilerServices.Unsafe.CompleteObjectInitialization(ref obj);"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void UnsafeAssemblyName_InComment_IsNoLongerDetected()
    {
        // 与 unsafe 关键字同类的历史误报：注释里出现程序集名不该命中。
        Assert.DoesNotContain(
            "System.Runtime.CompilerServices.Unsafe",
            CSharpCodeScanner.StripCommentsAndLiterals(
                "// 说明：等价于 System.Runtime.CompilerServices.Unsafe 的行为\nvar x = 1;"),
            StringComparison.Ordinal);
    }

    // ---- 正例：真正的 unsafe 代码必须仍被检出 ----

    [Theory]
    [InlineData("public unsafe ref struct Cursor { }")]
    [InlineData("private static unsafe int Encode(byte* p) => *p;")]
    [InlineData("static void F() { unsafe { int* p = null; } }")]
    [InlineData("internal unsafe fixed byte buffer[16];")]
    [InlineData("class C { unsafe ~C() { } }")]
    public void RealUnsafeCode_IsDetected(string source)
    {
        Assert.True(CSharpCodeScanner.ContainsUnsafeKeyword(source));
    }

    // ---- 扫描器边界的组合用例 ----

    [Fact]
    public void MixedCommentsAndUnsafeCode_IsDetected()
    {
        const string source = """
            // unsafe 仅出现在注释里
            /* unsafe */
            public static unsafe int Sum(ReadOnlySpan<byte> data)
            {
                fixed (byte* p = data)
                {
                    return p[0];
                }
            }
            """;

        Assert.True(CSharpCodeScanner.ContainsUnsafeKeyword(source));
    }

    [Fact]
    public void UnsafeAfterInterpolatedHole_IsStillDetected()
    {
        // 插值孔洞结束后扫描必须恢复代码语义，后续真 unsafe 不能被误吞。
        const string source = "var s = $\"a {1} b\"; static unsafe void F() { }";

        Assert.True(CSharpCodeScanner.ContainsUnsafeKeyword(source));
    }

    [Fact]
    public void StrippingKeepsLengthStableAndOnlyLeavesRealKeyword()
    {
        const string source = "int a; // unsafe\nstring b = \"unsafe\";\nunsafe { }";

        string stripped = CSharpCodeScanner.StripCommentsAndLiterals(source);

        Assert.Equal(source.Length, stripped.Length);
        Assert.DoesNotContain("// unsafe", stripped, StringComparison.Ordinal);
        Assert.DoesNotContain("\"unsafe\"", stripped, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(stripped, @"\bunsafe\b"));
    }
}
