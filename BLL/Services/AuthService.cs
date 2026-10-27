using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BLL.DTOs.Auth;
using BLL.Interfaces;
using BLL.Settings;
using DAL.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BLL.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName))
        {
            throw new ArgumentException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.LastName))
        {
            throw new ArgumentException("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new ArgumentException("Password is required.");
        }

        if (dto.Password != dto.ConfirmPassword)
        {
            throw new ArgumentException("Password and confirmation password do not match.");
        }

        string normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            CreatedAt = DateTime.UtcNow
        };

        var createUserResult = await _userManager.CreateAsync(user, dto.Password);

        if (!createUserResult.Succeeded)
        {
            string errors = string.Join(
                " ",
                createUserResult.Errors.Select(error => error.Description));

            throw new ArgumentException(errors);
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, "Customer");

        if (!addRoleResult.Succeeded)
        {
            string errors = string.Join(
                " ",
                addRoleResult.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"User was created but role assignment failed: {errors}");
        }

        return await CreateAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new ArgumentException("Email and password are required.");
        }

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        bool validPassword = await _userManager.CheckPasswordAsync(
            user,
            dto.Password);

        if (!validPassword)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return await CreateAuthResponseAsync(user);
    }

    private async Task<AuthResponseDto> CreateAuthResponseAsync(
        ApplicationUser user)
    {
        List<string> roles = (await _userManager.GetRolesAsync(user)).ToList();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.Key));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(
            _jwtSettings.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        string tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponseDto
        {
            Token = tokenValue,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            Roles = roles
        };
    }
}