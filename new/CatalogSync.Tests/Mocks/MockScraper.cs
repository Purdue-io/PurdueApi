using PurdueIo.Scraper;
using PurdueIo.Scraper.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurdueIo.CatalogSync.Tests.Mocks
{
    public class MockScraper : IScraper
    {
        public Task<ICollection<Term>> GetTermsAsync()
        {
            return Task.FromResult<ICollection<Term>>(new List<Term>()
            {
                new Term()
                {
                    Id = "202210",
                    Name = "Fall 2021",
                },
            });
        }

        public Task<ICollection<Subject>> GetSubjectsAsync(string termCode)
        {
            return Task.FromResult<ICollection<Subject>>(new List<Subject>()
            {
                new Subject()
                {
                    Code = "TEST",
                    Name = "Test Subject",
                },
            });
        }

        public Task<ICollection<Section>> GetSectionsAsync(string termCode, string subjectCode)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return Task.FromResult<ICollection<Section>>(new List<Section>()
            {
                new Section()
                {
                    Crn = "12345",
                    SectionCode = "000",
                    Meetings = new Meeting[]
                    {
                        new Meeting()
                        {
                            Type = "Lecture",
                            Instructors = new (string name, string email)[]
                            {
                                (name: "Hayden McAfee", email: "haydenmc@test.com"),
                            },
                            StartDate = new DateTimeOffset(2021, 8, 23, 0, 0, 0, 0,
                                timeZone.BaseUtcOffset),
                            EndDate = new DateTimeOffset(2021, 12, 11, 0, 0, 0, 0,
                                timeZone.BaseUtcOffset),
                            DaysOfWeek =
                                (DaysOfWeek.Monday | DaysOfWeek.Wednesday | DaysOfWeek.Friday),
                            StartTime = new DateTimeOffset(2021, 8, 30, 7, 30, 0, 0,
                                timeZone.BaseUtcOffset),
                            EndTime = new DateTimeOffset(2021, 8, 30, 8, 20, 0, 0,
                                timeZone.BaseUtcOffset),
                            BuildingCode = "TB",
                            BuildingName = "Test Building",
                            RoomNumber = "543",
                        },
                    },
                    SubjectCode = "TEST",
                    CourseNumber = "10100",
                    Type = "Lecture",
                    CourseTitle = "Intro to Test",
                    Description = "How to test things",
                    CreditHours = 1.0,
                    LinkSelf = "A0",
                    LinkOther = "A2",
                    CampusCode = "TC",
                    CampusName = "Test Campus",
                    Capacity = 16,
                    Enrolled = 8,
                    RemainingSpace = 8,
                    WaitListCapacity = 4,
                    WaitListCount = 2,
                    WaitListSpace = 2,
                }
            });
        }
    }
}