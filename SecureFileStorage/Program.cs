using FirebaseAdmin;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureFileStorage;
using SecureFileStorage.API.Authentication;
using SecureFileStorage.Application.Validators;
using SecureFileStorage.Infrastructure.Services;
using SecureFileStorage.Infrastructure.Services.Interfaces;
using SecureFileStorage.Models;
using SecureFileStorage.Models.Map;
using SecureFileStorage.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AzureBlobStorageService>();
builder.Services.AddScoped<LocalFileStorageService>();
builder.Services.AddScoped<FileStorageServiceResolver>();

builder.Services.AddScoped<IStorageService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
   // string? storageType = configuration.GetSection("StorageConfiguration:StorageType").Value;
    var storageType = configuration.GetSection("StorageConfiguration:StorageType").Value ?? "AzureBlobStorage"; // Varsayýlan deðer

    var resolver = provider.GetRequiredService<FileStorageServiceResolver>();
    return resolver.GetStorageService(storageType);
});
// Logging ekle
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Caching ekle
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddHttpClient<IAuthService, AuthService>();

 
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("firebase-config.json")
});


//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var secretKey = jwtSettings["Secret"];
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "FileSharedSecurityCode555666888"; // Varsayýlan deðer (GÜVENLÝ DEÐÝL!)
// JWT Kimlik Doðrulama Yapýlandýrmasý
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})

.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = false; // Lokal geliþtirme için HTTPS gereksinimini kaldýr
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Token süresi hassas olsun
    };
})
.AddJwtBearer("JwtBearer", options =>
{
    options.Authority = "https://securetoken.google.com/" + jwtSettings["Audience"]; // Firebase Auth Authority URL
    options.Audience = jwtSettings["Audience"]; // Firebase projenizin ID'si

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "https://securetoken.google.com/your_firebase_project_id", // Firebase Issuer URL
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"], // Firebase Audience (Proje ID'si)
        ValidateLifetime = true
    };
}); 
builder.Services.AddScoped<IAuthService, AuthService>();

// Local File Storage Service
builder.Services.AddKeyedScoped<IStorageService, LocalFileStorageService>("LocalFileStorage");

// Azure Blob Storage Service
builder.Services.AddKeyedScoped<IStorageService, AzureBlobStorageService>("AzureBlobStorage");
 
// MediatR CQRS
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Program.cs (ASP.NET 6 ve üzeri)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EditAccess", policy =>
        policy.RequireClaim("permissions", "edit"));
});


builder.Services.AddTransient<System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo { Title = "File Storage API", Version = "v1" });
//});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();

// API Dokümantasyonu (Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithAuth();


// CORS yapýlandýrmasý
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("http://example.com") // Ýzin verilen origin(ler)
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "File Secure Share V1");
        c.DefaultModelsExpandDepth(-1); // Model detaylarýný gizleyerek ekraný sadeleþtirme
    });
    app.MapOpenApi();
    // Yönlendirme (Routing)
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/")
        {
            context.Response.Redirect("/swagger/index.html");
            return;
        }

        await next();
    });
}

app.UseHttpsRedirection();
app.UseMiddleware<AuthenticationSchemeMiddleware>();
app.UseAuthentication(); // Kimlik doðrulama middleware'ini ekleyin
app.UseAuthorization(); // Yetkilendirme middleware'ini ekleyin

app.MapControllers();

app.UseCors("AllowSpecificOrigin");
app.Run();

 