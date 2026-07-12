namespace AcgFotos.Core.Storage
{
    /// <summary>
    /// Visibilidad de un recurso de storage.
    /// - Public: accesible sin autenticación (ej. branding del tenant que se muestra
    ///   en la pantalla de login, antes de que el usuario se autentique).
    /// - Private: requiere autenticación + validación de ownership (ej. archivos de un
    ///   usuario o documentos privados de un tenant).
    /// El provider usa este flag para decidir dónde ubica el recurso y cómo genera la URL.
    /// </summary>
    public enum StorageVisibility
    {
        Public,
        Private
    }
}
