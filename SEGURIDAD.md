# Controles de permisos

## Bypass en entorno de desarrollo

Para habilitar temporalmente el acceso sin validar permisos en entornos de desarrollo, utiliza la clave de configuración:

```json
"Seguridad": {
  "OmitirPermisosEnDev": true
}
```

- **Valor por defecto:** `false` en `appsettings.json` y `appsettings.Development.json`.
- **Ámbito:** Solo tiene efecto cuando `ASPNETCORE_ENVIRONMENT` es `Development`.
- **Advertencia:** Mantenerlo en `false` en servidores productivos evita que un entorno mal configurado quede sin control de permisos.
