using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.Entities.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Core.Utilities.Security.Encryption;

namespace Core.Utilities.Security.JWT;

public class JwtHelper : ITokenHelper
{
    private IConfiguration _configuration;
    private TokenOptions _tokenOptions;

    public JwtHelper(IConfiguration configuration)
    {
        _configuration = configuration;
        _tokenOptions = _configuration.GetSection("TokenOptions").Get<TokenOptions>();
    }

    public AccessToken CreateToken(User user)
    {
        if (_tokenOptions == null)
        {
            _tokenOptions = new TokenOptions
            {
                Audience = _configuration["TokenOptions:Audience"],
                Issuer = _configuration["TokenOptions:Issuer"],
                AccessTokenExpiration = int.Parse(_configuration["TokenOptions:AccessTokenExpiration"]),
                SecurityKey = _configuration["TokenOptions:SecurityKey"]
            };
        }

        var securityKey = SecurityKeyHelper.CreateSecurityKey(_tokenOptions.SecurityKey);
        var signingCredentials = SigningCredentialsHelper.CreateSigningCredentials(securityKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("InstitutionId", user.InstitutionId.ToString())
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        if (user.IsExpert)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Expert"));
        }
        if (user.IsOfficial) claims.Add(new Claim(ClaimTypes.Role, "Official"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddMinutes(_tokenOptions.AccessTokenExpiration),
            Issuer = _tokenOptions.Issuer,
            Audience = _tokenOptions.Audience,
            SigningCredentials = signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new AccessToken
        {
            Token = tokenHandler.WriteToken(token),
            Expiration = tokenDescriptor.Expires.Value
        };
    }
}