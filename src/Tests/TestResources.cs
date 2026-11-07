using System;
using System.IO;
using System.Text.Json;
using PurdueIo.Scraper.Connections.Models;

namespace PurdueIo.Tests
{
    // Access to the cached MyPurdue API responses embedded into the test assembly
    // (see the EmbeddedResource items in Tests.csproj). Tests using these never
    // touch the network.
    public static class TestResources
    {
        public const string SectionPage0 = "202720-CS-section-dump-0-500.json";
        public const string SectionPage500 = "202720-CS-section-dump-500-500.json";

        public static Stream Open(string fileName)
        {
            var stream = typeof(TestResources).Assembly
                .GetManifestResourceStream("Tests.Resources." + fileName);
            if (stream == null)
            {
                throw new FileNotFoundException(
                    $"Embedded resource 'Tests.Resources.{fileName}' not found.");
            }
            return stream;
        }

        public static string ReadText(string fileName)
        {
            using var stream = Open(fileName);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static MyPurdueSectionResponse LoadSectionPage(string fileName)
        {
            using var stream = Open(fileName);
            return JsonSerializer.Deserialize<MyPurdueSectionResponse>(stream);
        }
    }
}
