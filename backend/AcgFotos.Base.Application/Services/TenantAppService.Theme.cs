using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using AcgFotos.Core.Application;
using AcgFotos.Core.Exceptions;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Theme;
using AcgFotos.Core.Storage;
using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Localization.APIResources;


namespace AcgFotos.Base.Application.Services
{

    public partial class TenantAppService
    {

        // Mismas semantica que AplicacionAppService.GetAplicacionesPorTenantId: solo el
        // usuario root del tenant root puede ejecutar operaciones cross-tenant.
        private void EnsureRootCaller()
        {
            var rootTenantId = _configuration.GetValue<long>("RootTenantId");
            if (!(this.AppContext.IsRoot && this.AppContext.TenantId == rootTenantId))
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNotPrivilegesAccess);
            }
        }

        public async Task RegenerarTodosLosThemesAsync()
        {
            this.EnsureRootCaller();
            var tenants = await _tenantRepository.GetTenantsWithoutCustomThemeAsync();

            foreach (var tenant in tenants)
            {
                await this.RegenerarThemePorTenantIdAsync(tenant.Id);
            }
        }

        public async Task RegenerarThemePorTenantIdAsync(long tenantId)
        {
            this.EnsureRootCaller();
            var tenant = await _tenantRepository.GetByIdReadOnlyAsync(tenantId);

            if (tenant == null)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorTenantNotFound);
            }

            if (!string.IsNullOrEmpty(tenant.StyleSheetCssUrl))
            {
                throw new BusinessValidationException("El tenant utiliza un custom theme.");
            }

            // template-base.css es un asset del deploy de la app (vive en wwwroot/assets), no un
            // recurso de tenant en el storage. Se lee directo del filesystem para que el flujo
            // funcione igual con cualquier IStorageProvider (FileSystem/DB/Azure).
            var templatePath = Path.Combine(_env.WebRootPath, "assets", "template-base.css");
            var cssBaseContent = await File.ReadAllTextAsync(templatePath);

            // Color primario del tenant
            var colorPrimarioLight = tenant.ColorPrimarioLight != null ? tenant.ColorPrimarioLight : "#009688";
            var colorPrimarioDark = tenant.ColorPrimarioDark != null ? tenant.ColorPrimarioDark : "#23C5BE";

            // Convertir el color HEX a HSL
            var hslColorModelLight = ColorConverter.ConvertHexToHSL(colorPrimarioLight);
            var hslColorModelDark = ColorConverter.ConvertHexToHSL(colorPrimarioDark);

            // Redondeamos el HUE en 2 decimales
            var hueRoundedLight = Math.Round(hslColorModelLight.HslHue, 2).ToString().Replace(",", ".");
            var hueRoundedDark = Math.Round(hslColorModelDark.HslHue, 2).ToString().Replace(",", ".");

            // Reemplazamos los colores (principal)
            var newCssContent = cssBaseContent.Replace("hsl(177, 70%, 45%)", $"{hslColorModelDark.Hsl}");

            newCssContent = newCssContent.Replace("#22c3bb", $"{hslColorModelDark.Hsl}");

            newCssContent = newCssContent.Replace("#009485", $"{hslColorModelLight.Hsl}");

            newCssContent = newCssContent.Replace("hsl(174, 100%, 29%)", $"{hslColorModelLight.Hsl}");

            //Reemplazo los colores secundarios del background para login
            newCssContent = newCssContent.Replace("hsl(177, 70%, 10%)", $"hsl({hueRoundedDark}deg, 70%, 10%)");
            newCssContent = newCssContent.Replace("hsl(174, 70%, 10%)", $"hsl({hueRoundedLight}deg, 70%, 10%)");

            // Reemplazamos los colores (secundarios)
            newCssContent = newCssContent.Replace("hsl(177, 17%, 28%)", $"hsl({hueRoundedDark}deg, 17%, 28%)");
            newCssContent = newCssContent.Replace("hsl(174, 31%, 54%)", $"hsl({hueRoundedLight}deg, 31%, 54%)");

            //reemplazo el color brand-active-color
            newCssContent = newCssContent.Replace("hsla(177, 70%, 45%, 0.2)", $"hsla({hueRoundedDark}, 70%, 45%, 0.2)");
            newCssContent = newCssContent.Replace("hsla(174, 100%, 29%, 0.2)", $"hsla({hueRoundedLight}, 100%, 29%, 0.2)");

            var cssKey = ThemeKey(tenant.Codigo);
            await _storageProvider.DeleteAsync(cssKey);
            await _storageProvider.SaveAsync(cssKey, Encoding.UTF8.GetBytes(newCssContent), StorageContentTypes.FromFileName(cssKey), StorageVisibility.Public);
        }

        private async Task<TenantDto> SaveUploadedFilesAsync(TenantDto tenantDto, TenantDto dto)
        {
            // ---- logos --------------------------------------------------
            // DARK
            if (dto.LogoLoginDarkFile != null)
            {
                tenantDto.LogoLoginDarkUrl = await this.SaveFileAsync(DecodeBase64(dto.LogoLoginDarkFile.Base64), "LogoLoginDark." + dto.LogoLoginDarkFile.Extension, dto.Codigo);
            }
            else if (string.IsNullOrEmpty(dto.LogoLoginDarkUrl))
            {
                await this.DeleteImageAsync("LogoLoginDark", dto.Codigo);
            }

            if (dto.LogoHeaderDarkFile != null)
            {
                tenantDto.LogoHeaderDarkUrl = await this.SaveFileAsync(DecodeBase64(dto.LogoHeaderDarkFile.Base64), "LogoHeaderDark." + dto.LogoHeaderDarkFile.Extension, dto.Codigo);
            }
            else if (string.IsNullOrEmpty(dto.LogoHeaderDarkUrl))
            {
                await this.DeleteImageAsync("LogoHeaderDark", dto.Codigo);
            }

            // -------------------------------------------------------------
            // LIGHT
            if (dto.LogoLoginLightFile != null)
            {
                tenantDto.LogoLoginLightUrl = await this.SaveFileAsync(DecodeBase64(dto.LogoLoginLightFile.Base64), "LogoLoginLight." + dto.LogoLoginLightFile.Extension, dto.Codigo);
            }
            else if (string.IsNullOrEmpty(dto.LogoLoginLightUrl))
            {
                await this.DeleteImageAsync("LogoLoginLight", dto.Codigo);
            }

            if (dto.LogoHeaderLightFile != null)
            {
                tenantDto.LogoHeaderLightUrl = await this.SaveFileAsync(DecodeBase64(dto.LogoHeaderLightFile.Base64), "LogoHeaderLight." + dto.LogoHeaderLightFile.Extension, dto.Codigo);
            }
            else if (string.IsNullOrEmpty(dto.LogoHeaderLightUrl))
            {
                await this.DeleteImageAsync("LogoHeaderLight", dto.Codigo);
            }
            // -------------------------------------------------------------

            if (dto.FaviconFile != null)
            {
                tenantDto.FaviconUrl = await this.SaveFileAsync(DecodeBase64(dto.FaviconFile.Base64), "Favicon." + dto.FaviconFile.Extension, dto.Codigo);
            }
            else if (string.IsNullOrEmpty(dto.FaviconUrl))
            {
                await this.DeleteImageAsync("Favicon", dto.Codigo);
            }

            if (dto.ImagenFondoLoginLightFile != null)
            {
                tenantDto.ImagenFondoLoginLightUrl = await this.SaveFileAsync(DecodeBase64(dto.ImagenFondoLoginLightFile.Base64), "FondoLoginLight." + dto.ImagenFondoLoginLightFile.Extension, dto.Codigo);
            }
            else if (string.IsNullOrEmpty(dto.ImagenFondoLoginLightUrl))
            {
                await this.DeleteImageAsync("FondoLoginLight", dto.Codigo);
            }

            if (dto.ImagenFondoLoginDarkFile != null)
            {
                tenantDto.ImagenFondoLoginDarkUrl = await this.SaveFileAsync(DecodeBase64(dto.ImagenFondoLoginDarkFile.Base64), "FondoLoginDark." + dto.ImagenFondoLoginDarkFile.Extension, dto.Codigo);
            }
            else if (string.IsNullOrEmpty(dto.ImagenFondoLoginDarkUrl))
            {
                await this.DeleteImageAsync("FondoLoginDark", dto.Codigo);
            }

            if (dto.StyleSheetFile != null)
            {
                tenantDto.StyleSheetCssUrl = await this.SaveFileAsync(dto.StyleSheetFile, dto.Codigo + ".css", dto.Codigo);
            }
            else
            {
                if (string.IsNullOrEmpty(dto.StyleSheetCssUrl))
                {
                    await this.RegenerarThemePorTenantIdAsync(tenantDto.Id);
                }
            }

            return tenantDto;
        }

        private static byte[] DecodeBase64(string base64) =>
            Convert.FromBase64String(Base64Helper.Clean(base64));

        // Key del recurso: imágenes y themes bajo tenants/{codigo}/{tipo}/.
        private static string ImageKey(string tenantCodigo, string fileName) =>
            $"tenants/{tenantCodigo}/images/{fileName}";

        private static string ThemeKey(string tenantCodigo) =>
            $"tenants/{tenantCodigo}/themes/{tenantCodigo}.css";

        private async Task<string> SaveFileAsync(byte[] file, string fileName, string tenantCodigo)
        {
            // El CSS custom del tenant va a themes/; el resto a images/.
            var key = fileName.EndsWith(".css")
                ? $"tenants/{tenantCodigo}/themes/{fileName}"
                : ImageKey(tenantCodigo, fileName);

            await _storageProvider.SaveAsync(key, file, StorageContentTypes.FromFileName(fileName), StorageVisibility.Public);
            return _storageProvider.GetUrl(key, StorageVisibility.Public);
        }

        // Borra todas las variantes de extensión de una imagen (Logo.png, Logo.svg, ...).
        private Task DeleteImageAsync(string fileName, string tenantCodigo) =>
            _storageProvider.DeleteByPrefixAsync(ImageKey(tenantCodigo, fileName));
    }
}
