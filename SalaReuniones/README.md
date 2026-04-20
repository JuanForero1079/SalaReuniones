# Sistema de Gestión de Salas de Reuniones

Aplicación web desarrollada en **ASP.NET Core MVC (.NET 8)** para la gestión de reservas de salas dentro de una organización.

El sistema permite administrar **usuarios, salas y reservas**, con control de acceso por roles, visualización en calendario, generación automática de **archivos ICS** para integración con calendarios externos y generación de **reportes administrativos en PDF**.

Además, implementa un sistema de **estados dinámicos de reserva**, permitiendo identificar si una reunión está **Agendada, En proceso, Finalizada o Cancelada**, calculado automáticamente según la hora actual.

---

# Características Principales

* Gestión completa de **usuarios y roles**
* Administración de **salas de reuniones**
* Creación y gestión de **reservas**
* **Calendario interactivo**
* Estados dinámicos de reuniones
* **Exportación a calendario (.ICS)**
* **Dashboard administrativo con métricas**
* **Reportes PDF**
* Control de **sesiones por inactividad**
* Validaciones avanzadas para evitar conflictos de horario
* **Cancelación lógica de reservas**
* Validación de **integridad de usuario antes de guardar reservas**

---

# Arquitectura del Proyecto

El sistema sigue el patrón **MVC (Model – View – Controller)**.

## Models

Representan la estructura de datos y la interacción con la base de datos mediante **Entity Framework Core**.

## Views

Interfaz de usuario desarrollada con:

* Razor
* HTML
* Bootstrap
* JavaScript
* Bootstrap Icons

## Controllers

Encargados de manejar:

* lógica de negocio
* validaciones
* acceso a datos
* endpoints para interacción dinámica con el frontend
* generación de archivos ICS

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

## Generación de Reportes

* QuestPDF

## Integración con Calendarios

Generación automática de archivos **ICS** compatibles con:

* Google Calendar
* Outlook
* Apple Calendar

---

# Base de Datos

Nombre de la base de datos:

```
SalaReunionesDB
```

Implementada en **SQL Server** y gestionada mediante **Entity Framework Core**.

---

# Tablas Principales

## Tabla: Salas

Contiene la información de las salas disponibles.

| Campo    | Tipo          | Descripción                  |
| -------- | ------------- | ---------------------------- |
| Id       | int (PK)      | Identificador único          |
| Nombre   | nvarchar(100) | Nombre de la sala            |
| ColorHex | nvarchar(7)   | Color usado en el calendario |

Salas incluidas por defecto:

* **Sala Principal → #94A5A4**
* **Sala Secundaria → #3788d8**

---

## Tabla: Reservas

Tabla central del sistema.

| Campo      | Tipo          | Descripción                 |
| ---------- | ------------- | --------------------------- |
| Id         | int (PK)      | Identificador de la reserva |
| Fecha      | datetime2     | Día de la reserva           |
| HoraInicio | time          | Hora de inicio              |
| HoraFin    | time          | Hora de finalización        |
| Motivo     | nvarchar(250) | Motivo de la reunión        |
| Estado     | int           | Estado de la reserva        |
| SalaId     | int (FK)      | Sala reservada              |
| UsuarioId  | nvarchar(450) | Usuario creador             |

---

# Campo Motivo Opcional

El campo **Motivo** es opcional.

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

Estado almacenado en base de datos:

| Valor | Estado    |
| ----- | --------- |
| 0     | Activa    |
| 1     | Cancelada |

Estados calculados dinámicamente:

| Estado     | Descripción              | Color      |
| ---------- | ------------------------ | ---------- |
| Agendada   | La reunión aún no inicia | 🟢 Verde   |
| En proceso | La reunión ya inició     | 🟠 Naranja |
| Finalizada | La reunión terminó       | ⚫ Negro    |
| Cancelada  | Cancelada manualmente    | ⚪ Gris     |

Estos estados **no se almacenan en la base de datos**, se calculan en el backend.

---

# Relaciones de Base de Datos

* Una **Sala** puede tener múltiples **Reservas**
* Un **Usuario** puede crear múltiples **Reservas**
* Cada **Reserva** pertenece a una sola **Sala**
* Cada **Reserva** pertenece a un solo **Usuario**

---

# Sistema de Autenticación

Se utiliza **ASP.NET Identity**.

Tablas principales:

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

# Gestión de Usuarios

Funciones disponibles:

