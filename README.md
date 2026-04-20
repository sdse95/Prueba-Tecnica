# Patients API - Prueba Tecnica Backend .NET

API RESTful para gestion de pacientes usando ASP.NET Core, Entity Framework Core y SQL Server.

Solucion: `Patients.slnx`  
Proyecto API: `src/Patients.Api`  
Proyecto tests: `tests/Patients.Api.Tests`

## Stack
- ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- SQL Server
- xUnit
- Swagger/OpenAPI (Swashbuckle)

## Requisitos previos
- **.NET SDK** compatible con el proyecto (este repo esta en `net10.0`).
- **SQL Server** accesible desde tu maquina. El default del repo usa **LocalDB**: `(localdb)\\MSSQLLocalDB`.
- **Herramientas de EF Core CLI** (`dotnet-ef`) para aplicar migraciones desde consola (ver seccion **Crear base de datos**).

## Arquitectura resumida
La API esta organizada como un **monolito modular** (estilo microservicio ligero) dentro de un solo proyecto:
- `Features/Patients/Api`: controladores HTTP.
- `Features/Patients/Application`: casos de uso y reglas (incluye validacion de duplicados).
- `Features/Patients/Contracts`: DTOs de entrada/salida.
- `Features/Patients/Domain`: entidad persistida.
- `Infrastructure/Persistence`: `AppDbContext` y migraciones EF Core.

Esto mantiene el codigo agrupado por feature sin fragmentar la solucion en muchos ensamblados.

### Decisiones tecnicas (breve)
- **EF Core + migraciones** para crear y versionar el esquema de `Patients` (cumple el requisito de EF y facilita reproducibilidad).
- **Stored procedures** para consultas especificas (`listado paginado` y `creados despues de fecha`) porque el enunciado lo pide y permite delegar paginacion/filtrado a SQL Server.
- **Monolito modular por carpetas** en vez de multiples proyectos: reduce friccion en una prueba corta sin sacrificar separacion clara de responsabilidades.

## Endpoints requeridos
- `POST /api/patients`: crea paciente validando duplicado por `(DocumentType, DocumentNumber)`.
- `GET /api/patients`: lista paginada con filtros opcionales `name` y `documentNumber` (implementado consumiendo `dbo.sp_GetPatients`).
- `GET /api/patients/{id}`: obtiene detalle por id.
- `PUT /api/patients/{id}`: actualizacion flexible (parcial o total).
- `DELETE /api/patients/{id}`: elimina por id.
- `GET /api/patients/created-after?fromDate=...`: consulta via stored procedure.

## Comportamiento de PUT
`PUT /api/patients/{id}` se implementa como **flexible**:
- Permite enviar todos los campos o solo un subconjunto.
- Solo se actualizan los campos presentes en el body.
- Si la combinacion de documento resultante ya existe, responde `409 Conflict`.

## Estructura de datos
Tabla `Patients`:
- `PatientId` (PK, int, Identity)
- `DocumentType` (nvarchar(10))
- `DocumentNumber` (nvarchar(20))
- `FirstName` (nvarchar(80))
- `LastName` (nvarchar(80))
- `BirthDate` (date)
- `PhoneNumber` (nvarchar(20), null)
- `Email` (nvarchar(120), null)
- `CreatedAt` (datetime2, default `GETUTCDATE()`)

Adicionalmente, se define un indice unico compuesto en `(DocumentType, DocumentNumber)`.

## Procedimientos almacenados
Los procedimientos **no se versionan como archivos SQL en el repo**. Debes crearlos directamente en SQL Server (SSMS) sobre la base `PatientsDb`.

### `dbo.GetPatientsCreatedAfter` (pacientes creados despues de una fecha)

```sql
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Samuel Salcedo>
-- Create date: <20/04/2026>
-- Description:	<Procedimiento encargado de obtener los pacientes creados despues de una fecha especifica>
-- =============================================
CREATE OR ALTER PROCEDURE dbo.GetPatientsCreatedAfter
    @FromDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PatientId,
        DocumentType,
        DocumentNumber,
        FirstName,
        LastName,
        BirthDate,
        PhoneNumber,
        Email,
        CreatedAt
    FROM dbo.Patients
    WHERE CreatedAt > @FromDate
    ORDER BY CreatedAt DESC;
END;

GO
```

La API lo consume desde el endpoint `GET /api/patients/created-after?fromDate=...`.

### `dbo.sp_GetPatients` (listado paginado + filtros)

