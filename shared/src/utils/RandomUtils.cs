using System;

namespace GodotMultiplayerTemplate.Shared;

public static class RandomUtils
{
    public static float NextSingle(this Random random, float minValue, float maxValue) =>
        (float)random.NextDouble() * (maxValue - minValue) + minValue;

    public static float NextGaussian(this Random random, float mean, float std)
    {
        double u1 = 1.0 - random.NextDouble();
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1))
            * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)
        return (float)(mean + std * randStdNormal); // random normal(mean,stdDev^2)
    }
}
