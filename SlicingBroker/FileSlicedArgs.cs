using System;

namespace SlicingBroker
{
    public class FileSlicedArgs : EventArgs
    {
        public FileSlicedArgs(bool success, string filePath = null)
        {
            Success = success;
            SlicedFilePath = filePath;
        }

        public string SlicedFilePath { get; set; }
        public bool Success { get; set; }
    }
}