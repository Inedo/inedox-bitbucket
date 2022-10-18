using System.Runtime.CompilerServices;

namespace Inedo.Extensions.Bitbucket.Server;

[DisplayName("Bitbucket Server/Data Center")]
[Description("Provides integration for Bitbucket Server or Data Center repositories.")]
public sealed class BitbucketServerServiceInfo : GitService<BitbucketServerRepository, BitbucketAccount>
{
    public override string ServiceName => "Bitbucket Server/Data Center";
    public override string NamespaceDisplayName => "Project";
    public override string PasswordDisplayName => "Password or token";
    public override string ApiUrlDisplayName => "Server URL";
    public override string ApiUrlPlaceholderText => "https://my-bitbucket-server/";

    public override async IAsyncEnumerable<string> GetNamespacesAsync(GitServiceCredentials credentials, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = new BitbucketServerClient(credentials);
        await foreach (var p in client.GetProjectsAsync(cancellationToken).ConfigureAwait(false))
            yield return p.Name!;
    }
    public override async IAsyncEnumerable<string> GetRepositoryNamesAsync(GitServiceCredentials credentials, string serviceNamespace, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = new BitbucketServerClient(credentials);
        var project = await client.GetProjectByNameAsync(serviceNamespace, cancellationToken).ConfigureAwait(false);
        if (project != null)
        {
            await foreach (var r in client.GetRepositoriesAsync(project.Key!, cancellationToken).ConfigureAwait(false))
                yield return r.Name!;
        }
    }
}
