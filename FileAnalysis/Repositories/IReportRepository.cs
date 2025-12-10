using FileAnalysis.Domain;

namespace FileAnalysis.Repositories;

public interface IReportRepository
{
    Task SaveAsync(Report report);

    Task<Report?> GetAsync(string reportId);

    Task<IReadOnlyList<Report>> GetByWorkAsync(string workId);

    Task<bool> ExistsSameHashForOtherStudentAsync(string contentHash, string studentId);
}