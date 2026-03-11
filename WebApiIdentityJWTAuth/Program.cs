using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// JWT Authentication
string? jwtKey = builder.Configuration["Jwt:Key"]!; //för att signa token, måste vara minst 16 tecken
string? jwtIssuer = builder.Configuration["Jwt:Issuer"]; //för att validera token, måste matcha det som användes vid signering
string? jwtAudience = builder.Configuration["Jwt:Audience"]; // för att validera token, måste matcha det som användes vid signering

// Ställ in att använda JWT Bearer som autentiseringsmetod
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddJwtBearer("Bearer", options => // AddJtwBearer lägger till en autentiseringshandler som hanterar JWT tokens
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization(); //behövs för att kunna använda [Authorize] på endpoints

var app = builder.Build();

await app.SeedUsersAndRolesAsync();

app.UseAuthentication(); //Middleware för att kolla om requesten har en giltig JWT token och i så fall sätta HttpContext.User med rätt claims
app.UseAuthorization(); //Middleware för att kolla om användaren har rätt behörighet att nå en endpoint, baserat på [Authorize] attributet på endpointen

// POST /login
app.MapPost("/login", async (LoginRequest request, UserManager<IdentityUser> userManager) =>
{
    var user = await userManager.FindByNameAsync(request.Username);

    if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        return Results.Unauthorized();

    var roles = await userManager.GetRolesAsync(user);

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, user.UserName!),
        new(ClaimTypes.NameIdentifier, user.Id)
    };

    foreach (var role in roles)
        claims.Add(new Claim(ClaimTypes.Role, role));

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
});

app.MapGet("/public", () => "public");

app.MapGet("/protected", [Authorize] () => "logged in");

app.MapGet("/admin", [Authorize(Roles = "Admin")] () => "admin only");

app.Run();
