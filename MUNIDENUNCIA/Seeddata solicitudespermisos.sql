-- ===============================================================================
-- SCRIPT DE INICIALIZACIÓN DE DATOS DE PRUEBA
-- Sistema de Permisos de Construcción - Su Municipalidad  
-- Semana 3: Validación de Datos y Prevención de Inyecciones
-- ===============================================================================

USE [MUNIDENUNCIA];
GO

-- Verificar y limpiar datos existentes si es necesario
-- ADVERTENCIA: Este script borra datos existentes. Usar solo en ambiente de desarrollo/demostración

PRINT 'Iniciando carga de datos de prueba...'
GO

-- Limpiar tablas existentes (solo para demostración)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Comentarios')
BEGIN
    DELETE FROM Comentarios;
    PRINT 'Tabla Comentarios limpiada'
END
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'SolicitudesPermisos')
BEGIN
    DELETE FROM SolicitudesPermisos;
    PRINT 'Tabla SolicitudesPermisos limpiada'
END
GO

-- ===============================================================================
-- INSERCIÓN DE SOLICITUDES DE PERMISOS DE PRUEBA
-- ===============================================================================

PRINT 'Insertando solicitudes de permisos...'
GO

-- Solicitud 1: Vivienda unifamiliar en  (Pendiente)
INSERT INTO SolicitudesPermisos 
(CedulaPropietario, NombreCompletoPropietario, EmailPropietario, TelefonoPropietario,
 Distrito, DireccionCompleta, PlanoCatastrado,
 TipoConstruccion, AreaConstruccionM2, NumeroPlantas, DescripcionProyecto, PresupuestoEstimado,
 Estado, FechaSolicitud)
VALUES
('304560789', 'María Fernández Solís', 'maria.fernandez@email.com', '88776655',
 '', 'Del parque central de , 200 metros oeste y 100 metros sur', 'H-1-234567-2024',
 'ViviendaUnifamiliar', 120.50, 2, 
 'Construcción de vivienda unifamiliar de dos plantas con área social en primera planta y dormitorios en segunda planta. Incluye cochera para dos vehículos y área de jardín.',
 35000000,
 'Pendiente', DATEADD(day, -5, GETUTCDATE()));

-- Solicitud 2: Ampliación de vivienda en San Francisco (En Revisión)
INSERT INTO SolicitudesPermisos 
(CedulaPropietario, NombreCompletoPropietario, EmailPropietario, TelefonoPropietario,
 Distrito, DireccionCompleta, PlanoCatastrado,
 TipoConstruccion, AreaConstruccionM2, NumeroPlantas, DescripcionProyecto, PresupuestoEstimado,
 Estado, FechaSolicitud, FechaRevision)
VALUES
('105780234', 'Carlos Alberto Mora Vargas', 'c.mora@email.com', '87654321',
 'San Francisco', 'Contiguo a la escuela de San Francisco, casa color verde', 'H-1-345678-2024',
 'AmpliacionVivienda', 45.00, 1,
 'Ampliación de vivienda existente para agregar una habitación adicional y baño completo en primer nivel.',
 8500000,
 'EnRevision', DATEADD(day, -10, GETUTCDATE()), DATEADD(day, -2, GETUTCDATE()));

-- Solicitud 3: Local comercial en Mercedes (Aprobada)
INSERT INTO SolicitudesPermisos 
(CedulaPropietario, NombreCompletoPropietario, EmailPropietario, TelefonoPropietario,
 Distrito, DireccionCompleta, PlanoCatastrado,
 TipoConstruccion, AreaConstruccionM2, NumeroPlantas, DescripcionProyecto, PresupuestoEstimado,
 Estado, FechaSolicitud, FechaRevision, FechaAprobacion, RevisadoPor)
VALUES
('3101234567', 'Comercial  Sociedad Anónima', 'contacto@comercial.com', '22345678',
 'Mercedes', 'Avenida Central, Mercedes, frente al Banco Nacional', 'H-1-456789-2024',
 'LocalComercial', 200.00, 1,
 'Construcción de local comercial de una planta para tienda de conveniencia. Incluye área de ventas, bodega, baños para clientes y empleados, y área de parqueo.',
 45000000,
 'Aprobada', DATEADD(day, -30, GETUTCDATE()), DATEADD(day, -15, GETUTCDATE()), DATEADD(day, -15, GETUTCDATE()),
 'Ing. Roberto Jiménez Castro');

