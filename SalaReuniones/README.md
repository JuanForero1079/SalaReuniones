# Sistema de Gestión de Salas de Reuniones

Aplicación web desarrollada en **ASP.NET Core MVC** para la gestión de reservas de salas dentro de una organización.

Permite administrar usuarios, salas y reservas, con control de acceso por roles y visualización en calendario.

---

# Arquitectura del Proyecto

El sistema sigue el patrón **MVC (Modelo - Vista - Controlador)**:

* **Modelos (Models)** → Representan la estructura de la base de datos
* **Vistas (Views)** → Interfaz de usuario (Razor + HTML + JS)
* **Controladores (Controllers)** → Lógica del sistema

---

# Base de Datos: SalaReunionesDB

La base de datos está diseñada en **SQL Server** y gestionada con **Entity Framework Core**.

---

## TABLAS PRINCIPALES DEL SISTEMA

---

## Tabla: Salas

Contiene la información de las salas disponibles.

| Campo    | Tipo          | Descripción                                 |
| -------- | ------------- | ------------------------------------------- |
| Id       | int (PK)      | Identificador único de la sala              |
| Nombre   | nvarchar(100) | Nombre de la sala                           |
| ColorHex | nvarchar(7)   | Color en formato HEX usado en el calendario |

 Ejemplo:

* Sala Principal → `#94A5A4`
* Sala Secundaria → `#3788d8`

---

##  Tabla: Reservas

Es la tabla principal del sistema.

| Campo      | Tipo          | Descripción                       |
| ---------- | ------------- | --------------------------------- |
| Id         | int (PK)      | Identificador de la reserva       |
| Fecha      | datetime2     | Día de la reserva                 |
| HoraInicio | time          | Hora de inicio                    |
| HoraFin    | time          | Hora de finalización              |
| Motivo     | nvarchar(250) | Motivo de la reunión              |
| Estado     | int           | Estado de la reserva (0 = Activa) |
| SalaId     | int (FK)      | Relación con la sala              |
| UsuarioId  | nvarchar(450) | Usuario que creó la reserva       |

---

###  Relaciones de Reservas

* `SalaId` → referencia a **Salas.Id**
* `UsuarioId` → referencia a **AspNetUsers.Id**

Esto significa:

 Una reserva pertenece a:

* **1 sala**
* **1 usuario**

---

## TABLAS DE AUTENTICACIÓN (ASP.NET Identity)

El sistema usa Identity para login y roles.

---

## Tabla: AspNetUsers

Usuarios del sistema.

| Campo          | Descripción               |
| -------------- | ------------------------- |
| Id             | Identificador del usuario |
| UserName       | Nombre de usuario         |
| Email          | Correo electrónico        |
| PasswordHash   | Contraseña encriptada     |
| EmailConfirmed | Confirmación de email     |
| LockoutEnabled | Control de bloqueo        |

---

## Tabla: AspNetRoles

Roles del sistema:

| Rol           |
| ------------- |
| Administrador |
| Usuario       |
| Visualizador  |

---

## Tabla: AspNetUserRoles

Relaciona usuarios con roles.

 Permite que:

* Un usuario tenga uno o varios roles

---

## Otras tablas de Identity

* AspNetUserClaims → Permisos adicionales
* AspNetRoleClaims → Claims por rol
* AspNetUserLogins → Logins externos
* AspNetUserTokens → Tokens de autenticación

---

## Tabla: __EFMigrationsHistory

Control interno de Entity Framework.

Guarda todas las migraciones ejecutadas.

---

# RELACIONES CLAVE

* Usuario → muchas reservas
* Sala → muchas reservas
* Reserva → pertenece a una sala y a un usuario

---

# FUNCIONALIDADES DEL SISTEMA

---

## Usuarios

* Login con ASP.NET Identity
* Roles:

  * Administrador
  * Usuario
  * Visualizador

---

## Salas

* Creación de salas
* Color personalizado para calendario

---

## Reservas

* Crear reservas
* Editar reservas
* Eliminar reservas (solo administrador)
* Validaciones:

  * No permitir horarios inválidos
  * No permitir solapamientos

---

## Calendario (FullCalendar)

* Visualización dinámica
* Colores por sala
* Filtros por sala
* Consumo desde endpoint JSON:

```
/Reservas/GetReservas
```

---

# FRONTEND

Tecnologías utilizadas:

* Razor Pages
* Bootstrap
* JavaScript
* FullCalendar

---

## Validaciones en el frontend

* No permite fechas pasadas
* No permite horas pasadas (si es el día actual)
* Validación antes de enviar formulario

---

# EJECUCIÓN DEL PROYECTO

---

## Ejecutar en Visual Studio

1. Abrir la solución
2. Configurar cadena de conexión en `appsettings.json`
3. Ejecutar migraciones (si aplica)
4. Presionar **F5**

---

## Publicar en IIS (Servidor)

### 1. Publicar desde Visual Studio

* Click derecho proyecto → **Publish**
* Elegir carpeta

---

### 2. Configurar IIS

* Crear sitio web
* Ruta física → carpeta publicada
* Puerto → 80 o personalizado

---

### 3. Configurar conexión a base de datos

Actualizar en:

```
appsettings.json
```

Ejemplo:

```
Server=SERVIDOR;Database=SalaReunionesDB;Trusted_Connection=True;
```

---

### 4. Permisos

* Dar permisos a:

```
IIS_IUSRS
```

---

# SEGURIDAD

* Uso de `[Authorize]`
* Control por roles
* Validaciones backend
* Protección CSRF (`ValidateAntiForgeryToken`)

---

# DATOS INICIALES

El sistema incluye:

### Usuarios:

* [admin@empresa.com](mailto:admin@empresa.com) → Administrador
* [usuario@empresa.com](mailto:usuario@empresa.com) → Usuario
* [visor@empresa.com](mailto:visor@empresa.com) → Visualizador

### Salas:

* Sala Principal
* Sala Secundaria

---

# CONCLUSIÓN

Este sistema permite gestionar reservas de salas de forma segura, evitando conflictos de horario y controlando accesos por roles, con una interfaz clara y un calendario interactivo.

---
