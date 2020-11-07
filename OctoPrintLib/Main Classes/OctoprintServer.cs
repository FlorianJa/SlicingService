using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OctoPrintLib.Operations;
using OctoPrintLib.Messages;

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
        public string DomainNmaeOrIp { get; private set; }

        /// <summary>
        /// The end point Api Key like "ABCDE12345"
        /// </summary>
        public string ApplicationKey { get; private set; }

        /// <summary>
        /// The Websocket Client
        /// </summary>
        private ClientWebSocket webSocket { get; set; }
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

        private string username;
        
        private string sessionID;

        /// <summary>
        /// Creates a <see cref="T:OctoprintClient.OctoprintConnection"/> 
        /// </summary>
        /// <param name="baseURL">The endpoint Address like "http://192.168.1.2/"</param>
        /// <param name="aK">The Api Key of the User account you want to use. You can get this in the user settings</param>
        public OctoprintServer(string baseURL, string aK)
        {
            DomainNmaeOrIp = baseURL;
            ApplicationKey = aK;
            FileOperations = new OctoprintFileOperation(this);
            GeneralOperations = new OctoprintGeneral(this);
        }


        /// <summary>
        /// Starts the Websocket Thread.
        /// </summary>
        public async Task StartWebsocketAsync(string user, string sessionID)
        {
            if (!listening)
            {
                listening = true;
                this.username = user;
                this.sessionID = sessionID;
                await ConnectWebsocket();
                Task.Run(WebsocketDataReceiverHandler);
            }
        }

        private async Task ConnectWebsocket()
        {
            var canceltoken = CancellationToken.None;
            webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(GetWebsocketURI(),
                canceltoken);
        }

        private async Task AuthenticateWebsocketAsync(string user, string sessionID)
        {
            
            var json = JsonSerializer.Serialize(new WebsocketAuthMessage() { auth = user + ":" + sessionID });
            var tmp = Encoding.ASCII.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(tmp, 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private Uri GetWebsocketURI()
        {
            return new Uri("ws://" + DomainNmaeOrIp + "/sockjs/websocket");
        }

        private async Task WebsocketDataReceiverHandler()
        {
            var buffer = new byte[8096];
            StringBuilder stringbuilder = new StringBuilder();
            while (!webSocket.CloseStatus.HasValue && listening)
            {
                WebSocketReceiveResult received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string text = Encoding.UTF8.GetString(buffer, 0, received.Count);

                if (text.Last() == '}')//json messages end with }. If the text does not end with } this indicates there is more data (message bigger then buffer)
                {
                    stringbuilder.Append(text);
                    var tmp = stringbuilder.ToString();
                    if (tmp.Count(c => c == '{') == tmp.Count(c => c == '}'))//there is the rare opportunity that the last } is not the closing } for the first {. In a valid json string are the same amount of { and }
                    {
                        HandleWebSocketData(stringbuilder.ToString());
                        stringbuilder.Clear();
                    } 
                }
                else
                {
                    stringbuilder.Append(text);
                }

            }
        }

        private void HandleWebSocketData(string data)
        {
            var messageType = GetMessageType(data);

            switch (messageType)
            {
                case MessageType.Connected:
                    {
                        try
                        {
                            var tmp = JsonSerializer.Deserialize<WebSocketConnectedMessage>(data, new JsonSerializerOptions() { IgnoreNullValues = true });
                        }
                        catch (Exception)
                        {

                            break;
                        }
                        AuthenticateWebsocketAsync(username, sessionID);
                        break;
                    }
                case MessageType.History:
                    {
                        try
                        {
                            var tmp = JsonSerializer.Deserialize<HistoryMessage>(data, new JsonSerializerOptions() { IgnoreNullValues = true });
                        }
                        catch (Exception)
                        {
                            
                        }
                        break;
                    }
                case MessageType.Event:
                    {
                        try
                        {
                            //There are a lot of different event with different payloads. We focus on just a few like FileAdded.
                            var tmp = JsonSerializer.Deserialize<OctoprintEvent>(data, new JsonSerializerOptions() { IgnoreNullValues = true });

                            if(tmp.Event.type == "FileAdded")
                            {
                                //do something
                            }
                        }
                        catch (Exception)
                        {

                        }
                        break;
                    }
                case MessageType.Current:
                    {
                        try
                        {
                            var tmp = JsonSerializer.Deserialize<CurrentMessage>(data, new JsonSerializerOptions() { IgnoreNullValues = true });
                        }
                        catch (Exception)
                        {

                        }
                        break;
                    }
                case MessageType.Unknown:
                    break;
                default:
                    break;
            }
        }

        private MessageType GetMessageType(string data)
        {
            //Messages start with {"MESSAGETYPE":  -> start index: 2, length: index of : minus 3
            var type = data.Substring(2, data.IndexOf(':') - 3);

            switch (type)
            {
                case "connected": return MessageType.Connected;
                case "event": return MessageType.Event;
                case "history": return MessageType.History;
                case "current":return MessageType.Current;
                default: return MessageType.Unknown;
            }
        }
    }

    public enum MessageType
    {
        Connected,
        Event,
        History,
        Current,
        Unknown
    }
}
