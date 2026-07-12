using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class IdentityRoleEDConfig
    {
        public static void Configure(ModelBuilder modelBuilder)        {

            modelBuilder.Ignore<IdentityRole>();
            modelBuilder.Ignore<IdentityUserToken<string>>();
            modelBuilder.Ignore<IdentityUserRole<string>>();
            modelBuilder.Ignore<IdentityUserLogin<string>>();
            modelBuilder.Ignore<IdentityUserClaim<string>>();
            modelBuilder.Ignore<IdentityRoleClaim<string>>();
        }
    }
}
