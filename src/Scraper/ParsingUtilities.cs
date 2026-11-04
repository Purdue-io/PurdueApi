using System;

namespace PurdueIo.Scraper
{
    public static class ParsingUtilities
    {
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