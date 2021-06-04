using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GcodeToMesh;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OctoPrintLib;
using OctoPrintLib.Messages;
using SlicingBroker;

namespace SlicerConnector
{
    public class Startup
    {
        private string slicerPath;
        private string BasePath;

        public string ModelDownloadPath { get; private set; }
        public string GCodePath { get; private set; }
        public string MeshesPath { get; private set; }

        public string SlicingConfigPath { get; private set; }


        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            BasePath = configuration.GetValue<string>("BasePath");
            slicerPath = configuration.GetValue<string>("Slicer:Path");
            SlicingConfigPath = configuration.GetValue<string>("Slicer:ConfigPath");

            ModelDownloadPath = Path.Combine(BasePath, "Models");
            GCodePath = Path.Combine(BasePath, "GCode");

            if (!Directory.Exists(GCodePath))
            {
                Directory.CreateDirectory(GCodePath);
            }

            if (!Directory.Exists(ModelDownloadPath))
            {
                Directory.CreateDirectory(ModelDownloadPath);
            }

            if (!Directory.Exists(SlicingConfigPath))
            {
                Directory.CreateDirectory(SlicingConfigPath);
            }
        }

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
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
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
                            await HandleWebsocketConnectionAsync(context, webSocket);
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

        /// <summary>
        /// Handle requests for the SlicerConnector websocket
        /// </summary>
        /// <param name="context"></param>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task HandleWebsocketConnectionAsync(HttpContext context, WebSocket webSocket)
        {
            StringBuilder stringbuilder = new StringBuilder();

            await SendWelcomeMessage(webSocket);
            await SendSlicingProfils(webSocket);

            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string text = Encoding.UTF8.GetString(buffer, 0, received.Count);
                stringbuilder.Append(text);
                if (received.EndOfMessage)
                {
                    await HandleWebSocketDataAsync(webSocket, stringbuilder.ToString());
                    stringbuilder.Clear();
                }
            }

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        private async Task HandleWebSocketDataAsync(WebSocket webSocket, string data)
        {
            PrusaSlicerCLICommands commands;

            try
            {
                //convert json to object
                commands = JsonSerializer.Deserialize<PrusaSlicerCLICommands>(data, new JsonSerializerOptions() { IgnoreNullValues = true });
            }
            catch (Exception)
            {
                await SendErrorMessageForInvalidCommandsAsync(webSocket);
                return;
            }

            if (!commands.isValid())
            {
                await SendErrorMessageForInvalidCommandsAsync(webSocket);
                return;
            }

            // Ouput folder and config file path. These parameters need to be overwritten.
            commands.Output = GCodePath;
            if (!SetSlicingProfilPath(commands))
            {
                await SendErrorMessageForInvalidProfile(webSocket);
                return;
            }

            await SendSlicingStartedAsync(webSocket);

            var prusaSlicerBroker = new PrusaSlicerBroker(slicerPath);
            prusaSlicerBroker.FileSliced += PrusaSlicerBroker_FileSliced(webSocket);
            prusaSlicerBroker.DataReceived += async (sender, args) =>
            {
                await SendSlicingProgressMessageAsync(webSocket, args);
            };

            var fileUri = new Uri(commands.FileURI);
            await DownloadModelAsync(fileUri);

            var localPath = Path.Combine(ModelDownloadPath, fileUri.Segments[^1]);

            // use the local file on the disk
            commands.File = localPath;
            await prusaSlicerBroker.SliceAsync(commands);
        }

        private async Task<bool> DownloadModelAsync(Uri localtion)
        {
            using (WebClient webclient = new WebClient())
            {
                try
                {
                    await webclient.DownloadFileTaskAsync(localtion, Path.Combine(ModelDownloadPath, localtion.Segments[^1]));
                }
                catch (Exception e)
                {
                    return false;
                }
                return true;
            }
        }

        private bool SetSlicingProfilPath(PrusaSlicerCLICommands commands)
        {
            if (commands.LoadConfigFile != null)
            {
                var configFilePath = Path.Combine(SlicingConfigPath, commands.LoadConfigFile);
                if (File.Exists(configFilePath))
                {
                    commands.LoadConfigFile = configFilePath;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private EventHandler<FileSlicedArgs> PrusaSlicerBroker_FileSliced(WebSocket websocket)
        {
            Action<object, FileSlicedArgs> action = async (sender, e) =>
            {
                if (e.Success)
                {
                    await SendLinkToGCodeAsync(websocket, e.SlicedFilePath);
                    
                    //trigger webhocks
                }
            };
            return new EventHandler<FileSlicedArgs>(action);
        }

        #region Websocket Send methods
        private static async Task SendSlicingProgressMessageAsync(WebSocket webSocket, DataReceivedEventArgs args)
        {
            var slicingProgressMessage = new SlicingProgressMessage(args.Data).ToString();
            var slicingProgressMessageBytes = Encoding.ASCII.GetBytes(slicingProgressMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(slicingProgressMessageBytes, 0, slicingProgressMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendSlicingStartedAsync(WebSocket webSocket)
        {
            var slicingStartedMessage = new ProgressMessage(ProgressState.Started).ToString();
            var slicingStartedBytes = Encoding.ASCII.GetBytes(slicingStartedMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(slicingStartedBytes, 0, slicingStartedMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendErrorMessageForInvalidProfile(WebSocket webSocket)
        {
            var errorMessage = new ErrorMessage(ErrorType.InvalidProfile, "Invalid profile selected").ToString();
            var errorMessageBytes = Encoding.ASCII.GetBytes(errorMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(errorMessageBytes, 0, errorMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task SendWelcomeMessage(WebSocket webSocket)
        {
            var connectedMessage = new WelcomeMessage().ToString();
            var connectedMessageBytes = Encoding.ASCII.GetBytes(connectedMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(connectedMessageBytes, 0, connectedMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendSlicingProfils(WebSocket webSocket)
        {
            var profileList = new ProfileListMessage(GetConfigFileNames()).ToString();
            var profileListBytes = Encoding.ASCII.GetBytes(profileList);
            await webSocket.SendAsync(new ArraySegment<byte>(profileListBytes, 0, profileList.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task SendErrorMessageForInvalidCommandsAsync(WebSocket webSocket)
        {
            var errorMessage = new ErrorMessage(ErrorType.CommandError, "Command could not be parsed into Prusa CLI Command Object. Check JSON format.").ToString();
            var errorMessageBytes = Encoding.ASCII.GetBytes(errorMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(errorMessageBytes, 0, errorMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task SendLinkToGCodeAsync(WebSocket webSocket, string path)
        {
            //extract filename from path

            var fileName = path.Substring(path.LastIndexOf('\\')+1);
            var apiLink = "/api/gcode/" + fileName;

            var gcodeLinkMessage = new GcodeLinkMessage(apiLink).ToString();
            var gcodeLinkMessageBytes = Encoding.ASCII.GetBytes(gcodeLinkMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(gcodeLinkMessageBytes, 0, gcodeLinkMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        #endregion

        private List<string> GetConfigFileNames()
        {
            var ret = new List<string>();
            foreach (var path in Directory.GetFiles(SlicingConfigPath))
            {
                ret.Add(Path.GetFileName(path));
            }
            return ret;
        }

    }
}
