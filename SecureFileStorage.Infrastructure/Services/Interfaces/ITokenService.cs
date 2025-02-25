using System.Security.Claims;

public interface ITokenService
{
    string GenerateAccessToken(Claim[] claims);
    string GenerateRefreshToken();
}