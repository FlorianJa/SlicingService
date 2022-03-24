using System.Text.Json;

namespace SlicingServiceCommon
{
    public class FileSlicedArgs : EventArgs
    {
        public FileSlicedArgs(bool success, string filePath = null, int days = 0, int hours = 0, int minutes = 0, float usedFilament = 0f)
        {
            Success = success;
            SlicedFilePath = filePath;
            Days = days;
            Hours = hours;
            Minutes = minutes;
            UsedFilament = usedFilament;
        }

        public string SlicedFilePath { get; set; }
        public int Days { get; }
        public int Hours { get; }
        public int Minutes { get; }
        public float UsedFilament { get; }
        public bool Success { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}