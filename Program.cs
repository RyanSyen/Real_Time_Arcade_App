var builder = WebApplication.CreateBuilder(args);

// TODO(5B): Increase message size limit (default 32 KB)
builder.Services.AddSignalR(options => {
    options.MaximumReceiveMessageSize = null;
});

var app = builder.Build();

app.UseFileServer();
app.MapHub<ChatHub>("/hub");
app.Run();