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

        // Title of the course (e.g. Introduction to Computers)
        public string CourseTitle { get; init; }

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
    }
}