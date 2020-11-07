namespace OctoPrintLib
{
    public class Flags
    {
        public bool operational { get; set; }
        public bool printing { get; set; }
        public bool cancelling { get; set; }
        public bool pausing { get; set; }
        public bool resuming { get; set; }
        public bool finishing { get; set; }
        public bool closedOrError { get; set; }
        public bool error { get; set; }
        public bool paused { get; set; }
        public bool ready { get; set; }
        public bool sdReady { get; set; }
    }

    
}

   
