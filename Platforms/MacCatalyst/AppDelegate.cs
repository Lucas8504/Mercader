using Foundation;
global using System;
global using Microsoft.Maui;
global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Controls.Hosting;
global using Microsoft.Maui.Essentials;
global using Microsoft.Maui.Hosting;
global using Microsoft.Maui.Graphics;

namespace Mercader
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
