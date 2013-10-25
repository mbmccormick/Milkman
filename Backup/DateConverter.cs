using System;
using System.Text.RegularExpressions;

namespace IronCow
{
    /*
     * Input  	Definition
     * Today 	Today (tod also works)
     * Tomorrow 	Tomorrow (tom also works)
     * 25 Apr 	April 25 this year (unless April 25 has passed, in which case it assumes next year)
     * Apr 25 	April 25 this year (unless April 25 has passed, in which case it assumes next year)
     * 04/25/2008 	April 25, 2008
     * 25/04/2008 	April 25, 2008
     * 2008/04/25 	April 25, 2008
     * 2008-04-25 	April 25, 2008
     * 25th 	25th day of the current month
     * End of month 	Last day of the current month
     * Friday 	The next Friday to occur
     * Next Friday 	The second Friday to occur
     * Fri at 7pm 	Friday at 7:00pm
     * Fri @ 7pm 	Friday at 7:00pm
     * 6pm 	Today at 6:00pm (unless 6:00pm has passed, in which case it assumes tomorrow)
     * 18:00 	Today at 6:00pm (unless 6:00pm has passed, in which case it assumes tomorrow)
     * 5 hours 	5 hours from now
     * 2 days 	2 days from now
     * 3 weeks 	3 weeks from now
     */
    public static class DateConverter
    {
        private const string DaysOfWeekPattern = @"(?<dayofweek>mo(n(day)?)?|tu(e(s(day)?)?)?|we(d(nes(day)?)?)?|th(u(r(s(day)?)?)?)?|fr(i(day)?)?|sa(t(ur(day)?)?)?|su(n(day)?)?)";
        private const string TimePattern = @"(?<hours>\d\d?)(\:(?<minutes>\d\d))?(?<ampm>am|pm)?";

        public static DayOfWeek GetDayOfWeek(string input)
        {
            DayOfWeek dayOfWeek;
            if (TryGetDayOfWeek(input, out dayOfWeek))
                return dayOfWeek;
            throw new ArgumentException();
        }

        public static bool TryGetDayOfWeek(string input, out DayOfWeek dayOfWeek)
        {
            Match match = Regex.Match(input, "^" + DaysOfWeekPattern + "$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                dayOfWeek = DayOfWeek.Monday;
                return false;
            }
            else
            {
                string value = match.Groups["dayofweek"].Value.Substring(0, 2).ToLowerInvariant();
                switch (value)
                {
                    case "mo":
                        dayOfWeek = DayOfWeek.Monday;
                        return true;
                    case "tu":
                        dayOfWeek = DayOfWeek.Tuesday;
                        return true;
                    case "we":
                        dayOfWeek = DayOfWeek.Wednesday;
                        return true;
                    case "th":
                        dayOfWeek = DayOfWeek.Thursday;
                        return true;
                    case "fr":
                        dayOfWeek = DayOfWeek.Friday;
                        return true;
                    case "sa":
                        dayOfWeek = DayOfWeek.Saturday;
                        return true;
                    case "su":
                        dayOfWeek = DayOfWeek.Sunday;
                        return true;
                }
                dayOfWeek = DayOfWeek.Monday;
                return false;
            }
        }

        public static TimeSpan GetTimeSpan(string input)
        {
            MatchCollection matches = Regex.Matches(input, @"((?<num>\d+)\s*((?<minutes>min\w*)|(?<hours>h\w*)|(?<days>d\w*))\s+)+");
            if (matches.Count == 0)
                throw new ArgumentException();

            TimeSpan result = new TimeSpan();
            foreach (Match match in matches)
            {
                if (!match.Success)
                    throw new ArgumentException();

                int num = int.Parse(match.Groups["num"].Value);
                if (match.Groups["minutes"].Success)
                    result = result.Add(new TimeSpan(0, num, 0));
                else if (match.Groups["hours"].Success)
                    result = result.Add(new TimeSpan(num, 0, 0));
                else if (match.Groups["days"].Success)
                    result = result.Add(new TimeSpan(num, 0, 0, 0));
                else
                    throw new ArgumentException();
            }
            return result;
        }

