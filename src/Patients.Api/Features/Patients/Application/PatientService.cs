using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Patients.Api.Features.Patients.Contracts;
using Patients.Api.Features.Patients.Domain;
using Patients.Api.Infrastructure.Persistence;

namespace Patients.Api.Features.Patients.Application;

public class PatientService(AppDbContext context) : IPatientService
{
    public async Task<PatientResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await context.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == id, cancellationToken);

        return patient is null ? null : MapToResponse(patient);
    }

    public async Task<PagedResponse<PatientResponse>> GetPagedAsync(PatientQueryParameters query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var nameFilter = string.IsNullOrWhiteSpace(query.Name) ? null : query.Name.Trim();
        var documentFilter = string.IsNullOrWhiteSpace(query.DocumentNumber) ? null : query.DocumentNumber.Trim();

        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_GetPatients";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@Page", SqlDbType.Int) { Value = page });
            command.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });
            command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 100)
            {
                Value = nameFilter is null ? DBNull.Value : nameFilter
            });
            command.Parameters.Add(new SqlParameter("@DocumentNumber", SqlDbType.NVarChar, 50)
            {
                Value = documentFilter is null ? DBNull.Value : documentFilter
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var totalCount = 0;
            if (await reader.ReadAsync(cancellationToken))
            {
                totalCount = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
            }

            if (!await reader.NextResultAsync(cancellationToken))
            {
                return new PagedResponse<PatientResponse>
                {
                    Items = Array.Empty<PatientResponse>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            }

            var items = new List<PatientResponse>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(ReadPatientResponse(reader));
            }

            return new PagedResponse<PatientResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task<(bool Success, string? Error, PatientResponse? Patient)> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await context.Patients.AnyAsync(p =>
            p.DocumentType == request.DocumentType &&
            p.DocumentNumber == request.DocumentNumber,
            cancellationToken);

        if (exists)
        {
            return (false, "A patient with the same document already exists.", null);
        }

        var patient = new Patient
        {
            DocumentType = request.DocumentType.Trim(),
            DocumentNumber = request.DocumentNumber.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            BirthDate = request.BirthDate.Date,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim()
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync(cancellationToken);

        return (true, null, MapToResponse(patient));
    }

    public async Task<(bool Success, string? Error, PatientResponse? Patient)> UpdateAsync(int id, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.PatientId == id, cancellationToken);
        if (patient is null)
        {
            return (false, "Patient not found.", null);
        }

        var documentType = request.DocumentType?.Trim() ?? patient.DocumentType;
        var documentNumber = request.DocumentNumber?.Trim() ?? patient.DocumentNumber;

        var duplicateExists = await context.Patients.AnyAsync(p =>
            p.PatientId != id &&
            p.DocumentType == documentType &&
            p.DocumentNumber == documentNumber,
            cancellationToken);

        if (duplicateExists)
        {
            return (false, "A patient with the same document already exists.", null);
        }

        if (request.DocumentType is not null) patient.DocumentType = documentType;
        if (request.DocumentNumber is not null) patient.DocumentNumber = documentNumber;
        if (request.FirstName is not null) patient.FirstName = request.FirstName.Trim();
        if (request.LastName is not null) patient.LastName = request.LastName.Trim();
        if (request.BirthDate.HasValue) patient.BirthDate = request.BirthDate.Value.Date;
        if (request.PhoneNumber is not null) patient.PhoneNumber = request.PhoneNumber.Trim();
        if (request.Email is not null) patient.Email = request.Email.Trim();

        await context.SaveChangesAsync(cancellationToken);

        return (true, null, MapToResponse(patient));
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.PatientId == id, cancellationToken);
        if (patient is null)
        {
            return false;
        }

        context.Patients.Remove(patient);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<PatientResponse>> GetCreatedAfterAsync(DateTime fromDate, CancellationToken cancellationToken = default)
    {
        var fromDateUtc = fromDate.Kind == DateTimeKind.Utc ? fromDate : fromDate.ToUniversalTime();

        var patients = await context.Patients
            .FromSqlInterpolated($"EXEC dbo.GetPatientsCreatedAfter @FromDate={fromDateUtc}")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return patients.Select(MapToResponse).ToList();
    }

    private static PatientResponse MapToResponse(Patient patient)
    {
        return new PatientResponse
        {
            PatientId = patient.PatientId,
            DocumentType = patient.DocumentType,
            DocumentNumber = patient.DocumentNumber,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            BirthDate = patient.BirthDate,
            PhoneNumber = patient.PhoneNumber,
            Email = patient.Email,
            CreatedAt = patient.CreatedAt
        };
    }

    private static PatientResponse ReadPatientResponse(DbDataReader reader)
    {
        return new PatientResponse
        {
            PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
            DocumentType = reader.GetString(reader.GetOrdinal("DocumentType")),
            DocumentNumber = reader.GetString(reader.GetOrdinal("DocumentNumber")),
            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.GetString(reader.GetOrdinal("LastName")),
            BirthDate = reader.GetDateTime(reader.GetOrdinal("BirthDate")),
            PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
