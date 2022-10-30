namespace RemoteController.Server.ViewModels;

public class MainViewModel : DependencyObject
{
    public TcpClient Server { get; set; }

    public string RemoteAddress { get; set; }
    public ushort RemotePortNumber { get; set; }

    public string Process { get; set; }
    public string Method { get; set; }

    public bool IsConnected { get; set; }
    public bool ConnectionProcessStarted { get; set; }

    public ObservableCollection<CustomProcess> Processes
    {
        get { return (ObservableCollection<CustomProcess>)GetValue(ProcessesProperty); }
        set { SetValue(ProcessesProperty, value); }
    }
    public static readonly DependencyProperty ProcessesProperty =
        DependencyProperty.Register("Processes", typeof(ObservableCollection<CustomProcess>), typeof(MainViewModel));

    public object IcoValue
    {
        get { return (object)GetValue(IcoValueProperty); }
        set { SetValue(IcoValueProperty, value); }
    }
    public static readonly DependencyProperty IcoValueProperty =
        DependencyProperty.Register("IcoValue", typeof(object), typeof(MainViewModel));

    public RelayCommand ConnectToServerCommand { get; set; }
    public RelayCommand DisConnectServerCommand { get; set; }
    public RelayCommand RunOperationCommand { get; set; }
    public RelayCommand SetOperationCommand { get; set; }


    public MainViewModel()
    {
        Server = new TcpClient();

        RemoteAddress = GetLocalIpAddress();
        RemotePortNumber = 27001;

        Process = string.Empty;
        Method = string.Format("List");

        IsConnected = false;
        ConnectionProcessStarted = false;

        Processes = new ObservableCollection<CustomProcess>();
        IcoValue = new PackIcon() { Kind = PackIconKind.AccessPointNetwork, Height = 30, Width = 30 };

        ConnectToServerCommand = new(_ => Task.Factory.StartNew(ConnectToServer), _ => !string.IsNullOrWhiteSpace(RemoteAddress) && RemotePortNumber != default && !IsConnected && !ConnectionProcessStarted);
        DisConnectServerCommand = new(_ => Task.Factory.StartNew(DisConnectServer), _ => IsConnected);
        RunOperationCommand = new(_ => Task.Factory.StartNew(RunOperation), _ => IsConnected);
        SetOperationCommand = new(sender => Method = sender?.ToString(), _ => IsConnected);

        var timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (_, _) => CommandManager.InvalidateRequerySuggested();
        timer.Start();
    }


    public string GetLocalIpAddress()
    {
        string IpAddress = string.Empty;

        foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && item.OperationalStatus == OperationalStatus.Up)
            {
                IPInterfaceProperties adapterProperties = item.GetIPProperties();
                if (adapterProperties.GatewayAddresses.FirstOrDefault() != null)
                    foreach (UnicastIPAddressInformation ip in adapterProperties.UnicastAddresses)
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork) { IpAddress = ip.Address.ToString(); break; }
            }

            if (!string.IsNullOrWhiteSpace(IpAddress)) break;
        }

        if (string.IsNullOrWhiteSpace(IpAddress))
        {
            IpAddress = (from address in NetworkInterface.GetAllNetworkInterfaces().Select(x => x.GetIPProperties()).SelectMany(x => x.UnicastAddresses).Select(x => x.Address)
                         where !IPAddress.IsLoopback(address) && address.AddressFamily == AddressFamily.InterNetwork
                         select address).FirstOrDefault().ToString();
        }

        return IpAddress;
    }

    private void ConnectToServer()
    {
        var dispatcher = Application.Current.Dispatcher;

        ConnectionProcessStarted = true;

        try
        {
            Server.Connect(new IPEndPoint(IPAddress.Parse(RemoteAddress), RemotePortNumber));
        }
        catch
        {
            ConnectionProcessStarted = false; return;
        }

        IsConnected = true;
        ConnectionProcessStarted = false;

        dispatcher.InvokeAsync(() => IcoValue = new PackIcon() { Kind = PackIconKind.AccessPointNetwork, Height = 30, Width = 30 });
    }

    private void DisConnectServer()
    {
        var dispatcher = Application.Current.Dispatcher;

        try
        {
            var stream = Server.GetStream();
            var data = Encoding.Default.GetBytes("DisConnected");

            stream.Write(data, 0, data.Length);

            Server.Close();
        }
        catch { }

        Server = new TcpClient();
        IsConnected = false;

        dispatcher.InvokeAsync(() => IcoValue = new PackIcon() { Kind = PackIconKind.AccessPointNetworkOff, Height = 30, Width = 30 });
    }

    private void RunOperation()
    {
        var networkStream = Server.GetStream();
        var command = new NetCommand(Method);

        string jsonString = string.Empty;
        byte[]? bytes = null;
        int length = default;

        try
        {
            if (Method == "List")
            {
                jsonString = JsonSerializer.Serialize(command);
                bytes = Encoding.Default.GetBytes(jsonString);
                networkStream.Write(bytes, 0, bytes.Length);

                bytes = new byte[1024 * 10000];
                length = networkStream.Read(bytes, 0, bytes.Length);
                jsonString = Encoding.Default.GetString(bytes, 0, length);

                var processes = JsonSerializer.Deserialize<List<CustomProcess>>(jsonString);

                Application.Current.Dispatcher.Invoke(() => Processes = new ObservableCollection<CustomProcess>(processes));
            }
            else if (Method == "Create")
            {
                command.Content = JsonSerializer.Serialize(Process);

                jsonString = JsonSerializer.Serialize(command);
                bytes = Encoding.Default.GetBytes(jsonString);
                networkStream.Write(bytes, 0, bytes.Length);

                bytes = new byte[1024];
                length = networkStream.Read(bytes, 0, bytes.Length);
                jsonString = Encoding.Default.GetString(bytes, 0, length);

                MessageBox.Show(jsonString, "Response", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (Method == "Kill")
            {
                int processId;

                if (int.TryParse(Process, out processId))
                {
                    command.Content = JsonSerializer.Serialize(processId);

                    jsonString = JsonSerializer.Serialize(command);
                    bytes = Encoding.Default.GetBytes(jsonString);
                    networkStream.Write(bytes, 0, bytes.Length);

                    bytes = new byte[1024];
                    length = networkStream.Read(bytes, 0, bytes.Length);
                    jsonString = Encoding.Default.GetString(bytes, 0, length);

                    MessageBox.Show(jsonString, "Response", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else MessageBox.Show("Enter the process ID in the field!!", "Bad request", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (Method == "Help")
            {
                var helpMessage = "1) To create a process, write the name of the process in the field\n" +
                    "2) To close the process write the process ID in the field";

                MessageBox.Show(helpMessage, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else MessageBox.Show("Wrong operation!", "Bad request", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
            DisConnectServer();
        }
    }
}
