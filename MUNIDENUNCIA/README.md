SISTEMA DE CONSULTA DE IMPUESTOS MUNICIPALES (MUNIDENUNCIA)
CREDENCIALES PARA PRUEBAS

USUARIOS DEL SISTEMA:
--------------------
Ciudadano:
  Usuario: juan.perez
  Contraseña: 12345
  Cédula asociada: 1-0234-0567
Funcionario:
  Usuario: admin
  Contraseña: admin
CÉDULAS PARA CONSULTAS:
-----------------------
1-0234-0567 - Juan Pérez Mora (Adeuda ₡45,000)
2-0345-0678 - María Rodríguez López (Al día)
3-0456-0789 - Carlos Jiménez Castro (Adeuda ₡125,000)

Migración y Creación de Base de Datos
Abra Package Manager Console y ejecute los comandos de Entity Framework.
Add-Migration InitialCreate
Update-Database

El comando Add-Migration analiza los modelos y el contexto, comparándolos con cualquier migración anterior, 
y genera código que creará o modificará el esquema de base de datos. 
Revisar el archivo de migración generado en la carpeta Migrations para que vean el código SQL que Entity Framework ejecutará.

El comando Update-Database aplica las migraciones pendientes a la base de datos, creando todas las tablas necesarias. 
Después de ejecutarlo exitosamente, abra SQL Server Object Explorer en Visual Studio o SQL Server Management Studio 
para ver las tablas creadas.
Revise las tablas de Identity generadas automáticamente como AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims 
y otras. 
Observe también la tabla AuditLogs personalizada con sus índices.
Ejecute una consulta SELECT en AspNetUsers para mostrar el usuario administrador creado automáticamente. 
Muestre el campo PasswordHash para demostrar que las contraseñas nunca se almacenan en texto plano.

Ejecución y Demostración
Ejecute la aplicación presionando F5 en Visual Studio. El navegador debe abrirse automáticamente con HTTPS habilitado, 
mostrando la página de inicio.

Navegue a la página de login e inicie sesión con las credenciales del administrador. 
Observe cómo el sistema acepta credenciales válidas y redirige al usuario autenticado. 
Abra las herramientas de desarrollo del navegador y navegue a la pestaña de Application o Storage. 
Localice las cookies y muestre la cookie de autenticación con sus atributos HttpOnly, Secure y SameSite claramente visibles.

Cierre sesión y demuestre el proceso de bloqueo de cuenta intentando iniciar sesión repetidamente con una 
contraseña incorrecta. 
Observe cómo el mensaje de error cambia cuando quedan pocos intentos. Después del quinto intento fallido, 
observe la página de Lockout.
Abra SQL Server Management Studio o el Object Explorer de Visual Studio y consulte la tabla AuditLogs para mostrar 
todos los eventos registrados durante las pruebas.

SELECT 
    EventType,
    Description,
    IpAddress,
    Success,
    Timestamp
FROM AuditLogs
ORDER BY Timestamp DESC

Observe los archivos de log generados por Serilog en el directorio logs del proyecto. 
Abra el archivo del día actual y vea cómo los eventos están registrados en formato estructurado.
Inicie sesión nuevamente como administrador, navegue a la página de registro y cree un nuevo funcionario. 
Observe el mensaje de confirmación y el log en la consola simulando el envío del email.

--------------------------------------
En conclusion, el sistema implementa múltiples capas de seguridad según el principio de defensa en profundidad. 
Las políticas de contraseñas robustas son la primera línea de defensa. 
El bloqueo automático de cuentas previene ataques de fuerza bruta. 
La configuración segura de cookies protege las sesiones activas. 
El sistema completo de auditoría permite detección de incidentes y cumplimiento normativo.

INSTRUCCIONES DE EJECUCIÓN:
---------------------------
1. Descomprimir el archivo ZIP
2. Abrir terminal en la carpeta MuniVul/MuniVul.Web
3. Ejecutar: 
3.1 dotnet restore
3.2 dotnet run
3.3 o Ejecutar desde Visual Studio
4. Abrir navegador en http://localhost:5000

1. Revisar las cabeceras devueltas por el servidor:
curl -I https://localhost:7077