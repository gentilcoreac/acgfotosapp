using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using AcgFotos.Core.Session;
using System;
using System.Text;

namespace AcgFotos.Core.Security
{
    public class AuthenticationHelper
    {
        public static void ConfigureService(IServiceCollection service, IConfiguration configuration)
        {
            var jwtSecurityToken = configuration.GetSection("JwtSecurityToken").Get<JwtSecurityTokenConfig>();

            service
                .AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,

                        ValidIssuer = jwtSecurityToken.Issuer,
                        ValidAudience = jwtSecurityToken.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecurityToken.Key)),

                        // ValidateTokenReplay = true,
                        // ValidateTokenReplay sin un ITokenReplayCache configurado es no-op.
                        // Si en el futuro hace falta detección de replay, agregar cache distribuido (Redis). Considerar costos
                        SaveSigninToken = true,
                    };

                    o.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var securityRepository = context.HttpContext.RequestServices.GetRequiredService<ISecurityRepository>();
                            var appContext = RequestHeaderHandler.Encode(context.HttpContext.Request);

                            if (!appContext.IsRoot)
                            {
                                var isValidSecurityStamp = securityRepository.ValidateSecurityStamp(appContext.UserName, appContext.TenantId, appContext._securityStamp);
                                if (!isValidSecurityStamp)
                                {
                                    context.Fail(new Exception());
                                }
                            }
                        }
                    };
                });          
        }
    }
}
