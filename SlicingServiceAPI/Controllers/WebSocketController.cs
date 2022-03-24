using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SlicingServiceAPI.Controllers
{
    [ApiController]
    public class WebSocketController : Controller
    {
        private readonly SlicingService _slicingService;

        public WebSocketController(SlicingService slicingService)
        {
            _slicingService = slicingService;
        }

        [HttpGet("/ws")]
        [Authorize]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await new WebSocketHandler(_slicingService).Handle(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}
