namespace Mercader.Services.Interfaces
{
    public interface INavigationService
    {
        Task PushModalAsync<TPage>() where TPage : Page;
    }
}
