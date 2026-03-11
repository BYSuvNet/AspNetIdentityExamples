using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddJwtBearer("Bearer", options =>
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

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed users
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (await userManager.FindByNameAsync("user") == null)
    {
        var user = new IdentityUser { UserName = "user", Email = "user@example.com" };
        await userManager.CreateAsync(user, "Password123!");
    }

    if (await userManager.FindByNameAsync("admin") == null)
    {
        var admin = new IdentityUser { UserName = "admin", Email = "admin@example.com" };
        await userManager.CreateAsync(admin, "Password123!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}

app.UseAuthentication();
app.UseAuthorization();

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

// GET /public - open for all
app.MapGet("/public", () => "public");

// GET /protected - logged in users only
app.MapGet("/protected", [Authorize] () => "logged in");

// GET /admin - admin role only
app.MapGet("/admin", [Authorize(Roles = "Admin")] () => "admin only");

app.Run();

// ---- Supporting types ----

public record LoginRequest(string Username, string Password);

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}