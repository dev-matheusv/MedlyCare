namespace SFA.Api.Auth;

public sealed class JwtOptions
{
  public string Issuer { get; init; } = default!;
  public string Audience { get; init; } = default!;
  public string Secret { get; init; } = default!;
  public int ExpirationHours { get; init; } = 8; // default
}
