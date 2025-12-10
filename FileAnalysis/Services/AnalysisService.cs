using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FileAnalysis.Domain;
using FileAnalysis.Repositories;

namespace FileAnalysis.Services;

public class AnalysisService : IAnalysisService
{
    private readonly ITextFetcher _textFetcher;
    private readonly IReportRepository _repository;

    public AnalysisService(ITextFetcher textFetcher, IReportRepository repository)
    {
        _textFetcher = textFetcher;
        _repository = repository;
    }

    public async Task<Report> AnalyseAsync(CreateReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileId) || string.IsNullOrWhiteSpace(request.WorkId) || string.IsNullOrWhiteSpace(request.StudentId))
        {
            throw new ArgumentException("fileId, workId и studentId обязательны");
        }

        var text = await _textFetcher.GetTextAsync(request.FileId);
        var normalized = Normalize(text);
        var hash = ComputeSha256(normalized);

        var plagiarism = await _repository.ExistsSameHashForOtherStudentAsync(hash, request.StudentId);

        var report = new Report
        {
            ReportId = Guid.NewGuid().ToString("N"),
            WorkId = request.WorkId,
            StudentId = request.StudentId,
            FileId = request.FileId,
            ContentHash = hash,
            IsPlagiarism = plagiarism,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(report);
        return report;
    }

    private static string Normalize(string text)
    {
        var t = text.ToLowerInvariant();
        t = Regex.Replace(t, @"\p{P}+", " ");
        t = Regex.Replace(t, @"\s+", " ").Trim();
        return t;
    }

    private static string ComputeSha256(string text)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash);
    }
}