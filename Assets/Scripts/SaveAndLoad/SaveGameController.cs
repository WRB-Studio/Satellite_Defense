using System;
using System.IO;
using UnityEngine;

public static class SaveGameController
{
    public static IReadOnlySavegame Data => session != null ? session : defaults;
    public static bool CanSave => session != null && session.CanSave;
    public static bool HasPendingChanges => session != null && session.HasPendingChanges;
    public static string LastError => session?.LastError;
    public static string LoadMessage => session?.LoadMessage;
    public static event Action Changed;
    public static event Action<string> SaveFailed;

    private static readonly Savegame defaults = new();
    private static SaveSession session;
    private static string directoryOverride;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        session?.Dispose();
        session = null;
        directoryOverride = null;
        Changed = null;
        SaveFailed = null;
    }

    public static void Load()
    {
        session?.Dispose();
        string directory = directoryOverride ?? Path.Combine(Application.persistentDataPath, "Saves");
        session = new SaveSession(new FileSaveStore(directory));
        session.Changed += () => Changed?.Invoke();
        session.SaveFailed += message => SaveFailed?.Invoke(message);
        session.Load();
    }

    public static void EnsureLoaded()
    {
        if (session == null) Load();
    }

    public static void Tick(float unscaledDeltaTime) => session?.Tick(unscaledDeltaTime);
    public static bool Save() => session != null && session.Flush();
    public static bool TryPurchase(IngameEntity item) => session != null && session.TryPurchase(item);
    public static bool Select(IngameEntity item) => session != null && session.Select(item);
    public static long CreditCoins(long amount) => session?.CreditCoins(amount) ?? 0;
    public static bool RecordRound(long score, long coins) => session != null && session.RecordRound(score, coins);
    public static Savegame GetSnapshot() => session?.Snapshot() ?? new Savegame();

    public static IngameEntity ResolveActiveItem(IngameEntity.eEntityType category, GameObject[] prefabs)
    {
        IngameEntity fallback = null;
        foreach (var prefab in prefabs)
        {
            if (prefab == null || !prefab.TryGetComponent<IngameEntity>(out var item)) continue;
            if (item.entityType != category || item.id <= 0) continue;
            if (fallback == null) fallback = item;
            if (item.id == Data.GetActiveId(category) && Data.IsUnlocked(category, item.id)) return item;
        }
        if (fallback == null) throw new InvalidOperationException($"No valid shop items for {category}.");
        session?.UseFallback(fallback);
        return fallback;
    }

#if UNITY_EDITOR
    public static bool SetCoinsForEditor(long amount)
    {
        EnsureLoaded();
        return session.SetCoinsForEditor(amount);
    }

    public static void UnloadForEditor()
    {
        session?.Dispose();
        session = null;
        directoryOverride = null;
    }

    public static void UseStorageDirectoryForValidation(string directory)
    {
        session?.Dispose();
        session = null;
        directoryOverride = Path.GetFullPath(directory);
    }

    public static void CloseStorageForValidation()
    {
        session?.Dispose();
        session = null;
        directoryOverride = null;
    }
#endif
}
