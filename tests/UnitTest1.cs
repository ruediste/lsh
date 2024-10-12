using lsh.LshMath;

namespace tests;

public class Tests
{

    [Test]
    public void Test1()
    {
        // build test distribution
        var d = 20;
        var N = 200;
        var random = new Random(0);

        var data = Enumerable.Range(0, N / 4).SelectMany(_ =>
        {
            var v = DenseVector.RandomUniform(d, 0, 10, random);
            return new List<DenseVector> {
                v,
                v.Add(DenseVector.RandomUniform(d,0,1,random) ),
                v.Add(DenseVector.RandomUniform(d,0,1,random) ),
                v.Add(DenseVector.RandomUniform(d,0,1,random) )
            };
        }).ToArray();

        // calculate d_any
        var distances = new List<double>();
        var minDistances = new List<double>();
        for (int i = 0; i < data.Length; i++)
        {
            double minDist = double.PositiveInfinity;
            for (int j = 0; j < data.Length; j++)
            {
                if (i == j) continue;
                var dist = data[i].DistanceTo(data[j]);
                if (j > i)
                    distances.Add(dist);
                if (dist < minDist)
                {
                    minDist = dist;
                }
            }
            minDistances.Add(minDist);
        }

        var dAny = new HistogramPDF(distances);
        var dNn = new HistogramPDF(minDistances);

        dAny.Plot("dAny.png");
        dNn.Plot("dNn.png");
        Console.WriteLine("done1");
    }
}