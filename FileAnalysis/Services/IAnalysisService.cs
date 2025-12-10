using FileAnalysis.Domain;

namespace FileAnalysis.Services;

public interface IAnalysisService
{
    Task<Report> AnalyseAsync(CreateReportRequest request);
}