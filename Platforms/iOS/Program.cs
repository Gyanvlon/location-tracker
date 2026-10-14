using ObjCRuntime;
using UIKit;

namespace LocationTrackerApp;

/// <summary>Platform template entry point for launching the native Apple application.</summary>
public class Program
{
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