-- Solicitud 4: Apartamentos múltiples en Ulloa (Requiere Correcciones)
INSERT INTO SolicitudesPermisos 
(CedulaPropietario, NombreCompletoPropietario, EmailPropietario, TelefonoPropietario,
 Distrito, DireccionCompleta, PlanoCatastrado,
 TipoConstruccion, AreaConstruccionM2, NumeroPlantas, DescripcionProyecto, PresupuestoEstimado,
 Estado, FechaSolicitud, FechaRevision)
VALUES
('206890345', 'José Miguel Ramírez Quesada', 'jm.ramirez@email.com', '89012345',
 'Ulloa', 'Ulloa centro, 300 metros norte del cementerio', 'H-1-567890-2024',
 'ApartamentosMultiples', 380.00, 3,
 'Edificio de apartamentos de tres plantas con cuatro unidades por planta, para un total de 12 apartamentos. Cada unidad de 85 metros cuadrados con dos habitaciones.',
 125000000,
 'RequiereCorrecciones', DATEADD(day, -20, GETUTCDATE()), DATEADD(day, -8, GETUTCDATE()));

-- Solicitud 5: Remodelación estructural en  (Pendiente)
INSERT INTO SolicitudesPermisos 
(CedulaPropietario, NombreCompletoPropietario, EmailPropietario, TelefonoPropietario,
 Distrito, DireccionCompleta, PlanoCatastrado,
 TipoConstruccion, AreaConstruccionM2, NumeroPlantas, DescripcionProyecto, PresupuestoEstimado,
 Estado, FechaSolicitud)
VALUES
('403120987', 'Ana Lucía Vargas Rojas', 'ana.vargas@email.com', '86543210',
 '', 'Barrio Los Ángeles, casa #245', 'H-1-678901-2024',
 'RemodelacionEstructural', 75.00, 1,
 'Remodelación estructural que incluye demolición de pared interna para unir cocina y sala, reforzamiento de vigas, y actualización de instalaciones eléctricas.',
 12000000,
 'Pendiente', DATEADD(day, -3, GETUTCDATE()));

-- Solicitud 6: Oficinas en San Francisco (En Revisión)
INSERT INTO SolicitudesPermisos 
(CedulaPropietario, NombreCompletoPropietario, EmailPropietario, TelefonoPropietario,
 Distrito, DireccionCompleta, PlanoCatastrado,
 TipoConstruccion, AreaConstruccionM2, NumeroPlantas, DescripcionProyecto, PresupuestoEstimado,
 Estado, FechaSolicitud, FechaRevision)
VALUES
('3102345678', 'Inversiones del Norte S.A.', 'info@inversionesdelnorte.com', '22456789',
 'San Francisco', 'San Francisco, diagonal a Plaza Mayor', 'H-1-789012-2024',
 'Oficinas', 320.00, 2,
 'Edificio de oficinas de dos plantas con espacios para arrendamiento. Primera planta con 6 oficinas de 25m2 cada una, segunda planta con 4 oficinas de 35m2. Incluye ascensor, estacionamiento y áreas comunes.',
 85000000,
 'EnRevision', DATEADD(day, -12, GETUTCDATE()), DATEADD(day, -4, GETUTCDATE()));

-- Solicitud 7: Vivienda unifamiliar en Mercedes (Denegada)
INSERT INTO SolicitudesPermisos 
(CedulaPropietario, NombreCompletoPropietario, EmailPropietario, TelefonoPropietario,
 Distrito, DireccionCompleta, PlanoCatastrado,
 TipoConstruccion, AreaConstruccionM2, NumeroPlantas, DescripcionProyecto, PresupuestoEstimado,
 Estado, FechaSolicitud, FechaRevision, RevisadoPor)
VALUES
('507230456', 'Pedro Jiménez Murillo', 'p.jimenez@email.com', '85432109',
 'Mercedes', 'Mercedes Norte, lote baldío frente a la iglesia', 'H-1-890123-2024',
 'ViviendaUnifamiliar', 150.00, 2,
 'Construcción de vivienda de dos plantas en lote ubicado en zona de protección de río identificada en Plan Regulador.',
 42000000,
 'Denegada', DATEADD(day, -25, GETUTCDATE()), DATEADD(day, -18, GETUTCDATE()),
 'Arq. Sandra Mora Calderón');

-- Solicitud 8: Ampliación de vivienda en Ulloa (Aprobada)
INSERT INTO SolicitudesPermisos 
(CedulaPropietario, NombreCompletoPropietario, EmailPropietario, TelefonoPropietario,
 Distrito, DireccionCompleta, PlanoCatastrado,
 TipoConstruccion, AreaConstruccionM2, NumeroPlantas, DescripcionProyecto, PresupuestoEstimado,
 Estado, FechaSolicitud, FechaRevision, FechaAprobacion, RevisadoPor)
