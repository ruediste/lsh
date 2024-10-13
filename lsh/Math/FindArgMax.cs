namespace lsh.LshMath;

public static class FindArgMax
{
    public static double Compute(Func<double, double> f, double err)
    {
        // use exponential search to bracket the maximum
        var x = 1.0;
        var y0 = f(x / 2);
        var y1 = f(x);
        var y2 = f(2 * x);
        while (true)
        {
            // Console.WriteLine($"exp search {x}: {y0} {y1} {y2}");
            if (y0 > y1)
            {
                // move left
                x /= 2;
                y2 = y1;
                y1 = y0;
                y0 = f(x / 2);
            }
            else if (y2 > y1)
            {
                // move right
                x *= 2;
                y0 = y1;
                y1 = y2;
                y2 = f(x * 2);
            }
            else break;
        }


        // use golden section search to find the maximum
        // https://en.wikipedia.org/wiki/Golden-section_search
        var x0 = x / 2;
        var x1 = x;
        var x2 = 2 * x;
        while (x0 != x1 && x1 != x2 && (RelativeError(y0, y1) > err || RelativeError(y1, y2) > err))
        {
            // Console.WriteLine($"x {x0}\t{x1}\t{x2}");
            // Console.WriteLine($"  {y0}\t{y1}\t{y2}\n");
            if (x1 - x0 > x2 - x1)
            {
                var x3 = (x1 + x0) / 2;
                var y3 = f(x3);
                if (y3 > y1)
                {
                    y2 = y1;
                    x2 = x1;

                    y1 = y3;
                    x1 = x3;
                }
                else
                {
                    y0 = y3;
                    x0 = x3;
                }
            }
            else
            {
                var x3 = (x2 + x1) / 2;
                var y3 = f(x3);
                if (y3 > y1)
                {
                    y0 = y1;
                    x0 = x1;

                    y1 = y3;
                    x1 = x3;
                }
                else
                {
                    y2 = y3;
                    x2 = x3;
                }
            }
        }
        return x1;
    }

    private static double RelativeError(double a, double b)
    {
        var x = Math.Abs(a / b);
        if (x < 1)
            x = 1 / x;
        return x - 1;
    }
}