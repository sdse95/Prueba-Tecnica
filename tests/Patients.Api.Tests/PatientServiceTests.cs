using Microsoft.EntityFrameworkCore;
using Patients.Api.Features.Patients.Application;
using Patients.Api.Features.Patients.Contracts;
using Patients.Api.Features.Patients.Domain;
using Patients.Api.Infrastructure.Persistence;

namespace Patients.Api.Tests;

/// <summary>
/// Pruebas del servicio contra EF InMemory para CRUD y consultas por id.
/// GetPagedAsync y GetCreatedAfterAsync requieren SQL Server (stored procedures) y no se ejecutan aqui.
/// </summary>
public class PatientServiceTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        await using var ctx = BuildContext();
        var sut = new PatientService(ctx);

        var result = await sut.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPatient_WhenExists()
    {
        await using var ctx = BuildContext();
        ctx.Patients.Add(new Patient
        {
            DocumentType = "CC",
            DocumentNumber = "111",
            FirstName = "A",
            LastName = "B",
            BirthDate = new DateTime(1990, 1, 1)
        });
        await ctx.SaveChangesAsync();
        var id = ctx.Patients.Single().PatientId;

        var sut = new PatientService(ctx);
        var result = await sut.GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("111", result!.DocumentNumber);
    }

    [Fact]
    public async Task CreateAsync_PersistsPatient_AndReturnsResponse()
    {
        await using var ctx = BuildContext();
        var sut = new PatientService(ctx);
        var request = new CreatePatientRequest
        {
            DocumentType = "CC",
            DocumentNumber = "777",
            FirstName = "N",
            LastName = "M",
            BirthDate = new DateTime(2000, 5, 5)
        };

        var (success, error, patient) = await sut.CreateAsync(request, CancellationToken.None);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(patient);
        Assert.Equal("777", patient!.DocumentNumber);
        Assert.Equal(1, await ctx.Patients.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenDuplicateDocument()
    {
        await using var ctx = BuildContext();
        ctx.Patients.Add(new Patient
        {
            DocumentType = "CC",
            DocumentNumber = "dup",
            FirstName = "A",
            LastName = "B",
            BirthDate = new DateTime(1991, 1, 1)
        });
        await ctx.SaveChangesAsync();

        var sut = new PatientService(ctx);
        var request = new CreatePatientRequest
        {
            DocumentType = "CC",
            DocumentNumber = "dup",
            FirstName = "Other",
            LastName = "Name",
            BirthDate = new DateTime(1992, 1, 1)
        };

        var (success, error, patient) = await sut.CreateAsync(request, CancellationToken.None);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Null(patient);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenIdMissing()
    {
        await using var ctx = BuildContext();
        var sut = new PatientService(ctx);

        var (success, error, patient) = await sut.UpdateAsync(404, new UpdatePatientRequest { FirstName = "X" }, CancellationToken.None);

        Assert.False(success);
        Assert.Equal("Patient not found.", error);
        Assert.Null(patient);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenDocumentBelongsToAnotherPatient()
    {
        await using var ctx = BuildContext();
        ctx.Patients.AddRange(
            new Patient { DocumentType = "CC", DocumentNumber = "A1", FirstName = "A", LastName = "A", BirthDate = DateTime.Today },
            new Patient { DocumentType = "CC", DocumentNumber = "B2", FirstName = "B", LastName = "B", BirthDate = DateTime.Today });
        await ctx.SaveChangesAsync();
        var id1 = ctx.Patients.OrderBy(p => p.PatientId).First().PatientId;

        var sut = new PatientService(ctx);
        var (success, error, patient) = await sut.UpdateAsync(
            id1,
            new UpdatePatientRequest { DocumentNumber = "B2" },
            CancellationToken.None);

        Assert.False(success);
        Assert.Contains("already exists", error ?? "");
        Assert.Null(patient);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_WhenValid()
    {
        await using var ctx = BuildContext();
        ctx.Patients.Add(new Patient
        {
            DocumentType = "CC",
            DocumentNumber = "Z9",
            FirstName = "Old",
            LastName = "Name",
            BirthDate = new DateTime(1980, 1, 1)
        });
        await ctx.SaveChangesAsync();
        var id = ctx.Patients.Single().PatientId;

        var sut = new PatientService(ctx);
        var (success, error, response) = await sut.UpdateAsync(
            id,
            new UpdatePatientRequest { FirstName = "New" },
            CancellationToken.None);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("New", response!.FirstName);
        var reloaded = await ctx.Patients.AsNoTracking().SingleAsync();
        Assert.Equal("New", reloaded.FirstName);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenMissing()
    {
        await using var ctx = BuildContext();
        var sut = new PatientService(ctx);

        var deleted = await sut.DeleteAsync(123, CancellationToken.None);

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemovesRow_WhenExists()
    {
        await using var ctx = BuildContext();
        ctx.Patients.Add(new Patient
        {
            DocumentType = "TI",
            DocumentNumber = "del",
            FirstName = "D",
            LastName = "E",
            BirthDate = DateTime.Today
        });
        await ctx.SaveChangesAsync();
        var id = ctx.Patients.Single().PatientId;

        var sut = new PatientService(ctx);
        var deleted = await sut.DeleteAsync(id, CancellationToken.None);

        Assert.True(deleted);
        Assert.Equal(0, await ctx.Patients.CountAsync());
    }

    private static AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"patients-svc-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}
