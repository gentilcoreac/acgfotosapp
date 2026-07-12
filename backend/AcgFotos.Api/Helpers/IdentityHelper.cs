using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AcgFotos.Core.Security.Culture;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Data;
using System;

namespace AcgFotos.Api.Helpers
{
    public class IdentityHelper
    {
        public static void ConfigureService(IServiceCollection services, IConfiguration configuration)
        {
            var maxFailedAttempts = configuration.GetValue<int?>("ErroresIngresoPermitidos") ?? 5;

            services.AddDefaultIdentity<Usuario>(options =>
            {
                // Password policy (OWASP-aligned).
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 4;

                options.SignIn.RequireConfirmedAccount = true;

                // Lockout: 5 intentos por default (alineado con ErroresIngresoPermitidos en appsettings).
                // AllowedForNewUsers=true para que el lockout aplique desde el primer login.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = maxFailedAttempts;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddErrorDescriber<SpanishIdentityErrorDescriber>()
            .AddRoles<IdentityRole<long>>()
            .AddEntityFrameworkStores<AcgFotosDbContext>();

            // Iteraciones de PBKDF2 configurables (default Identity = 100k). Solo se sobreescribe si
            // la clave está presente y es > 0; prod/qa/dev quedan en el default salvo que se setee.
            // El perfil de E2E la baja (p. ej. 1000) para que el login deje de ser CPU-bound y la
            // suite no flakee por latencia — NO bajar en prod (debilita el hashing).
            var hasherIterations = configuration.GetValue<int?>("Security:PasswordHasherIterations");
            if (hasherIterations is int iterations && iterations > 0)
            {
                services.Configure<PasswordHasherOptions>(options => options.IterationCount = iterations);
            }
        }
    }
}
