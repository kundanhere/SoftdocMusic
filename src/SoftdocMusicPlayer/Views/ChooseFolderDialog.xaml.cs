using DataAccessLibrary;
using SoftdocMusicPlayer.ViewModels;

using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SoftdocMusicPlayer.Views
{
    public sealed partial class ChooseFolderDialog : ContentDialog
    {
        public ChooseFolderDialog()
        {
            RequestedTheme = (Window.Current.Content as FrameworkElement).RequestedTheme;
            this.InitializeComponent();
            this.UpdateList();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Hide();
        }

        private async void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = await ChooseFolderDialogViewModel.GetFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to the picked folder
                var entries = DataAccess.CountFolder(folder.Name, folder.Path);
                if (entries == 0)
                {
                    // To prevent duplicate entries.
                    DataAccess.InsertFolderProperties(folder.Name, folder.Path);
                    UpdateList();
                    await ChooseFolderDialogViewModel.SaveFilePropertiesAsync(folder);
                }
            }
        }

        private async void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var name = GetButtonChildElement(sender, "DirectoryName");
            var path = GetButtonChildElement(sender, "DirectoryPath");

            // Create the message dialog and set its content
            var messageDialog = new MessageDialog($"If you remove the '{name}' folder from App, it won't appear in Music anymore, but won't be deleted.", "Remove this folder?");
            messageDialog.Commands.Add(new UICommand("Remove Folder", null));
            messageDialog.Commands.Add(new UICommand("Cancel", null));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 1;

            var result = await messageDialog.ShowAsync();

            if (result.Label.Equals("Remove Folder"))
            {
                DataAccess.RemoveFileProperties(path);
                DataAccess.RemoveFolder(name, path);
                UpdateList();
            }
        }

        /// <summary>
        /// Returns the specified child element of the button.
        /// </summary>
        private string GetButtonChildElement(object obj, string name)
        {
            var element = (obj as Button).FindName(name);
            if (element == null)
                return string.Empty;
            var prop = (element as TextBlock).Text;
            return prop;
        }

        /// <summary>
        /// UpdateList()
        /// </summary>
        private void UpdateList()
        {
            icFolderList.ItemsSource = DataAccess.GetFolderProperties();
        }
    }
}
