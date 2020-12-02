using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using SlicingBroker;

namespace SlicerConnector
{
    public class Startup
    {
        private string slicerPath;

        public string ModelDownloadPath { get; private set; }
        public string GCodePath { get; private set; }
        public string MeshesPath { get; private set; }

        private OctoprintServer os;

        /// <summary>
        /// Domain name or IP of the OctoprintServer. Do not add protocol like http:// or https://. If a different Port than 80 is needed, specify it by :PORTNUMBER
        /// </summary>
        private string OctoPrintDomainNameOrIP;

        /// <summary>
        /// Application key for accessing th ocotprint
        /// </summary>
        private string OcotoprintApplicationKey;


        private string BasePath;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            var tmp = Configuration as ConfigurationRoot;

            OctoPrintDomainNameOrIP = configuration.GetValue<string>("OctoPrint:DomainNameOrIP");
            OcotoprintApplicationKey = configuration.GetValue<string>("OctoPrint:APIKey");
            BasePath = configuration.GetValue<string>("OctoPrint:BasePath");
            slicerPath = configuration.GetValue<string>("Slicer:Path");

            ModelDownloadPath = Path.Combine(BasePath, "Models");
            GCodePath = Path.Combine(BasePath, "GCode");
            MeshesPath = Path.Combine(BasePath, "Meshes");

            if (!File.Exists(slicerPath))
                throw new FileNotFoundException("The slicer application is not found in the given path");

            if (!Directory.Exists(ModelDownloadPath))
                Directory.CreateDirectory(ModelDownloadPath);


            os = new OctoprintServer(OctoPrintDomainNameOrIP, OcotoprintApplicationKey);
            os.FileAdded += Os_FileAdded;
            var x = os.GeneralOperations.Login();
            os.StartWebsocketAsync(x.name, x.session);
        }

        private async void Os_FileAdded(object sender, FileAddedEventArgs e)
        {
            if (e.Payload.type[0] == "model")
            {

                var downloadFullPath = Path.Combine(ModelDownloadPath, e.Payload.name);
                bool res = true;
                if (!System.IO.File.Exists(downloadFullPath))
                    res = await os.FileOperations.DownloadFileAsync(e.Payload.storage + "/" + e.Payload.path, downloadFullPath);

                if (res)
                {
                    SliceWithDefaultParameters(downloadFullPath);
                }
            }
        }

        private void SliceWithDefaultParameters(string inputFile)
        {
            Task.Run(async () =>
            {
                PrusaSlicerCLICommands commands = PrusaSlicerCLICommands.Default;
                commands.Output = GCodePath;
                if (!Directory.Exists(commands.Output))
                {
                    Directory.CreateDirectory(commands.Output);
                }

                commands.File = inputFile;
                var prusaSlicerBroker = new PrusaSlicerBroker(slicerPath);
                prusaSlicerBroker.FileSliced += PrusaSlicerBroker_FileSliced;
                await prusaSlicerBroker.SliceAsync(commands);
            });
        }

        private async void PrusaSlicerBroker_FileSliced(object sender, FileSlicedArgs e)
        {
            await UploadGCodeAsync(e.SlicedFilePath);
            GenerateMeshFromGcode(e.SlicedFilePath);
        }


        private async Task UploadGCodeAsync(string slicedFilePath)
        {
            await os.FileOperations.UploadFileAsync(slicedFilePath);

        }

        private void GenerateMeshFromGcode(string slicedFilePath)
        {
            //var gcodeAnalyser = new GcodeAnalyser();
            //gcodeAnalyser.MeshGenrerated += GcodeAnalyser_MeshGenrerated;
            //gcodeAnalyser.GenerateMeshFromGcode(slicedFilePath, MeshesPath);
        }

        //private void GcodeAnalyser_MeshGenrerated(object sender, bool e)
        //{

        //}

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
                            await HandleWebsocketConnection(context, webSocket);
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
        private async Task HandleWebsocketConnection(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                var receivedAsJson = Encoding.ASCII.GetString(buffer, 0, result.Count);

                //convert json to object
                PrusaSlicerCLICommands commands = JsonSerializer.Deserialize<PrusaSlicerCLICommands>(receivedAsJson, new JsonSerializerOptions() { IgnoreNullValues = true });
                commands.Output = GCodePath;
                if (!Directory.Exists(commands.Output))
                {
                    Directory.CreateDirectory(commands.Output);
                }
                if (commands.isValid())
                {
                    var prusaSlicerBroker = new PrusaSlicerBroker(slicerPath);
                    prusaSlicerBroker.FileSliced += PrusaSlicerBroker_FileSliced(webSocket);

                    prusaSlicerBroker.DataReceived += async (sender, args) =>
                    {
                        //var tmp = Encoding.ASCII.GetBytes("{" + args.Data +"}");
                        //await webSocket.SendAsync(new ArraySegment<byte>(tmp, 0, args.Data.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);
                    };

                    if (File.Exists(Path.Combine(ModelDownloadPath, commands.File)))
                    {
                        commands.File = Path.Combine(ModelDownloadPath, commands.File);
                        await prusaSlicerBroker.SliceAsync(commands);

                    }
                    else
                    {
                        // download file and slice afterwards
                    }
                }

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        private EventHandler<FileSlicedArgs> PrusaSlicerBroker_FileSliced(WebSocket websocket)
        {
            Action<object, FileSlicedArgs> action = (sender, e) =>
            {
                var _websocket = websocket;
                var gcodeAnalyser = new GcodeAnalyser();
                gcodeAnalyser.MeshGenrerated += GcodeAnalyser_MeshGenrerated(_websocket, Path.GetFileNameWithoutExtension(e.SlicedFilePath));
                gcodeAnalyser.GenerateMeshFromGcode(e.SlicedFilePath, MeshesPath);
            };

            return new EventHandler<FileSlicedArgs>(action);
        }


        private EventHandler<bool> GcodeAnalyser_MeshGenrerated(WebSocket websocket, string fileName)
        {
            Action<object, bool> action = (sender, e) =>
            {
                var _websocket = websocket;
                var _fileName = fileName;
                var args = new FileSlicedMessage("/api/Download/"+ _fileName).ToString();

                var tmp = Encoding.ASCII.GetBytes(args);

                _websocket.SendAsync(new ArraySegment<byte>(tmp, 0, args.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            };

            return new EventHandler<bool>(action);
        }
    }
}
