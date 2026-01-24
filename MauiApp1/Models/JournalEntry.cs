using SQLite;

namespace MauiApp1.Models;

public class JournalEntry
{
    [PrimaryKey]
    public string EntryDateKey { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string Title { get; set; } = "";
    public string ContentHtml { get; set; } = "";

    // Feature 3
    public string PrimaryMood { get; set; } = "";

    // Stored as CSV for simplicity
    public string SecondaryMoodsCsv { get; set; } = "";

    public string TagsCsv { get; set; } = "";
}
