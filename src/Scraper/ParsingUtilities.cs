using PurdueIo.Scraper.Models;
using System;

namespace PurdueIo.Scraper
{
    public static class ParsingUtilities
    {
        public static DaysOfWeek ParseDaysOfWeek(string daysOfWeek)
        {
            DaysOfWeek dow = 0;
            if (daysOfWeek.Contains("M")) { dow |= DaysOfWeek.Monday; }
            if (daysOfWeek.Contains("T")) { dow |= DaysOfWeek.Tuesday; }
            if (daysOfWeek.Contains("W")) { dow |= DaysOfWeek.Wednesday; }
            if (daysOfWeek.Contains("R")) { dow |= DaysOfWeek.Thursday; }
            if (daysOfWeek.Contains("F")) { dow |= DaysOfWeek.Friday; }
            if (daysOfWeek.Contains("S")) { dow |= DaysOfWeek.Saturday; }
            if (daysOfWeek.Contains("U")) { dow |= DaysOfWeek.Sunday; }
            return dow;
        }

        public static Tuple<TimeOnly?, TimeOnly?> ParseStartEndTime(string startEndTime)
        {
            TimeOnly? start = null;
            TimeOnly? end = null;
            string[] times = startEndTime.Split(new string[] { "-" }, StringSplitOptions.None);
            if (times.Length == 2)
            {
                start = TimeOnly.Parse(times[0].Trim());
                end = TimeOnly.Parse(times[1].Trim());
            }
            return new Tuple<TimeOnly?, TimeOnly?>(start, end);
        }

        public static Tuple<DateOnly?, DateOnly?> ParseStartEndDate(string startEndDate)
        {
            DateOnly? start = null;
            DateOnly? end = null;
            string[] dateArray = startEndDate.Split(new string[] { "-" }, StringSplitOptions.None);
            if (!startEndDate.Equals("TBA") && dateArray.Length == 2)
            {
                start = DateOnly.Parse(dateArray[0].Trim());
                end = DateOnly.Parse(dateArray[1].Trim());
            }
            return new Tuple<DateOnly?, DateOnly?>(start, end);
        }
    }
}