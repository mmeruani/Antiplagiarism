using FileStoring.Repositories;
using FileStoring.Services;
using FileStoring.Domain;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5001");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
var filesDir = Path.Combine(dataDir, "files");
Directory.CreateDirectory(dataDir);
Directory.CreateDirectory(filesDir);

var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "data/storing.db";
builder.Services.AddSingleton<ISubmissionRepository>(new SqliteSubmissionRepository($"Data Source={dbPath}"));
builder.Services.AddSingleton<IFileStorageService>(sp => new FileStorageService(sp.GetRequiredService<ISubmissionRepository>(), filesDir));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/files",
    async (IFormFile file, string studentId, string studentName, string assignmentId, IFileStorageService storage) =>
    {
        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "Файл не передан" });
        }

        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { message = "Поддерживаются только файлы .txt" });
        }

        var submission = await storage.SaveAsync(file, studentId, studentName, assignmentId);

        return Results.Ok(new
        {
            fileId = submission.FileId,
            workId = submission.WorkId,
            storedAt = submission.UploadedAt
        });
    })
    .DisableAntiforgery()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest);

app.MapGet("/files/{fileId}", async (string fileId, IFileStorageService storage) =>
{
    var file = await storage.GetFileAsync(fileId);
    if (file == null)
    {
        return Results.NotFound();
    }
    return Results.File(file.Value.Content, "text/plain", file.Value.FileName);
});

app.MapGet("/files/{fileId}/meta", async (string fileId, IFileStorageService storage) =>
{
    var s = await storage.GetMetaAsync(fileId);
    if (s == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(new
    {
        s.FileId,
        s.WorkId,
        s.StudentId,
        s.StudentName,
        s.AssignmentId,
        s.UploadedAt,
        s.Path
    });
});

app.Run();
