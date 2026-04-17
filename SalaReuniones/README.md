# Sistema de Gestión de Salas de Reuniones

Aplicación web desarrollada en **ASP.NET Core MVC (.NET 8)** para la gestión de reservas de salas dentro de una organización.

El sistema permite administrar **usuarios, salas y reservas**, con control de acceso por roles, visualización en calendario, generación automática de **archivos ICS para calendarios** y generación de **reportes administrativos con métricas y exportación a PDF**.

Además, implementa un sistema de **estados dinámicos de reserva**, permitiendo identificar si una reunión está **Agendada, En proceso, Finalizada o Cancelada**, calculado automáticamente según la hora actual.

---

# Arquitectura del Proyecto

El sistema sigue el patrón **MVC (Modelo - Vista - Controlador)**.

## Modelos (Models)

Representan la estructura de la base de datos y la lógica de datos mediante **Entity Framework Core**.

## Vistas (Views)

Interfaz de usuario desarrollada con:

* Razor
* HTML
* Bootstrap
* JavaScript
* Bootstrap Icons

## Controladores (Controllers)

Gestionan:

* lógica de negocio
* validaciones
* comunicación con la base de datos
* endpoints de API para frontend dinámico

---

# Tecnologías Utilizadas

## Backend

* ASP.NET Core MVC (.NET 8)
* Entity Framework Core
* ASP.NET Identity
* SQL Server

## Frontend

* Razor Views
* Bootstrap
* JavaScript
* FullCalendar
* Chart.js
* Bootstrap Icons

## Reportes

* QuestPDF

## Integración de Calendarios

Generación automática de archivos **ICS** compatibles con:

* Google Calendar
* Outlook
* Apple Calendar

---

# Base de Datos: SalaReunionesDB

La base de datos está implementada en **SQL Server** y gestionada mediante **Entity Framework Core**.

---

# Tablas Principales del Sistema

---

# Tabla: Salas

Contiene la información de las salas disponibles para reservas.

| Campo    | Tipo          | Descripción                      |
| -------- | ------------- | -------------------------------- |
| Id       | int (PK)      | Identificador único              |
| Nombre   | nvarchar(100) | Nombre de la sala                |
| ColorHex | nvarchar(7)   | Color utilizado en el calendario |

Salas incluidas en el sistema:

* **Sala Principal → #94A5A4**
* **Sala Secundaria → #3788d8**

El color permite distinguir visualmente cada sala en el calendario.

---

# Tabla: Reservas

Tabla central del sistema donde se almacenan todas las reservas.

| Campo      | Tipo          | Descripción                 |
| ---------- | ------------- | --------------------------- |
| Id         | int (PK)      | Identificador de la reserva |
| Fecha      | datetime2     | Día de la reserva           |
| HoraInicio | time          | Hora de inicio              |
| HoraFin    | time          | Hora de finalización        |
| Motivo     | nvarchar(250) | Motivo de la reunión        |
| Estado     | int           | Estado de la reserva        |
| SalaId     | int (FK)      | Sala reservada              |
| UsuarioId  | nvarchar(450) | Usuario que creó la reserva |

---

# Campo Motivo Opcional

El campo **Motivo** es opcional y permite crear reservas sin especificar motivo.

Configuración en el modelo:

```csharp
[MaxLength(250)]
public string? Motivo { get; set; }
```

Configuración en SQL Server:

```sql
ALTER TABLE Reservas
ALTER COLUMN Motivo NVARCHAR(250) NULL;
```

---

# Estados de Reserva

El campo **Estado** se almacena en base de datos mediante un Enum:

| Valor | Estado    |
| ----- | --------- |
| 0     | Activa    |
| 1     | Cancelada |

Adicionalmente, el sistema calcula **estados dinámicos según la hora actual**.

| Estado     | Descripción              | Color      |
| ---------- | ------------------------ | ---------- |
| Agendada   | La reunión aún no inicia | 🟢 Verde   |
| En proceso | La reunión ya inició     | 🟠 Naranja |
| Finalizada | La reunión terminó       | ⚫ Negro    |
| Cancelada  | Cancelada manualmente    | ⚪ Gris     |

Estos estados **no se almacenan en la base de datos**, sino que se calculan dinámicamente.

---

# Relaciones de Base de Datos

Relaciones principales:

* Una **Sala** puede tener muchas **Reservas**
* Un **Usuario** puede crear muchas **Reservas**
* Cada **Reserva** pertenece a una sola **Sala**
* Cada **Reserva** pertenece a un solo **Usuario**

---

# Sistema de Autenticación

El sistema utiliza **ASP.NET Identity**.

Tablas utilizadas:

* AspNetUsers
* AspNetRoles
* AspNetUserRoles
* AspNetUserClaims
* AspNetRoleClaims
* AspNetUserLogins
* AspNetUserTokens

---

# Roles del Sistema

| Rol           | Permisos                       |
| ------------- | ------------------------------ |
| Administrador | Control total del sistema      |
| Usuario       | Crear y gestionar sus reservas |
| Visualizador  | Solo visualizar reservas       |

---

# Funcionalidades del Sistema

---

# Gestión de Usuarios

Administración de usuarios mediante **ASP.NET Identity**.

Funciones disponibles:

