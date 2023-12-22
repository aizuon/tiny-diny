using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Serilog.Core;
using TinyDNS.Packets;
using TinyDNS.Serialization;

namespace TinyDNS;

public static class RecursiveResolver
{
    private const string RootServer = "198.41.0.4";

    private static readonly ILogger Logger =
        Serilog.Log.ForContext(Constants.SourceContextPropertyName, nameof(RecursiveResolver));

    private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

    public static ValueTask<IPAddress> Resolve(string qname)
    {
        return ResolveRecursive(qname, RootServer);
    }

    private static async ValueTask<IPAddress> ResolveRecursive(string qname, string server)
    {
        if (Cache.TryGetValue(qname, out IPAddress cachedIpAddress))
        {
            Logger.Debug("Cache hit for {QName}", qname);
            return cachedIpAddress;
        }

        var query = new DNSQuery
        {
            Header = new DNSHeader(),
            Question = new DNSQuestion { QName = qname }
        };
        Logger.Debug("Querying {Server} with {Query}", server, query);
        var req = query.Serialize();

        using var client = new UdpClient();
        client.Connect(IPAddress.Parse(server), 53);
        await client.SendAsync(req.Buffer.AsMemory());
        var res = await client.ReceiveAsync();

        var buffer = new BinaryBuffer(res.Buffer);
        var response = DNSResponse.Deserialize(buffer);
        Logger.Debug("Deserialized response: {Response}", response);

        foreach (var answer in response.Answers)
            if (answer.ParsedRData is IPAddress ip)
            {
                Cache.Set(qname, ip, TimeSpan.FromSeconds(answer.TTL));
                return ip;
            }

        var glueRecords = response.Additionals
            .Where(a => a.Type == 1 && a.ParsedRData is IPAddress)
            .ToDictionary(a => a.Name, a => a);
        foreach ((string nsHostname, var glueRecord) in glueRecords)
            Cache.Set(nsHostname, glueRecord.ParsedRData as IPAddress,
                TimeSpan.FromSeconds(glueRecord.TTL));

        foreach (var authority in response.Authorities)
            if (authority.Type == 2 && authority.ParsedRData is string nsHostname)
            {
                if (glueRecords.TryGetValue(nsHostname, out var glueRecord))
                {
                    var nsIP = glueRecord.ParsedRData as IPAddress;
                    return await ResolveRecursive(qname, nsIP.ToString());
                }

                var resolvedNsIp = await ResolveRecursive(nsHostname, RootServer);
                if (resolvedNsIp != null)
                    return await ResolveRecursive(qname, resolvedNsIp.ToString());
            }

        return null;
    }
}
