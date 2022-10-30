namespace RemoteController.Client.Models;

public class NetCommand
{
    public string Title { get; set; }

    public string? Content { get; set; }


    public NetCommand()
    {
        Title = string.Empty;
        Content = null;
    }

    public NetCommand(string title): this() => Title = title;
}
