using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// AUTH-03 / AUTH-04 — emision del JWT (logica con dependencias inyectables, sin DB).
    /// Protege el shape de claims, el calculo de isRoot contra RootTenantId y el claim firmado
    /// impersonatedBy.
    /// </summary>
    public class JwtSecurityTokenFactoryTests
    {
        private const long RootTenantId = 1;
        private const string ClientIp = "203.0.113.5";

        private static JwtSecurityTokenFactory BuildFactory()
        {
            var jwtConfig = Options.Create(new JwtSecurityTokenConfig
            {
                Key = "0123456789abcdef0123456789abcdef0123456789abcdef",
                Issuer = "CoreIdentity",
                Audience = "CoreIdentityUser",
                DurationInMinutes = 1440,
            });

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["RootTenantId"] = RootTenantId.ToString() })
                .Build();

            var ctx = new DefaultHttpContext();
            ctx.Connection.RemoteIpAddress = IPAddress.Parse(ClientIp);
            var accessor = new HttpContextAccessor { HttpContext = ctx };

            return new JwtSecurityTokenFactory(jwtConfig, configuration, accessor);
        }

        private static Dictionary<string, string> ClaimsOf(string jwt)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            // Si un claim se repitiera nos quedamos con el ultimo; ninguno de los de interes se repite.
            var dict = new Dictionary<string, string>();
            foreach (var c in token.Claims) dict[c.Type] = c.Value;
            return dict;
        }

        [Fact] // AUTH-03
        public void CreateInstance_emite_los_claims_esperados_y_HS256()
        {
            var factory = BuildFactory();

            var model = factory.CreateInstance(
                userId: 10, userName: "userb", email: "userb@tech-bi.com",
                tenantId: 2, isAdmin: false, securityStamp: "STAMP-XYZ");

            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(model.Token);
            var claims = ClaimsOf(model.Token);

            Assert.Equal("HS256", jwtToken.Header.Alg);
            Assert.Equal("userb", claims[JwtRegisteredClaimNames.Sub]);
            Assert.Equal("userb@tech-bi.com", claims[JwtRegisteredClaimNames.Email]);
            Assert.Equal("10", claims["userId"]);
            Assert.Equal("2", claims["tenant"]);
            Assert.Equal("False", claims["isRoot"]);     // tenant 2 != RootTenantId
            Assert.Equal("False", claims["isAdmin"]);
            Assert.Equal("STAMP-XYZ", claims["securityStamp"]);
            Assert.Equal(ClientIp, claims["ip"]);
            Assert.True(claims.ContainsKey(JwtRegisteredClaimNames.Jti));
            Assert.DoesNotContain("impersonatedBy", claims.Keys);

            // exp = now + DurationInMinutes (tolerancia amplia por el tiempo de ejecucion).
            var expected = DateTime.UtcNow.AddMinutes(1440);
            Assert.True(Math.Abs((jwtToken.ValidTo - expected).TotalMinutes) < 2);
            Assert.Equal(model.ValidTo, jwtToken.ValidTo);
        }

        [Fact] // AUTH-03
        public void CreateInstance_marca_isRoot_true_para_el_RootTenantId()
        {
            var factory = BuildFactory();

            var model = factory.CreateInstance(1, "root", "admin@tech-bi.com", RootTenantId, true, "S");

            Assert.True(model.IsRoot);
            Assert.Equal("True", ClaimsOf(model.Token)["isRoot"]);
            Assert.Equal("True", ClaimsOf(model.Token)["isAdmin"]);
        }

        [Fact] // AUTH-04
        public void CreateInstance_con_impersonatedBy_agrega_el_claim_firmado_y_scopea_al_destino()
        {
            var factory = BuildFactory();

            // Token de impersonalizacion: identidad = destino (userb t2); impersonatedBy = root (1).
            var model = factory.CreateInstance(10, "userb", "userb@tech-bi.com", 2, false, "S", impersonatedBy: 1);

            var claims = ClaimsOf(model.Token);
            Assert.Equal("1", claims["impersonatedBy"]);
            Assert.Equal("userb", claims[JwtRegisteredClaimNames.Sub]); // identidad del destino, no de root
            Assert.Equal("2", claims["tenant"]);
            Assert.Equal("False", claims["isRoot"]);
        }
    }
}
