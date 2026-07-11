namespace gud.Core.Utilities;

/// <summary>
/// Implements the Myers difference algorithm for calculating the differences
/// between two sequences. The Myers difference algorithm is an efficient
/// algorithm for computing the shortest edit script that transforms one
/// sequence into another.
/// </summary>
/// <remarks>
/// This class is designed to provide a detailed diff calculation suitable
/// for text or data comparison purposes. It can be used to identify additions,
/// deletions, and modifications between two sequences.
/// The MyersDiff class is efficient for calculating diffs, as it uses a
/// divide-and-conquer approach to determine the minimum set of changes needed
/// to transform one sequence into another. It is especially performant for
/// minimal changes.
/// This class assumes both sequences are immutable during calculation. Changing
/// the sequences being compared while the algorithm runs may lead to
/// unpredictable results.
/// Use this class when you need precise and efficient diffing for sequences
/// such as lines of text or elements in a list.
/// </remarks>
public static class MyersDiff
{
    /// <summary>
    /// Computes the differences between two sequences of strings using the Myers diff algorithm.
    /// </summary>
    /// <param name="a">The first sequence of strings to compare.</param>
    /// <param name="b">The second sequence of strings to compare.</param>
    /// <returns>A list of <see cref="DiffEdit"/> representing the differences between the two sequences,
    /// or <c>null</c> if no differences were found.</returns>
    public static List<DiffEdit>? Compute(string[] a, string[] b)
    {
        int n = a.Length, m = b.Length;
        var max = n + m;
        var v = new Dictionary<int, int> { [1] = 0 };
        var trace = new List<Dictionary<int, int>>();

        for (var d = 0; d <= max; d++)
        {
            trace.Add(new Dictionary<int, int>(v));
            for (var k = -d; k <= d; k += 2)
            {
                int x;
                if (k == -d || (k != d && v[k - 1] < v[k + 1]))
                {
                    x = v[k + 1];
                }
                else
                {
                    x = v[k - 1] + 1;
                }

                var y = x - k;

                while (x < n && y < m && a[x] == b[y])
                {
                    x++;
                    y++;
                }

                v[k] = x;

                if (x >= n && y >= m)
                    return Backtrack(trace, a, b, d);
            }
        }
        // Spot is unreachable
        return null;
    }

    /// <summary>
    /// Reconstructs the sequence of edit operations (insert, delete, and equal)
    /// to convert one sequence into another based on the computed trace
    /// from the Myers diff algorithm.
    /// </summary>
    /// <param name="trace">The trace data preserving the states of the diff paths at each depth of the algorithm.</param>
    /// <param name="a">The initial sequence of strings being compared.</param>
    /// <param name="b">The target sequence of strings being compared.</param>
    /// <param name="d">The depth or maximum edit distance between the two sequences.</param>
    /// <returns>A list of <see cref="DiffEdit"/> objects representing the sequence of edit operations.</returns>
    private static List<DiffEdit> Backtrack(List<Dictionary<int, int>> trace, string[] a, string[] b, int d)
    {
        var edits = new List<DiffEdit>();
        int x = a.Length, y = b.Length;

        for (var depth = d; depth > 0; depth--)
        {
            var v = trace[depth];
            var k = x - y;
            var prevK = (k == -depth || (k != depth && v[k - 1] < v[k + 1])) ? k + 1 : k - 1;
            var prevX = v[prevK];
            var prevY = prevX - prevK;

            while (x > prevX && y > prevY)
            {
                edits.Add(new DiffEdit(EditType.Equal, a[x - 1]));
                x--;
                y--;
            }
            
            if (x == prevX)
                edits.Add(new DiffEdit(EditType.Insert, b[y - 1]));
            else
                edits.Add(new DiffEdit(EditType.Delete, a[x - 1]));
            
            x = prevX;
            y = prevY;
        }

        while (x > 0 && y > 0)
        {
            edits.Add(new DiffEdit(EditType.Equal, a[x - 1]));
            x--; y--;
        }

        edits.Reverse();
        return edits;
    }
}

/// <summary>
/// Represents an insertion operation in a diff computation.
/// </summary>
/// <remarks>
/// This edit type is used when a line in the target sequence does not exist in the source sequence
/// and needs to be added. The <see cref="Insert"/> member indicates that the line is present in the target
/// but absent in the source, marking it as an insertion in the context of Myers diff algorithm.
/// </remarks>
public enum EditType
{
    /// <summary>
    /// Represents an edit operation where a line of text is unchanged between the source and target sequences.
    /// </summary>
    Equal,

    /// <summary>
    /// Represents an edit operation where a line of text is present in the target sequence
    /// but not in the source sequence.
    /// </summary>
    /// <remarks>
    /// This edit type is added to the sequence of edits to indicate an insertion operation
    /// during the diff computation. It signifies that the specific line exists in the target
    /// but requires addition compared to the source.
    /// </remarks>
    Insert,

    /// <summary>
    /// Represents an edit operation where a line of text is removed from the original sequence.
    /// </summary>
    Delete
}

/// <summary>
/// Represents the result of a diff operation for a single line of text.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and contain information about the type of
/// edit (e.g., insertion, deletion, or equality) and the content of the associated line.
/// </remarks>
public record DiffEdit(EditType Type, string Line);