using gud.Utilities;

namespace gud;

internal static class Program
{
    private static void Main(string[] args)
    {
        var fileA = args[0];
        var fileB = args[1];
        
        var a = File.ReadAllLines(fileA);
        var b = File.ReadAllLines(fileB);
        
        var edits = MyersDiff.Compute(a, b);
        foreach (var edit in edits!)
            Console.WriteLine($"{edit.Type,-6} {edit.Line}");
    }
}