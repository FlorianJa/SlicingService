namespace OctoPrintLib
{
    /// <summary>
    /// The base class for the different Trackers
    /// </summary>
    public class OctoprintTracker{
        protected OctoprintConnection Connection { get; set; }
        public OctoprintTracker(OctoprintConnection con)
        {
            Connection = con;
        }
    }
}