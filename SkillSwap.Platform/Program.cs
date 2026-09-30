using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using SkillSwap.Platform.Shared.Infrastructure.Localization;
using SkillSwap.Platform.Iam.Application.CommandServices;
using SkillSwap.Platform.Iam.Application.Internal.CommandServices;
using SkillSwap.Platform.Iam.Application.Internal.OutboundServices;
using SkillSwap.Platform.Iam.Application.Internal.QueryServices;
using SkillSwap.Platform.Iam.Application.QueryServices;
using SkillSwap.Platform.Iam.Domain.Repositories;
using SkillSwap.Platform.Iam.Domain.Services;
using SkillSwap.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;
using SkillSwap.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Extensions;
using SkillSwap.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using SkillSwap.Platform.Iam.Infrastructure.Tokens.Jwt.Services;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.OpenApi;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Localization (error messages resolved from Shared/Resources/Errors)
builder.Services.AddLocalization();

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

// Database (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Shared
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Identity & Access Bounded Context
builder.Services.AddOptions<TokenSettings>()
    .Bind(builder.Configuration.GetSection("TokenSettings"))
    .Validate(s => !string.IsNullOrWhiteSpace(s.Secret) && s.Secret.Length >= 32,
        "TokenSettings:Secret must be configured with at least 32 characters.")
    .ValidateOnStart();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IEmailDomainValidator, EmailDomainValidator>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

// English is the default language; any Spanish variant is served as es-419.
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures("en-US", LatinAmericanSpanishRequestCultureProvider.Culture)
    .AddSupportedUICultures("en-US", LatinAmericanSpanishRequestCultureProvider.Culture);
localizationOptions.RequestCultureProviders.Insert(0, new LatinAmericanSpanishRequestCultureProvider());
app.UseRequestLocalization(localizationOptions);

app.UseRequestAuthorization();

app.MapControllers();

app.Run();