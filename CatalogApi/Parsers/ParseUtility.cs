using PurdueIoDb.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogApi.Parsers
{
	public static class ParseUtility
	{
		public static DOW ParseDaysOfWeek(string daysOfWeek)
		{
			DOW dow = 0;
			if (daysOfWeek.Contains("M")) dow |= DOW.Monday;
			if (daysOfWeek.Contains("T")) dow |= DOW.Tuesday;
			if (daysOfWeek.Contains("W")) dow |= DOW.Wednesday;
			if (daysOfWeek.Contains("R")) dow |= DOW.Thursday;
			if (daysOfWeek.Contains("F")) dow |= DOW.Friday;
			if (daysOfWeek.Contains("S")) dow |= DOW.Saturday;
			if (daysOfWeek.Contains("U")) dow |= DOW.Sunday;
			return dow;
		}

		public static Tuple<DateTimeOffset, DateTimeOffset> ParseStartEndTime(string startEndTime, TimeZoneInfo timeZone)
		{
			var times = startEndTime.Split(new string[] { "-" }, StringSplitOptions.None);
			if (times.Count() != 2)
			{
				return new Tuple<DateTimeOffset, DateTimeOffset>(DateTimeOffset.MinValue, DateTimeOffset.MinValue);
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

		public static Tuple<DateTimeOffset, DateTimeOffset> ParseStartEndDate(string startEndDate, TimeZoneInfo timeZone)
		{
			var dateArray = startEndDate.Split(new string[] { "-" }, StringSplitOptions.None);
			if (startEndDate.Equals("TBA") || dateArray.Count() < 2)
			{
				return new Tuple<DateTimeOffset, DateTimeOffset>(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
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
