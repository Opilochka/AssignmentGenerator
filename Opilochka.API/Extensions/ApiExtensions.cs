using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Opilochka.Core.Auth;
using System.Text;

namespace Opilochka.API.Extensions
{
    public static class ApiExtensions
    {
        public static void AddApiAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtOptions = configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opt =>
                {
                    opt.RequireHttpsMetadata = false;
                    opt.TokenValidationParameters = new()
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorizationBuilder()
                .AddPolicy(RoleJWTData.AdminUserPolicyName, p =>
                {
                    p.RequireRole(RoleJWTData.AdminUserClaimName);
                })
                .AddPolicy(RoleJWTData.StudentUserPolicyName, p =>
                {
                    p.RequireClaim(RoleJWTData.RoleUserClaimName, RoleJWTData.StudentUserClaimName);
                })
                .AddPolicy(RoleJWTData.TeacherUserPolicyName, p =>
                {
                    p.RequireClaim(RoleJWTData.RoleUserClaimName, RoleJWTData.TeacherUserClaimName);
                });
        }

        public static void AddApiSwaggerGen(this IServiceCollection services)
        {
            services.AddSwaggerGen(opt =>
            {
                opt.SwaggerDoc("v1", new OpenApiInfo { Title = "TopSecret", Version = "v1" });
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
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            
        }
    }
}
