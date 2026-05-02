using System.Windows;
using SeatTooLong.Core.Localization;

namespace SeatTooLong.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow(string appName, string version, ILocalizationService localization)
    {
        InitializeComponent();

        Title = localization.Get("tray.about");
        TxtAppName.Text = appName;
        TxtVersion.Text = string.Format(localization.Get("about.version"), appName, version);
        BtnClose.Content = localization.Get("about.close");
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
