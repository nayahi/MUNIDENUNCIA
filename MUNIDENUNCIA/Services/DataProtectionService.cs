using Microsoft.AspNetCore.DataProtection;
using System;

namespace MUNIDENUNCIA.Services
{
    /// <summary>
    /// Implementación del servicio de protección de datos usando ASP.NET Core Data Protection API
    /// SEMANA 4: Control de Acceso y Protección de Datos
    /// 
    /// Este servicio utiliza el sistema de Data Protection de ASP.NET Core para cifrar
    /// y descifrar datos sensibles de forma segura. Proporciona:
    /// 
    /// 1. Cifrado simétrico robusto usando algoritmos modernos (AES-256)
    /// 2. Gestión automática de claves de cifrado
    /// 3. Rotación de claves integrada
    /// 4. Protección contra alteraciones (incluye HMAC)
    /// 5. Límite de tiempo de validez de datos cifrados (opcional)
    /// 
    /// VENTAJAS SOBRE CIFRADO MANUAL:
    /// - No necesita gestionar claves manualmente
    /// - No necesita gestionar IVs (vectores de inicialización)
    /// - Protección integrada contra ataques de padding oracle
    /// - APIs simples y a prueba de errores
    /// - Compatible con escenarios distribuidos (múltiples servidores)
    /// 
    /// ALMACENAMIENTO DE CLAVES:
    /// - Desarrollo: %LOCALAPPDATA%\ASP.NET\DataProtection-Keys
    /// - Producción: Configurar almacenamiento centralizado (Redis, Azure Key Vault, etc.)
    /// </summary>
    public class DataProtectionService : IDataProtectionService
    {
        private readonly IDataProtector _protector;

        /// <summary>
        /// Constructor que recibe el IDataProtectionProvider inyectado por DI
        /// </summary>
        /// <param name="dataProtectionProvider">
        /// Proveedor de Data Protection configurado en el sistema
        /// </param>
        public DataProtectionService(IDataProtectionProvider dataProtectionProvider)
        {
            // Crear un protector con un propósito específico
            // El "propósito" asegura que datos cifrados para un uso
            // no puedan descifrarse con un protector de otro propósito
            _protector = dataProtectionProvider.CreateProtector(
                "MUNIDENUNCIA.DatosSensibles.v1"
            );

            // EXPLICACIÓN PEDAGÓGICA del propósito:
            // El string "MUNIDENUNCIA.DatosSensibles.v1" es el "propósito" del cifrado.
            // 
            // ¿Por qué es importante?
            // - Aislamiento: Datos cifrados con un propósito solo pueden descifrarse
            //   con un protector del MISMO propósito
            // - Seguridad: Evita que datos de diferentes contextos se mezclen
            // - Versionado: El ".v1" permite cambiar el esquema en el futuro
            //
            // Ejemplo práctico:
            // Si ciframos una cédula con propósito "DatosSensibles"
            // y luego intentamos descifrarla con propósito "Tokens",
            // fallará - incluso con las mismas claves maestras.
        }

        /// <summary>
        /// Cifra un texto plano usando Data Protection API
        /// </summary>
        /// <param name="plainText">Texto a cifrar</param>
        /// <returns>Texto cifrado en formato Base64</returns>
        /// <exception cref="ArgumentNullException">Si plainText es null o vacío</exception>
        public string Protect(string plainText)
        {
            // Validación de entrada
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentNullException(nameof(plainText), 
                    "El texto a cifrar no puede ser nulo o vacío");
            }

            try
            {
                // Cifrar el texto usando Data Protection API
                // Internamente esto:
                // 1. Genera un IV aleatorio
                // 2. Cifra con AES-256-CBC
                // 3. Calcula HMAC-SHA256 para integridad
                // 4. Combina todo en formato propietario
                // 5. Codifica en Base64 para almacenamiento
                string encryptedText = _protector.Protect(plainText);

                // NOTA PEDAGÓGICA:
                // El texto cifrado resultante incluye:
                // - Magic header (identifica versión del formato)
                // - Key ID (identifica qué clave se usó)
                // - IV (Vector de inicialización)
                // - Texto cifrado
                // - HMAC (Hash Message Authentication Code)
                //
                // Todo esto se maneja automáticamente por Data Protection API

                return encryptedText;
            }
            catch (Exception ex)
            {
                // En producción, registrar el error sin exponer detalles
                // al usuario final
                throw new InvalidOperationException(
                    "Error al cifrar los datos. Contacte al administrador.", ex);
            }
        }

