var builder = WebApplication.CreateBuilder(args);


//app.MapGet("/", () => "Hello World!");

//Increase message size limit from 32KB to 128KB
builder.Services.AddSignalR(options => {
    options.MaximumReceiveMessageSize = 128 * 1024;
});

var app = builder.Build();

//enable static file serving
//meaning to allow static resources in root folder
app.UseFileServer();

//map to hub by linking to this link /hub
app.MapHub<ChatHub>("/chathub");
app.MapHub<DrawHub>("/drawhub");
app.MapHub<GameHub>("/gamehub");

app.Run();
