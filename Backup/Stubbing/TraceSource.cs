
namespace System.Diagnostics
{
#if IronCow_Mobile
    public enum TraceEventType
    {
        Critical,
        Error,
        Warningm,
        Information,
        Verbose,
        Start,
        Stop,
        Suspend,
        Resume,
        Transfer
    }

    public class TraceSource
    {
        public void TraceEvent(TraceEventType eventType, int id, string message, params object[] args)
        {
        }
    }
#endif
}
