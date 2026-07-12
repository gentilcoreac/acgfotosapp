using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.Storage;

namespace AcgFotos.Base.Controllers.Api
{
    /// <summary>
    /// Gestor de archivos genéricos por tenant, sobre IStorageProvider. Los archivos se aíslan por tenant (filtro global). 
    /// Privados → descarga por este endpoint con auth;
    /// Públicos → URL estática. 
    /// El Controller convierte el IFormFile a primitivos para no acoplar el AppService a Microsoft.AspNetCore.Http.
    /// </summary>
    [ApiController]
    [Route("api/general/files")]
    public class FileController : ApiControllerBase
    {
        private readonly IFileAppService _fileAppService;

        public FileController(IFileAppService fileAppService)
        {
            _fileAppService = fileAppService;
        }

        [HttpPost]
        public async Task<ActionResult<TenantFileDto>> Upload(
            IFormFile file,
            [FromQuery] StorageVisibility visibility = StorageVisibility.Private)
        {
            if (file == null || file.Length == 0)
            {
                return this.BadRequest();
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var dto = await _fileAppService.UploadAsync(ms.ToArray(), file.FileName, file.ContentType, visibility);
            return this.Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<List<TenantFileDto>>> List()
        {
            var files = await _fileAppService.ListAsync();
            return this.Ok(files);
        }

        [HttpGet]
        [Route("{id}/download")]
        public async Task<IActionResult> Download(long id)
        {
            var result = await _fileAppService.GetForDownloadAsync(id);
            return this.File(result.Content, result.ContentType, result.FileName);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _fileAppService.DeleteAsync(id);
            return this.Ok();
        }
    }
}
