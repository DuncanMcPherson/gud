using gud.Core.Utilities;
using Spectre.Console;

namespace gud.Utilities;

public record DiffLine(EditType Type, string Content, int? OldLineNo, int? NewLineNo);

public record DiffHunk(int OldStart, int OldCount, int NewStart, int NewCount, List<DiffLine> Lines);

public static class DiffRenderer
{
    public static List<DiffHunk> BuildHunks(List<DiffEdit> edits, int context = 5)
    {
        var numbered = new List<DiffLine>();
        int oldLine = 1, newLine = 1;
        foreach (var edit in edits)
        {
            switch (edit.Type)
            {
                case EditType.Equal:
                    numbered.Add(new DiffLine(edit.Type, edit.Line, oldLine, newLine));
                    oldLine++;
                    newLine++;
                    break;
                case EditType.Insert:
                    numbered.Add(new DiffLine(edit.Type, edit.Line, null, newLine));
                    newLine++;
                    break;
                case EditType.Delete:
                    numbered.Add(new DiffLine(edit.Type, edit.Line, oldLine, null));
                    oldLine++;
                    break;
            }
        }

        var hunks = new List<DiffHunk>();
        var current = new List<DiffLine>();

        void FlushHunk()
        {
            if (current.Count == 0 || current.All(l => l.Type == EditType.Equal))
            {
                current.Clear();
                return;
            }

            var oldNos = current.Where(l => l.OldLineNo.HasValue).Select(l => l.OldLineNo!.Value).ToList();
            var newNos = current.Where(l => l.NewLineNo.HasValue).Select(l => l.NewLineNo!.Value).ToList();
            hunks.Add(new DiffHunk(
                oldNos.FirstOrDefault(), oldNos.Count,
                newNos.FirstOrDefault(), newNos.Count,
                [..current]));
            current.Clear();
        }

        for (var i = 0; i < numbered.Count; i++)
        {
            if (numbered[i].Type != EditType.Equal)
            {
                current.Add(numbered[i]);
                continue;
            }

            var runStart = i;
            while (i < numbered.Count && numbered[i].Type == EditType.Equal) i++;
            var runLength = i - runStart;
            i--;

            if (runLength <= context * 2)
            {
                current.AddRange(numbered.GetRange(runStart, runLength));
            }
            else
            {
                current.AddRange(numbered.GetRange(runStart, context));
                FlushHunk();
                current.AddRange(numbered.GetRange(runStart + runLength - context, context));
            }
        }
        FlushHunk();
        return hunks;
    }

    public static void RenderFileDiff(string path, string[] oldLines, string[] newLines)
    {
        if (oldLines.Length > 5000 || newLines.Length > 5000)
        {
            if (!AnsiConsole.Confirm($"[bold]File {path} is too large to diff. Continue anyway?[/]", false))
                return;
        }
        var edits = MyersDiff.Compute(oldLines, newLines);
        if (edits is null)
            return;
        var hunks = BuildHunks(edits);
        if (hunks.Count == 0)
            return;
        var longestLine = Math.Max(oldLines.Length, newLines.Length);
        var padLength = Math.Max(4, longestLine.ToString().Length);
        
        AnsiConsole.MarkupLine($"[bold]diff --gud a/{path} b/{path}[/]");

        foreach (var hunk in hunks)
        {
            AnsiConsole.MarkupLine($"[cyan]@@ -{hunk.OldStart},{hunk.OldCount} +{hunk.NewStart},{hunk.NewCount} @@[/]");
            foreach (var line in hunk.Lines)
            {
                var content = Markup.Escape(line.Content);
                var oldNo = line.OldLineNo?.ToString().PadLeft(padLength) ?? new string(' ', padLength);
                var newNo = line.NewLineNo?.ToString().PadLeft(padLength) ?? new string(' ', padLength);

                switch (line.Type)
                {
                    case EditType.Equal:
                        AnsiConsole.MarkupLine($"{oldNo} {newNo}   {content}");
                        break;
                    case EditType.Insert:
                        AnsiConsole.MarkupLine($"[green]{oldNo} {newNo} + {content}[/]");
                        break;
                    case EditType.Delete:
                        AnsiConsole.MarkupLine($"[red]{oldNo} {newNo} - {content}[/]");
                        break;
                }
            }
        }
    }
}