using System.Text;
using System.Text.RegularExpressions;

namespace ChatApp.Shared.ArchitectureTests;

/// <summary>
/// 词法级 C# 源码扫描器：剥除注释与字符串/字符字面量，只把真正的代码留给关键字检测。
/// 架构测试曾用 <c>string.Contains("unsafe")</c> 子串扫描 src/，注释里恰好出现英文单词
/// "unsafe" 就会误报；这里用一个小状态机处理行/块注释、常规/逐义/原始字符串、字符字面量、
/// 插值孔洞（含孔内嵌套字符串与嵌套花括号）与预处理指令行，使检测结果只由代码本身决定。
/// 输出与输入等长（被剥除部分以空格占位），便于调试比对。
/// </summary>
internal static class CSharpCodeScanner
{
    private static readonly Regex UnsafeKeywordRegex = new(@"\bunsafe\b", RegexOptions.Compiled);

    /// <summary>
    /// 判断源码的代码部分（不含注释与字符串/字符字面量）是否出现 <c>unsafe</c> 关键字。
    /// </summary>
    public static bool ContainsUnsafeKeyword(string source) =>
        UnsafeKeywordRegex.IsMatch(StripCommentsAndLiterals(source));

    /// <summary>
    /// 返回剥除注释与字符串/字符字面量后的代码文本。解析不了的畸形结构按"保守丢弃"处理：
    /// 宁可把可疑片段整个当字面量丢掉，也不把字符串内容当代码误报。
    /// </summary>
    public static string StripCommentsAndLiterals(string source)
    {
        var output = new StringBuilder(source.Length);
        var scopes = new Stack<Scope>();
        scopes.Push(Scope.Code(isHole: false));

        bool atLineStart = true; // 预处理指令只在行首生效。
        int i = 0;
        while (i < source.Length)
        {
            char c = source[i];
            Scope scope = scopes.Peek();

            if (scope.Kind == ScopeKind.Code && scope.IsHole)
            {
                // 插值孔洞帧：'}' 在第一层深度时结束孔洞（回到宿主字符串帧）；更深层花括号
                // 每层压入自己的帧，配对 '}' 时弹出，嵌套孔内代码（如 new[] { 1, 2 }）因此不穿透。
                if (c == '}')
                {
                    scopes.Pop();
                    Blank(output, i, 1);
                    atLineStart = false;
                    i++;
                    continue;
                }

                if (c == '{')
                {
                    scopes.Push(Scope.Code(isHole: true));
                    Blank(output, i, 1);
                    atLineStart = false;
                    i++;
                    continue;
                }
            }

            if (scope.Kind == ScopeKind.Code)
            {
                i = StepCodeScope(source, i, output, scopes, ref atLineStart);
                continue;
            }

            i = StepStringScope(source, i, output, scopes, scope, ref atLineStart);
        }

        return output.ToString();
    }

    private enum ScopeKind
    {
        Code,
        String
    }

    private readonly record struct Scope(
        ScopeKind Kind,
        bool IsHole,
        bool Interpolated,
        bool Verbatim,
        int RawQuoteCount)
    {
        public static Scope Code(bool isHole) => new(ScopeKind.Code, isHole, false, false, 0);

        public static Scope String(bool interpolated, bool verbatim, int rawQuoteCount) =>
            new(ScopeKind.String, false, interpolated, verbatim, rawQuoteCount);
    }

    /// <summary>处理一个代码作用域字符：注释、预处理指令、字符串/字符字面量或普通代码。</summary>
    private static int StepCodeScope(string source, int i, StringBuilder output, Stack<Scope> scopes, ref bool atLineStart)
    {
        char c = source[i];

        if (c == '#' && atLineStart)
        {
            // 预处理指令 payload（region 名、pragma 等）不是代码，整行跳过，
            // 防止 #region unsafe pointer helpers 这类命名把注释级字样带进检测结果。
            return SkipToLineEnd(source, i, output, ref atLineStart);
        }

        if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
            return SkipToLineEnd(source, i, output, ref atLineStart);

        if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
            return SkipBlockComment(source, i, output, ref atLineStart);

        if (c == '\'')
        {
            int end = ConsumeCharLiteral(source, i, output);
            atLineStart = false;
            return end;
        }

        if (c == '"' && TryOpenStringScope(source, ref i, output, scopes))
        {
            atLineStart = false;
            return i;
        }

        output.Append(c);
        atLineStart = c is ' ' or '\t' ? atLineStart : c == '\n';
        return i + 1;
    }

    /// <summary>
    /// 处理字符串作用域内的一个字符：结束引号出栈、插值孔洞压栈、转义与内容按字面量丢弃。
    /// </summary>
    private static int StepStringScope(string source, int i, StringBuilder output, Stack<Scope> scopes, Scope scope, ref bool atLineStart)
    {
        char c = source[i];

        if (scope.RawQuoteCount > 0)
            return StepRawStringScope(source, i, output, scopes, scope);

        if (c == '"')
        {
            // 逐义字符串里 "" 是转义引号；单个引号（逐义与常规皆同）结束字符串。
            if (scope.Verbatim && i + 1 < source.Length && source[i + 1] == '"')
            {
                Blank(output, i, 2);
                return i + 2;
            }

            scopes.Pop();
            Blank(output, i, 1);
            atLineStart = false;
            return i + 1;
        }

        if (!scope.Verbatim && c == '\\')
        {
            Blank(output, i, Math.Min(2, source.Length - i));
            return i + 2;
        }

        if (scope.Interpolated && c == '{')
        {
            if (i + 1 < source.Length && source[i + 1] == '{')
            {
                Blank(output, i, 2);
                return i + 2;
            }

            // 插值孔洞开始：压入孔洞代码帧，主循环按代码扫描孔内内容，'}' 时回到本帧。
            scopes.Push(Scope.Code(isHole: true));
            Blank(output, i, 1);
            return i + 1;
        }

        if (scope.Interpolated && c == '}')
        {
            // 插值字符串文本里 }} 是字面大括号；单个 } 是畸形输入，按内容丢弃。
            if (i + 1 < source.Length && source[i + 1] == '}')
            {
                Blank(output, i, 2);
                return i + 2;
            }

            Blank(output, i, 1);
            return i + 1;
        }

        atLineStart = c == '\n';
        Blank(output, i, 1);
        return i + 1;
    }

