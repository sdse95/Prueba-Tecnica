using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Patients.Api.Features.Patients.Application;
using Patients.Api.Features.Patients.Contracts;

namespace Patients.Api.Features.Patients.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Reader,Editor")]
public class PatientsController(IPatientService patientService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PatientResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatients([FromQuery] PatientQueryParameters query, CancellationToken cancellationToken)
    {
        var patients = await patientService.GetPagedAsync(query, cancellationToken);
        return Ok(patients);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var patient = await patientService.GetByIdAsync(id, cancellationToken);
        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpPost]
    [Authorize(Roles = "Editor")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var result = await patientService.CreateAsync(request, cancellationToken);
        if (!result.Success)
        {
            return Conflict(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Patient!.PatientId }, result.Patient);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Editor")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        var result = await patientService.UpdateAsync(id, request, cancellationToken);
        if (!result.Success && result.Error == "Patient not found.")
        {
            return NotFound(new { message = result.Error });
        }

        if (!result.Success)
        {
            return Conflict(new { message = result.Error });
        }

        return Ok(result.Patient);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Editor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await patientService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("created-after")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PatientResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCreatedAfter([FromQuery] DateTime fromDate, CancellationToken cancellationToken)
    {
        var patients = await patientService.GetCreatedAfterAsync(fromDate, cancellationToken);
        return Ok(patients);
    }
}
