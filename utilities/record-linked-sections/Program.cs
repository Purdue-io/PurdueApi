using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PurdueIo.Scraper.Connections;

// Refreshes the linked-sections fixture consumed by src/Tests (see
// utilities/README.md). Drives the real MyPurdueConnection - and thus the real
// GroupLinkedSectionsAsync query heuristic - against the live service,
// recording every fetchLinkedSections response verbatim. What gets saved is
// exactly the set of responses production would need - nothing more. Synthesizing
// these responses is not an option: link identifiers are per-component-type
// buckets and can never join a lecture to its labs.
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var term = args.Length > 0 ? args[0] : "202720";
        var subject = args.Length > 1 ? args[1] : "CS";
        var outPath = args.Length > 2
            ? args[2]
            : Path.Combine(FindRepoRoot(), "src", "Tests", "Resources",
                $"{term}-{subject}-linked-sections.json");

        var recorder = new RecordingHandler();
        var connection = new MyPurdueConnection(
            NullLogger<MyPurdueConnection>.Instance, recorder);

        var sections = await connection.GetSectionsAsync(term, subject);
        Console.WriteLine($"sections fetched: {sections.Count}");

        var groups = await connection.GroupLinkedSectionsAsync(term, sections);
        Console.WriteLine($"groups: {groups.Count}, linked-section calls made: {recorder.Count}");

        // Coverage: every linked section should appear as a queried CRN or as a
        // member of some recorded response; the fake 404s on anything else.
        var linked = sections.Where(s => s.IsSectionLinked)
            .Select(s => s.Crn).ToHashSet();
        var covered = new HashSet<string>();
        var fixture = new JsonObject();
        foreach (var (crn, body) in recorder.Responses)
        {
            fixture[crn] = JsonNode.Parse(body);
            covered.Add(crn);
            foreach (var group in JsonNode.Parse(body)?["linkedData"]?.AsArray() ?? [])
            {
                foreach (var section in group?.AsArray() ?? [])
                {
                    covered.Add(section?["courseReferenceNumber"]?.GetValue<string>());
                }
            }
        }
        covered.RemoveWhere(c => !linked.Contains(c));
        var missing = linked.Except(covered).OrderBy(c => c).ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));
        File.WriteAllText(outPath,
            fixture.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"fixture written: {outPath}");
        Console.WriteLine($"linked coverage: {covered.Count}/{linked.Count}");
        if (missing.Count > 0)
        {
            Console.WriteLine($"WARNING, uncovered linked CRNs: {string.Join(", ", missing)}");
            return 1;
        }
        return 0;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "src", "PurdueIo.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException(
            "Could not locate repo root from " + AppContext.BaseDirectory);
    }
}

// HttpClientHandler.SendAsync is protected; this shim re-exposes it so the
// recording handler can forward without cloning the request message.
public class DirectHandler : HttpClientHandler
{
    public Task<HttpResponseMessage> Send(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        SendAsync(request, cancellationToken);
}

public class RecordingHandler : HttpMessageHandler
{
    private readonly DirectHandler inner = new DirectHandler
    {
        CookieContainer = new CookieContainer(),
        AllowAutoRedirect = false,
    };

    // Queried CRN -> verbatim response body, in query order.
    public List<KeyValuePair<string, string>> Responses { get; } = [];

    public int Count => Responses.Count;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await inner.Send(request, cancellationToken);
        if (request.RequestUri.AbsolutePath.EndsWith("/searchResults/fetchLinkedSections"))
        {
            var crn = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query)
                ["courseReferenceNumber"];
            Responses.Add(new KeyValuePair<string, string>(crn,
                await response.Content.ReadAsStringAsync(cancellationToken)));
            Console.WriteLine($"[{Count}] fetchLinkedSections {crn}");
        }
        return response;
    }
}
