using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SlicingBroker;

namespace SlicerConnector
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseRouting();

            //app.UseAuthorization();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllers();
            //});


            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };

            app.UseWebSockets(webSocketOptions);


            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
                        {
                            await Echo(context, webSocket);
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });

        }


        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            var receivedAsJson = Encoding.ASCII.GetString(buffer,0,result.Count);

            //convert json to object
            PrusaSlicerCLICommands commands = null; //JsonSerializer.Deserialize<PrusaSlicerCLICommands>(receivedAsJson);

            var prusaSlicerBroker = new PrusaSlicerBroker(@"C:\Users\FlorianJasche\Downloads\PrusaSlicer-2.3.0-alpha2+win64-202010241601\PrusaSlicer-2.3.0-alpha2+win64-202010241601\prusa-slicer-console.exe");
            prusaSlicerBroker.DataReceived += async (sender, args) => {
                var tmp = Encoding.ASCII.GetBytes(args.Data);
                await webSocket.SendAsync(new ArraySegment<byte>(tmp, 0, args.Data.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);
            };


            await prusaSlicerBroker.SliceAsync(commands);


            //switch commands & call different functions




            //create parameter for the slicer

            //start the slicer process

            //during slicing, send output of slicer to client






            //while (!result.CloseStatus.HasValue)
            //{
            //    await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

            //    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            //}


            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        
    }
}
