using System;
using System.Threading;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using OpenDash.OverlayCore.Services;

namespace OpenDash.DiscordChatOverlay;

internal static class Program
{
    private const string MutexName = "DiscordChatOverlay_SingleInstance";

    [STAThread]
    private static void Main()
    {
        LogService.Initialize("DiscordChatOverlay");

        // Catch fatal CLR exceptions on any thread (e.g. StackOverflow isn't catchable,
        // but AccessViolation and other SEH-backed exceptions are with this handler).
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledDomainException;

        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            LogService.Info("DiscordChatOverlay is already running. Exiting duplicate instance.");
            return;
        }

        try
        {
            LogService.Info("Program.Main: creating App instance.");
            var app = new App();

            // App.xaml sets ShutdownMode="OnExplicitShutdown" but that attribute is only
            // applied when the generated App.g.cs Main() calls InitializeComponent().
            // Calling InitializeComponent() before Run() triggers a DllNotFoundException
            // inside WPF's HwndSubclass before the message pump exists, so we set the
            // property directly instead.
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Catch unhandled exceptions on the WPF Dispatcher thread (UI thread).
            // Without this, a dispatcher exception silently terminates the process in
            // Release builds; here we log it and show a MessageBox so the user can report it.
            app.DispatcherUnhandledException += OnDispatcherUnhandledException;

            LogService.Info("Program.Main: calling app.Run().");
            app.Run();
        }
        catch (Exception ex)
        {
            ReportFatal("Unhandled exception in Application.Run", ex);
            throw;
        }
    }

    private static void OnDispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        ReportFatal("Unhandled WPF dispatcher exception", e.Exception);
        // Do not set e.Handled = true — let the default crash-and-exit behaviour proceed
        // so the process terminates rather than continuing in an unknown state.
    }

    private static void OnUnhandledDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ReportFatal("Unhandled AppDomain exception", ex);
    }

    /// <summary>
    /// Logs the exception and shows a MessageBox so the error is visible even if the
    /// log file is locked (e.g. the file is open in an IDE tab).
    /// </summary>
    private static void ReportFatal(string context, Exception ex)
    {
        LogService.Error(context, ex);

        try
        {
            MessageBox.Show(
                $"{context}:\n{ex.Message}\n\nSee log:\n{LogService.GetLogPath()}",
                "Discord Chat Overlay — Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // MessageBox itself failed — nothing more we can do.
        }
    }
}
