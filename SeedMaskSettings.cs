using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SeedMaskProbe;

/// <summary>
/// 外部 JSON 中唯一的伪造值配置。
/// </summary>
[DataContract]
public sealed class SeedMaskSettings
{
    [DataMember(Name = "fake_seed")]
    public string? FakeSeed { get; set; }
}

/// <summary>
/// 读取 SeedMaskProbe 外部 JSON 配置。
/// </summary>
public static class SeedMaskSettingsLoader
{
    public const string DefaultJson = "{\r\n  \"fake_seed\": \"246813579\"\r\n}\r\n";

    public static string? ReadFakeSeed(string json)
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
        DataContractJsonSerializer serializer = new(typeof(SeedMaskSettings));
        SeedMaskSettings? settings = serializer.ReadObject(stream) as SeedMaskSettings;
        return settings?.FakeSeed;
    }
}
