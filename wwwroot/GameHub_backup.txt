using Microsoft.AspNetCore.SignalR;

// ============================================================================================
// Class: Player
// ============================================================================================
    
public class Player
{
    private const int STEP = 1; // -> press space button, how many steps you want to move
    private const int FINISH = 100; // finishing step

    public string Id { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
    public int Count { get; set; } = 0;
    //count value is 0, before start movement
    //if count is greater or equals to FINISH, then considered win
    public bool isWin => Count >= FINISH;
    // if count is greater or equals to finish,then its true (win)
    
    public Player(string id, string icon, string name) => (Id, Icon, Name) = (id, icon, name); // player constructor to create player object

    public void Run() => Count += STEP; // increase count based on step given
}



// ============================================================================================
// Class: Game
// ============================================================================================

public class Game
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); //unique ID
    public Player? PlayerA { get; set; } = null;
    public Player? PlayerB { get; set; } = null;
    public bool IsWaiting { get; set; } = false; // a waiting for b

    public bool isEmpty => PlayerA == null && PlayerB == null;
    public bool isFull  => PlayerA != null && PlayerB != null;

    public string? AddPlayer(Player player) 
    {
        if (PlayerA == null)
        {
            PlayerA = player;
            IsWaiting = true;
            return "A";
        }
        else if (PlayerB == null)
        {
            PlayerB = player;
            IsWaiting = false;
            return "B";
        }

        return null;
    }
}



// ============================================================================================
// Class: GameHub 👦🏻👧🏻
// ============================================================================================

public class GameHub : Hub
{
    // ----------------------------------------------------------------------------------------
    // General
    // ----------------------------------------------------------------------------------------

    private static List<Game> games = new()
    {
        // new Game { PlayerA = new Player("P001", "👦🏻", "Boy") , IsWaiting = true },
        // new Game { PlayerA = new Player("P002", "👧🏻", "Girl"), IsWaiting = true },
    };

    public string Create()
    {
        var game = new Game();
        games.Add(game);
        return game.Id;
    }

    // ----------------------------------------------------------------------------------------
    // Functions
    // ----------------------------------------------------------------------------------------

    private async Task UpdateList(string? id = null)
    {
        var list = games.FindAll(g => g.IsWaiting);

        if (id == null)
        {
            await Clients.All.SendAsync("UpdateList", list);
        }
        else
        {
            await Clients.Client(id).SendAsync("UpdateList", list);
        }
    }

    // ----------------------------------------------------------------------------------------
    // Connected
    // ----------------------------------------------------------------------------------------

    public override async Task OnConnectedAsync()
    {
        string page = Context.GetHttpContext()?.Request.Query["page"] ?? "";

        switch (page)
        {
            case "list": await ListConnected(); break;
            case "game": await GameConnected(); break;
        }

        await base.OnConnectedAsync();
    }

    private async Task ListConnected()
    {
        string id = Context.ConnectionId;
        await UpdateList(id);
    }

    private async Task GameConnected()
    {
        string id     = Context.ConnectionId;
        string icon   = Context.GetHttpContext()?.Request.Query["icon"]   ?? "";
        string name   = Context.GetHttpContext()?.Request.Query["name"]   ?? "";
        string gameId = Context.GetHttpContext()?.Request.Query["gameId"] ?? "";

        var game = games.Find(g => g.Id == gameId);
        if (game == null || game.isFull)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }

        var player = new Player(id, icon, name);
        var letter = game.AddPlayer(player);
        await Groups.AddToGroupAsync(id, gameId);
        await Clients.Group(gameId).SendAsync("Ready", letter, game);
        await UpdateList();
    }

    // ----------------------------------------------------------------------------------------
    // Disconnected
    // ----------------------------------------------------------------------------------------

    public override async Task OnDisconnectedAsync(Exception? exception) 
    {
        string page = Context.GetHttpContext()?.Request.Query["page"] ?? "";

        switch (page)
        {
            case "list": ListDisconnected(); break;
            case "game": await GameDisconnected(); break;
        }

        await base.OnDisconnectedAsync(exception);
    }

    private void ListDisconnected()
    {
        // Do nothing
    }

    private async Task GameDisconnected()
    {
        string id     = Context.ConnectionId;
        string gameId = Context.GetHttpContext()?.Request.Query["gameId"] ?? "";

        var game = games.Find(g => g.Id == gameId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }

        if (game.PlayerA?.Id == id)
        {
            game.PlayerA = null;
            await Clients.Group(gameId).SendAsync("Left", "A");
        }
        else if (game.PlayerB?.Id == id) 
        {
            game.PlayerB = null;
            await Clients.Group(gameId).SendAsync("Left", "B");
        }

        if (game.isEmpty)
        {
            games.Remove(game);
            await UpdateList();
        }
    }

    // End of GameHub -------------------------------------------------------------------------
}