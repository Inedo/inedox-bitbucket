using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Inedo.Extensions.Bitbucket.Server;

internal sealed class BitbucketServerClient
{
    private const string ApiVersion = "1.0";
    private readonly HttpClient http;

    public BitbucketServerClient(GitServiceCredentials creds) : this(creds.ServiceUrl!, creds.UserName!, AH.Unprotect(creds.Password))
    {
    }
    public BitbucketServerClient(string baseUrl, string? userName, string? password)
    {
        this.http = SDK.CreateHttpClient();
        this.http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Rubbish", "1.0"));
        this.http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            this.http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(InedoLib.UTF8Encoding.GetBytes($"{userName}:{password}")));

        this.http.BaseAddress = new Uri(baseUrl);
    }

    public IAsyncEnumerable<RestProject> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        return this.GetPagedAsync($"rest/api/{ApiVersion}/projects", BitbucketJsonContext.Default.PagedRestProject, cancellationToken);
    }
    public async Task<RestProject?> GetProjectByNameAsync(string projectName, CancellationToken cancellationToken = default)
    {
        await foreach (var p in this.GetProjectsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase))
                return p;
        }

        return null;
    }
    public IAsyncEnumerable<RestRepository> GetRepositoriesAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        return this.GetPagedAsync($"rest/api/{ApiVersion}/projects/{Uri.EscapeDataString(projectKey)}/repos", BitbucketJsonContext.Default.PagedRestRepository, cancellationToken);
    }
    public async Task<RestRepository?> GetRepositoryByNameAsync(string projectKey, string repositoryName, CancellationToken cancellationToken = default)
    {
        await foreach (var r in this.GetRepositoriesAsync(projectKey, cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(r.Name, repositoryName, StringComparison.OrdinalIgnoreCase))
                return r;
        }

        return null;
    }
    public IAsyncEnumerable<RestBranch> GetBranchesAsync(string projectKey, string repositorySlug, CancellationToken cancellationToken = default)
    {
        return this.GetPagedAsync($"rest/api/{ApiVersion}/projects/{Uri.EscapeDataString(projectKey)}/repos/{Uri.EscapeDataString(repositorySlug)}/branches", BitbucketJsonContext.Default.PagedRestBranch, cancellationToken);
    }
    public IAsyncEnumerable<RestPullRequest> GetPullRequestsAsync(string projectKey, string repositorySlug, CancellationToken cancellationToken = default)
    {
        return this.GetPagedAsync($"rest/api/{ApiVersion}/projects/{Uri.EscapeDataString(projectKey)}/repos/{Uri.EscapeDataString(repositorySlug)}/pull-requests", BitbucketJsonContext.Default.PagedRestPullRequest, cancellationToken);
    }
    public async Task<RestPullRequest> GetPullRequestAsync(string projectKey, string repositorySlug, string pullRequestId, CancellationToken cancellationToken = default)
    {
        return (await this.http.GetFromJsonAsync(
            $"rest/api/{ApiVersion}/projects/{Uri.EscapeDataString(projectKey)}/repos/{Uri.EscapeDataString(repositorySlug)}/pull-requests/{Uri.EscapeDataString(pullRequestId)}",
            BitbucketJsonContext.Default.RestPullRequest,
            cancellationToken
        ).ConfigureAwait(false))!;
    }
    public async Task MergePullRequestAsync(string projectKey, string repositorySlug, string pullRequestId, string? message, string? strategy, int version, CancellationToken cancellationToken = default)
    {
        var url = $"/rest/api/{ApiVersion}/projects/{Uri.EscapeDataString(projectKey)}/repos/{Uri.EscapeDataString(repositorySlug)}/pull-requests/{Uri.EscapeDataString(pullRequestId)}/merge";

        var data = new RestMergePullRequest
        {
            Message = message,
            StrategyId = strategy,
            Version = version
        };

        using var response = await this.http.PostAsJsonAsync(url, data, BitbucketJsonContext.Default.RestMergePullRequest, cancellationToken).ConfigureAwait(false);
    }

    private async IAsyncEnumerable<T> GetPagedAsync<T>(string url, JsonTypeInfo<Paged<T>> jsonTypeInfo, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int start = 0;
        while (true)
        {
            using var response = await this.http.GetAsync(WithStart(url, start), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var page = await JsonSerializer.DeserializeAsync(stream, jsonTypeInfo, cancellationToken);
            if (page == null)
                yield break;

            if (page.Values != null)
            {
                foreach (var v in page.Values)
                    yield return v;
            }

            if (page.IsLastPage || page.Size < 1)
                yield break;

            start += page.Size;
        }
    }
    private static string WithStart(string url, int n)
    {
        if (url.Contains('?'))
            return $"{url}&start={n}";
        else
            return $"{url}?start={n}";
    }
}
