using TinyDNS;

namespace TinySandbox;

internal class Program
{
    private static async Task Main()
    {
        Log.Init();

        await RecursiveResolver.Resolve("www.example.com");
    }
}
