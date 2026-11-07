using System;
using System.Collections.Generic;
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
        public const string LinkedSectionsFixture = "202720-CS-linked-sections.json";

        // The term and subject the recorded fixtures above were captured for.
        public const string RecordedTerm = "202720";
        public const string RecordedTermName = "Spring 2027";
        public const string RecordedSubject = "CS";
        public const string RecordedSubjectName = "Computer Sciences";

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

        // The verbatim fetchLinkedSections responses recorded while running the real
        // GroupLinkedSectionsAsync heuristic against the live service, keyed by the
        // queried CRN. Also serves as the ground truth for class groupings.
        public static Dictionary<string, MyPurdueLinkedSectionsResponse> LoadLinkedSections()
        {
            using var stream = Open(LinkedSectionsFixture);
            return JsonSerializer
                .Deserialize<Dictionary<string, MyPurdueLinkedSectionsResponse>>(stream);
        }
    }
}
