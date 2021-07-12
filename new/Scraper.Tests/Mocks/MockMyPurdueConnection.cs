using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using PurdueIo.Scraper.Connections;

namespace PurdueIo.Scraper.Tests.Mocks
{
    class MockMyPurdueConnection : IMyPurdueConnection
    {
        public async Task<string> GetTermListPageAsync()
        {
            return await GetPageContentFromResourceAsync("bwckschd.p_disp_dyn_sched.html");
        }

        public async Task<string> GetSubjectListPageAsync(string termCode)
        {
            return await GetPageContentFromResourceAsync("bwckgens.p_proc_term_date.html");
        }

        public async Task<string> GetSectionListPageAsync(string termCode, string subjectCode)
        {
            return await GetPageContentFromResourceAsync("bwckschd.p_get_crse_unsec.html");
        }

        public async Task<string> GetSectionDetailsPageAsync(string termCode, string subjectCode)
        {
            return await GetPageContentFromResourceAsync("bwskfcls.P_GetCrse_Advanced.html");
        }

        // Retrieves page content from resource data embedded in the assembly
        // (see Resources/Pages directory)
        private async Task<string> GetPageContentFromResourceAsync(string pageName)
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var resourceReader = new StreamReader(assembly.GetManifestResourceStream(
                $"Scraper.Tests.Resources.Pages.{pageName}"));
            return await resourceReader.ReadToEndAsync();
        }
    }
}