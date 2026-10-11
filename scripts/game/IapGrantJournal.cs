#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace IdleAi;

/// <summary>
/// Write-ahead recovery for the two-file reward-save/token-ledger boundary.
/// Main save and ledger formats stay unchanged; recovery runs before gameplay loads.
/// </summary>
public sealed class IapGrantJournal
{
    private sealed record Entry(string Token, string BeforeHash, SaveGameData RewardSave, string SavedHash = "");
    private static readonly JsonSerializerOptions SaveOptions = new() { WriteIndented = true };
    private readonly string _path;
    private readonly string _savePath;
    public IapGrantJournal(string path, string savePath) { _path = path; _savePath = savePath; }
    public bool Exists => File.Exists(_path);
    private string SaveHash() => File.Exists(_savePath) ? Hash(File.ReadAllBytes(_savePath)) : "missing";
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string RewardHash(Entry entry) => Hash(JsonSerializer.SerializeToUtf8Bytes(entry.RewardSave, SaveOptions));
    private Entry Read()
    {
        var entry = JsonSerializer.Deserialize<Entry>(File.ReadAllText(_path));
        if (entry == null || string.IsNullOrWhiteSpace(entry.Token) || entry.RewardSave == null ||
            (entry.BeforeHash != "missing" && (entry.BeforeHash == null || entry.BeforeHash.Length != 64 ||
            !System.Linq.Enumerable.All(entry.BeforeHash, Uri.IsHexDigit))))
            throw new InvalidDataException("Invalid purchase recovery record.");
        return entry;
    }
    private void Write(Entry entry)
    {
        using (var stream = new FileStream(_path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, entry);
            stream.Flush(true);
        }
        File.Move(_path + ".tmp", _path, true);
    }
    public bool Prepare(string token, SaveGameData rewardSave)
    {
        try
        {
            if (Exists) return Read().Token == token;
            Write(new Entry(token, SaveHash(), rewardSave));
            return true;
        }
        catch (Exception) { return false; }
    }
    /// <summary>Persist the exact next main-save snapshot before it can replace the active file.</summary>
    public bool BeforeGameSave(SaveGameData snapshot)
    {
        if (!Exists) return true;
        try
        {
            var entry = Read();
            string current = SaveHash();
            bool saved = current == RewardHash(entry) || (entry.SavedHash != "" && current == entry.SavedHash);
            if (!saved && current != entry.BeforeHash) return false;
            Write(entry with { RewardSave = snapshot, SavedHash = saved ? current : entry.SavedHash });
            return true;
        }
        catch (Exception) { return false; }
    }
    public bool ConfirmSaved()
    {
        if (!Exists) return true;
        try
        {
            var entry = Read();
            string current = SaveHash();
            if (current != RewardHash(entry)) return entry.SavedHash != "" && current == entry.SavedHash;
            Write(entry with { SavedHash = current });
            return true;
        }
        catch (Exception) { return false; }
    }
    public bool Recover(Func<SaveGameData, bool> save, Func<string, bool> processed, Func<string, bool> complete)
    {
        if (!Exists) return true;
        try
        {
            var entry = Read();
            if (!processed(entry.Token))
            {
                string current = SaveHash();
                if (current == entry.BeforeHash)
                { if (!save(entry.RewardSave)) return false; }
                else if (current != RewardHash(entry) && current != entry.SavedHash)
                    return false; // Ambiguous/corrupt files fail closed.
                if (!complete(entry.Token)) return false;
            }
            File.Delete(_path);
            return true;
        }
        catch (Exception) { return false; }
    }
    public bool Clear()
    {
        try { File.Delete(_path); return true; }
        catch (Exception) { return false; }
    }
}
