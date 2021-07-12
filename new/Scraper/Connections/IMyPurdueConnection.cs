using System.Threading.Tasks;

namespace PurdueIo.Scraper.Connections
{
    public interface IMyPurdueConnection
    {
        Task<string> GetTermListPageAsync();

        Task<string> GetSubjectListPageAsync(string termCode);

        Task<string> GetSectionListPageAsync(string termCode, string subjectCode);

        Task<string> GetSectionDetailsPageAsync(string termCode, string subjectCode);
    }
}