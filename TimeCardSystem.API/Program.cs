// Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;
using TimeCardSystem.Infrastructure.Data;
using TimeCardSystem.Infrastructure.Repositories;
using TimeCardSystem.Infrastructure.Services;
using TimeCardSystem.API.ExceptionHandling;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Enhanced Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.EnableSensitiveDataLogging() // Move to the main options builder
          .EnableDetailedErrors()        // Move to the main options builder
          .UseSqlServer(
              builder.Configuration.GetConnectionString("DefaultConnection"),
              sqlServerOptionsAction: sqlOptions =>
              {
                  sqlOptions.CommandTimeout(30); // Use integer seconds instead of TimeSpan
                  sqlOptions.MigrationsAssembly("TimeCardSystem.Infrastructure");
              }
          )
);

// Configure Identity with enhanced options
builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;

    // Enhanced password requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings with enhanced security
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Name = "TimeCardSystem.Identity";
});

// Add Razor Pages support with enhanced configuration
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
});

// Add CORS with more restrictive policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("StrictCorsPolicy",
        policy => policy
            .WithOrigins("http://localhost:3000", "https://yourdomain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
    );
});

// Add Controllers and API Explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add repositories and services
builder.Services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add global exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Database Initialization with Enhanced Error Handling
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        // Ensure database is created and migrations are applied
        await context.Database.MigrateAsync();

        // Seed initial data if no users exist
        if (!userManager.Users.Any())
        {
            var testUser = new User
            {
                UserName = "test@example.com",
                Email = "test@example.com",
                NormalizedEmail = "TEST@EXAMPLE.COM",
                NormalizedUserName = "TEST@EXAMPLE.COM",
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Administrator
            };

            var result = await userManager.CreateAsync(testUser, "Password123!");

            if (!result.Succeeded)
            {
                // Log detailed error information
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"User creation error: {error.Description}");
                }
            }
        }
    }
}
catch (Exception ex)
{
    // Log critical initialization errors
    Console.WriteLine($"Critical error during database initialization: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("StrictCorsPolicy");

app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

// Global exception handler
app.UseExceptionHandler();

// Map endpoints
app.MapControllers();
app.MapRazorPages();

app.Run();