using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SlicingServiceAPI.Controllers
{
    
    [ApiController]
    public class DownloadController : Controller
    {
        private string DataPath;
        public DownloadController(IConfiguration configuration)
        {
            var basePath = configuration.GetValue<string>("BasePath");
            DataPath = Path.Combine(basePath, "GCode");
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
        }

        [HttpGet("api/gcode/{filename}")]
        [Authorize]
        public async Task<IActionResult> DownloadFile(string filename)
        {
            var filePath = Path.Combine(DataPath, filename);
            if (CheckFileAvailability(filename, filePath, out string message))
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;
                return File(memory, "application/octet-stream", Path.GetFileName(filePath));
            }

            else
            {
                return StatusCode(404, message);
            }
        }

        private bool CheckFileAvailability(string filename, string filepath, out string message)
        {
            message = "";
            if (String.IsNullOrWhiteSpace(filename))
            {
                message = "filename not present";
                return false;
            }

            if (!System.IO.File.Exists(filepath))
            {
                message = "The requested file was not found";
                return false;
            }

            return true;
        }
    }
}
