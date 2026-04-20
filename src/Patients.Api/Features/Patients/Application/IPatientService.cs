using Patients.Api.Features.Patients.Contracts;

namespace Patients.Api.Features.Patients.Application;

public interface IPatientService
{
    Task<PatientResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResponse<PatientResponse>> GetPagedAsync(PatientQueryParameters query, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, PatientResponse? Patient)> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, PatientResponse? Patient)> UpdateAsync(int id, UpdatePatientRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PatientResponse>> GetCreatedAfterAsync(DateTime fromDate, CancellationToken cancellationToken = default);
}
