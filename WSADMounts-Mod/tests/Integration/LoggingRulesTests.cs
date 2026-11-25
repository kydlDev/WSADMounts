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
/// </summary>
[TestFixture]
public class LoggingRulesTests
{
    static readonly Regex Signature = new Regex(@"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|internal|private|protected|static|override|virtual|sealed|unsafe|new|extern)\s+)*([\w<>\[\],\.\?]+(?:\s*<[^>]*>)?)\s+(\w+)\s*(<[^>]*>)?\s*\(.*\)\s*(?:where\s+[^{]*)?$");
    static readonly Regex Return = new Regex(@"(?<![\w.])return(\s+[^;]*)?;");
    static readonly HashSet<string> NotMethods = new HashSet<string> { "if", "for", "foreach", "while", "switch", "using", "lock", "catch", "return", "nameof", "typeof", "new", "else", "get", "set" };

    sealed class Method
    {
        public string File = "";
        public string Name = "";
        public int Open;
        public int Close;
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
            .Where(f => File.ReadAllLines(f).Any(l => Logs(Code(l)))).Select(Path.GetFileName).ToList();
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

    static bool Logs(string text) => text.Contains("ModLog.") || text.Contains("RuleLog.");

    [Test]
    public void EveryMethod_WritesADebugLine()
    {
        var silent = new List<string>();
        foreach (string f in SourceFiles())
        {
            string[] lines = File.ReadAllLines(f);
            foreach (Method m in Methods(f, lines))
                if (!Logs(string.Join("\n", lines.Skip(m.Open).Take(m.Close - m.Open + 1)))) silent.Add(Path.GetFileName(f) + ": " + m.Name);
        }
        Assert.That(silent, Is.Empty, "methods without any ModLog / RuleLog line (PROJECT_RULES 8; Claude/lessons/scripts/add_method_logging.py adds them)");
    }

    [Test]
    public void EveryReturn_IsLogged()
    {
        var silent = new List<string>();
        foreach (string f in SourceFiles())
        {
            string[] lines = File.ReadAllLines(f);
            foreach (Method m in Methods(f, lines))
                for (int k = m.Open + 1; k < m.Close; k++)
                {
                    string code = Code(lines[k]);
                    if (!Return.IsMatch(code) || code.Contains("yield") || code.Contains("=>")) continue;
                    if (!Logs(code) && !Logs(lines[k - 1])) silent.Add(Path.GetFileName(f) + ":" + (k + 1) + " in " + m.Name);
                }
        }
        Assert.That(silent, Is.Empty, "returns without a log line on them or right above (PROJECT_RULES 8: no method stops silently)");
    }
}
