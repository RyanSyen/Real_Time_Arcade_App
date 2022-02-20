using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    //provide namespace is optional

    private static int count = 0; //static allows all users to share the variable
                                  //eveytime a user connects, then it will increase by 1
                                  //the proper method should be to lock the variable when during increment and then unlock it to prevent multiple access issue

    public async Task SendText(string name, string message)
    {
        //await Clients.All -> send message to all clients
        //await Clients.Caller -> send message to caller (sender)
        //await Clients.Others -> everyone except the caller
        await Clients.Caller.SendAsync("ReceiveText", name, message, "caller");

        //SendAsync -> allow to call client method
        //first param is a client js method name
        //additional param is caller which represents the msg is sent from the caller
        await Clients.Others.SendAsync("ReceiveText", name, message, "others");
        //the last param is to differentiate the parties and also to do different styling
    }

    //handle connected & disconnected events
    public override async Task OnConnectedAsync()
    {
        //when user is connected, we want to retrieve their name which is embedded in the query param (/hub + param)
        string? name = Context.GetHttpContext()?.Request.Query["name"]; //-> may return a null value, thats why we put ?
        //if the front part Context.GetHttpContext() is null, then Request.Query["name"]; will not be executed and just return a null value
        //we have to make our datatype string nullable too by adding ?

        //null checking
        if (name != null)
        {
            count++;
            //the proper method would be to lock the variable and release after increment, since our app is small should be ok

            //call client method to display message
            await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> joined");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception) //exception? means the value is nullable
    {
        string? name = Context.GetHttpContext()?.Request.Query["name"];
        if (name != null)
        {
            count--;
            await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> left");
        }
        await base.OnDisconnectedAsync(exception);
    }

    //send image
    public async Task SendImage(string name, string url)
    {
        await Clients.Caller.SendAsync("ReceiveImage", name, url, "caller");
        await Clients.Others.SendAsync("ReceiveImage", name, url, "others");

    }

    public async Task SendYouTube(string name, string id)
    {
        await Clients.Caller.SendAsync("ReceiveYouTube", name, id, "caller");
        await Clients.Others.SendAsync("ReceiveYouTube", name, id, "others");
    }
}