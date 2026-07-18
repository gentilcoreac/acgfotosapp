using Microsoft.AspNetCore.Http;
using AcgFotos.Core.Domain;
using System.Collections.Generic;

namespace AcgFotos.Core.Session
{
    public interface IAppContext
    {
        /// <summary>
        /// Esta propiedad estará en true si el request es de un usuario no logueado.
        /// Solo debería suceder para aquellos endpoints no securizados
        /// </summary>
        string Token { get; }

        /// <summary>
        /// Esta propiedad estará en true si el request es de un usuario no logueado.
        /// Solo debería suceder para aquellos endpoints no securizados
        /// </summary>
        bool IsAnonymous { get; }

        /// <summary>
        /// Determina si el usuario es admin
        /// Para las clases que acceden debería ser transparente si es el loguedo o simulado 
        /// Tener presente que si es root, no va a dar que NO es admin.
        /// </summary>
        bool IsAdmin { get; }

      
        bool IsRoot { get; }

        /// <summary>
        /// Si el contexto es una sesión de impersonalización (ADR-0002), el <b>userId real de root</b>
        /// que está impersonando (claim firmado <c>impersonatedBy</c>); <c>null</c> si no se está impersonando.
        /// </summary>
        long? ImpersonatedBy { get; }

        /// <summary>
        /// User Id que está en el contexto.
        /// Para las clases que acceden debería ser transparente si es el loguedo o simulado 
        /// </summary>
        long UserId { get; }

        /// <summary>
        /// User name que está en el contexto. 
        /// Para las clases que acceden debería ser transparente si es el loguedo o simulado 
        /// </summary>
        string UserName { get; }


        /// <summary>
        /// Tenant que está en el contexto. 
        /// Para las clases que acceden debería ser transparente si es el loguedo o simulado 
        /// </summary>
        long TenantId { get; }

        /// <summary>
        /// True si el token es una sesión de FAMILIA (ADR-11: claim <c>sessionType=familia</c>), no un
        /// usuario de plataforma — no tiene fila en <c>gen_Usuarios</c>. Ver <see cref="FamiliaEventoId"/>
        /// y <see cref="FamiliaParticipanteIds"/>.
        /// </summary>
        bool IsFamiliaSession { get; }

        /// <summary>EventoId de la sesión de familia (claim <c>eventoId</c>); null fuera de una sesión de familia.</summary>
        long? FamiliaEventoId { get; }

        /// <summary>
        /// ParticipanteIds de la sesión de familia (claim <c>participanteId</c>, repetible — la sesión ya
        /// nace lista para más de un participante, docs/05: hermanos / persona en dos grupos). Vacío fuera
        /// de una sesión de familia.
        /// </summary>
        IReadOnlyCollection<long> FamiliaParticipanteIds { get; }

        /// <summary>
        /// Aplicación activa. Este dato viene del header.
        /// Actualmente, se valida el acceso a los items del menú y las cuentas mediante esta propiedad.
        /// Tener presente que se puede manipular desde el front.
        /// Tener presente que este campo está completo solo si es un usuario final o estoy simulando
        /// </summary>
        public long? AplicacionId { get; }

        void SetContext(HttpRequest request);
        void SetTenantId(long tenantId);
        void SetUserId(long userId);
        void SetUserAdmin(bool esAdmin);

        /// <summary>
        /// Arma un contexto SINTÉTICO para ejecuciones en background (scheduler de jobs): fija tenant/usuario y
        /// marca el contexto como NO anónimo, de modo que el filtro global de tenant y el estampado multi-tenant
        /// del DbContext apliquen igual que en un request autenticado de ese tenant. Solo debe llamarse en scopes
        /// creados fuera del pipeline HTTP (IServiceScopeFactory); nunca sobre el contexto de un request.
        /// </summary>
        void SetSystemContext(long tenantId, long userId, bool isAdmin);

        // bool IsGranted(string permiso);

        /// <summary>
        /// Valida si el usuario del contexto puede persistir entidades que hereden del multitenant.
        /// En el caso de que se haya logueado un usuario Root, solo lo podrá hacer si está simulado
        /// Solo deberia usarse en el FWK.
        /// </summary>
        /// <returns></returns>
        bool AllowSaveMultiTenantEntities();

        /// <summary>
        /// No usa esta propiedad fuera del fwk
        /// </summary>
        /// <returns></returns>
        bool CheckPemissions();
    }
}
