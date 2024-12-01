global using Android.App;
global using Android.Runtime;
global using System;
global using Microsoft.Maui;
global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Controls.Hosting;
global using Microsoft.Maui.Hosting;

namespace Mercader
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
