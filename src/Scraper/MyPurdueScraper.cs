using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PurdueIo.Scraper.Models;

namespace PurdueIo.Scraper
{
    public class MyPurdueScraper : IScraper
    {
        private readonly ILogger<MyPurdueScraper> logger;
        private readonly HttpClient httpClient;

        private const string BASE_URL =
            "https://apps.mypurdue.purdue.edu/StudentRegistrationSsb/ssb/classSearch/";

        // the service seems to enforce a 500-item limit before requiring pagination
        private const uint MAX_PAGINATION_COUNT = 500;

        public MyPurdueScraper(ILogger<MyPurdueScraper> logger)
        {
            this.logger = logger;
            this.httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(BASE_URL);

            // Pretend we're Firefox
            httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (X11; Linux x86_64; rv:157.0) Gecko/20100101 Firefox/157.0");
        }

        public async Task<ICollection<Section>> GetSectionsAsync(
            string termCode, string subjectCode)
        {
            throw new NotImplementedException();
        }

        public async Task<ICollection<Subject>> GetSubjectsAsync(
            string termCode)
        {
            var result = await httpClient.GetAsync(
                $"get_subject?searchTerm=&term={termCode}&offset=1&max={MAX_PAGINATION_COUNT}");
            if (result.IsSuccessStatusCode)
            {
                var subjects = JsonSerializer.Deserialize<List<Subject>>(
                    await result.Content.ReadAsStringAsync());

                // the subject names returned from the service are a little funky - they have
                // html-encoded characters and are prefixed with their codes.
                foreach (var subject in subjects)
                {
                    subject.Name = WebUtility.HtmlDecode(subject.Name);
                    subject.Name = subject.Name.Replace($"{subject.Code}-", "");
                }

                return subjects;
            }
            this.logger.LogError(
                "Received HTTP '{}' on GetSubjectsAsync: {}",
                result.StatusCode, await result.Content.ReadAsStringAsync());
            throw new ApplicationException(
                $"Could not fetch subject list: HTTP {result.StatusCode}: " +
                await result.Content.ReadAsStringAsync());
        }

        public async Task<ICollection<Term>> GetTermsAsync()
        {
            var result = await httpClient.GetAsync(
                $"getTerms?searchTerm=&offset=1&max={MAX_PAGINATION_COUNT}");
            if (result.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<List<Term>>(
                    await result.Content.ReadAsStringAsync());
            }
            this.logger.LogError(
                "Received HTTP '{}' on GetTermsAsync: {}",
                result.StatusCode, await result.Content.ReadAsStringAsync());
            throw new ApplicationException(
                $"Could not fetch term list: HTTP {result.StatusCode}: " +
                await result.Content.ReadAsStringAsync());
        }
    }
}