```sql
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Samuel Salcedo>
-- Create date: <20/04/2026>
-- Description:	<consulta de pacientes con paginación y filtros>
-- =============================================
CREATE OR ALTER PROCEDURE dbo.sp_GetPatients
    @Page INT = 1,
    @PageSize INT = 10,
    @Name NVARCHAR(100) = NULL,
    @DocumentNumber NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1) AS TotalRecords
    FROM Patients
    WHERE 
        (@Name IS NULL OR (FirstName + ' ' + LastName) LIKE '%' + @Name + '%')
        AND (@DocumentNumber IS NULL OR DocumentNumber = @DocumentNumber);
    SELECT *
    FROM Patients
    WHERE 
        (@Name IS NULL OR (FirstName + ' ' + LastName) LIKE '%' + @Name + '%')
        AND (@DocumentNumber IS NULL OR DocumentNumber = @DocumentNumber)
    ORDER BY CreatedAt DESC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
```

La API lo consume desde el endpoint `GET /api/patients`.

## Configuracion local
La cadena de conexion vive en:
- `src/Patients.Api/appsettings.json` (default)
- `src/Patients.Api/appsettings.Development.json` (override cuando `ASPNETCORE_ENVIRONMENT=Development`)

Por defecto ambos apuntan a LocalDB y a la base `PatientsDb`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PatientsDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Ajusta el `Server` segun tu instalacion real. Ejemplos comunes:
- LocalDB: `(localdb)\\MSSQLLocalDB`
- SQL Express: `localhost\\SQLEXPRESS`
- Instancia default: `localhost` o `.`

Si no usas LocalDB, cambia el `Server` y vuelve a ejecutar migraciones.

### Notas importantes sobre SQL Server
- **`Trusted_Connection=True`**: autenticacion integrada de Windows (recomendado para LocalDB).
- **`TrustServerCertificate=True`**: util en entornos locales para evitar problemas de cadena de certificados TLS al conectar. En produccion normalmente se define una politica TLS explicita.

### Troubleshooting rapido (LocalDB)
Si `dotnet ef database update` falla conectando a `(localdb)\\MSSQLLocalDB`, normalmente es una de estas causas:
- **LocalDB no instalado / instancia incorrecta**: instala SQL Server Express + LocalDB o ajusta el `Server=` al nombre real que te muestra SSMS.
- **Instancia detenida** (a veces): puedes iniciar la instancia con `sqllocaldb start MSSQLLocalDB` (si aplica en tu entorno).

## Crear base de datos (esquema + tabla `Patients`)
Este proyecto usa **EF Core migrations** para crear el esquema. La base de datos objetivo es la que indica `Database=` en `DefaultConnection` (por defecto `PatientsDb`).

### Opcion A (recomendada): migraciones por CLI
1. Instala la herramienta global (una sola vez por maquina):

```powershell
dotnet tool install --global dotnet-ef
```

Si ya la tienes instalada y quieres actualizarla:

```powershell
dotnet tool update --global dotnet-ef
```

2. Desde la raiz del repo, aplica migraciones:

```powershell
dotnet restore Patients.slnx
dotnet ef database update --project src/Patients.Api --startup-project src/Patients.Api
```

**Que hace `database update`**: crea la base (si no existe) y aplica las migraciones para crear la tabla `Patients` y sus indices.

### Opcion B: migraciones automaticas al iniciar (solo Development)
Si corres la API con `ASPNETCORE_ENVIRONMENT=Development`, `Program.cs` ejecuta `Database.Migrate()` al arrancar.

Esto es util para desarrollo local, pero **no sustituye** documentar el comando `dotnet ef database update` para ambientes/CI.

## Levantar proyecto
1. Configura SQL Server y valida que puedes conectarte con el `Server=` de `DefaultConnection`.
2. Restaurar dependencias:
   - `dotnet restore Patients.slnx`
3. Crear/aplicar esquema (base + tabla):
   - `dotnet ef database update --project src/Patients.Api --startup-project src/Patients.Api`
4. Crear/actualizar stored procedures:
   - ejecutar los scripts SQL de la seccion **Procedimientos almacenados** en la base `PatientsDb` (debe existir la tabla `Patients` antes).
5. Ejecutar API:
   - `dotnet run --project src/Patients.Api`
6. Probar en Swagger:
   - `https://localhost:<puerto>/swagger`

## Ejecutar pruebas
- `dotnet test Patients.slnx`

Incluye pruebas unitarias con **xUnit** y **Moq**:
- `PatientsControllerTests`: todos los endpoints del controlador con `IPatientService` mockeado (respuestas HTTP esperadas).
- `PatientServiceTests`: reglas de negocio y persistencia con **EF Core InMemory** (CRUD y consulta por id). Los metodos que dependen de **stored procedures** (`GetPagedAsync`, `GetCreatedAfterAsync`) se cubren a nivel de controlador; probar el SP real requiere SQL Server (prueba de integracion opcional).