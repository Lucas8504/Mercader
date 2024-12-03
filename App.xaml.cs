namespace Mercader
{
    public partial class App : Application
    {

        public static string? DatabasePath { get; private set; }
        public App()
        {
            InitializeComponent();
            DatabasePath = Path.Combine(FileSystem.AppDataDirectory, "mercader.db3");
            MainPage = new AppShell();
        }


    }

}
