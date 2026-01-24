using SQLite;

namespace MauiApp1.Models;

public class UserPin
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string PinHash { get; set; } = "";
}
