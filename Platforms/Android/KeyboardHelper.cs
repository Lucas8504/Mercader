#if ANDROID
using Android.Content;
using Android.Views.InputMethods;

namespace Mercader.Platforms.Android
{
    public static class KeyboardHelper
    {
        public static void Close()
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null)
                return;

            var imm = (InputMethodManager)
                activity.GetSystemService(Context.InputMethodService);

            var token = activity.CurrentFocus?.WindowToken;
            if (token != null)
            {
                imm.HideSoftInputFromWindow(token, HideSoftInputFlags.None);
            }

            activity.CurrentFocus?.ClearFocus();
        }
    }
}
#endif
