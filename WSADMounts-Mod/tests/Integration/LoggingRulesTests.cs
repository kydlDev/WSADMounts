using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WSADMounts.Tests.Integration;

/// <summary>
/// PROJECT_RULES 8 ("with debug on, absolutely everything is tracked", user 2026-11-20): every block-bodied method under src/ (not src/Testing) writes a
/// debug line (ModLog in game code, RuleLog in the pure code), and every `return` has a log line on it or right above it, so no method stops silently.
/// Origin: GunPowdered 2026-11-20 - the musket reload key failed in game and nothing was logged (229 of 416 game methods had no log line). Expression-bodied members (=>) are not
/// checked; give them a block body when they decide something.
///
/// PROPER logging, not just any logging (user 2026-11-25): a line that only proves "the method ran" cannot show why something failed
/// (Claude/lessons/DIAGNOSTIC_LOGGING.md 1: "if this fails, will ONE log show why?"). So the tests also check that
///  - a log CALL exists (ModLog.IsDue / RuleLog.IsOn / RuleLog.Due only decide whether to log, they are not a log line),
///  - a Verbose / Info / Warn / Error / Every message carries a value (an interpolated {hole}, a concatenation or a variable): the inputs, the decision
///    or the result, and is not a bare "Name()",
///  - the log on or above a `return` carries a value too (what is returned and why),
///  - every catch logs the exception it caught (the variable itself, not just "failed").
/// State / Decision / Check carry their what / from / to / reason / expected values as arguments and count as meaningful.
/// </summary>
[TestFixture]
public class LoggingRulesTests
{
    static readonly Regex Signature = new Regex(@"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|internal|private|protected|static|override|virtual|sealed|unsafe|new|extern)\s+)*([\w<>\[\],\.\?]+(?:\s*<[^>]*>)?)\s+(\w+)\s*(<[^>]*>)?\s*\(.*\)\s*(?:where\s+[^{]*)?$");
    static readonly Regex Return = new Regex(@"(?<![\w.])return(\s+[^;]*)?;");
    static readonly Regex LogCall = new Regex(@"\b(?:ModLog|RuleLog)\.(Verbose|Info|Warn|Error|State|Decision|Check|CheckRange|Every)\(");
    static readonly Regex Catch = new Regex(@"\bcatch\b\s*(?:\(\s*([\w\.]+)(?:\s+(\w+))?\s*\))?");
    static readonly HashSet<string> NotMethods = new HashSet<string> { "if", "for", "foreach", "while", "switch", "using", "lock", "catch", "return", "nameof", "typeof", "new", "else", "get", "set" };
    static readonly HashSet<string> Structured = new HashSet<string> { "State", "Decision", "Check", "CheckRange" };

    sealed class Method
    {
        public string File = "";
        public string Name = "";
        public int Open;
        public int Close;
    }

    /// <summary>One ModLog / RuleLog call: its kind, its message argument and the lines it spans (0-based).</summary>
    sealed class LogStatement
    {
        public string Kind = "";
        public string Message = "";
        public int First;
        public int Last;
    }

    static IEnumerable<string> SourceFiles() => Directory.GetFiles(TestPaths.Src, "*.cs", SearchOption.AllDirectories)
        .Where(f => !IsLogListener(f) && !f.Replace('\\', '/').Contains("/src/Testing/") && Path.GetFileName(f) != "RuleLog.cs");

    // code that receives every log line (a BepInEx ILogListener, Unity's logMessageReceived) must never log: each of its own lines would come back to it
    // and loop forever (KydlCore FlightListener, 2026-11-20: the log filled up and Valheim did not start)
    static bool IsLogListener(string file)
    {
        string text = File.ReadAllText(file);
        return text.Contains("ILogListener") || text.Contains("logMessageReceived");
    }

    [Test]
    public void LogListeners_NeverLog()
    {
        var looping = Directory.GetFiles(TestPaths.Src, "*.cs", SearchOption.AllDirectories).Where(IsLogListener)
            .Where(f => File.ReadAllLines(f).Any(l => MentionsLog(Code(l)))).Select(Path.GetFileName).ToList();
        Assert.That(looping, Is.Empty, "a log listener that logs calls itself again for every line it writes (endless loop)");
    }

    static List<Method> Methods(string file, string[] lines)
    {
        var found = new List<Method>();
        for (int i = 0; i + 1 < lines.Length; i++)
        {
            Match m = Signature.Match(lines[i]);
            if (!m.Success || lines[i + 1].Trim() != "{" || NotMethods.Contains(m.Groups[2].Value) || NotMethods.Contains(m.Groups[1].Value) || m.Groups[1].Value.Contains("=")) continue;
            int depth = 0, j = i + 1;
            for (; j < lines.Length; j++)
            {
                string code = Code(lines[j]);
                depth += code.Count(c => c == '{') - code.Count(c => c == '}');
                if (depth == 0) break;
            }
            found.Add(new Method { File = file, Name = m.Groups[2].Value, Open = i + 1, Close = j });
        }
        return found;
    }

    static string Code(string line)
    {
        int c = line.IndexOf("//", System.StringComparison.Ordinal);
        return c >= 0 ? line.Substring(0, c) : line;
    }

    static bool MentionsLog(string text) => text.Contains("ModLog.") || text.Contains("RuleLog.");

