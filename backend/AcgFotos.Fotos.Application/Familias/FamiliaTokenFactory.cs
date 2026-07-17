using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using AcgFotos.Core.Security;
using AcgFotos.Fotos.Application.Procesamiento;

namespace AcgFotos.Fotos.Application.Familias;

/// <summary>
/// Emisor del JWT de sesión de familia (ADR-11). Reutiliza el MISMO signing key/issuer/audience que
/// la plataforma (<see cref="JwtSecurityTokenConfig"/>, Core) — es la infraestructura JWT existente
/// que pide docs/01-arquitectura.md — pero con claims propios y una duración propia (no la de
/// <c>DurationInMinutes</c>, que es la sesión del fotógrafo/admin).
/// </summary>
public class FamiliaTokenFactory : IFamiliaTokenFactory
{
    /// <summary>Discrimina un token de familia de uno de plataforma (sin usuario en gen_Usuarios).</summary>
    public const string SessionTypeClaim = "sessionType";
    public const string SessionTypeFamilia = "familia";
    public const string EventoIdClaim = "eventoId";
    public const string ParticipanteIdClaim = "participanteId";

    private readonly JwtSecurityTokenConfig _jwtConfig;
    private readonly OpcionesFotos _opciones;

    public FamiliaTokenFactory(IOptions<JwtSecurityTokenConfig> jwt, OpcionesFotos opciones)
    {
        _jwtConfig = jwt.Value;
        _opciones = opciones;
    }

    public FamiliaTokenModel Crear(long tenantId, long eventoId, IReadOnlyCollection<long> participanteIds)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, "familia"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(SessionTypeClaim, SessionTypeFamilia),
            new Claim("tenant", tenantId.ToString()), // mismo nombre que AppContext.SetContext lee
            new Claim(EventoIdClaim, eventoId.ToString()),
        };
        claims.AddRange(participanteIds.Select(id => new Claim(ParticipanteIdClaim, id.ToString())));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key)),
            SecurityAlgorithms.HmacSha256);

        var jwtSecurityToken = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opciones.DuracionSesionFamiliaMinutos),
            signingCredentials: signingCredentials);

        return new FamiliaTokenModel
        {
            Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
            ValidTo = jwtSecurityToken.ValidTo,
        };
    }
}
