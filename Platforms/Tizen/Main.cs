using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace LocationTrackerApp;

/// <summary>Retained Tizen template entry point; this project currently targets Android.</summary>
class Program : MauiApplication
{
    // Delegate dependency registration to the shared application builder.
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    static void Main(string[] args)
    {
        // Enter the native event loop for the platform application.
        var app = new Program();
        app.Run(args);
    }
}
