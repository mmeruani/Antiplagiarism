namespace FileAnalysis.Domain;

public class Report
{
    public string ReportId { get; set; } = default!;
    public string WorkId { get; set; } = default!;
    public string StudentId { get; set; } = default!;
    public string FileId { get; set; } = default!;
    public string ContentHash { get; set; } = default!;
    public bool IsPlagiarism { get; set; }
    public DateTime CreatedAt { get; set; }
}