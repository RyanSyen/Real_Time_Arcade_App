using Microsoft.AspNetCore.SignalR;

//Point class (X, Y)
public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

}

// Command class (Name, Param)
public class Command
{
    public string Name { get; set; }
    public object[] Param { get; set; } //flexible to accept multiple datatype

    public Command(string name, object[] param) => (Name, Param) = (name, param);
}

public class DrawHub : Hub
{
    // TODO
    private static List<Command> commands = new();

    //Client functions need to program in the draw.html
    //SendLine(a,b,size,color)
    public async Task SendLine(Point a, Point b, int size, string color)
    {
        commands.Add(new Command("drawLine", new object[] { a, b, size, color })); // if somebody call the sendline method, it will remember the command name as well as the param into the command list
        await Clients.Others.SendAsync("ReceiveLine", a, b, size, color);
    }

    //SendCurve(a,b,c,size,color)
    public async Task SendCurve(Point a, Point b, Point c, int size, string color)
    {
        commands.Add(new Command("drawCurve", new object[] { a, b, c, size, color }));
        await Clients.Others.SendAsync("ReceiveCurve", a, b, c, size, color);
    }
    //SendImage(url)
    public async Task SendImage(string url)
    {
        commands.Clear();
        commands.Add(new Command("drawImage", new object[] { url }));
        await Clients.Others.SendAsync("ReceiveImage", url);
    }

    //SendClear()
    public async Task SendClear()
    {
        commands.Clear();
        await Clients.Others.SendAsync("ReceiveClear");
    }


    public override async Task OnConnectedAsync()
    {
        // everytime a new client is connected, we would like to send the command to the client
        await Clients.Caller.SendAsync("ReceiveCommands", commands); // -> commands will be a list which will become a js array
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}