VALUES
('308560123', 'Gabriela Soto Chacón', 'gsoto@email.com', '88990011',
 'Ulloa', 'Ulloa, residencial Las Flores, casa 23', 'H-1-901234-2024',
 'AmpliacionVivienda', 38.00, 1,
 'Ampliación en planta baja para oficina de trabajo desde casa (home office) con baño completo.',
 7500000,
 'Aprobada', DATEADD(day, -35, GETUTCDATE()), DATEADD(day, -22, GETUTCDATE()), DATEADD(day, -22, GETUTCDATE()),
 'Ing. Roberto Jiménez Castro');

-- Solicitud 9: Local comercial en  (Pendiente)
INSERT INTO SolicitudesPermisos 
(CedulaPropietario, NombreCompletoPropietario, EmailPropietario, TelefonoPropietario,
 Distrito, DireccionCompleta, PlanoCatastrado,
 TipoConstruccion, AreaConstruccionM2, NumeroPlantas, DescripcionProyecto, PresupuestoEstimado,
 Estado, FechaSolicitud)
VALUES
('109670234', 'Laura Méndez Araya', 'lmendez@email.com', '87778888',
 '', 'Centro de , avenida 6, local en primer piso', 'H-1-012345-2025',
 'LocalComercial', 85.00, 1,
 'Remodelación de local existente para cafetería gourmet. Incluye instalación de cocina comercial, área de exhibición de productos, baño público y área de mesas.',
 15000000,
 'Pendiente', DATEADD(day, -1, GETUTCDATE()));

-- Solicitud 10: Industria liviana en San Francisco (En Revisión)
INSERT INTO SolicitudesPermisos 
(CedulaPropietario, NombreCompletoPropietario, EmailPropietario, TelefonoPropietario,
 Distrito, DireccionCompleta, PlanoCatastrado,
 TipoConstruccion, AreaConstruccionM2, NumeroPlantas, DescripcionProyecto, PresupuestoEstimado,
 Estado, FechaSolicitud, FechaRevision)
VALUES
('3103456789', 'Manufacturas  Limitada', 'gerencia@manufac.com', '22567890',
 'San Francisco', 'Zona industrial San Francisco, bodega 15', 'H-1-123456-2025',
 'IndustriaLiviana', 450.00, 1,
 'Construcción de nave industrial para manufactura de productos plásticos. Incluye área de producción, oficinas administrativas, bodega de materia prima, bodega de producto terminado, y área de carga y descarga.',
 95000000,
 'EnRevision', DATEADD(day, -7, GETUTCDATE()), DATEADD(day, -1, GETUTCDATE()));

PRINT 'Solicitudes insertadas exitosamente'
GO

-- ===============================================================================
-- INSERCIÓN DE COMENTARIOS DE FUNCIONARIOS
-- ===============================================================================

PRINT 'Insertando comentarios de funcionarios...'
GO

-- Comentarios para solicitud #2 (En Revisión)
INSERT INTO Comentarios (SolicitudPermisoId, NombreFuncionario, CargoFuncionario, TextoComentario, FechaComentario, EsAprobacion, EsRechazo)
VALUES 
(2, 'Ing. Roberto Jiménez Castro', 'Ingeniero Civil', 
 'Se requiere presentar planos estructurales firmados por ingeniero responsable. Los planos arquitectónicos están correctos.',
 DATEADD(day, -2, GETUTCDATE()), 0, 0);

-- Comentarios para solicitud #3 (Aprobada)
INSERT INTO Comentarios (SolicitudPermisoId, NombreFuncionario, CargoFuncionario, TextoComentario, FechaComentario, EsAprobacion, EsRechazo)
VALUES 
(3, 'Arq. Sandra Mora Calderón', 'Arquitecta Municipal', 
 'Planos revisados y cumplen con todos los requisitos del Plan Regulador. Uso de suelo correcto para zona comercial.',
 DATEADD(day, -20, GETUTCDATE()), 0, 0);

INSERT INTO Comentarios (SolicitudPermisoId, NombreFuncionario, CargoFuncionario, TextoComentario, FechaComentario, EsAprobacion, EsRechazo)
VALUES 
(3, 'Ing. Roberto Jiménez Castro', 'Ingeniero Civil', 
 'Diseño estructural revisado y aprobado. Memoria de cálculo cumple con Código Sísmico de Costa Rica vigente. Se autoriza inicio de construcción.',
 DATEADD(day, -15, GETUTCDATE()), 1, 0);

