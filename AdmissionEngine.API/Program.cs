using System.Reflection;
using System.Text;
using AdmissionEngine.API.Middleware;
using AdmissionEngine.API.Services;
using AdmissionEngine.API.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi; // .NET 10 OpenAPI types namespace
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// 1. Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 2. Add Controllers & FluentValidation
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 3. Configure JWT Authentication
//var jwtSettings = builder.Configuration.GetSection("Jwt");
//var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "your_jwt_secret");

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.RequireHttpsMetadata = false;
//    options.SaveToken = true;
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = false,
//        ValidateAudience = false,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = jwtSettings["Issuer"],
//        ValidAudience = jwtSettings["Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(key)
//    };
//});

// 1. Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React ports
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 4. OpenAPI / Swagger Configuration with Bearer Token
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AdmissionEngine API",
        Version = "v1",
        Description = "API for automated candidate eligibility evaluation, course redistribution recommendations, and offer responses."
    });

    // 1. Define Security Scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer' followed by a space and your JWT token.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // 2. Add Security Requirement using single-parameter Func<OpenApiDocument, OpenApiSecurityRequirement>
    options.AddSecurityRequirement(doc =>
    {
        var req = new OpenApiSecurityRequirement();
        req[new OpenApiSecuritySchemeReference("Bearer")] = new List<string>();
        return req;
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
// 5. MongoDB Dependency Injection Registration
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration["MongoSettings:ConnectionString"]
                           ?? "mongodb://localhost:27017";  
    return new MongoClient(connectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration["MongoSettings:DatabaseName"] ?? "ewebar";
    return client.GetDatabase(databaseName);
});

// 6. Service Dependency Injection Registrations
builder.Services.AddScoped<IRedistributionEngine, RedistributionEngine>();
builder.Services.AddScoped<IAdmissionEngineService, AdmissionEngineService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

var app = builder.Build();

// 7. Global Exception Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 8. Middleware Pipeline
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AdmissionEngine API v1");
        c.RoutePrefix = "swagger";
    });
}

// Seed candidate datasets on startup
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    var seeder = new DataSeeder(database);

    // File 1
    string file1Path = Path.Combine(Directory.GetCurrentDirectory(), "2025_2026_Jamb_list.xlsx");
    if (File.Exists(file1Path))
    {
        await seeder.SeedCandidatesFromExcelAsync(file1Path);
    }

    // File 2 (Replace with your actual second filename)
    string file2Path = Path.Combine(Directory.GetCurrentDirectory(), "2024_2025_Jamb_list.xlsx");
    if (File.Exists(file2Path))
    {
        await seeder.SeedCandidatesFromExcelAsync(file2Path);
    }
}

// Enable CORS (Must be before MapControllers)
app.UseCors("AllowLocalhost");

app.UseHttpsRedirection();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();