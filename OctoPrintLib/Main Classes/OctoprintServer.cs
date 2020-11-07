using Newtonsoft.Json.Linq;
using OctoPrintLib.OctoPrintEvents;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace OctoPrintLib

{
    /// <summary>
    /// is the base Class connecting your project to different parts of Octoprint.
    /// </summary>
    public class OctoprintServer
    {
        /// <summary>
        /// The base URL like https://192.168.1.2/
        /// </summary>
        public string BaseURL { get; private set; }

        /// <summary>
        /// The end point Api Key like "ABCDE12345"
        /// </summary>
        public string ApplicationKey { get; private set; }

        /// <summary>
        /// The Websocket Client
        /// </summary>
        ClientWebSocket WebSocket { get; set; }
        /// <summary>
        /// Defines if the WebsocketClient is listening and the Tread is running
        /// </summary>
        public volatile bool listening;
        /// <summary>
        /// The size of the web socket buffer. Should work just fine, if the Websocket sends more, it will be split in 4096 Byte and reassembled in this class.
        /// </summary>
        public int WebSocketBufferSize = 4096;


        public OctoprintFileOperation FileOperations { get; private set; }

        public OctoprintGeneral GeneralOperations { get; private set; }

        /// <summary>
        /// Creates a <see cref="T:OctoprintClient.OctoprintConnection"/> 
        /// </summary>
        /// <param name="baseURL">The endpoint Address like "http://192.168.1.2/"</param>
        /// <param name="aK">The Api Key of the User account you want to use. You can get this in the user settings</param>
        public OctoprintServer(string baseURL, string aK)
        {
            BaseURL = baseURL;
            ApplicationKey = aK;
            FileOperations = new OctoprintFileOperation(this);
            GeneralOperations = new OctoprintGeneral(this);
        }


        /// <summary>
        /// Starts the Websocket Thread.
        /// </summary>
        public async Task WebsocketStartAsync()
        {
            if (!listening)
            {
                listening = true;
                await ConnectWebsocket();
                await Task.Run(WebsocketSyncAsync);
            }
        }

        private async Task ConnectWebsocket()
        {
            var canceltoken = CancellationToken.None;
            WebSocket = new ClientWebSocket();
            await WebSocket.ConnectAsync(GetWebsocketURI(),
                canceltoken);
        }

        private Uri GetWebsocketURI()
        {
            return new Uri("ws://" + BaseURL.Replace("https://", "").Replace("http://", "") + "sockjs/websocket");
        }

        private async Task WebsocketSyncAsync()
        {
            var buffer = new byte[8096];
            while (!WebSocket.CloseStatus.HasValue && listening)
            {
                WebSocketReceiveResult received = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string text = System.Text.Encoding.UTF8.GetString(buffer, 0, received.Count);
                HandleWebSocketData(text);
            }
        }

        private void HandleWebSocketData(string data)
        {
            JObject obj;
            try
            {
                obj = JObject.Parse(data);
            }
            catch
            {
                return;
            }

            JToken events = obj?.Value<JToken>("event");

            if (events != null)
            {
                string eventName = events.Value<string>("type");

                if (eventName == "FileAdded")
                {
                    JObject eventpayload = events.Value<JObject>("payload");
                    //var file = new OctoprintFile(eventpayload);

                    //if (file.Type == "stl")
                    //{
                    //    //Download file and trigger slicing process
                    //}
                }
            }
        }

    }
}
