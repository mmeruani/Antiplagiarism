using FileStoring.Domain;
using Microsoft.AspNetCore.Http;

namespace FileStoring.Services;

public interface IFileStorageService
{
    Task<Submission> SaveAsync(IFormFile file, string studentId, string studentName, string assignmentId);

    Task<(byte[] Content, string FileName)?> GetFileAsync(string fileId);

    Task<Submission?> GetMetaAsync(string fileId);
}