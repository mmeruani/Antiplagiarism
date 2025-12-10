using FileStoring.Domain;
using FileStoring.Repositories;
using Microsoft.AspNetCore.Http;

namespace FileStoring.Services;

public class FileStorageService : IFileStorageService
{
    private readonly ISubmissionRepository _repository;
    private readonly string _filesRoot;

    public FileStorageService(ISubmissionRepository repository, string filesRoot)
    {
        _repository = repository;
        _filesRoot = filesRoot;
        Directory.CreateDirectory(_filesRoot);
    }

    public async Task<Submission> SaveAsync(IFormFile file, string studentId, string studentName, string assignmentId)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Файл не передан или пустой", nameof(file));
        }

        if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(assignmentId))
        {
            throw new ArgumentException("studentId и assignmentId обязательны");
        }

        var workId = assignmentId;
        var fileId = Guid.NewGuid().ToString("N");
        var uploadedAt = DateTime.UtcNow;

        var storedFileName = $"{fileId}_{file.FileName}";
        var filePath = Path.Combine(_filesRoot, storedFileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var submission = new Submission
        {
            FileId = fileId,
            WorkId = workId,
            StudentId = studentId,
            StudentName = studentName,
            AssignmentId = assignmentId,
            UploadedAt = uploadedAt,
            Path = filePath
        };

        await _repository.SaveAsync(submission);
        return submission;
    }

    public async Task<(byte[] Content, string FileName)?> GetFileAsync(string fileId)
    {
        var submission = await _repository.GetAsync(fileId);
        if (submission == null || !File.Exists(submission.Path))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(submission.Path);
        var name = Path.GetFileName(submission.Path).Split('_', 2).LastOrDefault() ?? "file.bin";

        return (bytes, name);
    }

    public Task<Submission?> GetMetaAsync(string fileId)
    {
        return _repository.GetAsync(fileId);
    }
}
