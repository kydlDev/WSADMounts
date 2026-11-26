using System.IO;
using System.Linq;

namespace WSADMounts.Tests.Integration;

/// <summary>
/// PROJECT_RULES 41 (user, 2026-11-26): every mod ships at least 10 FAQ files in faq\ (Hexium shows them next to the mod page). The README may
/// be trimmed so that how-to / troubleshooting / multiplayer answers move into the FAQ. Hexium's format: faq\*.md or faq\<Section>\*.md, the
/// first line is ONE short question (shown as the title), the answer below it, Windows-safe names, at most 100 files.
/// The one exception: KydlTools (the user's development mod, never released).
/// </summary>
[TestFixture]
public class FaqRulesTests
{
    const int Minimum = 10;
    static string FaqDir { get { return Path.Combine(TestPaths.ModDir, "faq"); } }

    static string[] Files()
    {
        return Directory.Exists(FaqDir) ? Directory.GetFiles(FaqDir, "*.md", SearchOption.AllDirectories) : new string[0];
    }

    [Test]
    public void Faq_HasAtLeastTenFiles()
    {
        if ("WSADMounts" == "KydlTools") Assert.Ignore("KydlTools is never released: no FAQ minimum");

        var n = Files().Length;
        Assert.That(n, Is.GreaterThanOrEqualTo(Minimum), "faq\\ holds " + n + " .md file(s), the rule asks for at least " + Minimum + " (move how-to / troubleshooting from README.md into faq\\)");
        Assert.That(n, Is.LessThanOrEqualTo(100), "Hexium allows at most 100 FAQ files");
    }

    [Test]
    public void Faq_EveryFileStartsWithOneQuestionAndHasAnAnswer()
    {
        foreach (var f in Files())
        {
            var lines = File.ReadAllLines(f).Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            var name = f.Substring(FaqDir.Length + 1);
            Assert.That(lines.Length, Is.GreaterThanOrEqualTo(2), name + ": a question on the first line and an answer below it");
            Assert.That(lines[0].TrimStart('#', ' ').EndsWith("?"), Is.True, name + ": the first line is the question (ends with '?'), got '" + lines[0] + "'");
        }
    }

    [Test]
    public void Faq_TemplateSampleIsGone()
    {
        foreach (var f in Files())
            Assert.That(File.ReadAllText(f).Contains("Delete this sample file when the mod has real entries"), Is.False, f.Substring(FaqDir.Length + 1) + ": the template's sample FAQ is still there");
    }

    [Test]
    public void Faq_LayoutIsWhatHexiumReads()
    {
        foreach (var f in Directory.Exists(FaqDir) ? Directory.GetFiles(FaqDir, "*", SearchOption.AllDirectories) : new string[0])
        {
            var rel = f.Substring(FaqDir.Length + 1);
            var depth = rel.Split(Path.DirectorySeparatorChar).Length;
            Assert.That(depth, Is.LessThanOrEqualTo(2), rel + ": only faq\\*.md or faq\\<Section>\\*.md");
            Assert.That(Path.GetExtension(f) == ".md" || Path.GetFileName(f) == ".order.json", Is.True, rel + ": only .md files (and Hexium's .order.json)");
        }
    }
}
