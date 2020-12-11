using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SlicerConnector
{
    public class JsonMessage<T>
    {
        public string MessageType { get; set; }

        public T Payload { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class FileSlicedMessage: JsonMessage<string>
    {
        public FileSlicedMessage(string payload)
        {
            MessageType = "FileSliced";
            Payload = payload;
        }

        
    }

    public class SlicingProgressMessage : JsonMessage<string>
    {
        public SlicingProgressMessage(string payload)
        {
            MessageType = "SlicingProgress";
            Payload = payload;
        }
    }


    public class ErrorMessage : JsonMessage<string>
    {
        public ErrorMessage(string payload)
        {
            MessageType = "Error";
            Payload = payload;
        }
    }
}
