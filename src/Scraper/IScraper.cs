using PurdueIo.Scraper.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurdueIo.Scraper
{
    public interface IScraper
    {
        Task<ICollection<Term>> GetTermsAsync();

        Task<ICollection<Subject>> GetSubjectsAsync(string termCode);

        Task<ICollection<Section>> GetSectionsAsync(string termCode, string subjectCode);
    }
}