using System;

namespace PurdueIo.Scraper.Models
{
    public record Meeting
    {
        // Type of meeting. e.g. Lecture, Laboratory, etc.
        public string Type { get; init; }

        // List of attending instructor names and emails
        public (string name, string email)[] Instructors { get; init; }

        // Date of the very first meeting (start of the series)
        public DateTimeOffset StartDate { get; init; }

        // Date of the very last meeting (end of the series)
        public DateTimeOffset EndDate { get; init; }

        // Days of the week when this meeting is scheduled to occur
        public DaysOfWeek DaysOfWeek { get; init; }

        // The time each day this meeting begins
        public DateTimeOffset StartTime { get; init; }

        // The time each day this meeting ends
        public DateTimeOffset EndTime { get; init; }

        // The short name of the building where this meeting occurs
        public string BuildingCode { get; init; }

        // The name of the building where this meeting occurs
        public string BuildingName { get; init; }

        // The room number where this meeting occurs
        public string RoomNumber { get; init; }
    }
}