namespace OctoPrintLib
{
    public class Progress
    {
        public float? completion { get; set; }
        public int? filepos { get; set; }
        public int? printTime { get; set; }
        public int? printTimeLeft { get; set; }
        public string printTimeOrigin { get; set; }
    }

    
}