using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;

namespace AcgFotos.Core.Session
{
    public class AppContext : IAppContext
    {
        #region Private fields

        private string _userNameLogueado;
        private string _userNameSimulado;
        private long _tenantLogueadoId;
        private long? _tenantSimuladoId;

        private long _userLogueadoId;
        private long? _userSimuladoId;

        private bool _userLogueadoIsAdmin;
        private bool? _userSimuladoIsAdmin;

        private bool _isRoot;
        private long? _impersonatedBy;
        private bool _isSystemContext;
        public string _securityStamp { get; private set; }

        private bool _isFamiliaSession;
        private long? _familiaEventoId;
        private List<long> _familiaParticipanteIds = new();
        #endregion
        /// <summary>
        /// Token recibido en el header del request
        /// </summary>
        public string Token { get; private set; }

        /// <summary>
        /// Esta propiedad estará en true si el request es de un usuario no logueado.
        /// Solo debería suceder para aquellos endpoints no securizados
        /// </summary>
        public bool IsAnonymous
        {
            get
            {
                // Un contexto de sistema (background/scheduler) no tiene token pero NO es anónimo: el filtro
                // global de tenant y el estampado multi-tenant deben aplicar como en un request autenticado.
                return string.IsNullOrEmpty(this.Token) && !_isSystemContext;
            }
        }

        /// <summary>
        /// User name que está en el contexto. 
        /// Para las clases que acceden debería ser transparente si es el loguedo o simulado 
        /// </summary>
        public string UserName
        {
            get
            {
                if (!string.IsNullOrEmpty(_userNameSimulado) && _isRoot)
                {
                    return _userNameSimulado;
                }
                else
                {
                    return _userNameLogueado;
                }
            }
        }

        /// <summary>
        /// Tener presente que si el usuario logueado es Root esta variable siempre es true.
        /// Si el contexto está siendo simulado igualmente esta propiedad es true.
        /// IMPORTANTE: Esta propiedad no deberá ser usada fuera del framework, salvo en casos puntuales como en usuario y permisos.
        /// </summary>
        public bool IsRoot
        {
            get
            {
                return _isRoot;
            }
        }
        // internal bool IsRoot { get; set; }

        /// <inheritdoc />
        public bool IsFamiliaSession
        {
            get
            {
                return _isFamiliaSession;
            }
        }