* Crear usuarios
* Editar usuarios
* Asignar roles
* Buscar usuarios por nombre
* Filtrar usuarios por rol
* Paginación de usuarios
* Activar usuarios
* Inhabilitar usuarios (soft delete)
* Prevención de usuarios duplicados

El listado de usuarios incluye:

* **búsqueda por username**
* **filtro por rol**
* **ordenamiento automático por rol y nombre**
* **paginación para mejorar el rendimiento**

Acceso exclusivo para **Administrador**.

---

# Gestión de Salas

Permite administrar las salas disponibles.

Funciones:

* Crear salas
* Editar salas
* Definir color en calendario

---

# Gestión de Reservas

Funciones disponibles:

* Crear reservas
* Editar reservas
* Cancelar reservas
* Ver detalles
* Visualizar estado dinámico

---

# Validaciones del Sistema

El sistema incluye validaciones en **backend y frontend**.

Principales validaciones:

* No permitir reservas en el pasado
* La hora de fin debe ser mayor que la hora de inicio
* No permitir reservas superpuestas en la misma sala
* No permitir editar reservas finalizadas
* No permitir nombres de usuario duplicados
* Compatibilidad con **formatos de hora 12h (AM/PM) y 24h**

Las horas se almacenan en la base de datos usando el tipo:

```
time
```

Esto garantiza compatibilidad entre diferentes configuraciones regionales de los equipos.

---

# Cancelación de Reservas

El sistema implementa **cancelación lógica (soft cancel)**.

Cuando una reserva se cancela:

* se mantiene el registro en la base de datos
* el estado cambia a **Cancelada**
* no aparece en el calendario activo

Esto permite mantener **historial y auditoría**.

---

# Calendario de Reservas

El sistema incluye un calendario interactivo desarrollado con **FullCalendar**.

Funciones:

* vista semanal
* vista mensual
* colores por sala
* detalle de reserva al hacer clic

Endpoint utilizado:

```
/Reservas/GetReservas
```

---

# Integración con Calendarios (ICS)

Al crear una reserva el sistema genera un archivo **.ics** compatible con:

* Google Calendar
* Outlook
* Apple Calendar

Esto permite que el usuario agregue la reunión directamente a su calendario personal.

---

# Dashboard Administrativo

El sistema incluye un **panel administrativo con métricas y estadísticas**.

Indicadores:

* Total de reservas
* Reservas activas
* Reservas del día
* Total de usuarios

Gráficas generadas con **Chart.js**:

* reservas por sala
* reservas por día

---

# Reportes Administrativos

El sistema permite exportar reportes en **PDF profesional**.

Contenido del reporte:

* encabezado institucional
* métricas del sistema
* listado detallado de reservas

Tecnología utilizada:

**QuestPDF**

---

# Gestión de Sesiones

El sistema incluye **control de sesiones por inactividad**.

Características:

* cierre automático de sesión tras periodo de inactividad
* protección contra sesiones abiertas en equipos compartidos

---

# Ejecución del Proyecto

---

# Ejecutar en Visual Studio

1. Abrir la solución
2. Configurar la cadena de conexión en `appsettings.json`
3. Ejecutar migraciones
4. Presionar **F5**

---

# Despliegue en Servidor (IIS)

El sistema puede desplegarse en **Windows Server con IIS**.

---

# 1 Instalar IIS

```
Administrador del Servidor
→ Agregar roles y características
→ Servidor Web (IIS)
```

---

# 2 Instalar .NET Hosting Bundle

Descargar:

```
dotnet-hosting-8.x-win
```

Luego ejecutar:

```
iisreset
```

---

# 3 Publicar el Proyecto

En Visual Studio:

```
Click derecho proyecto
→ Publish
→ Folder
```

Ejemplo:

```
C:\Publicaciones\SalaReuniones
```

---

# 4 Copiar al Servidor

Copiar a:

```
C:\inetpub\SalaReuniones
```

---

# 5 Crear Application Pool

```
Application Pools
→ Add Application Pool
```

Configuración:

```
Nombre: SalaReunionesPool
.NET CLR Version: No Managed Code
Pipeline Mode: Integrated
```

---

# 6 Crear Sitio Web

```
Sites
→ Add Website
```

Configuración:

```
Site Name: SalaReuniones
Physical Path: C:\inetpub\SalaReuniones
Port: 80 o 8080
Application Pool: SalaReunionesPool
```

---

# 7 Permisos de Carpeta

Asignar permisos a:

```
IIS_IUSRS
```

con lectura y ejecución.

---

# 8 Configurar Base de Datos

Editar:

```
appsettings.json
```

Ejemplo:

```
Server=SERVIDORSQL;
Database=SalaReunionesDB;
Trusted_Connection=True;
TrustServerCertificate=True;
```

---

# Acceso al Sistema

Una vez publicado:

```
http://IP_DEL_SERVIDOR
```

---

# Seguridad

El sistema implementa:

* `[Authorize]`
* control de acceso por roles
* protección CSRF
* validaciones backend

---

# Salas Iniciales

El sistema incluye dos salas:

* Sala Principal
* Sala Secundaria

---

# Conclusión

Este sistema permite gestionar reservas de salas de forma **segura, organizada y eficiente**, evitando conflictos de horario y manteniendo historial completo.

Incluye herramientas modernas como:

* calendario interactivo
* dashboard administrativo
* exportación de reportes
* integración con calendarios
* administración avanzada de usuarios

lo que permite su uso en **entornos corporativos o institucionales**.