        public static FuzzyDateTime ParseDateTime(string input, DateFormat dateFormat)
        {
            // Packed format
            DateTime dateTime;
            if (DateTime.TryParseExact(input, "s", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateTime))
            {
                return new FuzzyDateTime(dateTime, true);
            }

            // "Today"
            if (string.Compare(input, "today", StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(input, "tod", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return new FuzzyDateTime(DateTime.Today, false);
            }

            // "Tomorrow"
            if (string.Compare(input, "tomorrow", StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(input, "tom", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return new FuzzyDateTime(DateTime.Today.AddDays(1), false);
            }

            // "25 Apr", or "Apr 25"
            string[] formats1 =
            {
               "dd MMM",        // 25 Apr
               "MMM dd",        // Apr 25
            };
            if (DateTime.TryParseExact(input, formats1, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dateTime))
            {
                if (dateTime < DateTime.Today)
                    dateTime = dateTime.AddYears(1);
                return new FuzzyDateTime(dateTime, false);
            }

            // "2008/04/25" or "2008-04-25"
            string[] formats2 =
            {
               "yyyy/MM/dd",    // 2008/04/25
               "yyyy-MM-dd"     // 2008-04-25
            };
            if (DateTime.TryParseExact(input, formats2, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dateTime))
            {
                return new FuzzyDateTime(dateTime, false);
            }

            // American and European date formats
            if (dateFormat == DateFormat.American)
            {
                if (DateTime.TryParseExact(input, "MM/dd/yy", System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dateTime))
                {
                    return new FuzzyDateTime(dateTime, false);
                }
                if (DateTime.TryParseExact(input, "MM/dd/yyyy", System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dateTime))
                {
                    return new FuzzyDateTime(dateTime, false);
                }
            }
            else if (dateFormat == DateFormat.European)
            {
                if (DateTime.TryParseExact(input, "dd/MM/yy", System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dateTime))
                {
                    return new FuzzyDateTime(dateTime, false);
                }
                if (DateTime.TryParseExact(input, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dateTime))
                {
                    return new FuzzyDateTime(dateTime, false);
                }
            }

            // "25th"
            Match match = Regex.Match(input, @"^\s*(?<day>\d\d?)(st|nd|rd|th)\s*$");
            if (match.Success)
            {
                // "25th"
                var today = DateTime.Today;
                var day = int.Parse(match.Groups["day"].Value);
                var result = new DateTime(today.Year, today.Month, day);
                if (day < today.Day)
                    result = result.AddMonths(1);
                return new FuzzyDateTime(result, false);
            }

            // "End of month"
            if (Regex.IsMatch(input, @"^\s*end\s+of\s+month\s*$", RegexOptions.IgnoreCase))
            {
                var today = DateTime.Today;
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                return new FuzzyDateTime(firstDayOfMonth.AddMonths(1).AddDays(-1), false);
            }

            // "Friday", or "Next Friday"
            match = Regex.Match(input, @"^\s*(?<next>next\s+)?" + DaysOfWeekPattern + @"\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var targetDayOfWeek = GetDayOfWeek(match.Groups["dayofweek"].Value);
                var result = DateTime.Today;
                while (result.DayOfWeek != targetDayOfWeek)
                    result = result.AddDays(1);
                if (match.Groups["next"].Success)   // If "next", add a week.
                    result = result.AddDays(7);
                return new FuzzyDateTime(result, false);
            }

            // "Fri at 7pm" or "Fri@7pm"
            match = Regex.Match(input, @"^\s*" + DaysOfWeekPattern + @"((\s*@\s*)|(\s+at\s+))" + TimePattern + @"\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var targetDayOfWeek = GetDayOfWeek(match.Groups["dayofweek"].Value);
                var result = DateTime.Today;
                while (result.DayOfWeek != targetDayOfWeek)
                    result = result.AddDays(1);
                int hours = int.Parse(match.Groups["hours"].Value);
                result = result.AddHours(hours);
                if (match.Groups["minutes"].Success)
                {
                    int minutes = int.Parse(match.Groups["minutes"].Value);
                    result = result.AddMinutes(minutes);
                }
                if (match.Groups["ampm"].Success &&
                    string.Compare(match.Groups["ampm"].Value, "pm", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    result = result.AddHours(12);
                }
                return new FuzzyDateTime(result, true);
            }

            // "6pm" or "18:00"
            match = Regex.Match(input, @"^\s*" + TimePattern + @"\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var result = DateTime.Today;
                int hours = int.Parse(match.Groups["hours"].Value);
                result = result.AddHours(hours);
                if (match.Groups["minutes"].Success)
                {
                    int minutes = int.Parse(match.Groups["minutes"].Value);
                    result = result.AddMinutes(minutes);
                }
                if (match.Groups["ampm"].Success &&
                    string.Compare(match.Groups["ampm"].Value, "pm", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    result = result.AddHours(12);
                }
                if (result < DateTime.Now)
                    result = result.AddDays(1);
                return new FuzzyDateTime(result, true);
            }

            // "20 minutes"
            match = Regex.Match(input, @"^\s*(?<num>\d+)\s*(m|mn|mins?|minutes?)\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int num = int.Parse(match.Groups["num"].Value);
                var result = DateTime.Now;
                result = result.AddMinutes(num);
                return new FuzzyDateTime(result, true);
            }

            // "5 hours"
            match = Regex.Match(input, @"^\s*(?<num>\d+)\s*(h|hours?|hrs?)\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int num = int.Parse(match.Groups["num"].Value);
                var result = DateTime.Now;
                result = result.AddHours(num);
                return new FuzzyDateTime(result, true);
            }

            // "2 days", or "2 days of today"
            match = Regex.Match(input, @"^\s*(?<num>\d+)\s*(d|days?)(\s+of\s+today)?\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int num = int.Parse(match.Groups["num"].Value);
                var result = DateTime.Today;
                result = result.AddDays(num);
                return new FuzzyDateTime(result, false);
            }

            // "3 weeks"
            match = Regex.Match(input, @"^\s*(?<num>\d+)\s*(w|weeks?)(\s+of\s+today)?\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int num = int.Parse(match.Groups["num"].Value);
                var result = DateTime.Today;
                result = result.AddDays(num * 7);
                return new FuzzyDateTime(result, false);
            }

            // "1 month"
            match = Regex.Match(input, @"^\s*(?<num>\d+)\s*(m|months?)(\s+of\s+today)?\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int num = int.Parse(match.Groups["num"].Value);
                var result = DateTime.Today;
                result = result.AddMonths(num);
                return new FuzzyDateTime(result, false);
            }

            // "1 year"
            match = Regex.Match(input, @"^\s*(?<num>\d+)\s*(y|years?)(\s+of\s+today)?\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int num = int.Parse(match.Groups["num"].Value);
                var result = DateTime.Today;
                result = result.AddYears(num);
                return new FuzzyDateTime(result, false);
            }

            // "now"
            match = Regex.Match(input, @"^now$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var result = DateTime.Now;
                return new FuzzyDateTime(result, true);
            }

            throw new ArgumentException();
        }

        public static string FormatDateTime(FuzzyDateTime fuzzyDateTime, DateFormat dateFormat, TimeFormat timeFormat)
        {
            TimeSpan fromToday = fuzzyDateTime.DateTime.Subtract(DateTime.Today);

            string timeString = string.Empty;
            if (fuzzyDateTime.HasTime)
            {
                switch (timeFormat)
                {
                    case TimeFormat.TwelveHours:
                        timeString = fuzzyDateTime.DateTime.ToString("hh:mm tt");
                        break;
                    case TimeFormat.TwentyFourHours:
                    default:
                        timeString = fuzzyDateTime.DateTime.ToString("HH:mm");
                        break;
                }
            }

            if (fromToday.Days == 0)
            {
                if (fuzzyDateTime.HasTime)
                {
                    return timeString;
                }
                else
                {
                    return "Today";
                }
            }
            else if (fromToday.Days == 1)
            {
                string result = "Tomorrow";
                if (fuzzyDateTime.HasTime)
                    result += " " + timeString;
                return result;
            }
            else if (fromToday.Days == -1)
            {
                string result = "Yesterday";
                if (fuzzyDateTime.HasTime)
                    result += " " + timeString;
                return result;
            }
            else if (fromToday.Days > 0 && fromToday.Days <= 6)
            {
                string result = fuzzyDateTime.DateTime.DayOfWeek.ToString();
                if (fuzzyDateTime.HasTime)
                    result += " " + timeString;
                return result;
            }
            else if (fromToday.Days > 6 && fuzzyDateTime.DateTime.Year == DateTime.Today.Year)
            {
                string format = "MMM dd";
                switch (dateFormat)
                {
                    case DateFormat.European:
                        format = "dd MMM";
                        break;
                    case DateFormat.American:
                    default:
                        format = "MMM dd";
                        break;
                }
                string result = fuzzyDateTime.DateTime.ToString(format);
                if (fuzzyDateTime.HasTime)
                    result += " " + timeString;
                return result;
            }
            else
            {
                string format = "MM/dd/yy";
                switch (dateFormat)
                {
                    case DateFormat.European:
                        format = "dd/MM/yy";
                        break;
                    case DateFormat.American:
                    default:
                        format = "MM/dd/yy";
                        break;
                }
                string result = fuzzyDateTime.DateTime.ToString(format);
                if (fuzzyDateTime.HasTime)
                    result += " " + timeString;
                return result;
            }
        }
    }
}
