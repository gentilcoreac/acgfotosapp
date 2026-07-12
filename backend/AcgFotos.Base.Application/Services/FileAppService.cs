using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;
using AcgFotos.Core.Storage;

namespace AcgFotos.Base.Application.Services
{
    public class FileAppService : IFileAppService
    {
        private const int DefaultMaxFileSizeMb = 25;

        private readonly IStorageProvider _storageProvider;
        private readonly IEntityBaseRepository<TenantFile> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppContext _appContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public FileAppService(
            IStorageProvider storageProvider,
            IEntityBaseRepository<TenantFile> repository,
            IUnitOfWork unitOfWork,
            IAppContext appContext,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IMapper mapper)
        {
            _storageProvider = storageProvider;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _appContext = appContext;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<TenantFileDto> UploadAsync(byte[] content, string fileName, string contentType, StorageVisibility visibility)
        {
            if (content == null || content.Length == 0)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorGenericInternal);
            }

            var maxBytes = (_configuration.GetValue<int?>("Storage:MaxFileSizeMB") ?? DefaultMaxFileSizeMb) * 1024L * 1024L;
            if (content.Length > maxBytes)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorGenericInternal);
            }

            var tenantId = _appContext.TenantId;
            var key = BuildKey(tenantId, fileName, visibility);

            await _storageProvider.SaveAsync(key, content, contentType, visibility);

            var entity = new TenantFile
            {
                TenantId = tenantId,
                FileName = fileName,
                ContentType = contentType,
                Length = content.Length,
                StorageKey = key,
                Visibility = visibility,
                CreatedByUserId = _appContext.UserId,
                CreateDatetime = DateTime.Now
            };

            _repository.Add(entity);
            await _unitOfWork.CommitAsync();

            return this.ToDto(entity);
        }

        public async Task<List<TenantFileDto>> ListAsync()
        {
            // El filtro global por TenantId acota al tenant del caller.
            var files = await _repository.GetAllAsync();
            return files.Select(this.ToDto).ToList();
        }

        public async Task<FileDownloadResult> GetForDownloadAsync(long id)
        {
            // GetByIdAsync respeta el filtro global: si el archivo es de otro tenant, devuelve null.
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new BusinessValidationException(MessagesAPI.ErrorGenericInternal);

            var content = await _storageProvider.ReadAsync(entity.StorageKey);
            return new FileDownloadResult
            {
                Content = content,
                ContentType = entity.ContentType,
                FileName = entity.FileName
            };
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new BusinessValidationException(MessagesAPI.ErrorGenericInternal);

            await _storageProvider.DeleteAsync(entity.StorageKey);
            _repository.DeleteById(id);
            await _unitOfWork.CommitAsync();
        }

        // Key única por archivo. Prefijo public/ o private/ define la zona en el provider.
        // El GUID evita colisiones y enumeración del nombre original.
        private static string BuildKey(long tenantId, string fileName, StorageVisibility visibility)
        {
            var zone = visibility == StorageVisibility.Public ? "public" : "private";
            var safeName = System.IO.Path.GetFileName(fileName);
            return $"{zone}/tenants/{tenantId}/files/{Guid.NewGuid():N}/{safeName}";
        }

        private TenantFileDto ToDto(TenantFile entity)
        {
            var dto = _mapper.Map<TenantFileDto>(entity);
            dto.Url = this.BuildUrl(entity);
            return dto;
        }

        private string BuildUrl(TenantFile entity)
        {
            if (entity.Visibility == StorageVisibility.Public)
            {
                return _storageProvider.GetUrl(entity.StorageKey, StorageVisibility.Public);
            }

            // Privado: URL del endpoint con autorización (no hay URL estática).
            var request = _httpContextAccessor.HttpContext.Request;
            return $"{request.Scheme}://{request.Host}/api/general/files/{entity.Id}/download";
        }
    }
}
