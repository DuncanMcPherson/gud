using gud.Core.Utilities;
using Spectre.Console;

namespace gud.Utilities;

public static class DiffRenderer
{
    public static void RenderFileDiff(string path, string[] oldLines, string[] newLines)
    {
        var edits = MyersDiff.Compute(oldLines, newLines);
        if (edits is null)
            return;
        AnsiConsole.MarkupLine($"[bold]diff --gud a/{path} b/{path}[/]");

        foreach (var edit in edits)
        {
            var line = Markup.Escape(edit.Line);
            switch (edit.Type)
            {
                case EditType.Equal:
                    AnsiConsole.MarkupLine($"  {line}");
                    break;
                case EditType.Insert:
                    AnsiConsole.MarkupLine($"[green]+ {line}[/]");
                    break;
                case EditType.Delete:
                    AnsiConsole.MarkupLine($"[red]- {line}[/]");
                    break;
            }
        }
    }
}