using SoftdocMusicPlayer.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace SoftdocMusicPlayer.Services
{
    public static class ThemeSelectorService
    {
        private const string SettingsKey = "AppBackgroundRequestedTheme";

        public static ElementTheme Theme { get; set; } = ElementTheme.Default;

        /// <summary>
        /// InitializeTheme()
        /// </summary>
        /// <returns></returns>
        public static async Task InitializeAsync()
        {
            Theme = await LoadThemeFromSettingsAsync();
        }

        /// <summary>
        /// set <paramref name="theme"/>
        /// </summary>
        /// <param name="theme"></param>
        public static async Task SetThemeAsync(ElementTheme theme)
        {
            Theme = theme;

            await SetRequestedThemeAsync();
            await SaveThemeInSettingsAsync(Theme);
        }

        /// <summary>
        /// set requested theme
        /// </summary>
        /// <returns></returns>
        public static async Task SetRequestedThemeAsync()
        {
            foreach (var view in CoreApplication.Views)
            {
                // set requested theme for all pages/views
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (Window.Current.Content is FrameworkElement frameworkElement)
                    {
                        frameworkElement.RequestedTheme = Theme;
                    }
                });
            }
            // Update title bar
            await SetupTitlebarAsync();
        }

        public static ElementTheme TrueTheme()
        {
            // get framework element
            // and return its actual theme.
            var frameworkElement = Window.Current.Content as FrameworkElement;
            return frameworkElement.ActualTheme;
        }

        public static string GetSystemControlForegroundColorForThemeHex()
        {
            return (TrueTheme() == ElementTheme.Dark ? "#FFFFFF" : "#000000");
        }

        /// <summary>
        /// update title bar
        /// </summary>
        private static async Task SetupTitlebarAsync()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView"))
            {
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                if (titleBar != null)
                {
                    // set title bar button's background color.
                    titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;

                    if (Theme == ElementTheme.Default)
                    {
                        // Windows default theme
                        if (TrueTheme() == ElementTheme.Dark)
                        {
                            // Dark theme
                            await SetLightColor(titleBar);
                        }
                        else
                        {
                            // Light theme
                            await SetDarkColor(titleBar);
                        }
                    }
                    else if (TrueTheme() == ElementTheme.Dark)
                    {
                        // App Dark theme
                        await SetLightColor(titleBar);
                    }
                    else
                    {
                        // App Light theme
                        await SetDarkColor(titleBar);
                    }
                    // extend application view into title bar.
                    CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                }
            }
        }

        /// <summary>
        /// update title bar for light theme
        /// </summary>
        private static async Task SetLightColor(ApplicationViewTitleBar titleBar)
        {
            await Task.Run(() =>
            {
                titleBar.ButtonForegroundColor = Windows.UI.Colors.White;
                titleBar.ButtonHoverForegroundColor = Windows.UI.Colors.White;
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(155, 25, 25, 25);
            });
        }

        /// <summary>
        /// update title bar for dark theme
        /// </summary>
        private static async Task SetDarkColor(ApplicationViewTitleBar titleBar)
        {
            await Task.Run(() =>
            {
                titleBar.ButtonForegroundColor = Windows.UI.Colors.Black;
                titleBar.ButtonHoverForegroundColor = Windows.UI.Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(155, 230, 230, 230);
            });
        }

        /// <summary>
        /// Return theme from settings. By default returns system default theme.
        /// </summary>
        public static async Task<ElementTheme> LoadThemeFromSettingsAsync()
        {
            ElementTheme cacheTheme = ElementTheme.Default;
            string themeName = await ApplicationData.Current.LocalSettings.ReadAsync<string>(SettingsKey);

            if (!string.IsNullOrEmpty(themeName))
            {
                Enum.TryParse(themeName, out cacheTheme);
            }

            return cacheTheme;
        }

        /// <summary>
        /// Save <paramref name="theme"/> in settings
        /// </summary>
        /// <param name="theme"></param>
        private static async Task SaveThemeInSettingsAsync(ElementTheme theme)
        {
            await ApplicationData.Current.LocalSettings.SaveAsync(SettingsKey, theme.ToString());
        }
    }
}
