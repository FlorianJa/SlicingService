using System;

namespace SlicingBroker
{
    public class FileSlicedArgs : EventArgs
    {
        public FileSlicedArgs(string filePath)
        {
            SlicedFilePath = filePath;
        }

        public string SlicedFilePath { get; set; }
    }
}