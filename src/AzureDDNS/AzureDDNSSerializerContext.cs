using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureDDNS;

[JsonSerializable(typeof(AzDdnsConfig))]
[JsonSerializable(typeof(IpifyResult))]

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,

    // Ignore default values to reduce the data sent after serialization
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

    // Do not indent content to reduce data usage
    WriteIndented = false,

    // Use SnakeCase because it is what the server provides
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.Unspecified,

    Converters = [typeof(Tingle.Extensions.Primitives.Converters.JsonIPAddressConverter)]
)]
internal partial class AzureDDNSSerializerContext : JsonSerializerContext { }
