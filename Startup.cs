using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Net.WebSockets;
using WebSocketServer.Middleware;

namespace WebSocketServer
{
    public class Startup
    {        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketConnectionManager();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseWebSockets();
            
            app.UseWebSocketServer();

            app.Run(async context => {
                Console.WriteLine("Hello from the 3rd request delegate");
                await context.Response.WriteAsync("Hello from 3rd request delegate.");
            });
        }
    }
}
