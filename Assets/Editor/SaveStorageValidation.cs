using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class SaveStorageValidation
{
    private const string RestartDirectory = "Logs/SaveRestartCheck";

    public static void PrepareRestartCheck()
    {
        var store = new FileSaveStore(Path.GetFullPath(RestartDirectory));
        if (!store.Load().CanWrite) throw new IOException("Restart fixture could not be initialized.");
        var data = new Savegame { premiumCoins = 10, bestScore = 100, activeWeaponID = 2 };
        data.unlockedWeaponIDs.Add(2);
        data.weaponLevels.Add(new EntityLevelEntry(2, 2));
        store.Write(SaveFileCodec.Encode(data));
        data.premiumCoins = 20;
        data.bestScore = 200;
        data.weaponLevels[0].level = 3;
        store.Write(SaveFileCodec.Encode(data));
        File.WriteAllText(store.TemporaryPath, SaveFileCodec.Encode(new Savegame { premiumCoins = 999 }));
        File.WriteAllText(Path.Combine(RestartDirectory, "writer-process.txt"), System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
    }

    // Run in a separate Unity invocation after ProjectValidation.RunBatch has exited.
    public static void VerifyRestartCheck()
    {
        int writer = int.Parse(File.ReadAllText(Path.Combine(RestartDirectory, "writer-process.txt")));
        int reader = System.Diagnostics.Process.GetCurrentProcess().Id;
        if (writer == reader) throw new InvalidOperationException("Restart check requires a different process.");
        using var session = new SaveSession(new FileSaveStore(Path.GetFullPath(RestartDirectory)));
        session.Load();
        if (!session.CanSave || session.Coins != 20 || session.BestScore != 200 ||
            session.GetActiveId(IngameEntity.eEntityType.Weapon) != 2 || session.GetLevel(IngameEntity.eEntityType.Weapon, 2, 3) != 3)
            throw new InvalidOperationException("Committed progress did not survive the process restart.");
        File.WriteAllText("Logs/Save-restart-result.txt", $"PASS: save loaded in a new Unity process (writer {writer}, reader {reader}); coins, score, selection, upgrade and interrupted-write handling verified.\n");
        string restartPath = Path.GetFullPath(RestartDirectory);
        if (!restartPath.StartsWith(Path.GetFullPath("Logs") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refusing to remove a restart fixture outside Logs.");
        Directory.Delete(restartPath, true);
    }

    public static void Run(Action<bool, string> require)
    {
        string root = Path.Combine(Path.GetFullPath("Logs"), "SaveTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        bool completed = false;
        try
        {
            CheckRecovery(Path.Combine(root, "recovery"), require);
            CheckTransactions(Path.Combine(root, "transactions"), require);
            CheckBlockedStorage(Path.Combine(root, "blocked"), require);
            CheckFutureBackup(Path.Combine(root, "future-backup"), require);
            completed = true;
        }
        catch (Exception error)
        {
            throw new InvalidOperationException("Storage check failed. Isolated files retained at " + root, error);
        }
        finally
        {
            string logs = Path.GetFullPath("Logs") + Path.DirectorySeparatorChar;
            string resolved = Path.GetFullPath(root);
            if (!resolved.StartsWith(logs, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("SaveTests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to clean a non-test storage directory.");
            if (completed && Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
    }

    private static void CheckRecovery(string directory, Action<bool, string> require)
    {
        var store = new FileSaveStore(directory);
        require(store.Load().NeedsSave, "A new installation needs an initial file.");
        var first = new Savegame { premiumCoins = 20, bestScore = 100 };
        string firstDocument = SaveFileCodec.Encode(first);
        store.Write(firstDocument);
        var second = first.Clone();
        second.premiumCoins = 30;
        store.Write(SaveFileCodec.Encode(second));
        require(new FileSaveStore(directory).Load().Data.premiumCoins == 30, "A fresh store must read the committed primary.");
        require(File.ReadAllText(store.BackupPath) == firstDocument, "Backup must contain the previous committed save.");

        File.WriteAllText(store.TemporaryPath, SaveFileCodec.Encode(new Savegame { premiumCoins = 999 }));
        require(new FileSaveStore(directory).Load().Data.premiumCoins == 30, "Uncommitted temporary writes must never override the primary.");
        File.WriteAllText(store.SavePath, "{truncated");
        var recoveredStore = new FileSaveStore(directory);
        var recovered = recoveredStore.Load();
        require(recovered.CanWrite && recovered.NeedsSave && recovered.Data.premiumCoins == 20, "A damaged primary must recover its valid backup.");
        recoveredStore.Write(SaveFileCodec.Encode(recovered.Data));
        require(File.ReadAllText(store.BackupPath) == firstDocument, "Repair must never rotate a damaged primary into the backup.");
        require(new FileSaveStore(directory).Load().Data.premiumCoins == 20, "Repaired save must survive reopening.");

        File.Delete(store.SavePath);
        require(new FileSaveStore(directory).Load().Data.premiumCoins == 20, "Interrupted primary replacement must recover the committed backup.");
        File.WriteAllText(store.BackupPath, "{}");
        require(!new FileSaveStore(directory).Load().CanWrite, "A structurally invalid backup must not be accepted as an empty save.");
        File.WriteAllText(store.SavePath, firstDocument.Replace("\\\"premiumCoins\\\":20", "\\\"premiumCoins\\\":21"));
        require(!SaveFileCodec.TryDecode(File.ReadAllText(store.SavePath), out _, out _), "A valid-JSON value change must fail its checksum.");
        require(!new FileSaveStore(directory).Load().CanWrite, "Two invalid files must block writing instead of resetting progress.");

        string futureDocument = firstDocument.Replace("\"version\":1", "\"version\":999");
        File.WriteAllText(store.SavePath, futureDocument);
        File.WriteAllText(store.BackupPath, firstDocument);
        var futureStore = new FileSaveStore(directory);
        require(!futureStore.Load().CanWrite, "A newer primary must not be downgraded to an older backup.");
        bool rejected = false;
        try { futureStore.Write(firstDocument); }
        catch (IOException) { rejected = true; }
        require(rejected && File.ReadAllText(store.SavePath) == futureDocument, "Unsupported saves must remain untouched.");
        File.Delete(store.SavePath);
        File.Delete(store.BackupPath);
        require(new FileSaveStore(directory).Load().Data.premiumCoins == 0, "An orphan temporary purchase must not become committed after restart.");
    }

    private static void CheckTransactions(string directory, Action<bool, string> require)
    {
        using var fixtureScene = new ProjectDataValidation.FixtureScene();
        var disk = new FileSaveStore(directory);
        using var controlled = new ControlledStore(disk);
        using var session = new SaveSession(controlled);
        session.Load();
        require(session.Flush(), "Initial save must succeed.");
        int writes = controlled.Writes;
        for (int i = 0; i < 20; i++) session.CreditCoins(1);
        require(controlled.Writes == writes && session.Coins == 20 && session.HasPendingChanges, "Coin pickups must update memory without individual disk writes.");
        require(new FileSaveStore(directory).Load().Data.premiumCoins == 0, "Buffered coins must not pretend to be persisted.");
        var snapshot = session.Snapshot();
        snapshot.premiumCoins = 9999;
        snapshot.unlockedWeaponIDs.Add(99);
        require(session.Coins == 20 && !session.IsUnlocked(IngameEntity.eEntityType.Weapon, 99), "External snapshots must not modify live progress.");

        controlled.Started.Reset();
        controlled.Release.Reset();
        session.Tick(SaveSession.AutosaveInterval);
        require(controlled.Started.Wait(5000), "Autosave must start on the worker.");
        require(controlled.LastThreadId != Thread.CurrentThread.ManagedThreadId, "Periodic file writes must run outside the game thread.");
        session.CreditCoins(5);
        controlled.Release.Set();
        WaitForWrite(session, require);
        require(session.HasPendingChanges && new FileSaveStore(directory).Load().Data.premiumCoins == 20, "Coins collected during a write must remain pending.");
        require(session.Flush() && new FileSaveStore(directory).Load().Data.premiumCoins == 25, "Flush must include changes made during an earlier background write.");
        require(controlled.MaxConcurrentWrites == 1, "Writers must never overlap.");

        int failures = 0;
        int callbackThread = 0;
        session.SaveFailed += _ => { failures++; callbackThread = Thread.CurrentThread.ManagedThreadId; };
        var fixture = new GameObject("Storage transaction fixture");
        try
        {
            var item = fixture.AddComponent<IngameEntity>();
            item.entityType = IngameEntity.eEntityType.Weapon;
            item.id = 2;
            item.cost = 5;
            item.maxEntityLevel = 3;
            item.upgradeBaseCost = 4;
            item.attribute.Add(new EntityAttribute { attributeType = EntityAttribute.eAttributeType.WeaponDamage });
            controlled.FailWrites = true;
            string before = SaveFileCodec.Encode(session.Snapshot());
            require(!session.TryPurchase(item) && !session.IsUnlocked(item.entityType, 2) && session.Coins == 25, "Failed purchase must not deduct or unlock anything.");
            require(SaveFileCodec.Encode(session.Snapshot()) == before, "Failed purchase must leave all live data unchanged.");
            require(failures == 1, "Failed purchase must be reported to the UI.");
            controlled.FailWrites = false;
            require(session.TryPurchase(item) && session.Coins == 20, "Purchase retry must charge exactly once.");
            var reopened = new FileSaveStore(directory).Load().Data;
            require(reopened.premiumCoins == 20 && reopened.IsUnlocked(item.entityType, 2), "Successful purchase must already exist on disk.");

            controlled.FailWrites = true;
            require(!session.Select(item) && session.GetActiveId(item.entityType) == 1, "Failed selection must keep the old loadout.");
            require(!session.TryPurchase(item) && session.GetLevel(item.entityType, 2, 3) == 1 && session.Coins == 20, "Failed upgrade must keep level and coins.");
            session.CreditCoins(7);
            session.Tick(SaveSession.AutosaveInterval);
            WaitForWrite(session, require);
            require(session.HasPendingChanges && session.Coins == 27 && session.LastError != null, "Background failure must retain pending rewards.");
            require(callbackThread == Thread.CurrentThread.ManagedThreadId, "Save failure callbacks must be delivered on the game thread.");
            controlled.FailWrites = false;
            require(session.Flush() && !session.HasPendingChanges && session.LastError == null, "Pending rewards must retry successfully after storage recovers.");

            // Cause a real filesystem failure at the temporary file path, not only a fake exception.
            Directory.CreateDirectory(disk.TemporaryPath);
            require(!session.TryPurchase(item) && session.Coins == 27 && session.GetLevel(item.entityType, 2, 3) == 1, "Real filesystem failure must roll back an upgrade.");
            Directory.Delete(disk.TemporaryPath);
            require(session.TryPurchase(item) && session.GetLevel(item.entityType, 2, 3) == 2 && session.Coins == 23, "Upgrade must work after the filesystem obstruction is removed.");

            controlled.FailWrites = true;
            require(!session.RecordRound(12345, 9) && session.BestScore == 12345 && session.Coins == 32 && session.HasPendingChanges,
                "Failed round save must preserve the reward in memory for retry.");
            controlled.FailWrites = false;
            require(session.Flush(), "Round reward retry must succeed.");
            using var restarted = new SaveSession(new FileSaveStore(directory));
            restarted.Load();
            require(restarted.BestScore == 12345 && restarted.Coins == 32 && restarted.GetLevel(item.entityType, 2, 3) == 2,
                "A new session must recover score, coins and upgrades from disk.");

            controlled.Started.Reset();
            controlled.Release.Reset();
            session.CreditCoins(2);
            session.Tick(SaveSession.AutosaveInterval);
            require(controlled.Started.Wait(5000), "Second background write must start.");
            // Release the older write while a critical transaction waits for it.
            var release = Task.Run(() => { Thread.Sleep(25); controlled.Release.Set(); });
            require(session.Select(item), "Selection must wait for earlier autosaves.");
            release.GetAwaiter().GetResult();
            Thread.Sleep(10);
            reopened = new FileSaveStore(directory).Load().Data;
            require(reopened.activeWeaponID == 2 && reopened.premiumCoins == 34, "An older autosave must never overwrite a later transaction.");
            require(controlled.MaxConcurrentWrites == 1, "Critical and background saves must remain serialized.");

            int changes = 0;
            session.Changed += () => changes++;
            require(session.SetCoinsForEditor(1234) && session.Coins == 1234 && changes == 1,
                "Inspector coin replacement must commit exactly once and notify displays.");
            require(new FileSaveStore(directory).Load().Data.Coins == 1234 && session.GetLevel(item.entityType, 2, 3) == 2,
                "Inspector replacement must persist without altering equipment levels.");
            controlled.FailWrites = true;
            require(!session.SetCoinsForEditor(9999) && session.Coins == 1234 && changes == 1,
                "Failed inspector write must leave coins and change notifications untouched.");
            controlled.FailWrites = false;
            require(session.SetCoinsForEditor(-10) && session.Coins == 0, "Inspector balance must clamp negatives to zero.");
            controlled.Started.Reset(); controlled.Release.Reset();
            session.CreditCoins(2); session.Tick(SaveSession.AutosaveInterval);
            require(controlled.Started.Wait(5000), "Coin replacement overlap fixture did not start autosave.");
            var releaseForReplacement = Task.Run(() => { Thread.Sleep(25); controlled.Release.Set(); });
            require(session.SetCoinsForEditor(50), "Coin replacement must wait for older autosave.");
            releaseForReplacement.GetAwaiter().GetResult();
            require(new FileSaveStore(directory).Load().Data.Coins == 50 && !session.HasPendingChanges && controlled.MaxConcurrentWrites == 1,
                "Older autosave overwrote inspector balance or writers overlapped.");
        }
        finally
        {
            controlled.FailWrites = false;
            controlled.Release.Set();
            UnityEngine.Object.DestroyImmediate(fixture);
        }
    }

    private static void CheckBlockedStorage(string directory, Action<bool, string> require)
    {
        Directory.CreateDirectory(directory);
        var disk = new FileSaveStore(directory);
        Directory.CreateDirectory(disk.SavePath);
        using var session = new SaveSession(disk);
        session.Load();
        require(!session.CanSave && session.LastError != null && !session.Flush(), "An inaccessible primary path must block writes.");
        require(session.CreditCoins(10) == 0, "A blocked save must not start new unsavable progression.");
        require(!session.SetCoinsForEditor(100), "Inspector must not overwrite blocked storage.");
        require(Directory.Exists(disk.SavePath), "Failed load must preserve the obstructing path.");
    }

    private static void CheckFutureBackup(string directory, Action<bool, string> require)
    {
        Directory.CreateDirectory(directory);
        var store = new FileSaveStore(directory);
        string future = SaveFileCodec.Encode(new Savegame()).Replace("\"version\":1", "\"version\":999");
        File.WriteAllText(store.BackupPath, future);
        var result = store.Load();
        require(!result.CanWrite && result.Message != null && File.ReadAllText(store.BackupPath) == future,
            "Future backup must block writes and remain untouched even with no primary.");
        bool rejected = false;
        try { store.Write(SaveFileCodec.Encode(new Savegame())); }
        catch (IOException) { rejected = true; }
        require(rejected, "Blocked future backup permitted a write.");
    }

    private static void WaitForWrite(SaveSession session, Action<bool, string> require)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (session.IsWriting && DateTime.UtcNow < deadline)
        {
            session.Tick(0f);
            Thread.Sleep(1);
        }
        require(!session.IsWriting, "Background write must finish within the test timeout.");
    }

    private sealed class ControlledStore : ISaveStore, IDisposable
    {
        private readonly ISaveStore inner;
        private int concurrent;
        public bool FailWrites;
        public int Writes;
        public int LastThreadId;
        public int MaxConcurrentWrites;
        public readonly ManualResetEventSlim Started = new(false);
        public readonly ManualResetEventSlim Release = new(true);

        public ControlledStore(ISaveStore inner) => this.inner = inner;
        public SaveLoadResult Load() => inner.Load();

        public void Write(string document)
        {
            int active = Interlocked.Increment(ref concurrent);
            MaxConcurrentWrites = Math.Max(MaxConcurrentWrites, active);
            Interlocked.Increment(ref Writes);
            LastThreadId = Thread.CurrentThread.ManagedThreadId;
            Started.Set();
            try
            {
                if (!Release.Wait(5000)) throw new IOException("Test writer timed out.");
                if (FailWrites) throw new IOException("Simulated full disk.");
                inner.Write(document);
            }
            finally { Interlocked.Decrement(ref concurrent); }
        }

        public void Dispose()
        {
            Started.Dispose();
            Release.Dispose();
        }
    }
}