* Crear usuarios
* Editar usuarios
* Asignar roles
* Buscar usuarios por nombre
* Filtrar usuarios por rol
* Paginación
* Activar usuarios
* Inhabilitar usuarios (soft delete)
* Prevención de duplicados

Acceso exclusivo para **Administrador**.

---

# Gestión de Salas

Funciones:

* Crear salas
* Editar salas
* Definir color para el calendario

---

# Gestión de Reservas

Funciones:

* Crear reservas
* Editar reservas
* Cancelar reservas
* Ver detalles
* Visualizar estado dinámico
* Descargar evento de calendario (.ICS)

---

# Validaciones del Sistema

Validaciones implementadas en **frontend y backend**.

Principales reglas:

* No permitir reservas en el pasado
* La hora de fin debe ser mayor que la de inicio
* No permitir reservas superpuestas en la misma sala
* Un usuario no puede reservar dos salas en el mismo horario
* No permitir editar reservas finalizadas
* Validación de existencia del usuario antes de guardar la reserva
* Prevención de duplicados en usuarios

Las horas se almacenan como:

```
time
```

Esto evita problemas de configuración regional.

---

# Cancelación de Reservas

El sistema implementa **cancelación lógica (soft cancel)**.

Cuando una reserva se cancela:

* el registro se mantiene
* el estado cambia a **Cancelada**
* no aparece en el calendario activo

Esto permite mantener **historial y auditoría**.

---

# Calendario de Reservas

Calendario interactivo implementado con **FullCalendar**.

Funciones:

* vista mensual
* vista semanal
* colores por sala
* detalle de reserva al hacer clic

Endpoint utilizado:

```
/Reservas/GetReservas
```

---

# Integración con Calendarios

Cada reserva puede descargarse como evento de calendario.

Archivo generado:

```
.ICS
```

Compatible con:

* Google Calendar
* Outlook
* Apple Calendar

Endpoint:

```
/Reservas/DescargarICS/{id}
```

---

# Dashboard Administrativo

Panel administrativo con métricas del sistema.

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

El sistema permite exportar **reportes PDF profesionales**.

Contenido del reporte:

* encabezado institucional
* métricas del sistema
* listado de reservas

Tecnología utilizada:

**QuestPDF**

---

# Gestión de Sesiones

Control automático de sesiones por inactividad.

Características:

* cierre automático tras periodo de inactividad
* protección en equipos compartidos

---

# Instalación y Ejecución

## 1 Clonar el repositorio

```
git clone https://github.com/usuario/sala-reuniones.git
```

---

## 2 Configurar cadena de conexión

Editar:

```
appsettings.json
```

Ejemplo:

```
Server=localhost;
Database=SalaReunionesDB;
Trusted_Connection=True;
TrustServerCertificate=True;
```

---

## 3 Ejecutar migraciones

```
Update-Database
```

---

## 4 Ejecutar el proyecto

```
F5
```

o

```
dotnet run
```

---

# Despliegue en IIS

El sistema puede desplegarse en **Windows Server con IIS**.

## 1 Instalar IIS

```
Administrador del Servidor
→ Agregar roles y características
→ Servidor Web (IIS)
```

---

## 2 Instalar .NET Hosting Bundle

Descargar:

```
dotnet-hosting-8.x-win
```

Luego ejecutar:

```
iisreset
```

---

## 3 Publicar el Proyecto

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

## 4 Copiar al Servidor

```
C:\inetpub\SalaReuniones
```

---

## 5 Crear Application Pool

```
Nombre: SalaReunionesPool
.NET CLR Version: No Managed Code
Pipeline Mode: Integrated
```

---

## 6 Crear Sitio Web

```
Site Name: SalaReuniones
Physical Path: C:\inetpub\SalaReuniones
Port: 80 o 8080
Application Pool: SalaReunionesPool
```

---

## 7 Permisos de Carpeta

Asignar permisos a:

```
IIS_IUSRS
```

con **lectura y ejecución**.

---

# Seguridad

El sistema implementa:

* `[Authorize]`
* control de acceso por roles
* protección **CSRF**
* validaciones backend
* control de integridad referencial en base de datos

---

# Conclusión

Este sistema permite gestionar reservas de salas de forma **segura, organizada y eficiente**, evitando conflictos de horario y manteniendo un historial completo de reuniones.

Incluye herramientas modernas como:

* calendario interactivo
* dashboard administrativo
* reportes en PDF
* integración con calendarios
* administración avanzada de usuarios

lo que permite su uso en **entornos corporativos e institucionales**.