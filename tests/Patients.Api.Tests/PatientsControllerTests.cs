using Microsoft.AspNetCore.Mvc;
using Moq;
using Patients.Api.Features.Patients.Api;
using Patients.Api.Features.Patients.Application;
using Patients.Api.Features.Patients.Contracts;

namespace Patients.Api.Tests;

public class PatientsControllerTests
{
    private static PatientResponse SamplePatient(int id = 1) => new()
    {
        PatientId = id,
        DocumentType = "CC",
        DocumentNumber = "123",
        FirstName = "Ana",
        LastName = "Lopez",
        BirthDate = new DateTime(1990, 1, 1),
        PhoneNumber = null,
        Email = "a@b.com",
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task GetPatients_ReturnsOk_WithPagedResponse()
    {
        var mock = new Mock<IPatientService>();
        var page = new PagedResponse<PatientResponse>
        {
            Items = [SamplePatient()],
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };
        mock.Setup(s => s.GetPagedAsync(It.IsAny<PatientQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var controller = new PatientsController(mock.Object);
        var result = await controller.GetPatients(new PatientQueryParameters(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<PagedResponse<PatientResponse>>(ok.Value);
        Assert.Single(payload.Items);
        Assert.Equal(1, payload.TotalCount);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenPatientExists()
    {
        var mock = new Mock<IPatientService>();
        mock.Setup(s => s.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SamplePatient(5));

        var controller = new PatientsController(mock.Object);
        var result = await controller.GetById(5, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PatientResponse>(ok.Value);
        Assert.Equal(5, dto.PatientId);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenPatientMissing()
    {
        var mock = new Mock<IPatientService>();
        mock.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PatientResponse?)null);

        var controller = new PatientsController(mock.Object);
        var result = await controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WhenPatientIsValid()
    {
        var basePatient = SamplePatient(10);
        var created = new PatientResponse
        {
            PatientId = basePatient.PatientId,
            DocumentType = basePatient.DocumentType,
            DocumentNumber = "123456",
            FirstName = basePatient.FirstName,
            LastName = basePatient.LastName,
            BirthDate = basePatient.BirthDate,
            PhoneNumber = basePatient.PhoneNumber,
            Email = basePatient.Email,
            CreatedAt = basePatient.CreatedAt
        };
        var mock = new Mock<IPatientService>();
        mock.Setup(s => s.CreateAsync(It.IsAny<CreatePatientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null, created));

        var controller = new PatientsController(mock.Object);
        var request = new CreatePatientRequest
        {
            DocumentType = "CC",
            DocumentNumber = "123456",
            FirstName = "Ana",
            LastName = "Lopez",
            BirthDate = new DateTime(1990, 1, 1),
            Email = "ana@test.com"
        };

        var result = await controller.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var payload = Assert.IsType<PatientResponse>(createdResult.Value);
        Assert.Equal("123456", payload.DocumentNumber);
        Assert.Equal(nameof(PatientsController.GetById), createdResult.ActionName);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenDocumentAlreadyExists()
    {
        var mock = new Mock<IPatientService>();
        mock.Setup(s => s.CreateAsync(It.IsAny<CreatePatientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "A patient with the same document already exists.", (PatientResponse?)null));

        var controller = new PatientsController(mock.Object);
        var request = new CreatePatientRequest
        {
            DocumentType = "CC",
            DocumentNumber = "999999",
            FirstName = "Carla",
            LastName = "Ramirez",
            BirthDate = new DateTime(1992, 5, 8)
        };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenSuccessful()
    {
        var updated = SamplePatient(3);
        var mock = new Mock<IPatientService>();
        mock.Setup(s => s.UpdateAsync(3, It.IsAny<UpdatePatientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null, updated));

        var controller = new PatientsController(mock.Object);
        var result = await controller.Update(3, new UpdatePatientRequest { FirstName = "X" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<PatientResponse>(ok.Value);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenPatientMissing()
    {
        var mock = new Mock<IPatientService>();
        mock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePatientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Patient not found.", (PatientResponse?)null));

        var controller = new PatientsController(mock.Object);
        var result = await controller.Update(1, new UpdatePatientRequest(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsConflict_WhenDuplicateDocument()
    {
        var mock = new Mock<IPatientService>();
        mock.Setup(s => s.UpdateAsync(2, It.IsAny<UpdatePatientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "A patient with the same document already exists.", (PatientResponse?)null));

        var controller = new PatientsController(mock.Object);
        var result = await controller.Update(2, new UpdatePatientRequest { DocumentNumber = "dup" }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenDeleted()
    {
        var mock = new Mock<IPatientService>();
        mock.Setup(s => s.DeleteAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var controller = new PatientsController(mock.Object);
        var result = await controller.Delete(7, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        var mock = new Mock<IPatientService>();
        mock.Setup(s => s.DeleteAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var controller = new PatientsController(mock.Object);
        var result = await controller.Delete(7, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetCreatedAfter_ReturnsOk_WithCollection()
    {
        var list = new List<PatientResponse> { SamplePatient(1), SamplePatient(2) };
        var mock = new Mock<IPatientService>();
        mock.Setup(s => s.GetCreatedAfterAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var controller = new PatientsController(mock.Object);
        var result = await controller.GetCreatedAfter(DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyCollection<PatientResponse>>(ok.Value);
        Assert.Equal(2, payload.Count);
    }
}
