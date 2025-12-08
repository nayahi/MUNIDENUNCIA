namespace MUNIDENUNCIA.Services
{
    /// <summary>
    /// Interfaz para el servicio de protección de datos usando ASP.NET Core Data Protection API
    /// SEMANA 4: Control de Acceso y Protección de Datos
    /// 
    /// Este servicio proporciona métodos para cifrar y descifrar datos sensibles
    /// de forma segura usando las mejores prácticas de ASP.NET Core.
    /// </summary>
    public interface IDataProtectionService
    {
        /// <summary>
        /// Cifra un texto plano usando Data Protection API
        /// </summary>
        /// <param name="plainText">Texto a cifrar (ej: "1-0234-0567")</param>
        /// <returns>Texto cifrado en formato seguro</returns>
        /// <example>
        /// string cedulaCifrada = service.Protect("1-0234-0567");
        /// // Retorna: "CfDJ8..." (texto cifrado)
        /// </example>
        string Protect(string plainText);

        /// <summary>
        /// Descifra un texto cifrado previamente con Protect()
        /// </summary>
        /// <param name="cipherText">Texto cifrado a descifrar</param>
        /// <returns>Texto plano original</returns>
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// Si el texto cifrado es inválido o fue alterado
        /// </exception>
        /// <example>
        /// string cedulaOriginal = service.Unprotect("CfDJ8...");
        /// // Retorna: "1-0234-0567"
        /// </example>
        string Unprotect(string cipherText);
    }
}
