using System;

namespace AcgFotos.Core.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class Permission : Attribute
    {
        public Permission(string[] permisos)
        {
            Permiso = permisos;
        }
        public Permission(string permiso)
        {
            string[] permisoString = { permiso };
            this.Permiso = permisoString;
        }
        public string[] Permiso { get; set; }

    }
}
