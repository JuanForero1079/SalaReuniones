# Sistema de Gestión de Salas de Reuniones

Aplicación web desarrollada en **ASP.NET Core MVC (.NET 8)** para la gestión de reservas de salas dentro de una organización.

El sistema permite administrar **usuarios, salas y reservas**, con control de acceso por roles, visualización en calendario y generación de **reportes administrativos con métricas y exportación a PDF**.

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

Ejemplo:

* Sala Principal → `#94A5A4`
* Sala Secundaria → `#3788d8`

El color se utiliza para distinguir visualmente cada sala en el calendario.

---

# Tabla: Reservas

Es la tabla central del sistema y contiene todas las reservas registradas.

| Campo      | Tipo          | Descripción                 |
| ---------- | ------------- | --------------------------- |
| Id         | int (PK)      | Identificador de la reserva |
| Fecha      | datetime2     | Día de la reserva           |
| HoraInicio | time          | Hora de inicio              |
| HoraFin    | time          | Hora de finalización        |
| Motivo     | nvarchar(250) | Motivo de la reunión        |
| Estado     | int           | Estado de la reserva        |
| SalaId     | int (FK)      | Relación con la sala        |
| UsuarioId  | nvarchar(450) | Usuario que creó la reserva |

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

Estos estados **no se almacenan directamente en la base de datos**, sino que se calculan dinámicamente mediante lógica de tiempo en el sistema.

Esto permite mantener un **historial completo de reservas sin modificar registros antiguos**.

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
* Eliminar reservas (solo administrador)
* Ver detalles de reservas
* Visualizar estado dinámico de reservas

---

# Validaciones del Sistema

El sistema implementa validaciones tanto en **backend como en frontend**.

Validaciones principales:

* No permitir reservas en el pasado
* La hora de fin debe ser mayor que la hora de inicio
* No permitir reservas que se solapen en la misma sala
* No permitir editar reservas finalizadas
* No permitir eliminar reservas finalizadas

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
* filtro por sala
* detalle de reserva al hacer clic

Endpoint utilizado:

```
/Reservas/GetReservas
```

Las reservas canceladas se excluyen del calendario automáticamente.

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

# Filtros del Dashboard

El dashboard permite filtrar estadísticas por rango de fechas.

Filtros disponibles:

* Hoy
* Últimos 7 días
* Últimos 30 días
* Últimos 6 meses
* Último año
* Histórico completo
* Rango personalizado

Las métricas, gráficas y reportes se actualizan según el rango seleccionado.

---

# Reportes Administrativos

El sistema permite exportar reportes en **PDF profesional**.

Los reportes incluyen:

* encabezado institucional
* logo del sistema
* fecha de generación
* métricas de reservas
* listado detallado de reservas
* estado dinámico de cada reserva

Tecnología utilizada:

**QuestPDF**

---

# API Interna para Dashboard

El sistema expone endpoints utilizados por el frontend.

Ejemplo:

```
/Reportes/GetDashboardData
```

Devuelve datos en formato **JSON** para alimentar las gráficas de Chart.js.

---

# Frontend

Tecnologías utilizadas:

* Razor Views
* Bootstrap
* JavaScript
* FullCalendar
* Chart.js
* Bootstrap Icons

---

# Validaciones en el Frontend

El sistema también implementa validaciones antes de enviar formularios:

* no permitir fechas pasadas
* no permitir horas pasadas si la fecha es el día actual
* verificación de campos obligatorios

---

# Ejecución del Proyecto

---

# Ejecutar en Visual Studio

1. Abrir la solución
2. Configurar la cadena de conexión en `appsettings.json`
3. Ejecutar migraciones si es necesario
4. Presionar **F5**

---

# Publicación en IIS

## 1 Publicar desde Visual Studio

* Click derecho en el proyecto
* Seleccionar **Publish**
* Elegir publicación a carpeta

---

## 2 Configurar IIS

Crear nuevo sitio web:

* seleccionar la carpeta publicada
* configurar puerto

---

## 3 Configurar conexión a base de datos

Actualizar en:

```
appsettings.json
```

Ejemplo:

```
Server=SERVIDOR;
Database=SalaReunionesDB;
Trusted_Connection=True;
TrustServerCertificate=True;
```

---

## 4 Permisos del servidor

Dar permisos a:

```
IIS_IUSRS
```

---

# Seguridad

El sistema implementa varias capas de seguridad:

* `[Authorize]`
* control de acceso por roles
* validaciones en backend
* protección CSRF con `ValidateAntiForgeryToken`
* restricción de edición o eliminación de reservas finalizadas

---

# Datos Iniciales

Usuarios de prueba incluidos:

Administrador
[admin@empresa.com](mailto:admin@empresa.com)

Usuario
[usuario@empresa.com](mailto:usuario@empresa.com)

Visualizador
[visor@empresa.com](mailto:visor@empresa.com)

---

# Salas Iniciales

* Sala Principal
* Sala Secundaria

---

# Conclusión

Este sistema permite gestionar reservas de salas de forma **segura, organizada y eficiente**, evitando conflictos de horario y manteniendo historial completo de reservas.

Incluye herramientas modernas como **calendario interactivo, dashboard administrativo con métricas, filtros avanzados y generación de reportes en PDF**, lo que facilita la toma de decisiones dentro de la organización.

El proyecto sigue **buenas prácticas de arquitectura MVC y seguridad en ASP.NET Core**, permitiendo su escalabilidad y mantenimiento en entornos empresariales.
