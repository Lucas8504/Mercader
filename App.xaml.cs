namespace Mercader
{
    public partial class App : Application
    {
        static DataRepository? dataRepo;

        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        public static DataRepository DataRepo
        {
            get
            {
                if (dataRepo == null)
                {
                    dataRepo = new DataRepository(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "mercader.db3"));
                }
                return dataRepo;
            }
        }
    }

}
