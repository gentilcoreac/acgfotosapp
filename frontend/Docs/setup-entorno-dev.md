# Setup del entorno de desarrollo — Cliente

> Guía para levantar el front Angular desde cero en dev local. Credenciales de usuarios de prueba están en el drive del proyecto.

## Stack local

| Componente | Tecnología           | URL                      |
| ---------- | -------------------- | ------------------------ |
| Cliente    | Angular 20, Vite     | `http://localhost:4200`  |
| API        | .NET 10, IIS Express | `http://localhost:30000` |

---

## 1. Clonar y preparar

```
Repo: https://dev.azure.com/AcgFotosInterno/PowerBIEmbedded/_git/Cliente
Branch de trabajo: mainInformes
```

```powershell
npm install
```

---

## 2. Configuración del entorno

El archivo de entorno apunta a la API local. Sin modificación adicional:

```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:30000/api',
};
```

No hay variables de entorno de Azure en el front — el cliente nunca maneja credenciales de Power BI ni de Azure AD. Solo consume la API.

---

## 3. Arrancar

```powershell
npm start
```

Abre `http://localhost:4200`. La API debe estar corriendo en `http://localhost:30000` antes de intentar login.

---

## 4. Usuarios de prueba

| Usuario    | Rol           | Tenant |
| ---------- | ------------- | ------ |
| `mnanda`   | Admin         | 3      |
| `root`     | Root          | 1      |
| `cliente1` | Usuario final | 1      |

Passwords en el drive del proyecto.

---

## 5. Módulo Reportes — requisitos de DB

Los menús del módulo Reportes (IDs 10001–10006) no están en el seed.sql. Ver `Docs/setup-entorno-dev.md` en el repo API para el SQL de inserción.

Estados relevantes:

- `VerReporte` (ID 10005): `Estado=0` — es destino de navegación interna, no sección de menú.
- `MisReportes` (ID 10006): `Estado=1` — visible en sidebar del usuario final.

---

## 6. Verificar funcionamiento

1. Login con `mnanda` o `root`.
2. Sidebar muestra secciones de Reportes: Gestión de Carpetas, Gestión de Reportes, Mis Reportes.
3. Desde Mis Reportes, clic en un reporte → abre `/ver-reporte/:id` con el visor incrustado.
4. Si el visor redirige al menú, verificar que `gen_Menus` ID 10005 tenga `Estado=1` en DB.
