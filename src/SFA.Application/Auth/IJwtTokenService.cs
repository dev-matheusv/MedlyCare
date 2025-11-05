namespace SFA.Application.Auth;

public interface IJwtTokenService
{
  TokenResult CreateToken(Guid userId, int codEmpresa, string nome, IEnumerable<string> roles);
}

public record TokenResult(string AccessToken, DateTime ExpiresAtUtc, string TokenType = "Bearer");
