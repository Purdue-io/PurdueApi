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

        public static Tuple<DateTimeOffset, DateTimeOffset> ParseStartEndTime(string startEndTime,
            TimeZoneInfo timeZone)
        {
            string[] times = startEndTime.Split(new string[] { "-" }, StringSplitOptions.None);
            if (times.Length != 2)
            {
                return new Tuple<DateTimeOffset, DateTimeOffset>(DateTimeOffset.MinValue,
                    DateTimeOffset.MinValue);
            }
            else
            {
                var start = DateTime.Parse(times[0].Trim());
                var startTzOffset = timeZone.GetUtcOffset(start);
                var startDto = new DateTimeOffset(start, startTzOffset);
                
                var end = DateTime.Parse(times[1].Trim());
                var endTzOffset = timeZone.GetUtcOffset(end);
                var endDto = new DateTimeOffset(end, endTzOffset);

                return new Tuple<DateTimeOffset, DateTimeOffset>(startDto, endDto);
            }
        }

        public static Tuple<DateTimeOffset, DateTimeOffset> ParseStartEndDate(string startEndDate,
            TimeZoneInfo timeZone)
        {
            string[] dateArray = startEndDate.Split(new string[] { "-" }, StringSplitOptions.None);
            if (startEndDate.Equals("TBA") || dateArray.Length < 2)
            {
                return new Tuple<DateTimeOffset, DateTimeOffset>(DateTimeOffset.MinValue,
                    DateTimeOffset.MaxValue);
            }
            else
            {
                var start = DateTime.Parse(dateArray[0].Trim());
                var startTzOffset = timeZone.GetUtcOffset(start);
                var startDto = new DateTimeOffset(start, startTzOffset);

                var end = DateTime.Parse(dateArray[1].Trim());
                var endTzOffset = timeZone.GetUtcOffset(end);
                var endDto = new DateTimeOffset(end, endTzOffset);

                return new Tuple<DateTimeOffset, DateTimeOffset>(startDto, endDto);
            }
        }
    }
}