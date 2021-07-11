using System.Collections.Generic;
using System.Threading.Tasks;
using PurdueIo.Scraper.Models;

namespace PurdueIo.Scraper
{
    public class MyPurdueScraper : IScraper
    {
        public Task<ICollection<Section>> GetSectionsAsync(string termCode, string subjectCode)
        {
            throw new System.NotImplementedException();
        }

        public Task<ICollection<Subject>> GetSubjectsAsync(string termCode)
        {
            throw new System.NotImplementedException();
        }

        public Task<ICollection<Term>> GetTermsAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}