using System.Threading;

namespace SpeakerKeepAwake;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var instanceMutex = new Mutex(initiallyOwned: true, @"Local\SpeakerKeepAwake-8E5352C7", out var isFirstInstance);
        if (!isFirstInstance) return;

        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.System);
        using var context = new TrayApplicationContext();
        Application.Run(context);
        GC.KeepAlive(instanceMutex);
    }
}
