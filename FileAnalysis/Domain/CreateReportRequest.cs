namespace FileAnalysis.Domain;

public class CreateReportRequest
{
    public string FileId { get; set; } = default!;
    public string WorkId { get; set; } = default!;
    public string StudentId { get; set; } = default!;
}