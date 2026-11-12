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

        // Role and year level come from the email format, the user does not choose them
        var (position, level) = EmailRoleParser.Parse(email);

        var user = new User
        {
            Email = email,
            FullName = dto.FullName.Trim(),
            PasswordHash = PasswordHasher.Hash(dto.Password),
            Role = UserRole.User,
            Position = position,
            Level = level
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

    public async Task ChangePasswordAsync(string email, ChangePasswordDto dto)
    {
        var user = await _users.GetByEmailAsync(email.ToLowerInvariant())
                   ?? throw new ServiceException("Account not found.", 404);
        if (!PasswordHasher.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new ServiceException("Your current password is incorrect.");

        user.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
        await _users.SaveAsync();
    }

    private void EnsureAngelicumEmail(string email)
    {
        var domain = _config["Auth:AllowedEmailDomain"];
        if (string.IsNullOrWhiteSpace(domain)) return;
        if (!email.EndsWith("@" + domain.Trim().ToLowerInvariant()))
            throw new ServiceException($"Please use your Angelicum account (@{domain}). Outsiders can use the Visitor page.");
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var isAdmin = user.Role is UserRole.Admin or UserRole.SuperAdmin;
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("email", user.Email),
            new("name", user.FullName),
            new("role", user.Role.ToString()),
            new("position", user.Position.ToString()),
            new("level", user.Level.ToString())
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
            user.Email, user.FullName, isAdmin, user.Role == UserRole.SuperAdmin,
            user.AdminDepartment, user.Position, user.Level);
    }
}
