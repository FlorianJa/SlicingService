using System.Net;

namespace SlicingServiceAPI
{
    public static class DownloadHelper
    {
        public static async Task<bool> DownloadModelAsync(Uri adress, string localFullFileName)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(adress);
                    using (var fs = new FileStream(localFullFileName, FileMode.OpenOrCreate))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
