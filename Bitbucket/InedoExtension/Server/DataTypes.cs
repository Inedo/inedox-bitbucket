using System.Text.Json.Serialization;

namespace Inedo.Extensions.Bitbucket.Server;

[JsonSerializable(typeof(RestProject))]
[JsonSerializable(typeof(Paged<RestProject>))]
[JsonSerializable(typeof(Paged<RestRepository>))]
[JsonSerializable(typeof(Paged<RestBranch>))]
[JsonSerializable(typeof(Paged<RestPullRequest>))]
[JsonSerializable(typeof(RestMergePullRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class BitbucketJsonContext : JsonSerializerContext
{
}

internal sealed class Paged<T>
{
    public int Size { get; set; }
    public int Limit { get; set; }
    public bool IsLastPage { get; set; }
    public IEnumerable<T>? Values { get; set; }
    public int Start { get; set; }
}

internal sealed class RestProject
{
    public string? Name { get; set; }
    public string? Key { get; set; }
    public int Id { get; set; }
    public string? Type { get; set; }
    public bool Public { get; set; }
    public string? Description { get; set; }
    public string? Scope { get; set; }
}

internal sealed class RestRepository : IGitRepositoryInfo
{
    public string? Name { get; set; }
    public int Id { get; set; }
    public string? DefaultBranch { get; set; }
    public string? Slug { get; set; }
    public RestRepoLinks? Links { get; set; }

    private string? BrowseUrl => this.Links?.Self?.FirstOrDefault()?.Href;

    string IGitRepositoryInfo.RepositoryUrl => this.Links?.Clone?.FirstOrDefault(u => u.Name == "http")?.Href ?? string.Empty;
    string? IGitRepositoryInfo.BrowseUrl => this.BrowseUrl;

    string? IGitRepositoryInfo.GetBrowseUrlForTarget(GitBrowseTarget target)
    {
        var browseUrl = this.BrowseUrl;
        if (string.IsNullOrEmpty(browseUrl))
            return null;

        return target.Type switch
        {
            GitBrowseTargetType.Branch => $"{browseUrl}?at={Uri.EscapeDataString($"refs/heads/{target.Value}")}",
            _ => $"{browseUrl}?at={Uri.EscapeDataString(target.Value)}"
        };
    }
}

internal sealed class RestBranch
{
    public string? Id { get; set; }
    public string? DisplayId { get; set; }
    public string? Type { get; set; }
    public string? LatestCommit { get; set; }
    public bool IsDefault { get; set; }
}

internal sealed class RestPullRequest
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? State { get; set; } // DECLINED, MERGED, OPEN
    public bool Open { get; set; }
    public int Version { get; set; }
    public RestBranch? ToRef { get; set; }
    public RestBranch? FromRef { get; set; }
    public RestPullRequestLinks? Links { get; set; }
}

internal sealed class RestPullRequestLinks
{
    public IEnumerable<RestLink>? Self { get; set; }
}

internal sealed class RestRepoLinks
{
    public IEnumerable<RestLink>? Clone { get; set; }
    public IEnumerable<RestLink>? Self { get; set; }
}

internal sealed class RestLink
{
    public string? Href { get; set; }
    public string? Name { get; set; }
}

internal sealed class RestMergePullRequest
{
    public bool AutoSubject { get; set; }
    public string? Message { get; set; }
    public string? StrategyId { get; set; }
    public int Version { get; set; }
}
