using SlicingServiceCommon;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SlicingServiceAPI
{
    internal class WebSocketHandler
    {
        private readonly SlicingService _slicingService;

        public WebSocketHandler(SlicingService slicingService)
        {
            _slicingService = slicingService;
        }


        internal async Task Handle(WebSocket webSocket)
        {
            StringBuilder stringbuilder = new StringBuilder();

            await SendWelcomeMessage(webSocket);
            await SendSlicingProfils(webSocket, _slicingService.GetConfigFileNames());

            var buffer = new byte[1024 * 8];

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string text = Encoding.UTF8.GetString(buffer, 0, received.Count);
                stringbuilder.Append(text);
                if (received.EndOfMessage)
                {
                    //await HandleDataInput(webSocket, stringbuilder.ToString());
                    stringbuilder.Clear();
                }
            }

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        private async Task ParseInput(WebSocket webSocket, string data)
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


            try
            {
                await SendSlicingStartedAsync(webSocket);

                var slicingResult = await _slicingService.SliceAsync(commands);

                await SendFileScliceCompledtedMessage(webSocket, slicingResult);

            }
            catch (FileNotFoundException e)
            {
                await SendErrorMessageForInvalidProfile(webSocket);
            }
            

            
            //var prusaSlicerBroker = new PrusaSlicerBroker(slicerPath);
            //prusaSlicerBroker.FileSliced += PrusaSlicerBroker_FileSliced(webSocket);
            //prusaSlicerBroker.DataReceived += async (sender, args) =>
            //{
            //    await SendSlicingProgressMessageAsync(webSocket, args);
            //};
            //await prusaSlicerBroker.SliceAsync(commands);
        }

        private static async Task SendWelcomeMessage(WebSocket webSocket)
        {
            var connectedMessage = new WelcomeMessage().ToString();
            var connectedMessageBytes = Encoding.ASCII.GetBytes(connectedMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(connectedMessageBytes, 0, connectedMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendSlicingProfils(WebSocket webSocket, List<string> profiles)
        {
            var profileList = new ProfileListMessage(profiles).ToString();
            var profileListBytes = Encoding.ASCII.GetBytes(profileList);
            await webSocket.SendAsync(new ArraySegment<byte>(profileListBytes, 0, profileList.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
        private static async Task SendFileScliceCompledtedMessage(WebSocket webSocket, FileSlicedArgs fileSlicedArgs)
        {
            //extract filename from path

            var fileName = fileSlicedArgs.SlicedFilePath.Substring(fileSlicedArgs.SlicedFilePath.LastIndexOf('\\') + 1);
            var apiLink = "/api/gcode/" + fileName;

            fileSlicedArgs.SlicedFilePath = apiLink;

            var gcodeLinkMessage = new SlicingCompletedMessage(fileSlicedArgs).ToString();
            var gcodeLinkMessageBytes = Encoding.ASCII.GetBytes(gcodeLinkMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(gcodeLinkMessageBytes, 0, gcodeLinkMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
               

        private static async Task SendErrorMessageForInvalidCommandsAsync(WebSocket webSocket)
        {
            var errorMessage = new ErrorMessage(ErrorType.CommandError, "Command could not be parsed into Prusa CLI Command Object. Check JSON format.").ToString();
            var errorMessageBytes = Encoding.ASCII.GetBytes(errorMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(errorMessageBytes, 0, errorMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendErrorMessageForInvalidProfile(WebSocket webSocket)
        {
            var errorMessage = new ErrorMessage(ErrorType.InvalidProfile, "Invalid profile selected").ToString();
            var errorMessageBytes = Encoding.ASCII.GetBytes(errorMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(errorMessageBytes, 0, errorMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendSlicingStartedAsync(WebSocket webSocket)
        {
            var slicingStartedMessage = new ProgressMessage(ProgressState.Started).ToString();
            var slicingStartedBytes = Encoding.ASCII.GetBytes(slicingStartedMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(slicingStartedBytes, 0, slicingStartedMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