    private static int StepRawStringScope(string source, int i, StringBuilder output, Stack<Scope> scopes, Scope scope)
    {
        char c = source[i];

        if (c == '"')
        {
            int run = 0;
            while (i + run < source.Length && source[i + run] == '"')
                run++;
            if (run >= scope.RawQuoteCount)
            {
                scopes.Pop();
                Blank(output, i, scope.RawQuoteCount);
                return i + scope.RawQuoteCount;
            }

            Blank(output, i, run);
            return i + run;
        }

        if (scope.Interpolated && c == '{')
        {
            if (i + 1 < source.Length && source[i + 1] == '{')
            {
                Blank(output, i, 2);
                return i + 2;
            }

            scopes.Push(Scope.Code(isHole: true));
            Blank(output, i, 1);
            return i + 1;
        }

        if (scope.Interpolated && c == '}' && i + 1 < source.Length && source[i + 1] == '}')
        {
            Blank(output, i, 2);
            return i + 2;
        }

        return i + 1;
    }

    /// <summary>
    /// 在代码作用域尝试打开一个字符串作用域（识别 $/@ 前缀的两种顺序、常规/逐义/原始引号串与空字符串）。
    /// 返回 false 表示当前位置不构成字符串开头。成功时推进 i 越过开头引号并压栈字符串帧。
    /// </summary>
    private static bool TryOpenStringScope(string source, ref int i, StringBuilder output, Stack<Scope> scopes)
    {
        int quoteStart = i;
        int quoteRun = 0;
        while (quoteStart + quoteRun < source.Length && source[quoteStart + quoteRun] == '"')
            quoteRun++;

        // 前缀识别按源码回溯；$ 与 @ 两种顺序（$@"、@$"）都支持。$ 在合法 C# 里只出现在插值前缀。
        int j = quoteStart;
        int dollars = 0;
        bool verbatim = false;
        while (j > 0 && source[j - 1] == '$')
        {
            dollars++;
            j--;
        }

        if (j > 0 && source[j - 1] == '@')
        {
            verbatim = true;
            j--;
            while (j > 0 && source[j - 1] == '$')
            {
                dollars++;
                j--;
            }
        }

        if (verbatim)
        {
            // 逐义字符串开头固定一个引号；@ 前缀与原始字符串互斥。
            Blank(output, j, quoteStart - j + 1);
            i = quoteStart + 1;
            scopes.Push(Scope.String(dollars > 0, true, 0));
            return true;
        }

        if (quoteRun == 2)
        {
            Blank(output, j, quoteStart - j + 2);
            i = quoteStart + 2;
            return true; // 空字符串 ""，整体丢弃。
        }

        if (quoteRun >= 3)
        {
            Blank(output, j, quoteStart - j + quoteRun);
            i = quoteStart + quoteRun;
            scopes.Push(Scope.String(dollars > 0, false, quoteRun));
            return true;
        }

        Blank(output, j, quoteStart - j + 1);
        i = quoteStart + 1;
        scopes.Push(Scope.String(dollars > 0, false, 0));
        return true;
    }

    private static int ConsumeCharLiteral(string source, int i, StringBuilder output)
    {
        Blank(output, i, 1);
        i++;
        while (i < source.Length && source[i] != '\'')
        {
            if (source[i] == '\\' && i + 1 < source.Length)
            {
                Blank(output, i, 2);
                i += 2;
            }
            else
            {
                Blank(output, i, 1);
                i++;
            }
        }

        if (i < source.Length)
        {
            Blank(output, i, 1);
            i++;
        }

        return i;
    }

    private static int SkipToLineEnd(string source, int i, StringBuilder output, ref bool atLineStart)
    {
        while (i < source.Length && source[i] != '\n')
        {
            Blank(output, i, 1);
            i++;
        }

        if (i < source.Length)
        {
            Blank(output, i, 1);
            i++;
        }

        atLineStart = true;
        return i;
    }

    private static int SkipBlockComment(string source, int i, StringBuilder output, ref bool atLineStart)
    {
        Blank(output, i, 2);
        i += 2;
        while (i < source.Length)
        {
            if (source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/')
            {
                Blank(output, i, 2);
                return i + 2;
            }

            if (source[i] == '\n')
                atLineStart = true;

            Blank(output, i, 1);
            i++;
        }

        return i;
    }

    /// <summary>
    /// 把 [start, start+count) 置为空格。多数调用发生在消费时刻（位置等于当前输出长度，追加即可）；
    /// 字符串前缀（$/@）在早前迭代已被当普通代码输出，此时按下标覆写。
    /// </summary>
    private static void Blank(StringBuilder output, int start, int count)
    {
        for (int k = 0; k < count; k++)
        {
            int position = start + k;
            if (position < output.Length)
                output[position] = ' ';
            else
                output.Append(' ');
        }
    }
}
