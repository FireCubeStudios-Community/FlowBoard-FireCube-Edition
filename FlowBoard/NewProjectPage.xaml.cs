using FlowBoard.Helpers;
using FlowBoard.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FlowBoard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewProjectPage : Page
    {
        public NewProjectPage()
        {
            this.InitializeComponent();
            WindowService.Initialize(AppTitleBar);
        }

        private async void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            Ring.Visibility = Visibility.Visible;
            Content.Opacity = 0;
            await Task.Run(() =>
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    string fileName = Name.Text;
                    if (fileName == "")
                    {
                        fileName = await FileHelper.GetSequentialNewWhiteboardFilename();
                    }

                    try
                    {
                        if (Name.Text != "" && await FileHelper.IsFilePresent(fileName))
                        {
                            Ring.Visibility = Visibility.Collapsed;
                            Content.Opacity = 1;
                            Status.Text = "Project with the same name already exists";
                            return;
                        }
                    }
                    catch
                    {
                        //Invalid file name
                        Ring.Visibility = Visibility.Collapsed;
                        Content.Opacity = 1;
                        Status.Text = "Invalid project name";
                        return;
                    }
                    bool success = await FileHelper.CreateProjectAsync(fileName, UIHelper.IndexToColor(Backgrounds.SelectedIndex).Color);
                    if(!success)
                    {
                        Ring.Visibility = Visibility.Collapsed;
                        Content.Opacity = 1;
                        Status.Text = "Something went wrong";
                    }
                })
            );
        }
    }
}
