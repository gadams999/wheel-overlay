using System.Threading;
using OpenDash.OverlayCore.Services;

namespace OpenDash.DiscordChatOverlay;

internal static class Program
{
    private const string MutexName = "DiscordChatOverlay_SingleInstance";

    [STAThread]
    private static void Main()
    {
        LogService.Initialize("DiscordChatOverlay");

        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            LogService.Info("DiscordChatOverlay is already running. Exiting duplicate instance.");
            return;
        }

        try
        {
            var app = new App();
            app.Run();
        }
        catch (Exception ex)
        {
            LogService.Error("Unhandled exception in Application.Run", ex);
            throw;
        }
    }
}
