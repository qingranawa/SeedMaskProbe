using System;
using System.Globalization;
using System.Security.Cryptography;

namespace SeedMaskProbe;

/// <summary>
/// 生成客户端可接受且不等于真实 Seed 的伪造值。
/// </summary>
public static class SeedMasking
{
    public const string RandomPlaceholder = "${RANDOM}";

    public const int DefaultFallbackSeed = 246813579;

    private const int MinimumSeed = 1;

    private const int MaximumSeed = int.MaxValue - 1;

    public static int GetSafeDecoy(int configuredSeed, int actualSeed)
    {
        int decoySeed = NormalizeSeed(configuredSeed);

        if (decoySeed == actualSeed)
        {
            return decoySeed == MaximumSeed ? MinimumSeed : decoySeed + 1;
        }

        return decoySeed;
    }

    public static int ResolveConfiguredValue(
        string? configuredValue,
        int actualSeed,
        Func<int> randomSeedFactory,
        int fallbackSeed = DefaultFallbackSeed)
    {
        int candidateSeed;

        if (IsRandomPlaceholder(configuredValue))
        {
            candidateSeed = randomSeedFactory();
        }
        else if (!int.TryParse(
                     configuredValue?.Trim(),
                     NumberStyles.Integer,
                     CultureInfo.InvariantCulture,
                     out candidateSeed))
        {
            candidateSeed = fallbackSeed;
        }

        return GetSafeDecoy(candidateSeed, actualSeed);
    }

    public static bool IsRandomPlaceholder(string? configuredValue)
    {
        return string.Equals(
            configuredValue?.Trim(),
            RandomPlaceholder,
            StringComparison.OrdinalIgnoreCase);
    }

    public static int GenerateRandomSeed()
    {
        using RandomNumberGenerator generator = RandomNumberGenerator.Create();
        byte[] bytes = new byte[sizeof(uint)];
        generator.GetBytes(bytes);

        uint rawValue = BitConverter.ToUInt32(bytes, 0);
        return (int)(rawValue % MaximumSeed) + MinimumSeed;
    }

    private static int NormalizeSeed(int seed)
    {
        if (seed < MinimumSeed)
        {
            return MinimumSeed;
        }

        return seed > MaximumSeed ? MaximumSeed : seed;
    }
}
