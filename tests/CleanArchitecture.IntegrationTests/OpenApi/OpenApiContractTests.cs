using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArchitecture.IntegrationTests.Infrastructure;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.OpenApi;

/// <summary>
/// Pins the published contract: any change to routes, parameters, schemas or responses fails this test until the
/// approved snapshot is updated in the same change, so contract drift is always reviewed. On a mismatch the actual
/// document is written next to it as <c>*.received.json</c>; approve it by replacing the approved file, or run the
/// tests with <c>UPDATE_SNAPSHOTS=1</c>.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class OpenApiContractTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string SnapshotName = "openapi.v1";

    // Relaxed escaping keeps backticks and quotes readable in the reviewed diff; the file is never served.
    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [Fact]
    public async Task Document_matches_the_approved_contract()
    {
        using var client = CreateAnonymousClient();

        var json = await client.GetStringAsync(OpenApiDocument.Path, CancellationToken);

        await MatchesSnapshotAsync(Normalize(json));
    }

    private static string Normalize(string json) =>
        JsonNode.Parse(json)!.ToJsonString(Indented).ReplaceLineEndings("\n");

    private static async Task MatchesSnapshotAsync(string actual, [CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var approvedPath = Path.Combine(directory, $"{SnapshotName}.approved.json");
        var receivedPath = Path.Combine(directory, $"{SnapshotName}.received.json");

        if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1")
        {
            await File.WriteAllTextAsync(approvedPath, actual, CancellationToken);
        }

        var approved = File.Exists(approvedPath)
            ? (await File.ReadAllTextAsync(approvedPath, CancellationToken)).ReplaceLineEndings("\n")
            : null;

        if (approved == actual)
        {
            File.Delete(receivedPath);
            return;
        }

        await File.WriteAllTextAsync(receivedPath, actual, CancellationToken);
        approved.ShouldBe(
            actual,
            $"The OpenAPI document changed. Review {receivedPath} and, if intended, replace {approvedPath} with it.");
    }
}
