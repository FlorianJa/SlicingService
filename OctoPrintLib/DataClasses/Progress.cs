namespace OctoPrintLib
{
    public class Progress
    {
        public object completion { get; set; }
        public object filepos { get; set; }
        public object printTime { get; set; }
        public object printTimeLeft { get; set; }
        public object printTimeOrigin { get; set; }
    }

    
}