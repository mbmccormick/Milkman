using IronCow.Rest;

namespace IronCow
{
    public class UserSettings
    {
        public string TimeZone { get; private set; }
        public DateFormat DateFormat { get; private set; }
        public TimeFormat TimeFormat { get; private set; }
        public string DefaultList { get; private set; }
        public string Language { get; private set; }

        internal UserSettings(RawSettings settings)
        {
            TimeZone = settings.TimeZone;
            DateFormat = (DateFormat)settings.DateFormat;
            TimeFormat = (TimeFormat)settings.TimeFormat;
            DefaultList = settings.DefaultList;
            Language = settings.Language;
        }
    }
}