-- Comentarios para solicitud #4 (Requiere Correcciones)
INSERT INTO Comentarios (SolicitudPermisoId, NombreFuncionario, CargoFuncionario, TextoComentario, FechaComentario, EsAprobacion, EsRechazo)
VALUES 
(4, 'Arq. Sandra Mora Calderón', 'Arquitecta Municipal', 
 'El diseño arquitectónico debe ajustarse para cumplir con retiros mínimos establecidos en artículo 23 del Reglamento de Construcciones. Se requiere modificar fachada norte reduciendo 1.5 metros.',
 DATEADD(day, -8, GETUTCDATE()), 0, 0);

INSERT INTO Comentarios (SolicitudPermisoId, NombreFuncionario, CargoFuncionario, TextoComentario, FechaComentario, EsAprobacion, EsRechazo)
VALUES 
(4, 'Ing. Roberto Jiménez Castro', 'Ingeniero Civil', 
 'Adicionalmente, se debe presentar estudio de impacto vial debido al número de unidades habitacionales. Coordinar con Departamento de Obras Públicas.',
 DATEADD(day, -8, GETUTCDATE()), 0, 0);

-- Comentarios para solicitud #6 (En Revisión)
INSERT INTO Comentarios (SolicitudPermisoId, NombreFuncionario, CargoFuncionario, TextoComentario, FechaComentario, EsAprobacion, EsRechazo)
VALUES 
(6, 'Ing. Roberto Jiménez Castro', 'Ingeniero Civil', 
 'Planos estructurales en revisión. Se solicita aclaración sobre cimentación propuesta dado el tipo de suelo identificado en estudio geotécnico.',
 DATEADD(day, -4, GETUTCDATE()), 0, 0);

-- Comentarios para solicitud #7 (Denegada)
INSERT INTO Comentarios (SolicitudPermisoId, NombreFuncionario, CargoFuncionario, TextoComentario, FechaComentario, EsAprobacion, EsRechazo)
VALUES 
(7, 'Arq. Sandra Mora Calderón', 'Arquitecta Municipal', 
 'El lote propuesto se encuentra en Zona de Protección de Río según Plan Regulador vigente, artículo 45. No se permite construcción de viviendas en esta zona. Solicitud DENEGADA.',
 DATEADD(day, -18, GETUTCDATE()), 0, 1);

-- Comentarios para solicitud #8 (Aprobada)
INSERT INTO Comentarios (SolicitudPermisoId, NombreFuncionario, CargoFuncionario, TextoComentario, FechaComentario, EsAprobacion, EsRechazo)
VALUES 
(8, 'Ing. Roberto Jiménez Castro', 'Ingeniero Civil', 
 'Ampliación cumple con todos los requisitos técnicos y normativos. Se autoriza construcción. Permiso válido por 6 meses.',
 DATEADD(day, -22, GETUTCDATE()), 1, 0);

-- Comentarios para solicitud #10 (En Revisión)
INSERT INTO Comentarios (SolicitudPermisoId, NombreFuncionario, CargoFuncionario, TextoComentario, FechaComentario, EsAprobacion, EsRechazo)
VALUES 
(10, 'Lic. Fernando Solís Brenes', 'Gestor Ambiental', 
 'Se requiere presentar Declaración Jurada de Compromisos Ambientales (D1) ante SETENA. Una vez aprobada, reenviar para continuar proceso de permiso municipal.',
 DATEADD(day, -1, GETUTCDATE()), 0, 0);

PRINT 'Comentarios insertados exitosamente'
GO

-- ===============================================================================
-- VERIFICACIÓN DE DATOS CARGADOS
-- ===============================================================================

PRINT ''
PRINT 'Verificando datos insertados...'
PRINT ''

SELECT COUNT(*) AS 'Total Solicitudes' FROM SolicitudesPermisos;
SELECT Estado, COUNT(*) AS Cantidad FROM SolicitudesPermisos GROUP BY Estado ORDER BY Estado;
SELECT * FROM SolicitudesPermisos;

PRINT ''
SELECT COUNT(*) AS 'Total Comentarios' FROM Comentarios;
SELECT * FROM Comentarios;

PRINT ''
PRINT '==============================================================================='
PRINT 'Carga de datos de prueba completada exitosamente'
PRINT '==============================================================================='
GO