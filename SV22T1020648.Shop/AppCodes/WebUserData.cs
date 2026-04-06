using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SV22T1020648.Shop;

public class WebUserData
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public ClaimsPrincipal CreatePrincipal()
    {
        var claims = new List<Claim>
        {
            new(nameof(UserId), UserId),
            new(nameof(UserName), UserName),
            new(nameof(DisplayName), DisplayName),
            new(nameof(Address), Address),
            new(nameof(Province), Province),
            new(nameof(Phone), Phone)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}