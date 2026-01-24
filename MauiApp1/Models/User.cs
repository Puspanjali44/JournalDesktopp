using SQLite;

namespace MauiApp1.Models;

public class User
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique]
    public string Email { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Pin { get; set; } = "";
}
