using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SFA.Application.Auth;

namespace SFA.Api.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> opt, TimeProvider? clock = null) : IJwtTokenService
{
  private readonly JwtOptions _opt = opt.Value;
  private readonly TimeProvider _clock = clock ?? TimeProvider.System; // melhor p/ testabilidade

  public TokenResult CreateToken(int userId, int codEmpresa, string nome, IEnumerable<string> roles)
  {
    var now = _clock.GetUtcNow().UtcDateTime;

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Secret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, userId.ToString()),
      new("cod_empresa", codEmpresa.ToString()),
      new(ClaimTypes.Name, nome),
    };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var token = new JwtSecurityToken(
      issuer: _opt.Issuer,
      audience: _opt.Audience,
      claims: claims,
      notBefore: now,
      expires: now.AddHours(_opt.ExpirationHours),
      signingCredentials: creds
    );

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return new TokenResult(jwt, now.AddHours(_opt.ExpirationHours));
  }
}
