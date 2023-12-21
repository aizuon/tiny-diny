using System.Net;
using Serilog;
using Serilog.Core;
using TinyDNS;
using Log = Serilog.Log;

public static class RecursiveResolver
{
    private static readonly ILogger Logger =
        Log.ForContext(Constants.SourceContextPropertyName, nameof(RootResolver));

    public static async Task<IPAddress> Resolve(string qname)
    {
        var response = await RootResolver.Resolve(qname);

        return response;
    }
}
