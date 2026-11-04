using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventManagement.Model.Domain;
using EventManagement.Model.DTO;
using EventManagement.Repository;
using Microsoft.IdentityModel.Tokens;

namespace EventManagement.Service;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository users, IConfiguration config)
    {
        _users = users;
        _config = config;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterAccountDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        EnsureAngelicumEmail(email);

        if (await _users.GetByEmailAsync(email) != null)
            throw new ServiceException("An account with this email already exists.");

        var user = new User
        {
            Email = email,
            FullName = dto.FullName.Trim(),
            PasswordHash = PasswordHasher.Hash(dto.Password),
            Role = UserRole.User
        };
        await _users.AddAsync(user);
        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        EnsureAngelicumEmail(email);

        var user = await _users.GetByEmailAsync(email);
        if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
            throw new ServiceException("Invalid email or password.");   // 400 on purpose (UI treats 401 as expired session)

        return BuildResponse(user);
    }

    private void EnsureAngelicumEmail(string email)
    {
        var domain = _config["Auth:AllowedEmailDomain"];
        if (string.IsNullOrWhiteSpace(domain)) return;
        if (!email.EndsWith("@" + domain.Trim().ToLowerInvariant()))
            throw new ServiceException($"Please use your Angelicum account (@{domain}).");
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("email", user.Email),
            new("name", user.FullName),
            new("role", user.Role.ToString())
        };
        if (user.AdminDepartment.HasValue)
            claims.Add(new Claim("dept", user.AdminDepartment.Value.ToString()));

        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(double.Parse(jwt["ExpiryHours"] ?? "8")),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthResponseDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.Email, user.FullName, user.Role == UserRole.Admin, user.AdminDepartment);
    }
}