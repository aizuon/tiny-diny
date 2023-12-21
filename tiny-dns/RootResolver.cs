using System.Net;
using System.Net.Sockets;
using Serilog;
using Serilog.Core;
using TinyDNS.Packets;

namespace TinyDNS;

public static class RootResolver
{
    private const string RootServer = "198.41.0.4";

    private static readonly ILogger Logger =
        Serilog.Log.ForContext(Constants.SourceContextPropertyName, nameof(RootResolver));

    public static Task<IPAddress> Resolve(string qname)
    {
        return ResolveRecursive(qname, RootServer);
    }

    private static async Task<IPAddress> ResolveRecursive(string qname, string server)
    {
        var query = new DNSQuery
        {
            Header = new DNSHeader(),
            Question = new DNSQuestion { QName = qname }
        };
        var req = query.Serialize();

        using var client = new UdpClient();
        client.Connect(IPAddress.Parse(server), 53);
        await client.SendAsync(req.Buffer, req.Buffer.Length);
        var res = await client.ReceiveAsync();

        var buffer = new BinaryBuffer(res.Buffer);
        var response = DNSResponse.Deserialize(buffer);
        Logger.Debug("Deserialized response: {Response}", response);

        foreach (var answer in response.Answers)
            if (answer.ParsedRData is IPAddress ip)
                return ip;

        var glueRecords = response.Additionals
            .Where(a => a.Type == 1)
            .ToDictionary(a => a.Name, a => a.ParsedRData as IPAddress);

        foreach (var authority in response.Authorities)
            if (authority.Type == 2 && authority.ParsedRData is string nsHostname)
            {
                if (glueRecords.TryGetValue(nsHostname, out var nsIp))
                    return await ResolveRecursive(qname, nsIp.ToString());

                var resolvedNsIp = await ResolveRecursive(nsHostname, RootServer);
                if (resolvedNsIp != null)
                    return await ResolveRecursive(qname, resolvedNsIp.ToString());
            }

        return null;
    }
}
