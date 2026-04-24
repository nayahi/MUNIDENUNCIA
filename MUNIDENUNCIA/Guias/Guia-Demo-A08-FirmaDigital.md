# Guía de Demostración — A08: Firma Digital de Documentos

**Semana 3 · OWASP Top 10 A08:2021 — Software and Data Integrity Failures**
**Tiempo total: ~15 minutos**

---

## La idea que debe quedar grabada

> Firmar un documento no oculta su contenido — garantiza que **no fue modificado** desde que lo emitió el sistema. Es como el sello oficial en papel, pero matemáticamente imposible de falsificar.

---

## Paso 1 — La pregunta que abre el tema (2 min)

**"Un ciudadano descarga un reporte de sus denuncias. Tres días después dice que el reporte fue alterado y que originalmente decía otra cosa. ¿Cómo prueba la municipalidad que el documento que ella emitió es exactamente ese?"**

Sin firma digital: no puede. Con firma digital: ejecuta un comando de OpenSSL en 5 segundos y tiene la prueba.

---

## Paso 2 — Demo en browser: descargar el reporte firmado (3 min)

Loguear como `juan.perez / 12345` y navegar a `/Firma`.

1. Seleccionar tipo de reporte y hacer click en **"Descargar ZIP firmado"**
2. Abrir el ZIP descargado — mostrar los 3 archivos:
   - `reporte-denuncias.txt` — el documento en texto plano, **completamente legible**
   - `reporte-denuncias.sig` — la firma digital (256 bytes de datos binarios)
   - `LEEME.txt` — instrucciones de verificación

**Punto clave:** la firma NO cifra el documento. El `.txt` se puede abrir en el Bloc de notas. La firma solo prueba integridad y autenticidad.

---

## Paso 3 — Abrir el documento y modificarlo a mano (2 min)

Abrir `reporte-denuncias.txt` en cualquier editor de texto. Cambiar una sola letra — por ejemplo cambiar "Municipalidad" por "Municipalodad".

**Preguntar al aula:** "¿Esto detecta la firma?" — dejar que respondan antes de verificar.

---

## Paso 4 — Verificar con OpenSSL (3 min)

### Primero: descargar la clave pública

```bash
# Desde el browser: /Firma/ClavePublica → guarda munidenuncia-public-key.pem
# O con curl:
curl http://localhost:5013/Firma/ClavePublica -o munidenuncia-public-key.pem
```

### Verificar el documento original (debe pasar)

```bash
openssl dgst -sha256 -sigopt rsa_padding_mode:pss \
  -verify munidenuncia-public-key.pem \
  -signature reporte-denuncias.sig \
  reporte-denuncias.txt
```

```
Verified OK   ← el documento es auténtico
```

### Verificar el documento modificado (debe fallar)

```bash
# (después de cambiar "Municipalidad" por "Municipalodad")
openssl dgst -sha256 -sigopt rsa_padding_mode:pss \
  -verify munidenuncia-public-key.pem \
  -signature reporte-denuncias.sig \
  reporte-denuncias.txt
```

```
Verification Failure   ← modificación detectada
```

**Este es el momento más impactante de la demo.** Una sola letra cambiada → fallo total de verificación. No hay "verificación parcial".

---

## Paso 5 — Explicar qué ocurre internamente (2 min)

Dibujar en pizarrón o mostrar diagrama:

```
Al firmar:
  documento.txt  →  SHA-256  →  hash (32 bytes)
                                    ↓
                          cifrar con clave PRIVADA
                                    ↓
                              reporte.sig  (256 bytes)

Al verificar:
  documento.txt  →  SHA-256  →  hash_A
  reporte.sig    →  descifrar con clave PÚBLICA  →  hash_B

  hash_A == hash_B  →  Verified OK
  hash_A != hash_B  →  Verification Failure
```

**Por qué RSA-2048 + SHA-256 + PSS:**
- SHA-256 → resistente a colisiones (no se puede fabricar otro documento con el mismo hash)
- RSA-2048 → seguro hasta ~2030 según NIST
- PSS → padding más robusto que el anterior PKCS#1 v1.5

---

## Paso 6 — Mostrar el código (1 min)

Abrir `Services/FirmaDigitalService.cs` y señalar las 3 líneas que importan:

```csharp
private const int    TamanioClaveBits   = 2048;
private const string HashAlgorithm      = "SHA-256";
private const string Padding            = "PSS";

// Firmar:
return _privateKey.SignData(datos, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

// Verificar:
return _publicKey.VerifyData(datos, firma, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
```

---

## Paso 7 — Conexión A08↔A09 (1 min)

Señalar que el CSV de auditoría también se firma. Navegar a `/Auditoria` → "Exportar CSV firmado".

**"Si alguien con acceso al servidor modifica los logs para borrar su rastro, la firma del CSV lo evidencia."** La auditoría es inviolable porque está firmada con la misma clave.

---

## Resumen visual

```
Sin firma digital:              Con firma digital:
Ciudadano: "esto fue alterado"  Ciudadano: "esto fue alterado"
Municipio: "no fue así"         Municipio: ejecuta openssl → Verified OK
                                Fin del debate.
```

---

## Conexión Ley 8968

**Art. 10** — integridad de los datos: la firma digital es la implementación técnica del principio de integridad. Garantiza que los datos que salen del sistema son exactamente los que el sistema emitió — protege al ciudadano Y a la institución.

**No repudio:** la municipalidad no puede negar haber emitido un documento firmado con su clave privada. El ciudadano no puede alegar que el documento fue alterado si la firma verifica.

---

## Script de demo completo (sin browser)

Ver `Scripts/Demo-A08-FirmaDigital.ps1` — demuestra los 7 pasos del ciclo completo de firma en PowerShell, incluyendo rotación de claves y verificación con OpenSSL.

```powershell
pwsh -ExecutionPolicy Bypass -File Scripts/Demo-A08-FirmaDigital.ps1 -Auto
```

---

## Archivos relevantes

| Archivo | Qué muestra |
|---------|-------------|
| `Services/FirmaDigitalService.cs` | RSA-2048, generación de claves, firmar, verificar |
| `Controllers/FirmaController.cs` | Endpoints, whitelist ZIP path traversal, descarga |
| `Views/Firma/Index.cshtml` | Formulario de descarga y verificación en browser |
| `Scripts/Demo-A08-FirmaDigital.ps1` | Demo completa sin dependencias externas |