        /// <summary>
        /// Descifra un texto cifrado previamente con Protect()
        /// </summary>
        /// <param name="cipherText">Texto cifrado a descifrar</param>
        /// <returns>Texto plano original</returns>
        /// <exception cref="ArgumentNullException">Si cipherText es null o vacío</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// Si el texto fue alterado o la clave de cifrado cambió
        /// </exception>
        public string Unprotect(string cipherText)
        {
            // Validación de entrada
            if (string.IsNullOrEmpty(cipherText))
            {
                throw new ArgumentNullException(nameof(cipherText), 
                    "El texto cifrado no puede ser nulo o vacío");
            }

            try
            {
                // Descifrar el texto usando Data Protection API
                // Esto automáticamente:
                // 1. Valida el HMAC (detecta alteraciones)
                // 2. Busca la clave correcta usando el Key ID
                // 3. Descifra usando AES-256-CBC
                // 4. Valida el padding
                // 5. Retorna el texto plano
                string decryptedText = _protector.Unprotect(cipherText);

                // SEGURIDAD INCORPORADA:
                // Si el texto cifrado fue alterado de cualquier forma,
                // o si se intenta descifrar con claves incorrectas,
                // se lanza CryptographicException automáticamente.
                
                return decryptedText;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                // Error de descifrado - datos corruptos o clave incorrecta
                // NO exponer detalles técnicos al usuario
                throw new InvalidOperationException(
                    "No se pudieron descifrar los datos. Los datos pueden estar corruptos " +
                    "o las claves de cifrado han cambiado.");
            }
            catch (Exception ex)
            {
                // Otros errores inesperados
                throw new InvalidOperationException(
                    "Error al descifrar los datos. Contacte al administrador.", ex);
            }
        }
    }

    // ============================================================================
    // NOTAS PEDAGÓGICAS ADICIONALES SOBRE DATA PROTECTION API
    // ============================================================================
    //
    // 1. ROTACIÓN DE CLAVES:
    //    Data Protection API soporta rotación automática de claves.
    //    Las claves antiguas se mantienen para poder descifrar datos antiguos,
    //    pero los nuevos cifrados usan la clave más reciente.
    //
    // 2. LÍMITE DE TIEMPO:
    //    Se puede crear un protector con tiempo límite:
    //    var timeLimitedProtector = _protector.ToTimeLimitedDataProtector();
    //    timeLimitedProtector.Protect(data, TimeSpan.FromDays(7));
    //    Útil para tokens de reseteo de contraseña, etc.
    //
    // 3. ALMACENAMIENTO EN MÚLTIPLES SERVIDORES:
    //    Para aplicaciones distribuidas, configurar en Program.cs:
    //    services.AddDataProtection()
    //        .PersistKeysToFileShare(new DirectoryInfo(@"\\server\share\keys"))
    //        .SetApplicationName("MUNIDENUNCIA");
    //
    // 4. INTEGRACIÓN CON AZURE KEY VAULT:
    //    Para máxima seguridad en la nube:
    //    services.AddDataProtection()
    //        .PersistKeysToAzureBlobStorage(...)
    //        .ProtectKeysWithAzureKeyVault(...);
    //
    // 5. NO USAR PARA:
    //    - Contraseñas (usar Identity con hash)
    //    - Datos que no necesitan recuperarse (usar hash unidireccional)
    //    - Cifrado de archivos grandes (usar streaming encryption)
    //
    // 6. IDEAL PARA:
    //    - Datos personales (cédulas, teléfonos, direcciones)
    //    - Información médica
    //    - Números de tarjetas enmascarados
    //    - Tokens temporales
    //    - Cookies de autenticación (uso interno de ASP.NET Core)
    //
}
