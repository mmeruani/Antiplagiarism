using FileAnalysis.Domain;
using Microsoft.Data.Sqlite;

namespace FileAnalysis.Repositories;

public class SqliteReportRepository : IReportRepository
{
    private readonly string _connectionString;

    public SqliteReportRepository(string connectionString)
    {
        _connectionString = connectionString;
        Initialize();
    }

    private void Initialize()
    {
        using var con = new SqliteConnection(_connectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS reports(
              reportId TEXT PRIMARY KEY,
              workId TEXT NOT NULL,
              studentId TEXT NOT NULL,
              fileId TEXT NOT NULL,
              contentHash TEXT NOT NULL,
              plagiarism INTEGER NOT NULL,
              createdAt TEXT NOT NULL
            );
        """;
        cmd.ExecuteNonQuery();
    }

    public async Task SaveAsync(Report r)
    {
        await using var con = new SqliteConnection(_connectionString);
        await con.OpenAsync();

        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            INSERT INTO reports(
              reportId, workId, studentId, fileId,
              contentHash, plagiarism, createdAt
            )
            VALUES ($r,$w,$sid,$f,$h,$p,$ts);
        """;

        cmd.Parameters.AddWithValue("$r", r.ReportId);
        cmd.Parameters.AddWithValue("$w", r.WorkId);
        cmd.Parameters.AddWithValue("$sid", r.StudentId);
        cmd.Parameters.AddWithValue("$f", r.FileId);
        cmd.Parameters.AddWithValue("$h", r.ContentHash);
        cmd.Parameters.AddWithValue("$p", r.IsPlagiarism ? 1 : 0);
        cmd.Parameters.AddWithValue("$ts", r.CreatedAt.ToString("O"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Report?> GetAsync(string reportId)
    {
        await using var con = new SqliteConnection(_connectionString);
        await con.OpenAsync();

        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT reportId, workId, studentId, fileId,
                   contentHash, plagiarism, createdAt
            FROM reports
            WHERE reportId = $r
            LIMIT 1;
        """;
        cmd.Parameters.AddWithValue("$r", reportId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new Report
        {
            ReportId = reader.GetString(0),
            WorkId = reader.GetString(1),
            StudentId = reader.GetString(2),
            FileId = reader.GetString(3),
            ContentHash = reader.GetString(4),
            IsPlagiarism = reader.GetInt32(5) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(6))
        };
    }

    public async Task<IReadOnlyList<Report>> GetByWorkAsync(string workId)
    {
        var result = new List<Report>();

        await using var con = new SqliteConnection(_connectionString);
        await con.OpenAsync();

        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT reportId, workId, studentId, fileId, contentHash, plagiarism, createdAt
            FROM reports
            WHERE workId = $w
            ORDER BY createdAt;
        """;
        cmd.Parameters.AddWithValue("$w", workId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Report
            {
                ReportId = reader.GetString(0),
                WorkId = reader.GetString(1),
                StudentId = reader.GetString(2),
                FileId = reader.GetString(3),
                ContentHash = reader.GetString(4),
                IsPlagiarism = reader.GetInt32(5) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind)
            });
        }
        return result;
    }

    public async Task<bool> ExistsSameHashForOtherStudentAsync(string contentHash, string studentId)
    {
        await using var con = new SqliteConnection(_connectionString);
        await con.OpenAsync();

        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM reports
            WHERE contentHash = $h AND studentId <> $sid;
        """;
        cmd.Parameters.AddWithValue("$h", contentHash);
        cmd.Parameters.AddWithValue("$sid", studentId);

        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        return count > 0;
    }
}
