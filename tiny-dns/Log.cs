using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace TinyDNS;

public static class Log
{
    public static void Init()
    {
        Serilog.Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(log =>
                log.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log/log_.log"),
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] |{SrcContext}| {Message}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true))
            .WriteTo.Async(console =>
                console.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] |{SrcContext}| {Message}{NewLine}{Exception}"))
            .Enrich.With<ContextEnricher>()
#if DEBUG
            .MinimumLevel.Verbose()
#else
            .MinimumLevel.Information()
#endif
            .CreateLogger();
    }
}

public sealed class ContextEnricher : ILogEventEnricher
{
    private const int MaxLength = 18;
    private const string EmptyContext = "NULL";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var (_, value) = logEvent.Properties.FirstOrDefault(x => x.Key == Constants.SourceContextPropertyName);
        var ctx = (value != null ? value.ToString().Replace('\"', ' ') : EmptyContext).AsSpan();

        if (ctx.Length > MaxLength)
            ctx = ctx[..MaxLength];

        string newCtx = string.Empty;
        if (ctx.Length < MaxLength)
        {
            int l = (int)Math.Ceiling((double)(MaxLength - ctx.Length) / 2);
            newCtx = new string(' ', l);
        }

        newCtx = $"{newCtx}{ctx}{newCtx}";

        if (newCtx.Length > MaxLength)
            newCtx = newCtx[..MaxLength];

        var eventType = propertyFactory.CreateProperty("SrcContext", newCtx);
        logEvent.AddPropertyIfAbsent(eventType);
    }
}
