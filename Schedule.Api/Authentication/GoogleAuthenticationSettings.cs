namespace Schedule.Api.Authentication;

public class GoogleAuthenticationSettings
{
    public bool IsConfigured { get; init; }
    public required string HostedDomain { get; init; }
    public required string WebBaseUrl { get; init; }
}
