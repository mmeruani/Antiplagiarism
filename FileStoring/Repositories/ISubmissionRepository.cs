using FileStoring.Domain;

namespace FileStoring.Repositories;

public interface ISubmissionRepository
{
    Task SaveAsync(Submission submission);

    Task<Submission?> GetAsync(string fileId);
}