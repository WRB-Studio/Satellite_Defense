using System;
using System.IO;
using System.Text;

public interface ISaveStore
{
    SaveLoadResult Load();
    void Write(string document);
}

public sealed class SaveLoadResult
{
    public Savegame Data { get; }
    public bool NeedsSave { get; }
    public bool CanWrite { get; }
    public string Message { get; }

    public SaveLoadResult(Savegame data, bool needsSave = false, bool canWrite = true, string message = null)
    {
        Data = data;
        NeedsSave = needsSave;
        CanWrite = canWrite;
        Message = message;
    }
}

public sealed class FileSaveStore : ISaveStore
{
    public string SavePath { get; }
    public string BackupPath { get; }
    public string TemporaryPath => SavePath + ".tmp";

    private readonly object ioLock = new();
    private string lastCommittedDocument;
    private bool canWrite;

    public FileSaveStore(string directory)
    {
        string fullPath = Path.GetFullPath(directory);
        SavePath = Path.Combine(fullPath, "savegame.json");
        BackupPath = Path.Combine(fullPath, "savegame.backup.json");
    }

    public SaveLoadResult Load()
    {
        lock (ioLock)
        {
            canWrite = false;
            lastCommittedDocument = null;
            try
            {
                string primary = ReadIfPresent(SavePath);
                if (SaveFileCodec.TryDecode(primary, out var data, out bool future))
                {
                    canWrite = true;
                    lastCommittedDocument = primary;
                    return new SaveLoadResult(data);
                }
                if (future) return Blocked("This save requires a newer game version. Please update the game.");

                string backup = ReadIfPresent(BackupPath);
                if (SaveFileCodec.TryDecode(backup, out data, out future))
                {
                    canWrite = true;
                    lastCommittedDocument = backup;
                    return new SaveLoadResult(data, needsSave: true,
                        message: "The last save could not be loaded. Your backup was restored.");
                }
                if (future) return Blocked("This backup requires a newer game version. Please update the game.");
                if (primary != null || backup != null)
                    return Blocked("Your save and backup could not be read. They have been kept unchanged.");

                // A temporary file is an uncommitted write, possibly an unfinished purchase.
                // Never promote it on load, even when no primary file exists yet.
                canWrite = true;
                return new SaveLoadResult(new Savegame(), needsSave: true);
            }
            catch (Exception error) when (IsStorageError(error))
            {
                return Blocked("Save storage is unavailable. Check free space and restart the game.");
            }
        }
    }

    private static SaveLoadResult Blocked(string message) =>
        new(new Savegame(), canWrite: false, message: message);

    private static string ReadIfPresent(string path)
    {
        try { return File.ReadAllText(path, Encoding.UTF8); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    public void Write(string document)
    {
        lock (ioLock)
        {
            if (!canWrite) throw new IOException("The save store has not been loaded successfully.");
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            WriteVerified(TemporaryPath, document);

            if (lastCommittedDocument != null)
            {
                string backupTemporary = BackupPath + ".tmp";
                WriteVerified(backupTemporary, lastCommittedDocument);
                // Even if a platform cannot replace atomically, the primary remains intact here.
                ReplaceFile(backupTemporary, BackupPath);
            }

            // Backup contains the last committed snapshot before primary replacement starts.
            ReplaceFile(TemporaryPath, SavePath);
            lastCommittedDocument = document;
        }
    }

    private static void WriteVerified(string path, string document)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(document);
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(flushToDisk: true);
        }
        if (!string.Equals(File.ReadAllText(path, Encoding.UTF8), document, StringComparison.Ordinal))
            throw new IOException("Save verification failed.");
    }

    private static void ReplaceFile(string temporary, string destination)
    {
        if (!File.Exists(destination))
        {
            File.Move(temporary, destination);
            return;
        }
        try { File.Replace(temporary, destination, null); }
        catch (PlatformNotSupportedException)
        {
            // Recoverable fallback: the other committed file is retained throughout.
            File.Delete(destination);
            File.Move(temporary, destination);
        }
    }

    public static bool IsStorageError(Exception error) =>
        error is IOException || error is UnauthorizedAccessException ||
        error is System.Security.SecurityException || error is NotSupportedException;
}
