namespace SeedMaskProbe.Tests;

internal static class Program
{
    private static int Main()
    {
        AssertEqual(
            246813579,
            SeedMasking.GetSafeDecoy(246813579, 135791357),
            "合法配置值应直接作为伪造值");

        int collisionValue = SeedMasking.GetSafeDecoy(135791357, 135791357);
        AssertNotEqual(135791357, collisionValue, "伪造值不能等于真实 Seed");
        AssertInRange(collisionValue, "碰撞修正后的伪造值必须是正整数");

        int invalidValue = SeedMasking.GetSafeDecoy(0, 135791357);
        AssertNotEqual(135791357, invalidValue, "非法配置值修正后不能等于真实 Seed");
        AssertInRange(invalidValue, "非法配置值修正后必须是正整数");

        string fixedValue = SeedMaskSettingsLoader.ReadFakeSeed(
            "{\"fake_seed\":\"246813579\"}")
            ?? throw new InvalidOperationException("JSON 固定值不应为空");
        AssertEqual("246813579", fixedValue, "JSON 应读取固定伪造值");

        string randomValue = SeedMaskSettingsLoader.ReadFakeSeed(
            "{\"fake_seed\":\"${RANDOM}\"}")
            ?? throw new InvalidOperationException("JSON 随机占位符不应为空");
        AssertEqual("${RANDOM}", randomValue, "JSON 应读取随机占位符");

        int resolvedFixed = SeedMasking.ResolveConfiguredValue(
            fixedValue,
            135791357,
            () => throw new InvalidOperationException("固定值不应调用随机生成器"));
        AssertEqual(246813579, resolvedFixed, "固定值应直接解析为伪造 Seed");

        int resolvedRandom = SeedMasking.ResolveConfiguredValue(
            randomValue,
            135791357,
            () => 987654321);
        AssertEqual(987654321, resolvedRandom, "随机占位符应使用本回合生成值");

        int resolvedFallback = SeedMasking.ResolveConfiguredValue(
            "invalid",
            135791357,
            () => throw new InvalidOperationException("非法值不应调用随机生成器"));
        AssertEqual(SeedMasking.DefaultFallbackSeed, resolvedFallback, "非法值应回退到备用值");

        int customFallback = SeedMasking.ResolveConfiguredValue(
            "invalid",
            135791357,
            () => throw new InvalidOperationException("非法值不应调用随机生成器"),
            333333333);
        AssertEqual(333333333, customFallback, "非法值应使用配置的备用值");

        Console.WriteLine("SeedMasking tests passed: 9");
        return 0;
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"{message}；Expected={expected}; Actual={actual}");
        }
    }

    private static void AssertNotEqual(int unexpected, int actual, string message)
    {
        if (unexpected == actual)
        {
            throw new InvalidOperationException($"{message}；Value={actual}");
        }
    }

    private static void AssertEqual(string expected, string actual, string message)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{message}；Expected={expected}; Actual={actual}");
        }
    }

    private static void AssertInRange(int value, string message)
    {
        if (value < 1 || value > int.MaxValue)
        {
            throw new InvalidOperationException($"{message}；Value={value}");
        }
    }
}
