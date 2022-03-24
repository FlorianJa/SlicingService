using SlicingServiceCommon;
using System.Text.Json;

namespace SlicingServiceAPI
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

    public class FileSlicedMessageArgs
    {
        public string File { get; set; }
        public string FilamentLength { get; set; }
        public string PrintTime { get; set; }

        public FileSlicedMessageArgs()
        {

        }
    }
    public class FileSlicedMessage: JsonMessage<FileSlicedMessageArgs>
    {
        public FileSlicedMessage(FileSlicedMessageArgs payload)
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

    public enum ProgressState
    {
        Started
    }

    public class ProgressMessage : JsonMessage<ProgressState>
    {
        public ProgressMessage(ProgressState state)
        {
            MessageType = "Progress";
            Payload = state;
        }
    }



    public enum ErrorType
    {
        CommandError,
        FileNotFound,
        InvalidProfile
    }

    [Serializable]
    public class ErrorMessagePayload
    {
        public ErrorType ErrorType { get; set; }
        public string Message { get; set; }

        private ErrorMessagePayload() { }

        public ErrorMessagePayload(ErrorType errorType, string message)
        {
            ErrorType = errorType;
            Message = message;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    

    public class ErrorMessage : JsonMessage<ErrorMessagePayload>
    {
        public ErrorMessage(ErrorType errorType, string message)
        {
            MessageType = "Error";
            Payload = new ErrorMessagePayload(errorType, message);
        }
    }
    public class SlicingCompletedMessage : JsonMessage<FileSlicedArgs>
    {
        public SlicingCompletedMessage(FileSlicedArgs message)
        {
            MessageType = "SlicingCompleted";
            Payload = message;
        }
    }
    public class ProfileListMessage : JsonMessage<List<string>>
    {
        public ProfileListMessage(List<string> payload)
        {
            MessageType = "Profiles";
            Payload = payload;
        }
    }

    public class WelcomeMessage : JsonMessage<string>
    {
        public WelcomeMessage()
        {
            MessageType = "Connected";
            Payload = "connected";
        }
    }
    
}
