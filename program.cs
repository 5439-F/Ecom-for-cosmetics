using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using Angular_Project_7.Server.Models;
using Angular_Project_7.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURE SERVICES ==========

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MongoDB Configuration
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

// MongoDB Client
builder.Services.AddSingleton<IMongoClient>(s =>
    new MongoClient(builder.Configuration.GetValue<string>("MongoDBSettings:ConnectionString")));

// Register UserService
builder.Services.AddSingleton<UserService>();

// JWT Authentication Configuration
var jwtConfig = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtConfig["Key"] ?? throw new InvalidOperationException("JWT Key is missing"));

// Ensure the key length is sufficient for HS256 (256-bit / 32-byte)
if (key.Length < 32)
{
    throw new InvalidOperationException("The JWT key must be at least 256 bits (32 bytes) for HS256 encryption.");
}

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
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig["Issuer"] ?? throw new InvalidOperationException("Issuer is missing"),
        ValidAudience = jwtConfig["Audience"] ?? throw new InvalidOperationException("Audience is missing"),
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder.WithOrigins("https://127.0.0.1:52305")  // Adjust to match your frontend origin
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());  // Allows cookies, authentication headers, etc.
});

var app = builder.Build();

// ========== MIDDLEWARE ==========

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable HTTPS redirection for production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Apply the CORS policy globally
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Global exception handling middleware for better error messages
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(new { message = "Internal Server Error" }.ToString());
    });
});

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();