        /// <inheritdoc />
        public long? FamiliaEventoId
        {
            get
            {
                return _familiaEventoId;
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<long> FamiliaParticipanteIds
        {
            get
            {
                return _familiaParticipanteIds;
            }
        }

        /// <summary>
        /// Si el contexto es una sesión de impersonalización (ADR-0002), devuelve el <b>userId real de root</b>
        /// que está impersonando (claim firmado <c>impersonatedBy</c>); <c>null</c> si no se está impersonando.
        /// El token va scopeado al usuario destino (<see cref="UserId"/>/<see cref="TenantId"/> ya son del
        /// destino), así que esta propiedad es solo para auditoría/autorización del propio flujo de impersonalización.
        /// </summary>
        public long? ImpersonatedBy
        {
            get
            {
                return _impersonatedBy;
            }
        }

        /// <summary>
        /// User Id que está en el contexto.
        /// Para las clases que acceden debería ser transparente si es el loguedo o simulado 
        /// </summary>
        public long UserId
        {
            get
            {
                if (_userSimuladoId.HasValue && _isRoot)
                {
                    return _userSimuladoId.Value;
                }
                else
                {
                    return _userLogueadoId;
                }
            }
        }

        /// <summary>
        /// Tenant que está en el contexto. 
        /// Para las clases que acceden debería ser transparente si es el loguedo o simulado 
        /// </summary>
        public long TenantId
        {
            get
            {
                if (_tenantSimuladoId.HasValue && _isRoot)
                {
                    return _tenantSimuladoId.Value;
                }
                else
                {
                    return _tenantLogueadoId;
                }
            }
        }

        /// <summary>
        /// Determina si el usuario es admin
        /// Para las clases que acceden debería ser transparente si es el loguedo o simulado 
        /// Tener presente que si es root, no va a dar que NO es admin.
        /// </summary>
        public bool IsAdmin
        {
            get
            {
                if (_userSimuladoIsAdmin.HasValue && _isRoot)
                {
                    return _userSimuladoIsAdmin.Value;
                }
                else
                {
                    return _userLogueadoIsAdmin;
                }
            }
        }

        /// <summary>
        /// Aplicación activa. Este dato viene del header.
        /// Actualmente, se valida el acceso a los items del menú y las cuentas mediante esta propiedad.
        /// Tener presente que se puede manipular desde el front.
        /// </summary>
        public long? AplicacionId { get; private set; }

        public string DatabaseName { get; private set; }

        public void SetContext(HttpRequest request)
        {
            if (request != null)
            {
                //Se setea el idioma para las traducciones de los mensajes desde la API
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(request.Headers["cultureInfo"].ToString());

                var accessToken = request.Headers[HeaderNames.Authorization].ToString();

                if (!string.IsNullOrEmpty(accessToken))
                {
                    accessToken = accessToken.Replace("Bearer ", "").Replace("bearer ", "");
                    this.Token = accessToken;
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

                    var claim = securityToken.Claims;
                    var subKey = claim.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub);
                    var securityStamp = claim.FirstOrDefault(c => c.Type == "securityStamp");
                    var tenant = claim.FirstOrDefault(claim => claim.Type == "tenant");
                    var isRoot = claim.FirstOrDefault(claim => claim.Type == "isRoot");
                    var isAdmin = claim.FirstOrDefault(claim => claim.Type == "isAdmin");
                    var userId = claim.FirstOrDefault(claim => claim.Type == "userId");
                    var impersonatedBy = claim.FirstOrDefault(claim => claim.Type == "impersonatedBy");

                    if (subKey != null)
                    {
                        _userNameLogueado = subKey.Value;
                    }
                    if (securityStamp != null)
                    {
                        _securityStamp = securityStamp.Value;
                    }
                    if (tenant != null)
                    {
                        _tenantLogueadoId = Convert.ToInt64(tenant.Value);
                    }
                    if (isRoot != null)
                    {
                        _isRoot = Convert.ToBoolean(isRoot.Value);
                    }
                    if (isAdmin != null)
                    {
                        _userLogueadoIsAdmin = Convert.ToBoolean(isAdmin.Value);
                    }

                    if (userId != null)
                    {
                        _userLogueadoId = Convert.ToInt64(userId.Value);

                    }
                    if (impersonatedBy != null && !string.IsNullOrEmpty(impersonatedBy.Value))
                    {
                        _impersonatedBy = Convert.ToInt64(impersonatedBy.Value);
                    }

                    // Claims del JWT de sesión de familia (ADR-11, AcgFotos.Fotos.Application.Familias.
                    // FamiliaTokenFactory). Core no puede referenciar el vertical (regla de arquitectura),
                    // así que los nombres de claim son literales acá — deben mantenerse sincronizados con
                    // las constantes de FamiliaTokenFactory.
                    var sessionType = claim.FirstOrDefault(c => c.Type == "sessionType");
                    if (sessionType != null && sessionType.Value == "familia")
                    {
                        _isFamiliaSession = true;

                        var eventoId = claim.FirstOrDefault(c => c.Type == "eventoId");
                        if (eventoId != null)
                        {
                            _familiaEventoId = Convert.ToInt64(eventoId.Value);
                        }

                        _familiaParticipanteIds = claim
                            .Where(c => c.Type == "participanteId")
                            .Select(c => Convert.ToInt64(c.Value))
                            .ToList();
                    }

                    var aplicacionId = request.Headers["aplicacionId"].ToString();

                    if (!string.IsNullOrEmpty(aplicacionId))
                    {
                        this.AplicacionId = Convert.ToInt64(aplicacionId);
                    }

                    var databaseName = request.Headers["databaseName"].ToString();

                    if (databaseName != null)
                    {
                        this.DatabaseName = databaseName;
                    }

                    

                    this.SetSimulatedContext(request);
                }
            }
        }

        public void SetTenantId(long tenantId)
        {
            _tenantLogueadoId = tenantId;
        }
        public void SetUserId(long userId)
        {
            _userLogueadoId = userId;
        }
        public void SetUserAdmin(bool esAdmin)
        {
            _userLogueadoIsAdmin = esAdmin;
        }

        /// <inheritdoc />
        public void SetSystemContext(long tenantId, long userId, bool isAdmin)
        {
            _isSystemContext = true;
            _tenantLogueadoId = tenantId;
            _userLogueadoId = userId;
            _userLogueadoIsAdmin = isAdmin;
            _userNameLogueado = "system";
            // _isRoot queda false: AllowSaveMultiTenantEntities() devuelve true y el filtro de tenant scopea
            // por _tenantLogueadoId (sin simulación).
        }

        private void SetSimulatedContext(HttpRequest request)
        {
            var tenantId = request.Headers["SimulatedTenant"].ToString();

            if (!string.IsNullOrEmpty(tenantId))
            {
                _tenantSimuladoId = Convert.ToInt64(tenantId);
            }
            var userId = request.Headers["SimulatedUserId"].ToString();

            if (!string.IsNullOrEmpty(userId))
            {
                _userSimuladoId = Convert.ToInt64(userId);
            }
            var userName = request.Headers["SimulatedUserName"].ToString();

            if (!string.IsNullOrEmpty(userName))
            {
                _userNameSimulado = userName;

            }

            var isAdmin = request.Headers["SimulatedIsAdmin"].ToString();

            if (!string.IsNullOrEmpty(isAdmin))
            {
                _userSimuladoIsAdmin = Convert.ToBoolean(isAdmin); ;

            }
        }


        /// <summary>
        /// Valida si el usuario del contexto puede persistir entidades que hereden del multitenant.
        /// En el caso de que se haya logueado un usuario Root, solo lo podrá hacer si está simulado
        /// </summary>
        /// <returns></returns>
        public bool AllowSaveMultiTenantEntities()
        {
            if (this.TenantId == _tenantLogueadoId && _isRoot)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// No usa esta propiedad fuera del fwk
        /// </summary>
        /// <returns></returns>
        public bool CheckPemissions()
        {
            return (!this.IsRoot || (this.IsRoot && !string.IsNullOrEmpty(_userNameSimulado)));
           
        }
    }
}
