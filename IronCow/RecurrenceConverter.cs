using System;
using System.Text;
using System.Text.RegularExpressions;

namespace IronCow
{
    public enum Frequency
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public static class RecurrenceConverter
    {
        private static string GetNumberWithOrdinal(string number)
        {
            if (string.IsNullOrEmpty(number))
                return number;

            int actualNumber;
#if IronCow_Mobile
            try { actualNumber = int.Parse(number); }
            catch (Exception) { throw new ArgumentException("This string is not a valid number", "number"); }
#else
            if (!int.TryParse(number, out actualNumber))
                throw new ArgumentException("This string is not a valid number", "number");
#endif
            return GetNumberWithOrdinal(actualNumber);
        }

        private static string GetNumberWithOrdinal(int number)
        {
            string suffix = string.Empty;
            if (number >= 21)
            {
                int lastDigit = number % 10;
                switch (lastDigit)
                {
                    case 1:
                        suffix = "st";
                        break;
                    case 2:
                        suffix = "nd";
                        break;
                    case 3:
                        suffix = "rd";
                        break;
                    default:
                        suffix = "th";
                        break;
                }
            }
            else if (number >= 0)
            {
                switch (number)
                {
                    case 1:
                        suffix = "st";
                        break;
                    case 2:
                        suffix = "nd";
                        break;
                    case 3:
                        suffix = "rd";
                        break;
                    default:
                        suffix = "th";
                        break;
                }
            }
            return number.ToString() + suffix;
        }

        private static DayOfWeek GetDayOfWeek(string shortName)
        {
            switch (shortName)
            {
                case "MO":
                    return DayOfWeek.Monday;
                case "TU":
                    return DayOfWeek.Tuesday;
                case "WE":
                    return DayOfWeek.Wednesday;
                case "TH":
                    return DayOfWeek.Thursday;
                case "FR":
                    return DayOfWeek.Friday;
                case "SA":
                    return DayOfWeek.Saturday;
                case "SU":
                    return DayOfWeek.Sunday;
                default:
                    throw new ArgumentException();
            }
        }

        private static string GetFrequencyName(Frequency Frequency)
        {
            switch (Frequency)
            {
                case Frequency.Daily:
                    return "day";
                case Frequency.Weekly:
                    return "week";
                case Frequency.Monthly:
                    return "month";
                case Frequency.Yearly:
                    return "year";
                default:
                    throw new NotImplementedException();
            }
        }

        private static Frequency GetFrequency(string input)
        {
            switch (input.ToLower())
            {
                case "day":
                case "days":
                    return Frequency.Daily;
                case "week":
                case "weeks":
                    return Frequency.Weekly;
                case "month":
                case "months":
                    return Frequency.Monthly;
                case "year":
                case "years":
                    return Frequency.Yearly;
                default:
                    throw new ArgumentException();
            }
        }

        public static string FormatRecurrence(Recurrence recurrence, DateFormat dateFormat)
        {
            return FormatRecurrence(recurrence.Repeat, recurrence.IsEvery, dateFormat);
        }

        public static string FormatRecurrence(string repeat, bool isEvery, DateFormat dateFormat)
        {
            int count = 0;
            int interval = 0;
            string byMonthDay = null;
            string byDay = null;
            string until = null;
            Frequency frequency = Frequency.Daily;

            string[] repeatParts = repeat.Split(';');
            foreach (var part in repeatParts)
            {
                string[] partKeyValue = part.Split('=');
                switch (partKeyValue[0])
                {
                    case "FREQ":
                        frequency = (Frequency)Enum.Parse(typeof(Frequency), partKeyValue[1], true);
                        break;
                    case "INTERVAL":
                        interval = int.Parse(partKeyValue[1]);
                        break;
                    case "BYMONTHDAY":
                        byMonthDay = partKeyValue[1];
                        break;
                    case "BYDAY":
                        byDay = partKeyValue[1];
                        break;
                    case "UNTIL":
                        until = partKeyValue[1];
                        break;
                    case "COUNT":
                        count = int.Parse(partKeyValue[1]);
                        break;
                    default:
                        break;
                }
            }

            string s;

            if (isEvery)
            {
                s = "every ";
                if (!string.IsNullOrEmpty(byMonthDay))
                {
                    if (frequency == Frequency.Monthly)
                    {
                        s += "month on the " + GetNumberWithOrdinal(byMonthDay);
                    }
                }
                else if (!string.IsNullOrEmpty(byDay))
                {
                    if (frequency == Frequency.Monthly)
                    {
                        s += "month on the ";
                        if (byDay.StartsWith("-1"))
                        {
                            s += "last ";
                            byDay = byDay.Substring(2);
                        }
                        else if (byDay.StartsWith("-2"))
                        {
                            s += "2nd last ";
                            byDay = byDay.Substring(2);
                        }
                        else if (byDay.IndexOfAny(new char[] { '1', '2', '3', '4', '5' }) == 0)
                        {
                            s += GetNumberWithOrdinal(byDay.Substring(0, 1)) + " ";
                            byDay = byDay.Substring(1);
                        }
                    }

                    if (interval != 0)
                    {
                        s += GetNumberWithOrdinal(interval) + " ";
                    }

                    if (byDay == "MO,TU,WE,TH,FR")
                    {
                        s += "weekday";
                    }
                    else if (byDay == "SA,SU")
                    {
                        s += "weekend";
                    }
                    else
                    {
                        string[] days = byDay.Split(',');
                        for (int i = 0; i < days.Length; i++)
                        {
                            if (i > 0)
                                s += ", ";
                            s += GetDayOfWeek(days[i]).ToString();
                        }
                    }
                }
                else
                {
                    if (interval > 1)
                    {
                        s += interval + " " + GetFrequencyName(frequency) + "s";
                    }
                    else
                    {
                        s += GetFrequencyName(frequency);
                    }
                }

                if (count > 0)
                {
                    s += " for " + count.ToString() + " times";
                }

                if (!string.IsNullOrEmpty(until))
                {
                    string formattedUntil = until;
                    DateTime untilDateTime;
                    if (DateTime.TryParse(until, out untilDateTime))
                    {
                        switch (dateFormat)
                        {
                            case DateFormat.European:
                                formattedUntil = untilDateTime.ToString("dd/MM/yy");
                                break;
                            case DateFormat.American:
                            default:
                                formattedUntil = untilDateTime.ToString("MM/dd/yy");
                                break;
                        }
                    }
                    s += " until " + formattedUntil;
                }
            }
            else
            {
                s = "after " + interval + " " + GetFrequencyName(frequency);
                if (interval > 1)
                    s += "s";
            }

            return s;
        }

        public static Recurrence ParseRecurrence(string input, DateFormat dateFormat)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentNullException();

            int offset;
            bool isEvery;
            StringBuilder result = new StringBuilder();

            input = input.ToLower();
            string[] words = input.Split(' ', '\t');
            if (string.Equals(words[0], "every"))
            {
                isEvery = true;

                DayOfWeek dayOfWeek;
                if (DateConverter.TryGetDayOfWeek(words[1], out dayOfWeek))
                {
                    // "Every Tuesday", or "Every Monday and Wednesday"
                    result.Append("FREQ=WEEKLY;");
                    result.AppendFormat("BYDAY={0}", dayOfWeek.ToString().Substring(0, 2).ToUpper());

                    for (int i = 2; i < words.Length; i++)
                    {
                        if ((i % 2) == 0)
                        {
                            // Stop if we encounter an "until" or "for" clause.
                            if (string.Equals(words[i], "until") ||
                                string.Equals(words[i], "for"))
                            {
                                offset = i;
                                break;
                            }

                            if (!string.Equals(words[i], "and"))
                                throw new ArgumentException();
                        }
                        else
                        {
                            if (!DateConverter.TryGetDayOfWeek(words[i], out dayOfWeek))
                                throw new ArgumentException();
                            result.AppendFormat(",{0}", dayOfWeek.ToString().Substring(0, 2).ToUpper());
                        }
                    }
                    offset = words.Length;
                }
                else if (string.Equals(words[1], "weekday"))
                {
                    // "Every weekday"
                    result.Append("FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR");
                    offset = 2;
                }
                else if (string.Equals(words[1], "weekend"))
                {  
                    // "Every weekend"
                    result.Append("FREQ=WEEKLY;BYDAY=SA,SU");
                    offset = 2;
                }
                else if (string.Equals(words[1], "day"))
                {
                    // "Every day"
                    result.Append("FREQ=DAILY;INTERVAL=1");
                    offset = 2;
                }
                else if (string.Equals(words[1], "week"))
                {
                    // "Every week"
                    result.Append("FREQ=WEEKLY;INTERVAL=1");
                    offset = 2;
                }
                else if (string.Equals(words[1], "month"))
                {
                    // "Every month"
                    result.Append("FREQ=MONTHLY;INTERVAL=1");
                    offset = 2;

                    if (words.Length >= 4 &&
                        string.Equals(words[2], "on") &&
                        string.Equals(words[3], "the"))
                    {
                        if (words.Length == 4)
                            throw new ArgumentException();
                        Match match = Regex.Match(words[4], @"(?<dayofmonth>[0-9]+)(st|nd|rd|th)");
                        if (match.Success)
                        {
                            int dayOfMonth = int.Parse(match.Groups["dayofmonth"].Value);
                            if (words.Length == 5)
                            {
                                // "Every month on the 5th"
                                result.AppendFormat(";BYMONTHDAY={0}", dayOfMonth);
                                offset = 5;
                            }
                            else
                            {
                                // "Every month on the 2nd last Friday" or "Every month on the 3rd Tuesday"
                                offset = 5;
                                if (string.Equals(words[5], "last"))
                                {
                                    result.AppendFormat(";BYDAY=-{0}", dayOfMonth);
                                    offset = 6;
                                }

                                if (words.Length <= offset)
                                    throw new ArgumentException();
                                if (!DateConverter.TryGetDayOfWeek(words[offset], out dayOfWeek))
                                    throw new ArgumentException();

                                result.AppendFormat(";BYDAY={0}", dayOfWeek.ToString().Substring(0, 2).ToUpper());
                                offset += 1;
                            }
                        }
                        else
                        {
                            if (string.Equals(words[4], "last"))
                            {
                                // "Every month on the last Friday"
                                dayOfWeek = DateConverter.GetDayOfWeek(words[5]);
                                result.AppendFormat(";BYDAY=-1{0}", dayOfWeek.ToString().Substring(0, 2).ToUpper());
                                offset = 6;
                            }
                            else
                            {
                                throw new ArgumentException();
                            }
                        }
                    }
                }
                else if (string.Equals(words[1], "year"))
                {
                    // "Every year"
                    result.Append("FREQ=YEARLY");
                    offset = 2;
                }
                else if (string.Equals(words[2], "days"))
                {
                    // "Every 2 days"
                    int interval = int.Parse(words[1]);
                    result.AppendFormat("FREQ=DAILY;INTERVAL={0}", interval);
                    offset = 3;
                }
                else if (string.Equals(words[2], "weeks"))
                {
                    // "Every 2 weeks"
                    int interval = int.Parse(words[1]);
                    result.AppendFormat("FREQ=WEEKLY;INTERVAL={0}", interval);
                    offset = 3;
                }
                else if (string.Equals(words[2], "months"))
                {
                    // "Every 2 months"
                    int interval = int.Parse(words[1]);
                    result.AppendFormat("FREQ=MONTHLY;INTERVAL={0}", interval);
                    offset = 3;
                }
                else if (string.Equals(words[2], "years"))
                {
                    // "Every 2 years"
                    int interval = int.Parse(words[1]);
                    result.AppendFormat("FREQ=YEARLY;INTERVAL={0}", interval);
                    offset = 3;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            else if (string.Equals(words[0], "after"))
            {
                isEvery = false;

                if (words.Length < 3)
                    throw new ArgumentException();
                
                int interval;
                if (words[1] == "a" || words[1] == "one")
                {
                    interval = 1;
                }
                else if (words[1] == "two")
                {
                    interval = 2;
                }
                else
                {
                    interval = int.Parse(words[1]);
                }

                Frequency frequency = GetFrequency(words[2]);
                result.AppendFormat("FREQ={0}", frequency.ToString().ToUpper());
                result.AppendFormat(";INTERVAL={0}", interval);
                offset = 3;
            }
            else
            {
                throw new ArgumentException("Repeat intervals must start with 'Every' or 'After'.");
            }

            if (words.Length > offset)
            {
                if (string.Equals(words[offset], "until"))
                {
                    // "...until 1/2/2007"
                    if (words.Length <= offset + 1)
                        throw new ArgumentException();
                    StringBuilder dateBuilder = new StringBuilder();
                    for (int i = offset + 1; i < words.Length; i++)
                    {
                        dateBuilder.Append(words[i]);
                    }
                    FuzzyDateTime date = DateConverter.ParseDateTime(dateBuilder.ToString(), dateFormat);
                    result.AppendFormat(";UNTIL={0}", date.DateTime.ToString("yyyyMMddTHHmmss"));
                    offset = words.Length;
                }
                else if (string.Equals(words[offset], "for"))
                {
                    // "...for 20 times"
                    if (words.Length <= offset + 2)
                        throw new ArgumentException();
                    if (words[offset + 2] != "times")
                        throw new ArgumentException();
                    int count = int.Parse(words[offset + 1]);
                    result.AppendFormat(";COUNT={0}", count);
                    offset = offset + 3;
                }
            }

            if (words.Length > offset)
                throw new ArgumentException();

            return new Recurrence(result.ToString(), isEvery);
        }
    }
}
