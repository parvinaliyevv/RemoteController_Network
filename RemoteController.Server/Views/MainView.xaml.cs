namespace RemoteController.Server.Views;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();

        DataContext = new MainViewModel();
    }


    private void CloseApp_ButtonClicked(object sender, RoutedEventArgs e)
    {
        (DataContext as MainViewModel).DisConnectServerCommand.Execute(null);
        Close();
    }

    private void DragWindow_MouseDown(object sender, MouseButtonEventArgs e) => DragMove();

    private void WindowMinimize_ButtonClicked(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
}
