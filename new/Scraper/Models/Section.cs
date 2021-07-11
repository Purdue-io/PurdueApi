using System;

namespace PurdueIo.Scraper.Models
{
    public record Section
    {
        // Section CRN number (e.g. 68475)
        public string Crn { get; init; }

        // Section code (usually three characters - used to differentiate sections)
        public string SectionCode { get; init; }

        // Set of meetings scheduled for this section
        public Meeting[] Meetings { get; init; }

        // Subject code of the course (e.g. CS)
        public string SubjectCode { get; init; }

        // Course number (e.g. 11000)
        public string CourseNumber { get; init; }

        // Type of section (e.g. Lecture)
        public string Type { get; init; }

        // Description of the course
        public string Description { get; init; }

        // Number of credit hours gained by taking this section
        public double CreditHours { get; init; }

        // Link ID of this section (e.g. A2, used to group required sections)
        public string LinkSelf { get; init; }

        // Link ID of other section (e.g. A1, used to group required sections)
        public string LinkOther { get; init; }

        // Short code name for the campus where this section is scheduled
        public string CampusCode { get; init; }

        // The full name of the campus where this section is scheduled
        public string CampusName { get; init; }

        // This section's attendance capacity
        public int Capacity { get; init; }

        // This section's enrollment count (referred to as 'Actual' by MyPurdue)
        public int Enrolled { get; init; }

        // Remaining space for enrollment (usually Capacity - Enrolled)
        public int RemainingSpace { get; init; }

        // Wait list capacity
        public int WaitListCapacity { get; init; }

        // How many students are on the wait list
        public int WaitListCount { get; init; }

        // How much space is currently available on the wait list (usually Capacity - Count)
        public int WaitListSpace { get; init; }
    }
}