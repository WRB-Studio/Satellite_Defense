using System;
using System.Threading.Tasks;

public sealed class SaveSession : IReadOnlySavegame, IDisposable
{
    public const float AutosaveInterval = 5f;
    public long BestScore => data.bestScore;
    public long Coins => data.premiumCoins;
    public bool CanSave { get; private set; }
    public bool HasPendingChanges => revision != savedRevision;
    public bool IsWriting => pendingWrite != null;
    public string LastError { get; private set; }
    public string LoadMessage { get; private set; }
    public event Action Changed;
    public event Action<string> SaveFailed;

    private readonly ISaveStore store;
    private Savegame data = new();
    private long revision;
    private long savedRevision;
    private long pendingRevision;
    private float elapsed;
    private Task pendingWrite;

    public SaveSession(ISaveStore store) => this.store = store ?? throw new ArgumentNullException(nameof(store));

    public void Load()
    {
        CompletePendingWrite(wait: true);
        var result = store.Load();
        data = result.Data;
        CanSave = result.CanWrite;
        LoadMessage = result.Message;
        LastError = CanSave ? null : result.Message;
        revision = result.NeedsSave ? 1 : 0;
        savedRevision = 0;
        elapsed = 0f;
        Changed?.Invoke();
    }

    public Savegame Snapshot() => data.Clone();
    public bool IsUnlocked(IngameEntity.eEntityType category, int id) => data.IsUnlocked(category, id);
    public int GetLevel(IngameEntity.eEntityType category, int id, int maxLevel) => data.GetLevel(category, id, maxLevel);
    public int GetActiveId(IngameEntity.eEntityType category) => data.GetActiveId(category);

    public void Tick(float unscaledDeltaTime)
    {
        CompletePendingWrite(wait: false);
        if (!CanSave || !HasPendingChanges) return;
        elapsed += Math.Max(0f, unscaledDeltaTime);
        if (pendingWrite != null || elapsed < AutosaveInterval) return;
        elapsed = 0f;
        string document = SaveFileCodec.Encode(data);
        pendingRevision = revision;
        // JSON creation and gameplay callbacks stay on the main thread; only file I/O runs here.
        pendingWrite = Task.Run(() => store.Write(document));
    }

    public bool Flush()
    {
        CompletePendingWrite(wait: true);
        if (!CanSave) return false;
        if (!HasPendingChanges) return true;
        if (!WriteNow(data)) return false;
        savedRevision = revision;
        elapsed = 0f;
        return true;
    }

    public bool TryPurchase(IngameEntity item)
    {
        if (!CanSave) return false;
        CompletePendingWrite(wait: true);
        var candidate = data.Clone();
        if (!candidate.TryPurchase(item) || !WriteNow(candidate, "Purchase could not be saved. No coins were spent.")) return false;
        Commit(candidate);
        return true;
    }

    public bool Select(IngameEntity item)
    {
        if (!CanSave || item == null || !IsUnlocked(item.entityType, item.id)) return false;
        if (GetActiveId(item.entityType) == item.id) return Flush();
        CompletePendingWrite(wait: true);
        var candidate = data.Clone();
        if (!candidate.Select(item.entityType, item.id) || !WriteNow(candidate, "Selection could not be saved. Please try again.")) return false;
        Commit(candidate);
        return true;
    }

    private void Commit(Savegame candidate)
    {
        data = candidate;
        revision++;
        savedRevision = revision;
        elapsed = 0f;
        Changed?.Invoke();
    }

    public long CreditCoins(long amount)
    {
        if (!CanSave) return 0;
        long credited = data.CreditCoins(amount);
        if (credited > 0) MarkChanged();
        return credited;
    }

#if UNITY_EDITOR
    public bool SetCoinsForEditor(long amount)
    {
        if (!CanSave) return false;
        CompletePendingWrite(wait: true);
        var candidate = data.Clone();
        candidate.premiumCoins = Math.Max(0L, amount);
        if (!WriteNow(candidate, "Coin balance could not be saved. No coins were changed.")) return false;
        Commit(candidate);
        return true;
    }
#endif

    public bool RecordRound(long score, long coins)
    {
        if (!CanSave) return false;
        long previousBest = data.bestScore;
        data.bestScore = Math.Max(previousBest, score);
        long credited = data.CreditCoins(coins);
        if (previousBest != data.bestScore || credited > 0) MarkChanged();
        return Flush();
    }

    public void UseFallback(IngameEntity item)
    {
        if (!CanSave || (IsUnlocked(item.entityType, item.id) && GetActiveId(item.entityType) == item.id)) return;
        if (!IsUnlocked(item.entityType, item.id)) data.GetUnlockedIds(item.entityType).Add(item.id);
        data.Select(item.entityType, item.id);
        MarkChanged();
    }

    private void MarkChanged()
    {
        if (!HasPendingChanges) elapsed = 0f;
        revision++;
        Changed?.Invoke();
    }

    private bool WriteNow(Savegame candidate, string errorMessage = null)
    {
        try
        {
            store.Write(SaveFileCodec.Encode(candidate));
            LastError = null;
            return true;
        }
        catch (Exception error) when (FileSaveStore.IsStorageError(error))
        {
            ReportFailure(errorMessage);
            return false;
        }
    }

    private void CompletePendingWrite(bool wait)
    {
        if (pendingWrite == null || (!wait && !pendingWrite.IsCompleted)) return;
        try
        {
            pendingWrite.GetAwaiter().GetResult();
            savedRevision = pendingRevision;
            LastError = null;
        }
        catch (Exception error) when (FileSaveStore.IsStorageError(error))
        {
            ReportFailure();
        }
        finally { pendingWrite = null; }
    }

    private void ReportFailure(string message = null)
    {
        message ??= "Progress could not be saved. Check free space; saving will be retried.";
        bool changed = LastError != message;
        LastError = message;
        if (changed) SaveFailed?.Invoke(message);
    }

    public void Dispose()
    {
        Flush();
        Changed = null;
        SaveFailed = null;
    }
}
