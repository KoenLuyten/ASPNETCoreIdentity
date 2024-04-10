using ASPNETCoreIdentity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseInMemoryDatabase("AppDb"));

builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgenHandler>();
builder.Services.AddAuthorization(options => options.AddPolicy("LegalDrinkingAge", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21))
    ));



builder.Services
    .AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });

});

var app = builder.Build();

app.MapIdentityApi<IdentityUser>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await SeedRole("admin");
await SeedRole("superadmin");
var claims = new List<Claim>
{
    new Claim(ClaimTypes.DateOfBirth, new DateTime(1985,11,6).ToShortDateString()),
    new Claim(ClaimTypes.Country, "BE")
};
await SeedUser("koen.luyten@xebia.com", "Test!123", "admin", claims);

app.Run();

async Task SeedRole(string role)
{
    var roleManager = builder.Services.BuildServiceProvider().GetRequiredService<RoleManager<IdentityRole>>();
    await roleManager.CreateAsync(new IdentityRole(role));
}

async Task SeedUser(string email, string password, string role, IEnumerable<Claim> claims)
{
    var userManager = builder.Services.BuildServiceProvider().GetRequiredService<UserManager<IdentityUser>>();
    
    var user = new IdentityUser
    {
        UserName = email,
        Email = email,
        EmailConfirmed = true
    };

    await userManager.CreateAsync(user, password);
    await userManager.AddToRoleAsync(user, role);
    await userManager.AddClaimsAsync(user, claims);
}