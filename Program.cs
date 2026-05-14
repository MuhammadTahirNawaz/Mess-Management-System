using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Semester_Project.Data;
using Semester_Project.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyForMessManagementSystem2025!@#$%";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Add Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120); // Increased to 2 hours
    options.IOTimeout = TimeSpan.FromSeconds(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax; // Prevent session loss on redirects
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Allow both HTTP and HTTPS
    options.Cookie.Name = ".MessManagement.Session"; // Custom cookie name
});

// Register Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Register QR Code Service
builder.Services.AddScoped<IQRCodeService, QRCodeService>();

// Register JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();

// Register Face Recognition Service
builder.Services.AddScoped<IFaceRecognitionService, FaceRecognitionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// Custom middleware to ensure session persistence and log session issues
app.Use(async (context, next) =>
{
    var path = context.Request.Path.ToString();
    
    // Skip session check for public pages and static files
    if (!path.Contains("/Login") && 
        !path.Contains("/Register") && 
        !path.StartsWith("/Home/Index") &&
        !path.StartsWith("/lib/") &&
        !path.StartsWith("/css/") &&
        !path.StartsWith("/js/"))
    {
        var studentEmail = context.Session.GetString("StudentEmail");
        var adminEmail = context.Session.GetString("AdminEmail");
        
        // If neither session exists and user is trying to access protected pages
        if (string.IsNullOrEmpty(studentEmail) && string.IsNullOrEmpty(adminEmail))
        {
            // Only log for actual page requests, not for static resources
            if (!path.Contains(".") || path.EndsWith(".cshtml"))
            {
                Console.WriteLine($"[SESSION WARNING] No active session for path: {path}");
            }
        }
        else
        {
            // Session exists - ensure it's refreshed (this keeps the session alive)
            if (!string.IsNullOrEmpty(studentEmail))
            {
                context.Session.SetString("StudentEmail", studentEmail);
            }
            if (!string.IsNullOrEmpty(adminEmail))
            {
                context.Session.SetString("AdminEmail", adminEmail);
            }
        }
    }
    
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Login}/{id?}");

app.Run();
