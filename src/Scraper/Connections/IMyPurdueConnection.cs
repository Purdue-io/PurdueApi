using System.Threading.Tasks;

namespace PurdueIo.Scraper.Connections
{
    // Represents a connection to MyPurdue that provides raw page content
    public interface IMyPurdueConnection
    {
        // Retrieves the contents of bwckschd.p_disp_dyn_sched from MyPurdue
        Task<string> GetTermListPageAsync();

        // Retrieves the contents of bwckgens.p_proc_term_date from MyPurdue for the given term
        Task<string> GetSubjectListPageAsync(string termCode);

        // Retrieves the contents of bwckschd.p_get_crse_unsec from MyPurdue for the given term
        // and subject
        Task<string> GetSectionListPageAsync(string termCode, string subjectCode);

        // Retrieves the contents of bwskfcls.P_GetCrse_Advanced from MyPurdue for the given term
        // and subject
        Task<string> GetSectionDetailsPageAsync(string termCode, string subjectCode);
    }
}