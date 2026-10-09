using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProjectValidation
{
    private const string RunningKey = "SatelliteDefense.Validation.Running";
    private const string BatchKey = "SatelliteDefense.Validation.Batch";
    private const string SaveDirectoryKey = "SatelliteDefense.Validation.SaveDirectory";
    private const string ResultPath = "Logs/Validation-results.txt";
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private static readonly List<string> failures = new();
    private static int assertions;
    private static double deadline;

    static ProjectValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += CheckTimeout;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void IsolateSave()
    {
        if (SessionState.GetBool(RunningKey, false))
            SaveGameController.UseStorageDirectoryForValidation(SessionState.GetString(SaveDirectoryKey, ""));
    }

    [MenuItem("Tools/Validation/Run regression checks")]
    public static void RunFromMenu() => Begin(false);
    public static void RunBatch() => EditorApplication.delayCall += () => Begin(true);

    public static void BuildAndroidDevelopment()
    {
        bool useKeystore = PlayerSettings.Android.useCustomKeystore;
        try
        {
            PlayerSettings.Android.useCustomKeystore = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = BuildTarget.Android,
                locationPathName = "Builds/SatelliteDefense-development.apk",
                options = BuildOptions.Development
            });
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/Android-build-result.txt", $"{report.summary.result}: {report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings, {report.summary.totalSize} bytes\n");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Android development build failed.");
        }
        finally { PlayerSettings.Android.useCustomKeystore = useKeystore; }
    }

    private static void Begin(bool batch)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run validation from edit mode.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ResultPath, "Satellite Defense regression checks\n");
        failures.Clear();
        assertions = 0;
        try
        {
            CheckSavegame();
            SaveStorageValidation.Run(Require);
            CheckAssets();
            SaveStorageValidation.PrepareRestartCheck();
            Append($"PASS: savegame and asset checks ({assertions} assertions)");
            SessionState.SetString(SaveDirectoryKey, Path.Combine(Path.GetFullPath("Logs"), "PlaySaveTests-" + Guid.NewGuid().ToString("N")));
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool("SatelliteDefense.Validation.Passed", false);
            SessionState.SetBool(BatchKey, batch);
            SessionState.SetString("SatelliteDefense.Validation.PreviousStartScene",
                AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Append("FAIL: " + exception);
            Debug.LogException(exception);
            if (batch) EditorApplication.Exit(1);
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            deadline = EditorApplication.timeSinceStartup + 120;
            failures.Clear();
            assertions = 0;
            Application.logMessageReceived += CaptureError;
            new GameObject("Regression checks").AddComponent<PlayModeValidationRunner>().StartChecks();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SaveGameController.CloseStorageForValidation();
            string saveDirectory = SessionState.GetString(SaveDirectoryKey, "");
            if (saveDirectory.StartsWith(Path.Combine(Path.GetFullPath("Logs"), "PlaySaveTests-"), StringComparison.OrdinalIgnoreCase) && Directory.Exists(saveDirectory))
                Directory.Delete(saveDirectory, true);
            SessionState.EraseString(SaveDirectoryKey);
            SessionState.SetBool(RunningKey, false);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                SessionState.GetString("SatelliteDefense.Validation.PreviousStartScene", ""));
            bool passed = SessionState.GetBool("SatelliteDefense.Validation.Passed", false);
            if (SessionState.GetBool(BatchKey, false)) EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    private static void CheckTimeout()
    {
        if (SessionState.GetBool(RunningKey, false) && deadline > 0 && EditorApplication.timeSinceStartup > deadline)
            Finish(new TimeoutException("Play-mode checks exceeded their timeout."));
    }

    private static void CaptureError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            failures.Add(message + "\n" + stack);
    }

    internal static void Finish(Exception exception = null)
    {
        Application.logMessageReceived -= CaptureError;
        if (exception != null) failures.Add(exception.ToString());
        bool passed = failures.Count == 0;
        Append($"Play-mode assertions completed: {assertions}");
        Append(passed ? $"PASS: play-mode checks ({assertions} assertions)" : "FAIL:\n" + string.Join("\n", failures));
        SessionState.SetBool("SatelliteDefense.Validation.Passed", passed);
        deadline = 0;
        EditorApplication.ExitPlaymode();
    }

    private static void Append(string message)
    {
        File.AppendAllText(ResultPath, message + "\n");
        Debug.Log(message);
    }

    internal static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void CheckSavegame()
    {
        var save = new Savegame { bestScore = 123, premiumCoins = 75 };
        Require(SaveFileCodec.TryDecode(SaveFileCodec.Encode(save), out var decoded, out _) &&
            decoded.bestScore == 123 && decoded.premiumCoins == 75, "Save envelope must preserve balances.");
        Require(!SaveFileCodec.TryDecode("{broken", out _, out _), "Malformed JSON must be rejected.");
        Require(!SaveFileCodec.TryDecode("{}", out _, out _), "Empty JSON must not reset progress.");
        Require(!SaveFileCodec.TryDecode(JsonUtility.ToJson(save), out _, out _), "Unverified legacy data must not be imported.");
        save.unlockedPlanetIDs.Add(1);
        Require(!save.IsValid(), "Duplicate IDs must be rejected.");
        save = new Savegame { premiumCoins = -1 };
        Require(!save.IsValid(), "Negative balances must be rejected.");
        save = new Savegame { weaponLevels = null };
        Require(!save.IsValid(), "Missing categories must be rejected.");

        var fixture = new GameObject("Purchase fixture");
        try
        {
            var item = fixture.AddComponent<IngameEntity>();
            item.entityType = IngameEntity.eEntityType.Weapon;
            item.id = 2;
            item.cost = 30;
            item.upgradeBaseCost = 20;
            item.upgradeCostMultiplier = 2f;
            item.maxEntityLevel = 3;
            item.attribute.Add(new EntityAttribute { attributeType = EntityAttribute.eAttributeType.WeaponDamage, initialValue = 1, attributeIncrement = 1 });
            string definition = JsonUtility.ToJson(item);
            save = new Savegame { premiumCoins = 90 };
            Require(!save.Select(item.entityType, item.id), "Locked items cannot be selected.");
            Require(save.TryPurchase(item) && save.premiumCoins == 60 && save.GetLevel(item.entityType, item.id, 3) == 1, "Purchase must charge once and unlock level one.");
            Require(save.Select(item.entityType, item.id), "Purchased item must be selectable.");
            Require(save.TryPurchase(item) && save.premiumCoins == 40 && save.GetLevel(item.entityType, item.id, 3) == 2, "First upgrade price/level mismatch.");
            Require(save.TryPurchase(item) && save.premiumCoins == 0 && save.GetLevel(item.entityType, item.id, 3) == 3, "Second upgrade price/level mismatch.");
            save.premiumCoins = 100;
            Require(!save.TryPurchase(item) && save.premiumCoins == 100, "Max-level upgrades must never charge.");
            item.id = 3;
            save.premiumCoins = 29;
            Require(!save.TryPurchase(item) && !save.IsUnlocked(item.entityType, 3) && save.premiumCoins == 29, "Insufficient funds must leave all data unchanged.");
            item.id = 2;
            Require(JsonUtility.ToJson(item) == definition, "Transactions must not mutate prefab definitions.");
            Require(SaveFileCodec.TryDecode(SaveFileCodec.Encode(save), out var restored, out _) && restored.activeWeaponID == 2 && restored.GetLevel(item.entityType, 2, 3) == 3, "Selection and levels must survive a save/reload.");
            save.premiumCoins = long.MaxValue - 2;
            Require(save.CreditCoins(100) == 2 && save.premiumCoins == long.MaxValue, "Coin overflow must saturate.");
            Require(save.CreditCoins(-10) == 0, "Negative rewards must not deduct coins.");
            Require(Utilities.Round(double.PositiveInfinity) == long.MaxValue && Utilities.Round(float.PositiveInfinity) == int.MaxValue, "Rounding must not overflow.");
        }
        finally { Object.DestroyImmediate(fixture); }
    }

    private static void CheckAssets()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs", "Assets/Joystick Pack/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + path + "/" + transform.name);
        }
        var identities = new HashSet<(IngameEntity.eEntityType, int)>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/IngameEntities" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (!prefab.TryGetComponent<IngameEntity>(out var item)) continue;
            Require(item.id > 0 && item.entityType != IngameEntity.eEntityType.None && identities.Add((item.entityType, item.id)), "Invalid/duplicate catalog ID: " + prefab.name);
            Require(item.maxEntityLevel >= 1 && item.cost >= 0 && item.upgradeBaseCost >= 0 && item.upgradeCostMultiplier > 0f, "Invalid prices or max level: " + prefab.name);
            var attributes = new HashSet<EntityAttribute.eAttributeType>();
            foreach (var attribute in item.attribute)
            {
                Require(attribute != null && attribute.attributeType != EntityAttribute.eAttributeType.None && attributes.Add(attribute.attributeType), "Duplicate/invalid attribute: " + prefab.name);
                float value = attribute.GetAttributeEffect(item.maxEntityLevel);
                Require(!float.IsNaN(value) && !float.IsInfinity(value), "Invalid attribute value: " + prefab.name);
            }
        }
        Require(identities.Count == 36, "All 36 existing catalog items must remain available.");
    }

    internal static IEnumerator CheckPlayMode()
    {
        for (int i = 0; i < 120 && (!GameController.Instance || !GameController.Instance.IsInitialized); i++) yield return null;
        var game = GameController.Instance;
        Require(game && game.IsInitialized && game.State == GameController.GameState.MainMenu, "Main scene must initialize.");
        Require(SaveGameController.Data.Coins == 0, "Tests must start with their isolated save.");
        UIController.Instance.splashScreen.SetActive(false);
        foreach (var component in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(component.gameObject) == 0, "Missing scene script: " + component.name);

        var data = SaveGameController.Data;
        SaveGameController.CreditCoins(1000000);
        for (int round = 0; round < 3; round++)
        {
            UIMainMenu.Instance.btnShop.onClick.Invoke();
            Require(game.State == GameController.GameState.Shop && Time.timeScale == 0f, "Shop must freeze the preview.");
            var shop = UIShopMenu.Instance;
            foreach (var tab in new[] { shop.btnTabPlanets, shop.btnTabWeapons, shop.btnTabEnemyTypes, shop.btnTabBackgrounds })
            {
                tab.onClick.Invoke();
                int count = 0;
                while (shop.btnRight.interactable && count++ < 20) shop.btnRight.onClick.Invoke();
                Require(count < 20, "Shop navigation must terminate at the catalog boundary.");
            }
            shop.btnTabWeapons.onClick.Invoke();
            while (shop.btnLeft.interactable) shop.btnLeft.onClick.Invoke();
            shop.btnRight.onClick.Invoke();
            var weapon = game.weaponPrefabs[1].GetComponent<Weapon>();
            int oldLevel = weapon.Level;
            bool owned = weapon.IsUnlocked;
            long coins = data.Coins;
            long price = owned ? weapon.GetAttributeCostByLevel(oldLevel) : weapon.cost;
            if (!owned || oldLevel < weapon.maxEntityLevel)
            {
                if (round == 0)
                {
                    var disk = new FileSaveStore(SessionState.GetString(SaveDirectoryKey, ""));
                    Directory.CreateDirectory(disk.TemporaryPath);
                    try
                    {
                        shop.btnBuyUpgrade.onClick.Invoke();
                        Require(data.Coins == coins && weapon.IsUnlocked == owned && weapon.Level == oldLevel,
                            "Shop button must leave coins, ownership and level unchanged when disk writing fails.");
                        Require(!string.IsNullOrEmpty(SaveGameController.LastError), "Shop save failure must have a user-facing message.");
                    }
                    finally { Directory.Delete(disk.TemporaryPath); }
                }
                shop.btnBuyUpgrade.onClick.Invoke();
                Require(data.Coins == coins - price, "One button click must charge exactly one transaction.");
                Require(weapon.Level == (owned ? oldLevel + 1 : 1), "One button click must purchase exactly one level.");
                Require(SaveGameController.LastError == null, "Successful retry must clear the save error.");
            }
            shop.btnStateSelect.onClick.Invoke();
            Require(data.GetActiveId(IngameEntity.eEntityType.Weapon) == weapon.id, "Selection must persist.");
            shop.btnClose.onClick.Invoke();
            UIMainMenu.Instance.btnPlay.onClick.Invoke();
            Require(game.IsPlaying && Time.timeScale == 1f && game.ActiveWeapon.Level == weapon.Level, "New round must use the selected, upgraded weapon.");
            Require(game.CurrentLives == Mathf.Min(game.MaxLives, Utilities.Round(game.GetAttribute(EntityAttribute.eAttributeType.PlanetStartHP))), "Starting lives must use the active planet.");
            Require(game.imgLiveParent.GetComponentsInChildren<UnityEngine.UI.Image>().Length == game.CurrentLives, "Visible life icons must match health.");

            game.ActiveWeapon.UpgradeEmitters();
            game.ActiveWeapon.ActivateJumpLaser();
            var planetRotation = game.ActivePlanet.transform.rotation;
            SaveGameController.CreditCoins(3);
            game.Pause();
            Require(new FileSaveStore(SessionState.GetString(SaveDirectoryKey, "")).Load().Data.premiumCoins == data.Coins,
                "Pausing must flush buffered coins to disk.");
            yield return new WaitForSecondsRealtime(0.15f);
            Require(game.State == GameController.GameState.Paused && Time.timeScale == 0f, "Pause state mismatch.");
            Require(Quaternion.Angle(planetRotation, game.ActivePlanet.transform.rotation) < 0.01f && game.ActiveWeapon.IsJumpLaserActive, "Pause must freeze planet movement and power-up duration.");
            UIPauseMenu.Instance.btnContinue.onClick.Invoke();
            Require(game.IsPlaying && Time.timeScale == 1f, "Continue must resume.");
            if (round == 0)
            {
                SaveGameController.CreditCoins(4);
                game.SendMessage("OnApplicationPause", true);
                Require(game.State == GameController.GameState.Paused && !SaveGameController.HasPendingChanges &&
                    new FileSaveStore(SessionState.GetString(SaveDirectoryKey, "")).Load().Data.premiumCoins == data.Coins,
                    "Application background callback must pause and flush pending coins.");
                game.Resume();
                SaveGameController.CreditCoins(2);
                game.SendMessage("OnApplicationFocus", false);
                Require(game.State == GameController.GameState.Paused && !SaveGameController.HasPendingChanges &&
                    new FileSaveStore(SessionState.GetString(SaveDirectoryKey, "")).Load().Data.premiumCoins == data.Coins,
                    "Focus loss callback must pause and flush pending coins.");
                game.Resume();
            }
            game.ChangeLife(int.MaxValue, false);
            Require(game.CurrentLives == game.MaxLives, "Healing must clamp to the upgraded maximum.");

            var coin = Object.Instantiate(PowerUpController.Instance.itemCoin).GetComponent<PowerUp>();
            coin.Init(5);
            coins = data.Coins;
            long expected = Math.Max(1L, coin.addPremiumCoins + (long)Utilities.Round(game.GetAttribute(EntityAttribute.eAttributeType.BonusCoinValue)));
            coin.Collect();
            coin.Collect();
            Require(data.Coins == coins + expected, "A coin pickup must pay exactly once.");
            ScoreController.Instance.AddScore(100);
            long score = ScoreController.Instance.Score;
            coins = data.Coins;
            game.EndRound();
            long reward = score / Math.Max(1, PremiumCoinController.Instance.premiumCoinsPerScore);
            Require(data.Coins == coins + reward && data.BestScore >= score, "Game-over rewards must be saved before animations.");
            var persisted = new FileSaveStore(SessionState.GetString(SaveDirectoryKey, "")).Load();
            Require(persisted.CanWrite && persisted.Data.premiumCoins == data.Coins && persisted.Data.bestScore == data.BestScore,
                "Game-over rewards must be committed to the actual file before animations.");
            game.EndRound();
            Require(data.Coins == coins + reward, "Game over must be idempotent.");
            UIPauseMenu.Instance.btnReplay.onClick.Invoke();
            Require(game.IsPlaying && ScoreController.Instance.Score == 0 && game.ActiveWeapon.WeaponLevel == 1 &&
                !game.ActiveWeapon.IsJumpLaserActive && EnemyController.Instance.Kills == 0, "Replay must reset all round progress.");
            game.ReturnToMainMenu();
            yield return null;
        }

        // Active levels survive a serialization round trip, including every category.
        Require(SaveFileCodec.TryDecode(SaveFileCodec.Encode(SaveGameController.GetSnapshot()), out var reloaded, out _) &&
            reloaded.activeWeaponID == data.GetActiveId(IngameEntity.eEntityType.Weapon) &&
            reloaded.GetLevel(IngameEntity.eEntityType.Weapon, reloaded.activeWeaponID, 100) == game.ActiveWeapon.Level,
            "Upgraded loadout must survive serialization.");

        game.OpenShop();
        foreach (var category in new[] { IngameEntity.eEntityType.Planet, IngameEntity.eEntityType.Weapon,
            IngameEntity.eEntityType.Background, IngameEntity.eEntityType.Enemy })
        {
            foreach (var prefab in game.GetCategoryItems(category))
            {
                var item = prefab.GetComponent<IngameEntity>();
                if (!item.IsUnlocked) Require(SaveGameController.TryPurchase(item), "Catalog purchase failed: " + prefab.name);
                while (item.Level < item.maxEntityLevel && item.attribute.Count > 0)
                    Require(SaveGameController.TryPurchase(item), "Catalog upgrade failed: " + prefab.name);
                Require(game.SelectItem(item) && game.GetActiveItem(category) == item, "Every catalog item must instantiate and activate: " + prefab.name);
                yield return null;
            }
        }
        game.CloseShop();

        // Verify that the joystick accepts one finger and resets when its panel is disabled.
        game.enableJoystickControll = true;
        game.StartNewGame();
        yield return null;
        var joystick = game.joystick;
        var pointer = new PointerEventData(EventSystem.current) { pointerId = 7, position = new Vector2(50, 50) };
        joystick.OnPointerDown(pointer);
        Require(joystick.IsPressed && joystick.AxisOptions == AxisOptions.Both, "Joystick press/getter must work.");
        joystick.OnPointerUp(new PointerEventData(EventSystem.current) { pointerId = 8 });
        Require(joystick.IsPressed, "A second finger must not release the joystick.");
        game.Pause();
        Require(!joystick.IsPressed && joystick.Direction == Vector2.zero, "Hiding the joystick must clear its input.");
        game.enableJoystickControll = false;
        game.StartNewGame();

        // Actual 2D physics contact: upgraded damage must be applied once, with no Start-time bonus.
        EnemyController.Instance.enabled = false;
        EnemyController.Instance.RemoveAllEnemies();
        var enemyPrefab = ((EnemyType)game.GetActiveItem(IngameEntity.eEntityType.Enemy)).enemyPrefabs[0];
        var enemy = Object.Instantiate(enemyPrefab, new Vector3(0, 3, 0), Quaternion.identity).GetComponent<Enemy>();
        enemy.Init(new Vector2(0, 3));
        typeof(Enemy).GetField("healthPoints", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 10);
        typeof(Enemy).GetField("moveSpeed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 0f);
        enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        var bullet = Object.Instantiate(game.ActiveWeapon.normalLaserPrefab, new Vector3(0, 2, 0), Quaternion.identity).GetComponent<Bullet>();
        bullet.Init(game.ActiveWeapon, Vector2.up, false, Color.white, false, 3, 5f);
        yield return new WaitForSeconds(0.5f);
        Require((int)typeof(Enemy).GetField("healthPoints", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(enemy) == 7, "Physics hit must apply exactly the supplied damage.");
        enemy.Hit(100, enemy.transform.position);
        int kills = EnemyController.Instance.Kills;
        long points = ScoreController.Instance.Score;
        enemy.Hit(100, enemy.transform.position);
        Require(EnemyController.Instance.Kills == kills && ScoreController.Instance.Score == points, "Multiple lethal hits must not duplicate score or kills.");
        yield return null;
        EnemyController.Instance.enabled = true;
        // A revive planet can survive exactly one lethal hit per round.
        game.ReturnToMainMenu();
        game.OpenShop();
        var revivePlanet = game.planetPrefabs.Select(prefab => prefab.GetComponent<Planet>()).First(item =>
            item.GetAttributeByType(EntityAttribute.eAttributeType.PlanetRevive)?.GetAttributeEffect(item.Level) > 0f);
        Require(game.SelectItem(revivePlanet), "Revive planet must be selectable.");
        game.CloseShop();
        game.StartNewGame();
        game.ActivePlanet.Hit(game.MaxLives);
        Require(game.IsPlaying && game.CurrentLives > 0, "First lethal hit must revive the planet.");
        game.ActivePlanet.Hit(game.MaxLives);
        Require(game.State == GameController.GameState.GameOver, "Second lethal hit must end the round.");
        game.ReturnToMainMenu();
        yield return null;
    }
}

public class PlayModeValidationRunner : MonoBehaviour
{
    public void StartChecks() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        var checks = ProjectValidation.CheckPlayMode();
        while (true)
        {
            object current = null;
            bool next = false;
            Exception error = null;
            try
            {
                next = checks.MoveNext();
                if (next) current = checks.Current;
            }
            catch (Exception exception) { error = exception; }
            if (error != null || !next)
            {
                ProjectValidation.Finish(error);
                yield break;
            }
            yield return current;
        }
    }
}


