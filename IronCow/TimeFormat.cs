
namespace IronCow
{
    public enum TimeFormat
    {
        /// <summary>
        /// 12 hour time with day period (e.g. 5pm)
        /// </summary>
        TwelveHours,
        /// <summary>
        /// 24 hour time (e.g. 17:00)
        /// </summary>
        TwentyFourHours,
        /// <summary>
        /// Default time format (12 hour time).
        /// </summary>
        Default = TwelveHours
    }
}