    /// <summary>Every ModLog / RuleLog call of the file with its message argument (parentheses and commas are counted outside string literals only).</summary>
    static List<LogStatement> LogStatements(string[] lines)
    {
        string text = string.Join("\n", lines.Select(Code));
        var found = new List<LogStatement>();
        foreach (Match m in LogCall.Matches(text))
        {
            int i = m.Index + m.Length, depth = 1, argStart = i;
            bool inString = false;
            var args = new List<string>();
            for (; i < text.Length && depth > 0; i++)
            {
                char c = text[i];
                if (c == '"' && (i == 0 || text[i - 1] != '\\')) { inString = !inString; continue; }
                if (inString) continue;
                if (c == '(' || c == '[') depth++;
                else if (c == ')' || c == ']') depth--;
                else if (c == ',' && depth == 1) { args.Add(text.Substring(argStart, i - argStart)); argStart = i + 1; }
            }
            args.Add(text.Substring(argStart, System.Math.Max(0, i - 1 - argStart)));
            string kind = m.Groups[1].Value;
            string message = kind == "Every" ? args.Last().Trim() : args[0].Trim();
            found.Add(new LogStatement
            {
                Kind = kind,
                Message = message,
                First = text.Take(m.Index).Count(ch => ch == '\n'),
                Last = text.Take(i).Count(ch => ch == '\n'),
            });
        }
        return found;
    }

    /// <summary>The message names a value (an input, a decision, a result): an interpolated {hole}, a concatenation, or a variable.</summary>
    static bool HasValue(string message)
    {
        if (!message.StartsWith("\"") && !message.StartsWith("$\"") && !message.StartsWith("@\"")) return message.Length > 0;
        if (message.StartsWith("$\"")) return message.Contains("{");
        return Regex.IsMatch(message, "\"\\s*\\+|\\+\\s*\"");
    }

    /// <summary>A bare "Name()": text that says nothing the code position does not already say.</summary>
    static bool ContentFree(string message) => Regex.IsMatch(message, "^\\$?\"[\\w\\.]+\\(\\)\"$");

    static bool Meaningful(LogStatement s) => Structured.Contains(s.Kind) || (HasValue(s.Message) && !ContentFree(s.Message));

    [Test]
    public void EveryMethod_WritesADebugLine()
    {
        var silent = new List<string>();
        foreach (string f in SourceFiles())
        {
            string[] lines = File.ReadAllLines(f);
            List<LogStatement> logs = LogStatements(lines);
            foreach (Method m in Methods(f, lines))
                if (!logs.Any(s => s.First >= m.Open && s.First <= m.Close)) silent.Add(Path.GetFileName(f) + ": " + m.Name);
        }
        Assert.That(silent, Is.Empty, "methods without a ModLog / RuleLog CALL (IsDue / IsOn / Due are only conditions; PROJECT_RULES 8; Claude/lessons/scripts/add_method_logging.py adds them)");
    }

    [Test]
    public void EveryLogLine_CarriesAValue_NotJustThatTheCodeRan()
    {
        var empty = new List<string>();
        foreach (string f in SourceFiles())
            foreach (LogStatement s in LogStatements(File.ReadAllLines(f)))
                if (!Meaningful(s)) empty.Add(Path.GetFileName(f) + ":" + (s.First + 1) + " " + s.Kind + "(" + s.Message + ")");
        Assert.That(empty, Is.Empty, "log lines that name no input, decision or result (a constant text or a bare \"Name()\"): add the values that would show why this fails - DIAGNOSTIC_LOGGING.md 1");
    }

    [Test]
    public void EveryReturn_IsLogged()
    {
        var silent = new List<string>();
        foreach (string f in SourceFiles())
        {
            string[] lines = File.ReadAllLines(f);
            List<LogStatement> logs = LogStatements(lines);
            foreach (Method m in Methods(f, lines))
                for (int k = m.Open + 1; k < m.Close; k++)
                {
                    string code = Code(lines[k]);
                    if (!Return.IsMatch(code) || code.Contains("yield") || code.Contains("=>")) continue;
                    // a meaningful log call on the return's line or ending on the line above it
                    if (!logs.Any(s => Meaningful(s) && (s.Last == k || s.Last == k - 1))) silent.Add(Path.GetFileName(f) + ":" + (k + 1) + " in " + m.Name);
                }
        }
        Assert.That(silent, Is.Empty, "returns without a log call that names a value, on the line or right above it (PROJECT_RULES 8: no method stops silently; say what is returned and why)");
    }

    [Test]
    public void EveryCatch_LogsTheExceptionItCaught()
    {
        var bad = new List<string>();
        foreach (string f in SourceFiles())
        {
            string[] lines = File.ReadAllLines(f);
            List<LogStatement> logs = LogStatements(lines);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = Code(lines[i]);
                Match c = Catch.Match(code);
                if (!c.Success || !Regex.IsMatch(code, @"(^|[\s}])catch\b")) continue;
                string variable = c.Groups[2].Value;
                int depth = 0, end = i;
                bool opened = false;
                for (int j = i; j < lines.Length; j++)
                {
                    string body = Code(lines[j]);
                    depth += body.Count(ch => ch == '{') - body.Count(ch => ch == '}');
                    if (body.Contains("{")) opened = true;
                    end = j;
                    if (opened && depth <= 0) break;
                }
                bool logsIt = variable.Length > 0 && logs.Any(s => s.First >= i && s.First <= end
                    && string.Join("\n", lines.Skip(s.First).Take(s.Last - s.First + 1)).Contains(variable));
                if (!logsIt) bad.Add(Path.GetFileName(f) + ":" + (i + 1) + (variable.Length == 0 ? " catch without a variable" : " catch (" + variable + ") does not log it"));
            }
        }
        Assert.That(bad, Is.Empty, "a catch must log the exception: catch (SpecificException ex) -> ModLog line with ex.GetType().FullName / ex.Message, what the code was doing and what it does instead (DIAGNOSTIC_LOGGING.md 2)");
    }
}
