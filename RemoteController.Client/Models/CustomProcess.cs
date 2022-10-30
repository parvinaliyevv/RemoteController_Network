namespace RemoteController.Client.Models;

public class CustomProcess
{
    public int? ProcessId { get; set; }
    public string? ProcessName { get; set; }
    public string? MachineName { get; set; }


    public CustomProcess()
    {
        ProcessId = null;
        ProcessName = null;
        MachineName = null;
    }

    public CustomProcess(Process process)
    {
        ProcessId = process.Id;
        ProcessName = process.ProcessName;
        MachineName = Environment.MachineName;
    }
}
