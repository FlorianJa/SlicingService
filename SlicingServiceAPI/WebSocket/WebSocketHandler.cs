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
        private readonly WebSocket _webSocket;

        public WebSocketHandler(SlicingService slicingService, WebSocket webSocket)
        {
            _slicingService = slicingService;
            _webSocket = webSocket;

            _slicingService.output += slicingService_output;
        }

        private async void slicingService_output(object? sender, string e)
        {
            var message = e;
            var messageBytes = Encoding.ASCII.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes, 0, message.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        internal async Task Handle()
        {
            StringBuilder stringbuilder = new StringBuilder();

            await SendWelcomeMessage();
            await SendSlicingProfils(_slicingService.GetConfigFileNames());
            
            var buffer = new byte[1024 * 8];

            while (_webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult received = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string text = Encoding.UTF8.GetString(buffer, 0, received.Count);
                stringbuilder.Append(text);
                if (received.EndOfMessage)
                {
                    await ParseInput(stringbuilder.ToString());
                    stringbuilder.Clear();
                }
            }

            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        private async Task ParseInput(string data)
        {
            PrusaSlicerCLICommands commands;// = PrusaSlicerCLICommands.Default;
            //commands.FileURI = "https://github.com/CreativeTools/3DBenchy/raw/master/Single-part/3DBenchy.stl";

            try
            {
                //convert json to object
                commands = JsonSerializer.Deserialize<PrusaSlicerCLICommands>(data, new JsonSerializerOptions() { IgnoreNullValues = true });
            }
            catch (Exception)
            {
                await SendErrorMessageForInvalidCommandsAsync();
                return;
            }

            if (!commands.isValid())
            {
                await SendErrorMessageForInvalidCommandsAsync();
                return;
            }

            await SendSlicingStartedAsync();
            FileSlicedArgs slicingResult;
            try
            {
                slicingResult = await _slicingService.SliceAsync(commands);
            }
            catch (FileNotFoundException e)
            {
                await SendErrorMessageForInvalidProfile();
                return;
            }

            await SendFileScliceCompledtedMessage(slicingResult);

        }

        private async Task SendWelcomeMessage()
        {
            var connectedMessage = new WelcomeMessage().ToString();
            var connectedMessageBytes = Encoding.ASCII.GetBytes(connectedMessage);
            await _webSocket.SendAsync(new ArraySegment<byte>(connectedMessageBytes, 0, connectedMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendSlicingProfils(List<string> profiles)
        {
            var profileList = new ProfileListMessage(profiles).ToString();
            var profileListBytes = Encoding.ASCII.GetBytes(profileList);
            await _webSocket.SendAsync(new ArraySegment<byte>(profileListBytes, 0, profileList.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
        private async Task SendFileScliceCompledtedMessage(FileSlicedArgs fileSlicedArgs)
        {
            //extract filename from path

            var fileName = fileSlicedArgs.SlicedFilePath.Substring(fileSlicedArgs.SlicedFilePath.LastIndexOf('\\') + 1);
            var apiLink = "/api/gcode/" + fileName;

            fileSlicedArgs.SlicedFilePath = apiLink;

            var gcodeLinkMessage = new SlicingCompletedMessage(fileSlicedArgs).ToString();
            var gcodeLinkMessageBytes = Encoding.ASCII.GetBytes(gcodeLinkMessage);
            await _webSocket.SendAsync(new ArraySegment<byte>(gcodeLinkMessageBytes, 0, gcodeLinkMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
               

        private async Task SendErrorMessageForInvalidCommandsAsync()
        {
            var errorMessage = new ErrorMessage(ErrorType.CommandError, "Command could not be parsed into Prusa CLI Command Object. Check JSON format.").ToString();
            var errorMessageBytes = Encoding.ASCII.GetBytes(errorMessage);
            await _webSocket.SendAsync(new ArraySegment<byte>(errorMessageBytes, 0, errorMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendErrorMessageForInvalidProfile()
        {
            var errorMessage = new ErrorMessage(ErrorType.InvalidProfile, "Invalid profile selected").ToString();
            var errorMessageBytes = Encoding.ASCII.GetBytes(errorMessage);
            await _webSocket.SendAsync(new ArraySegment<byte>(errorMessageBytes, 0, errorMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendSlicingStartedAsync()
        {
            var slicingStartedMessage = new ProgressMessage(ProgressState.Started).ToString();
            var slicingStartedBytes = Encoding.ASCII.GetBytes(slicingStartedMessage);
            await _webSocket.SendAsync(new ArraySegment<byte>(slicingStartedBytes, 0, slicingStartedMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
