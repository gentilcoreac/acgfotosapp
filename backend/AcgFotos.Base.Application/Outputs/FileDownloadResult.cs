namespace AcgFotos.Base.Application.Outputs
{
    /// <summary>Resultado de descarga: el binario + metadata para que el Controller arme el FileResult.</summary>
    public class FileDownloadResult
    {
        public byte[] Content { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}
