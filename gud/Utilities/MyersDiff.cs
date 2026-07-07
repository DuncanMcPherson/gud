namespace gud.Utilities;

public static class MyersDiff
{
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
        return null;
    }

    private static List<DiffEdit>? Backtrack(List<Dictionary<int, int>> trace, string[] a, string[] b, int d)
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

public enum EditType { Equal, Insert, Delete }
public record DiffEdit(EditType Type, string Line);