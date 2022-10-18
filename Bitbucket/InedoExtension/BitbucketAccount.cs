using System.Security;
using Inedo.Extensions.Bitbucket.Server;

namespace Inedo.Extensions.Bitbucket;

[DisplayName("Bitbucket Account")]
[Description("Use a Bitbucket to connect to Bitbucket resources.")]
public sealed class BitbucketAccount : GitServiceCredentials<BitbucketServerServiceInfo>
{
    [Required]
    [Persistent]
    [DisplayName("User name")]
    public override string? UserName { get; set; }
    [Required]
    [Persistent]
    [DisplayName("Password or personal access token")]
    [FieldEditMode(FieldEditMode.Password)]
    public override SecureString? Password { get; set; }

    public override RichDescription GetCredentialDescription() => new(this.UserName);

    public override RichDescription GetServiceDescription()
    {
        return string.IsNullOrEmpty(this.ServiceUrl) || !this.TryGetServiceUrlHostName(out var hostName)
            ? new("Bitbucket")
            : new("Bitbucket (", new Hilite(hostName), ")");
    }
}
