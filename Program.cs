using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// var builder = WebApplication.CreateBuilder(args);

// var builder = WebApplication.CreateBuilder(args);

// TODO(5B): Increase message size limit (default 32 KB)
// builder.Services.AddSignalR(options => {
//     options.MaximumReceiveMessageSize = null;
// });

// var app = builder.Build();

// app.UseFileServer();
// app.MapHub<ChatHub>("/hub");
// app.Run();

namespace Assignment
{
    public class Program
    {

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }

}


//app.MapGet("/", () => "Hello World!");

//Increase message size limit from 32KB to 128KB
// builder.Services.AddSignalR(options => {
//     options.MaximumReceiveMessageSize = 128 * 1024;
// });

// var app = builder.Build();

//enable static file serving
//meaning to allow static resources in root folder
// app.UseFileServer();

//map to hub by linking to this link /hub
// app.MapHub<ChatHub>("/chathub");
// app.MapHub<DrawHub>("/drawhub");
// app.MapHub<GameHub>("/gamehub");

// app.Run();
