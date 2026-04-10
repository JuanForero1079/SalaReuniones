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

* Generación de archivos **ICS** compatibles con:

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

| Campo    | Tipo          | Descripción                    |
| -------- | ------------- | ------------------------------ |
| Id       | int (PK)      | Identificador único de la sala |
| Nombre   | nvarchar(100) | Nombre de la sala              |
| ColorHex | nvarchar(7)   | Color usado en el calendario   |

Salas incluidas en el sistema:

* **Sala Principal → #94A5A4**
* **Sala Secundaria → #3788d8**

El color se utiliza para distinguir visualmente cada sala en el calendario.

---

# Tabla: Reservas

Es la tabla central del sistema y contiene todas las reservas registradas.

| Campo      | Tipo          | Descripción                     |
| ---------- | ------------- | ------------------------------- |
| Id         | int (PK)      | Identificador de la reserva     |
| Fecha      | datetime2     | Día de la reserva               |
| HoraInicio | time          | Hora de inicio                  |
| HoraFin    | time          | Hora de finalización            |
| Motivo     | nvarchar(250) | Motivo de la reunión (opcional) |
| Estado     | int           | Estado de la reserva            |
| SalaId     | int (FK)      | Relación con la sala            |
| UsuarioId  | nvarchar(450) | Usuario que creó la reserva     |

---

# Campo Motivo Opcional

El campo **Motivo** fue configurado como **opcional**, permitiendo crear reservas sin especificar un motivo.

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

El campo **Estado** se maneja mediante un **Enum en C#**:

* **0 → Activa**
* **1 → Cancelada**

Sin embargo, el sistema calcula **estados dinámicos adicionales** basados en la hora actual.

| Estado     | Descripción                               | Color      |
| ---------- | ----------------------------------------- | ---------- |
| Agendada   | La reunión aún no inicia                  | 🟢 Verde   |
| En proceso | La reunión ya inició pero no ha terminado | 🟠 Naranja |
| Finalizada | La reunión ya terminó                     | ⚫ Negro    |
| Cancelada  | La reserva fue cancelada manualmente      | ⚪ Gris     |

Estos estados **no se almacenan directamente en la base de datos**, sino que se calculan dinámicamente mediante lógica de tiempo.

---

# Relaciones de Base de Datos

Las relaciones principales del sistema son:

* Una **Sala** puede tener muchas **Reservas**
* Un **Usuario** puede crear muchas **Reservas**
* Cada **Reserva** pertenece a una sola **Sala**
* Cada **Reserva** pertenece a un solo **Usuario**

---

# Sistema de Autenticación

El sistema utiliza **ASP.NET Identity**.

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

# Funcionalidades del Sistema

---

# Gestión de Usuarios

Administración de usuarios mediante **ASP.NET Identity**.

Funciones:

* Crear usuarios
* Asignar roles
* Editar usuarios
* Eliminar usuarios

Solo accesible para **Administrador**.

---

# Gestión de Salas

Permite administrar las salas disponibles en el sistema.

Funciones:

* Crear salas
* Editar salas
* Definir color de visualización en calendario

---

# Gestión de Reservas

Funciones disponibles:

* Crear reservas
* Editar reservas
* Cancelar reservas
* Ver detalles de reservas
* Visualizar estado dinámico de reservas

---

# Paginación de Reservas

Para mejorar el rendimiento cuando existen muchas reservas, el listado implementa **paginación**.

Características:

* Navegación por páginas
* Carga optimizada de registros
* Mejora en tiempos de respuesta

Esto permite escalar el sistema cuando existan **cientos o miles de reservas**.

---

# Validaciones del Sistema

El sistema implementa validaciones tanto en **backend como en frontend**.

Validaciones principales:

* No permitir reservas en el pasado
* La hora de fin debe ser mayor que la hora de inicio
* No permitir reservas que se solapen en la misma sala
* No permitir editar reservas finalizadas

---

# Cancelación de Reservas

El sistema utiliza **cancelación lógica (soft cancel)**.

Cuando una reserva se cancela:

* se mantiene el registro en la base de datos
* se cambia el estado a **Cancelada**
* no aparece en el calendario

Esto permite mantener historial y auditoría.

---

# Calendario de Reservas

El sistema incluye un calendario interactivo desarrollado con **FullCalendar**.

Funciones:

* visualización semanal
* visualización mensual
* colores diferenciados por sala
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

Esto permite que los usuarios agreguen la reunión a su calendario personal automáticamente.

---

# Dashboard Administrativo

El sistema incluye un **panel administrativo con métricas y estadísticas**.

Indicadores principales:

* Total de reservas
* Reservas activas
* Reservas del día
* Total de usuarios registrados

Incluye **gráficas dinámicas con Chart.js**:

* Reservas por sala
* Reservas por día

---

# Reportes Administrativos

El sistema permite exportar reportes en **PDF profesional**.

Los reportes incluyen:

* encabezado institucional
* métricas del sistema
* listado detallado de reservas

Tecnología utilizada:

**QuestPDF**

---

# Ejecución del Proyecto

---

# Ejecutar en Visual Studio

1. Abrir la solución
2. Configurar cadena de conexión en `appsettings.json`
3. Ejecutar migraciones si es necesario
4. Presionar **F5**

---

# Despliegue en Servidor (IIS)

El sistema puede desplegarse en **Windows Server con IIS**.

---

# 1 Instalar IIS

En el servidor:

```
Administrador del Servidor
→ Agregar roles y características
→ Servidor Web (IIS)
```

---

# 2 Instalar .NET Hosting Bundle

Descargar e instalar:

```
dotnet-hosting-8.x-win
```

Este paquete instala:

* .NET Runtime
* ASP.NET Core Runtime
* ASP.NET Core Module para IIS

Después ejecutar:

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

Ejemplo de carpeta:

```
C:\Publicaciones\SalaReuniones
```

---

# 4 Copiar al Servidor

Copiar los archivos publicados a:

```
C:\inetpub\SalaReuniones
```

---

# 5 Crear Application Pool

En IIS:

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

En IIS:

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

Dar permisos a:

```
IIS_IUSRS
```

con permisos de lectura y ejecución.

---

# 8 Configurar Conexión a Base de Datos

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

# 9 Acceso al Sistema

Una vez publicado, el sistema estará disponible en:

```
http://IP_DEL_SERVIDOR
```

---

# Seguridad

El sistema implementa varias capas de seguridad:

* `[Authorize]`
* control de acceso por roles
* protección CSRF
* validaciones en backend

---

# Salas Iniciales

El sistema incluye dos salas configuradas:

* Sala Principal
* Sala Secundaria

---

# Conclusión

Este sistema permite gestionar reservas de salas de forma **segura, organizada y eficiente**, evitando conflictos de horario y manteniendo historial completo de reservas.

Incluye herramientas modernas como **calendario interactivo, dashboard administrativo, exportación de reportes y generación automática de eventos en calendario**, permitiendo su uso en entornos empresariales.
