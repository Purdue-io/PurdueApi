using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PurdueIo.Scraper.Connections;

namespace PurdueIo.Tests.Mocks
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
            var pageName = $"{termCode}.{subjectCode}.bwckschd.p_get_crse_unsec.html";
            if (PageResourceExists(pageName))
            {
                return await GetPageContentFromResourceAsync(pageName);
            }
            return await GetPageContentFromResourceAsync("empty_bwckschd.p_get_crse_unsec.html");
        }

        public async Task<string> GetSectionDetailsPageAsync(string termCode, string subjectCode)
        {
            var pageName = $"{termCode}.{subjectCode}.bwskfcls.P_GetCrse_Advanced.html";
            if (PageResourceExists(pageName))
            {
                return await GetPageContentFromResourceAsync(pageName);
            }
            return await GetPageContentFromResourceAsync("empty_bwskfcls.P_GetCrse_Advanced.html");
        }

        private bool PageResourceExists(string pageName)
        {
            pageName = $"Tests.Resources.Pages.{pageName}";
            var assembly = this.GetType().GetTypeInfo().Assembly;
            return assembly.GetManifestResourceNames().Any(r => (r == pageName));
        }

        // Retrieves page content from resource data embedded in the assembly
        // (see Resources/Pages directory)
        private async Task<string> GetPageContentFromResourceAsync(string pageName)
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var resourceReader = new StreamReader(assembly.GetManifestResourceStream(
                $"Tests.Resources.Pages.{pageName}"));
            return await resourceReader.ReadToEndAsync();
        }
    }
}