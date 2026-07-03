using Mercader.Services.Interfaces;

namespace Mercader.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task PushModalAsync<TPage>() where TPage : class
        {
            var page = _serviceProvider.GetRequiredService<TPage>();
            await Shell.Current.Navigation.PushModalAsync((Page)(object)page!);
        }
    }
}
