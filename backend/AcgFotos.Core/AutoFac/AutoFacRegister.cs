using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AcgFotos.Core.AutoFac
{
    public class AutoFacRegister
    {
        public static List<Assembly> GetApplicationModules(IConfiguration configuration)
        {
            var modules = configuration.GetSection("AppModulesName").Value;
            if (modules != null)
            {
                var modulesArray = modules.ToString().Split(";");

                var assemblies = new List<Assembly>();
                foreach (var moduleName in modulesArray)
                {

                    var applicationAssembly = Assembly.Load(moduleName + ".Application");
                    assemblies.Add(applicationAssembly);
                }

                return assemblies;
            }
            else
            {
                throw new Exception("No se pudieron recuperar m�dulos para registrar");
            }
        }
        public static  void RegisterControllerModules(IServiceCollection services, 
                                                      IConfiguration configuration)
        {
            var modules = configuration.GetSection("AppModulesName").Value;
            if (modules != null)
            {
                var modulesArray = modules.ToString().Split(";");

                var builder = services.AddMvc(opts =>
                {
                    var authIsEnabled = configuration.GetValue<bool>("AuthenticationEnabled");
                    if (!authIsEnabled)
                    {
                        opts.Filters.Add<AllowAnonymousFilter>();
                    }
                    else
                    {
                        var authenticatedUserPolicy = new AuthorizationPolicyBuilder()
                                  .RequireAuthenticatedUser()
                                  .Build();
                        opts.Filters.Add(new AuthorizeFilter(authenticatedUserPolicy));
                    }
                });
                foreach (var moduleName in modulesArray)
                {

                    var controllerAssembly = Assembly.Load(moduleName + ".Controllers");
                    builder = builder.AddApplicationPart(controllerAssembly);
                }

                builder.AddControllersAsServices().
                        AddJsonOptions(options =>
                                       options.JsonSerializerOptions.ReferenceHandler =
                                       System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
            }
            else
            {
                throw new Exception("No se pudieron recuperar m�dulos para registrar");
            }
        }
    }
}
