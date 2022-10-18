using System.Runtime.CompilerServices;
using Inedo.Extensibility.Credentials;

namespace Inedo.Extensions.Bitbucket.Server;

[DisplayName("Bitbucket Repository")]
[Description("Connect to a Bitbucket repository for source code integration.")]
public sealed class BitbucketServerRepository : GitServiceRepository<BitbucketAccount>
{
    [Required]
    [Persistent]
    [DisplayName("Project")]
    public string? ProjectName { get; set; }
    [Required]
    [Persistent]
    [DisplayName("Repository")]
    public override string? RepositoryName { get; set; }

    public override string? Namespace { get => this.ProjectName; set => this.ProjectName = value; }

    public override RichDescription GetDescription() => new($"{this.ProjectName}/{this.RepositoryName}");

    public override async IAsyncEnumerable<GitRemoteBranch> GetRemoteBranchesAsync(ICredentialResolutionContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (project, repo, client) = await this.GetInitDataAsync(context, cancellationToken).ConfigureAwait(false);

        await foreach (var b in client.GetBranchesAsync(project.Key!, repo.Slug!, cancellationToken).ConfigureAwait(false))
        {
            if (!GitObjectId.TryParse(b.LatestCommit, out var hash) || b.DisplayId == null)
                continue;

            yield return new GitRemoteBranch(hash, b.DisplayId);
        }
    }
    public override async IAsyncEnumerable<GitPullRequest> GetPullRequestsAsync(ICredentialResolutionContext context, bool includeClosed = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (project, repo, client) = await this.GetInitDataAsync(context, cancellationToken).ConfigureAwait(false);

        await foreach (var r in client.GetPullRequestsAsync(project.Key!, repo.Slug!, cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrEmpty(r.FromRef?.DisplayId) || string.IsNullOrEmpty(r.ToRef?.DisplayId))
                continue;

            yield return new GitPullRequest(r.Id.ToString(), r.Links?.Self?.FirstOrDefault()?.Href, r.Title, !r.Open, r.FromRef.DisplayId, r.ToRef.DisplayId);
        }
    }
    public override async Task<IGitRepositoryInfo> GetRepositoryInfoAsync(ICredentialResolutionContext context, CancellationToken cancellationToken = default)
    {
        var (_, repo, _) = await this.GetInitDataAsync(context, cancellationToken).ConfigureAwait(false);
        return repo;
    }
    public override async Task MergePullRequestAsync(ICredentialResolutionContext context, string id, string headCommit, string? commitMessage = null, string? method = null, CancellationToken cancellationToken = default)
    {
        var (project, repo, client) = await this.GetInitDataAsync(context, cancellationToken).ConfigureAwait(false);

        var pullRequest = await client.GetPullRequestAsync(project.Key!, repo.Slug!, id, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(pullRequest.ToRef?.LatestCommit, headCommit, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot merge; head commits differ.");

        await client.MergePullRequestAsync(project.Key!, repo.Slug!, id, commitMessage, method, pullRequest.Version, cancellationToken).ConfigureAwait(false);
    }
    public override Task SetCommitStatusAsync(ICredentialResolutionContext context, string commit, string status, string? description = null, string? statusContext = null, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Bitbucket does not have commit statuses.");
    }

    private async Task<InitData> GetInitDataAsync(ICredentialResolutionContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(this.ProjectName) || string.IsNullOrEmpty(this.RepositoryName))
            throw new InvalidOperationException($"{nameof(ProjectName)} and {nameof(RepositoryName)} are required.");

        if (this.GetCredentials(context) is not BitbucketAccount creds)
            throw new InvalidOperationException($"Invalid credentials; expected {nameof(BitbucketAccount)}.");

        var client = new BitbucketServerClient(creds);

        var project = await client.GetProjectByNameAsync(this.ProjectName, cancellationToken).ConfigureAwait(false);
        if (project?.Key == null)
            throw new InvalidOperationException($"Project {this.ProjectName} not found.");

        var repo = await client.GetRepositoryByNameAsync(project.Key, this.RepositoryName, cancellationToken).ConfigureAwait(false);
        if (repo?.Slug == null)
            throw new InvalidOperationException($"Repository {this.RepositoryName} not found in project {this.ProjectName}.");

        return new InitData(project, repo, client);
    }

    private sealed record class InitData(RestProject Project, RestRepository Repo, BitbucketServerClient Client);
}
