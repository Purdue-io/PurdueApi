using PurdueIo.Scraper;
using PurdueIo.Scraper.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurdueIo.CatalogSync.Tests.Mocks
{
    public class MockScraper : IScraper
    {
        private ICollection<Term> terms;
        private ICollection<Subject> subjects;
        private ICollection<Section> sections;

        public MockScraper(ICollection<Term> terms, ICollection<Subject> subjects,
            ICollection<Section> sections)
        {
            this.terms = terms;
            this.subjects = subjects;
            this.sections = sections;
        }

        public Task<ICollection<Term>> GetTermsAsync()
        {
            return Task.FromResult<ICollection<Term>>(terms);
        }

        public Task<ICollection<Subject>> GetSubjectsAsync(string termCode)
        {
            return Task.FromResult<ICollection<Subject>>(subjects);
        }

        public Task<ICollection<Section>> GetSectionsAsync(string termCode, string subjectCode)
        {
            return Task.FromResult<ICollection<Section>>(sections);
        }
    }
}