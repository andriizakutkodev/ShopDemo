﻿namespace API.Extensions;

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using API.Validators;
using Application.Interfaces;
using Application.Services;
using FluentValidation;
using Persistence.Interfaces;
using Persistence.Repositories;
using Application.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Application.Mappers;
using CloudinaryDotNet;

/// <summary>
/// Provides extension methods for registering dependencies into the <see cref="IServiceCollection"/>.
/// This method simplifies the process of loading application-specific dependencies.
/// </summary>
public static class ProgramExtensions
{
    /// <summary>
    /// The connection string key value.
    /// </summary>
    public const string CONNECTIONSTRINGKEY = "Default";

    /// <summary>
    /// The jwt section key value.
    /// </summary>
    public const string JWTSECTIONKEY = "Jwt";

    /// <summary>
    /// The jwt audience key value.
    /// </summary>
    public const string JWTAUDIENCEKEY = "Jwt:Audience";

    /// <summary>
    /// The jwt issuer key value.
    /// </summary>
    public const string JWTISSUERKEY = "Jwt:Issuer";

    /// <summary>
    /// The jwt secret key value.
    /// </summary>
    public const string JWTSECRETKEY = "Jwt:SecretKey";

    /// <summary>
    /// The cloudinary section key.
    /// </summary>
    public const string CLOUDINARYSECTIONKEY = "Cloudinary";

    /// <summary>
    /// Registers application dependencies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register dependencies with.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> to get access to config.</param>
    public static void RegisterDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterDbContext(services, configuration);
        RegisterServices(services);
        RegisterRepositories(services);
        RegisterValidators(services);
        RegisterOptions(services, configuration);
        RegisterAuthentication(services, configuration);
        RegisterSwaggerConfiguration(services);
        RegisterCors(services);
        RegisterAutoMapper(services);
        RegisterCloudinary(services, configuration);
    }

    private static void RegisterDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opt =>
        {
            opt.UseNpgsql(configuration.GetConnectionString(CONNECTIONSTRINGKEY), b =>
            {
                b.MigrationsAssembly(typeof(AppDbContext).Assembly);
            }).UseLazyLoadingProxies();
        });
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IPasswordHandlerService, PasswordHandlerService>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IVehicleModelService, VehicleModelService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IImageService, ImageService>();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVehicleModelRepository, VehicleModelRepository>();
        services.AddScoped<IImageRepository, ImageRepository>();
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();
    }

    private static void RegisterOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JWTSECTIONKEY));
        services.Configure<CloudinaryOptions>(configuration.GetSection(CLOUDINARYSECTIONKEY));
    }

    private static void RegisterAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = configuration[JWTISSUERKEY],
                ValidAudience = configuration[JWTAUDIENCEKEY],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration[JWTSECRETKEY] !)),
            };
        });
    }

    private static void RegisterSwaggerConfiguration(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            });
        });
    }

    private static void RegisterCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins("http://localhost:4200");
                builder.AllowAnyMethod();
                builder.AllowAnyHeader();
            });
        });
    }

    private static void RegisterAutoMapper(IServiceCollection services)
    {
        services.AddAutoMapper(typeof(AutoMapperProfile));
    }

    private static void RegisterCloudinary(IServiceCollection services, IConfiguration configuration)
    {
        var cloudinaryOptions = configuration.GetSection(CLOUDINARYSECTIONKEY).Get<CloudinaryOptions>() ?? throw new Exception("Cloudinary options is not provided.");

        var account = new Account(cloudinaryOptions.CloudName, cloudinaryOptions.ApiKey, cloudinaryOptions.ApiSecret);

        services.AddSingleton(new Cloudinary(account));
    }
}
