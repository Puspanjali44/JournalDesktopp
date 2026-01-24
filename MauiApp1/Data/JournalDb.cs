using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MauiApp1.Models;
using SQLite;

namespace MauiApp1.Data;

public class JournalDb
{
    private SQLiteAsyncConnection? _db;

    /* =============================
       INITIALIZATION
    ============================= */

    private async Task InitAsync()
    {
        if (_db != null)
            return;

        var dbPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "journal.db3"
        );

        _db = new SQLiteAsyncConnection(dbPath);

        // Tables
        await _db.CreateTableAsync<JournalEntry>();
        await _db.CreateTableAsync<UserPin>();
    }

    private static string DateKey(DateTime date)
        => date.ToString("yyyy-MM-dd");

    /* =============================
       PIN SECURITY (FEATURE 9)
    ============================= */

    public async Task<bool> HasPinAsync()
    {
        await InitAsync();
        return await _db!
            .Table<UserPin>()
            .CountAsync() > 0;
    }

    public async Task SetPinAsync(string pin)
    {
        await InitAsync();

        // Only ONE PIN allowed
        await _db!.DeleteAllAsync<UserPin>();

        var record = new UserPin
        {
            PinHash = BCrypt.Net.BCrypt.HashPassword(pin)
        };

        await _db.InsertAsync(record);
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        await InitAsync();

        var record = await _db!
            .Table<UserPin>()
            .FirstOrDefaultAsync();

        if (record == null)
            return false;

        return BCrypt.Net.BCrypt.Verify(pin, record.PinHash);
    }

    /* =============================
       READ (BASIC)
    ============================= */

    public async Task<JournalEntry?> GetByDateAsync(DateTime date)
    {
        await InitAsync();
        var key = DateKey(date);

        return await _db!
            .Table<JournalEntry>()
            .Where(e => e.EntryDateKey == key)
            .FirstOrDefaultAsync();
    }

    public async Task<List<JournalEntry>> GetLatestAsync(int take = 20)
    {
        await InitAsync();

        return await _db!
            .Table<JournalEntry>()
            .OrderByDescending(e => e.EntryDateKey)
            .Take(take)
            .ToListAsync();
    }

    /* =============================
       PAGINATION (FEATURE 6)
    ============================= */

    public async Task<List<JournalEntry>> GetEntriesPagedAsync(int skip, int take)
    {
        await InitAsync();

        return await _db!
            .Table<JournalEntry>()
            .OrderByDescending(e => e.EntryDateKey)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        await InitAsync();

        return await _db!
            .Table<JournalEntry>()
            .CountAsync();
    }

    /* =============================
       SEARCH + FILTER (FEATURE 7)
    ============================= */

    public async Task<List<JournalEntry>> SearchPagedAsync(
        string? search,
        string? mood,
        string? tag,
        int skip,
        int take)
    {
        await InitAsync();

        var query = _db!.Table<JournalEntry>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.Title.Contains(search) ||
                e.ContentHtml.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(mood))
        {
            query = query.Where(e => e.PrimaryMood == mood);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(e =>
                e.TagsCsv != null && e.TagsCsv.Contains(tag));
        }

        return await query
            .OrderByDescending(e => e.EntryDateKey)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> SearchCountAsync(
        string? search,
        string? mood,
        string? tag)
    {
        await InitAsync();

        var query = _db!.Table<JournalEntry>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.Title.Contains(search) ||
                e.ContentHtml.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(mood))
        {
            query = query.Where(e => e.PrimaryMood == mood);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(e =>
                e.TagsCsv != null && e.TagsCsv.Contains(tag));
        }

        return await query.CountAsync();
    }

    /* =============================
       STREAK TRACKING (FEATURE 8)
    ============================= */

    public async Task<List<DateTime>> GetAllEntryDatesAsync()
    {
        await InitAsync();

        var entries = await _db!
            .Table<JournalEntry>()
            .OrderBy(e => e.EntryDateKey)
            .ToListAsync();

        return entries
            .Select(e => DateTime.Parse(e.EntryDateKey))
            .ToList();
    }

    public async Task<(int current, int longest, int missed)> GetStreakStatsAsync()
    {
        var dates = await GetAllEntryDatesAsync();

        if (!dates.Any())
            return (0, 0, 0);

        dates = dates.Distinct().OrderBy(d => d).ToList();

        int longest = 1;
        int tempStreak = 1;

        for (int i = 1; i < dates.Count; i++)
        {
            if ((dates[i] - dates[i - 1]).Days == 1)
            {
                tempStreak++;
                longest = Math.Max(longest, tempStreak);
            }
            else
            {
                tempStreak = 1;
            }
        }

        DateTime today = DateTime.Today;
        int current = dates.Contains(today) ? 1 : 0;

        for (int i = dates.Count - 1; i > 0 && current > 0; i--)
        {
            if ((dates[i] - dates[i - 1]).Days == 1)
                current++;
            else
                break;
        }

        int totalDays = (today - dates.First()).Days + 1;
        int missed = totalDays - dates.Count;

        return (current, longest, missed);
    }

    /* =============================
       CREATE / UPDATE (UPSERT)
    ============================= */

    public async Task<int> UpsertAsync(
        DateTime date,
        string title,
        string contentHtml,
        string primaryMood,
        List<string> secondaryMoods,
        List<string> tags)
    {
        await InitAsync();

        var key = DateKey(date);
        var now = DateTime.Now;

        var secCsv = string.Join(",", secondaryMoods);
        var tagCsv = string.Join(",", tags);

        var existing = await GetByDateAsync(date);

        if (existing == null)
        {
            var entry = new JournalEntry
            {
                EntryDateKey = key,
                CreatedAt = now,
                UpdatedAt = now,
                Title = title ?? "",
                ContentHtml = contentHtml ?? "",
                PrimaryMood = primaryMood ?? "",
                SecondaryMoodsCsv = secCsv,
                TagsCsv = tagCsv
            };

            return await _db!.InsertAsync(entry);
        }
        else
        {
            existing.UpdatedAt = now;
            existing.Title = title ?? "";
            existing.ContentHtml = contentHtml ?? "";
            existing.PrimaryMood = primaryMood ?? "";
            existing.SecondaryMoodsCsv = secCsv;
            existing.TagsCsv = tagCsv;

            return await _db!.UpdateAsync(existing);
        }
    }

    /* =============================
       DELETE
    ============================= */

    public async Task<int> DeleteByDateAsync(DateTime date)
    {
        await InitAsync();

        var existing = await GetByDateAsync(date);
        if (existing == null)
            return 0;

        return await _db!.DeleteAsync(existing);
    }
    public async Task<List<JournalEntry>> GetEntriesByDateRangeAsync(DateTime from, DateTime to)
    {
        await InitAsync();

        var fromKey = from.ToString("yyyy-MM-dd");
        var toKey = to.ToString("yyyy-MM-dd");

        return await _db!
            .Table<JournalEntry>()
            .Where(e => e.EntryDateKey.CompareTo(fromKey) >= 0 && e.EntryDateKey.CompareTo(toKey) <= 0)
            .OrderBy(e => e.EntryDateKey)
            .ToListAsync();
    }

}
