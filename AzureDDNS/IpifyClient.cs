using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SC = AzureDDNS.AzureDDNSSerializerContext;

namespace AzureDDNS;

// https://www.ipify.org
public class IpifyClient(HttpClient client)
{
    public async Task<IpifyResult?> GetAsync(CancellationToken cancellationToken = default)
    {
        // api64.ipify.org will give either IPv4 or IPv6
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api64.ipify.org?format=json");
        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync(SC.Default.IpifyResult, cancellationToken);
    }
}

public record IpifyResult([property: JsonPropertyName("ip")] IPAddress IP);
