global using System;
global using Microsoft.Maui;
global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Controls.Hosting;
global using Microsoft.Maui.Hosting;
global using Microsoft.Maui.Graphics;
namespace Mercader
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Configura tu ventana principal aquí
            var window = new Window(new AppShell());
            return window;
        }

    }
}
