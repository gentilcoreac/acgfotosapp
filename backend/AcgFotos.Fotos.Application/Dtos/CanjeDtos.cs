namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>Código que la familia canjea (desde el form manual o el path param del link del QR).</summary>
public class CanjeInputDto
{
    public string Codigo { get; set; } = string.Empty;
}

/// <summary>Resultado del canje: el token de sesión + los datos para saludar a la familia sin otro round-trip.</summary>
public class CanjeResultDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ValidTo { get; set; }
    public long EventoId { get; set; }
    public string NombreEvento { get; set; } = string.Empty;
    public List<ParticipanteSesionDto> Participantes { get; set; } = new();
}

public class ParticipanteSesionDto
{
    public long Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
