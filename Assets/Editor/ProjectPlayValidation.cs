using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Type = EntityAttribute.eAttributeType;

public static partial class ProjectPlayValidation
{
    public static IEnumerable<(string name, Func<IEnumerator> check)> Cases()
    {
        yield return ("Direct gameplay startup", DirectStartup);
        yield return ("Missing catalog selection preserves old progress", MissingSelection);
        yield return ("Menu preview cannot award progress", Preview);
        yield return ("Shop buttons and scene round trips", ShopTransitions);
        yield return ("All catalog items and levels", AllCatalog);
        yield return ("Pause background and focus", PauseLifecycle);
        yield return ("Round rewards and replay cleanup", RewardsReplay);
        yield return ("Powerup gates and automatic emitters", WeaponGates);
        yield return ("Pickup healing and exactly once rewards", Pickups);
        yield return ("Drop cooldown duplicate and lifetime", DropLifecycle);
        yield return ("Enemy split safety and fragment rules", SplitSafety);
        yield return ("Difficulty speed ramp affects live enemies", Difficulty);
        yield return ("Actual projectile physics and lifetime", ProjectilePhysics);
        yield return ("Revival and impulse cooldown", PlanetAbilities);
        yield return ("Score and low health threshold", ScoreThreshold);
        yield return ("Joystick pointer ownership aiming and release", JoystickInput);
        yield return ("UI fallback and authored geometry", UiState);
        yield return ("Audio pool and paused toast queue", AudioAndToasts);
        yield return ("Exit guard and shutdown flush", ExitGuard);
        yield return ("Blocked save returns to menu", BlockedSave);
    }

    private static void Require(bool value, string message) => ProjectValidation.Require(value, message);
    private static void Near(float value, float expected, string message, float tolerance = .0001f) => ProjectValidation.Near(value, expected, message, tolerance);
    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingFieldException(target.GetType().Name, name);
    private static T Get<T>(object target, string name) => (T)Field(target, name).GetValue(target);
    private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
    private static void Call(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

    private static IEnumerator WaitScene(string path, bool dismissSplash = true)
    {
        float deadline = Time.realtimeSinceStartup + 10;
        while ((SceneManager.GetActiveScene().path != path || !GameController.Instance || !GameController.Instance.IsInitialized) && Time.realtimeSinceStartup < deadline) yield return null;
        Require(SceneManager.GetActiveScene().path == path && GameController.Instance && GameController.Instance.IsInitialized, "Scene initialization timed out: " + path);
        if (dismissSplash && UIController.Instance.splashScreen) UIController.Instance.splashScreen.SetActive(false);
        EnemyController.Instance.enabled = false;
        EnemyController.Instance.RemoveAllEnemies();
        if (PowerUpController.Instance) PowerUpController.Instance.RemoveAllItems();
        yield return null;
    }

    private static IEnumerator Load(string path)
    {
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        yield return WaitScene(path);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) ProjectDataValidation.CheckHierarchy(root, path);
    }

    private static void QuietCombat()
    {
        EnemyController.Instance.enabled = false;
        EnemyController.Instance.RemoveAllEnemies();
        Set(EnemyController.Instance, "splitChance", 0f);
        if (PowerUpController.Instance) { PowerUpController.Instance.RemoveAllItems(); PowerUpController.Instance.dropChance = 0; }
        GameController.Instance.ActiveWeapon.ClearBullets();
    }

    private static Enemy EnemyAt(Vector2 position, bool fragment = false)
    {
        var game = GameController.Instance;
        var prefab = ((EnemyType)game.GetActiveItem(IngameEntity.eEntityType.Enemy)).enemyPrefabs[0];
        var enemy = Object.Instantiate(prefab, position, Quaternion.identity, game.EffectsRoot).GetComponent<Enemy>();
        enemy.transform.localScale = Vector3.one * .3f;
        enemy.isSplitPiece = fragment;
        enemy.Init(Vector2.zero);
        return enemy;
    }

    private static PowerUp Pickup(GameObject prefab)
    {
        var item = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, GameController.Instance.EffectsRoot).GetComponent<PowerUp>();
        item.Init(5);
        return item;
    }

    private static IEnumerator Equip(Func<IngameEntity, bool> predicate)
    {
        yield return Load(ProjectValidation.MenuScene);
        SaveGameController.CreditCoins(1000000);
        var game = GameController.Instance; game.OpenShop();
        var item = ProjectDataValidation.Categories().SelectMany(category => game.GetCategoryItems(category)).Select(prefab => prefab.GetComponent<IngameEntity>()).First(predicate);
        if (!item.IsUnlocked) Require(game.PurchaseItem(item), "Cannot unlock ability fixture.");
        Require(game.SelectItem(item), "Cannot select ability fixture.");
        game.CloseShop(); game.StartNewGame();
        yield return WaitScene(ProjectValidation.GameScene);
        QuietCombat();
    }

    private static IEnumerator DirectStartup()
    {
        yield return Load(ProjectValidation.GameScene);
        var game = GameController.Instance;
        Require(game.isGameplayScene && game.IsPlaying && Time.timeScale == 1 && !UIMainMenu.Instance && !UIShopMenu.Instance, "Direct Ingame start must create a round without menu components.");
        Require(game.CurrentLives == game.Stats.StartLives && game.ActivePlanet && game.ActiveWeapon && game.ActiveBackground && UIIngameHud.Instance, "Direct start did not load full equipment/HUD.");
        Require(SaveGameController.Data.Coins == 0 && SaveGameController.CanSave, "Direct start must use a fresh isolated save.");
    }

    private static IEnumerator MissingSelection()
    {
        var save = new Savegame { premiumCoins = 75, bestScore = 123, activeWeaponID = 999 };
        save.unlockedWeaponIDs.Add(999); save.weaponLevels.Add(new EntityLevelEntry(999, 4));
        var disk = new FileSaveStore(ProjectValidation.TestSaveDirectory); disk.Load(); disk.Write(SaveFileCodec.Encode(save));
        yield return Load(ProjectValidation.MenuScene);
        Require(GameController.Instance.ActiveWeapon.id == 1 && SaveGameController.Data.GetActiveId(IngameEntity.eEntityType.Weapon) == 1, "Missing selected item did not fall back to available equipment.");
        Require(SaveGameController.Data.Coins == 75 && SaveGameController.Data.BestScore == 123 && SaveGameController.Data.IsUnlocked(IngameEntity.eEntityType.Weapon, 999) && SaveGameController.Data.GetLevel(IngameEntity.eEntityType.Weapon, 999, 10) == 4, "Fallback erased unrelated old progress.");
        var persisted = new FileSaveStore(ProjectValidation.TestSaveDirectory).Load().Data;
        Require(persisted.activeWeaponID == 1 && persisted.Coins == 75, "Fallback selection not persisted.");
    }

    private static IEnumerator Preview()
    {
        yield return Load(ProjectValidation.MenuScene);
        var game = GameController.Instance;
        Require(game.State == GameController.GameState.MainMenu && game.IsSimulationRunning && !game.IsPlaying, "Preview simulation state changed.");
        Require(!PowerUpController.Instance && !ScoreController.Instance && !UIIngameHud.Instance, "Preview has gameplay-only controllers.");
        string before = SaveFileCodec.Encode(SaveGameController.GetSnapshot());
        Set(EnemyController.Instance, "splitChance", 0f);
        var enemy = EnemyAt(new Vector2(0, 3)); enemy.Hit(int.MaxValue, enemy.transform.position);
        game.ActiveWeapon.ReduceShotInterval(float.MaxValue); game.ActiveWeapon.UpgradeEmitters();
        int emitters = game.ActiveWeapon.WeaponLevel;
        game.ActivePlanet.Hit(int.MaxValue);
        Require(game.State == GameController.GameState.MainMenu && game.ActivePlanet && game.ActiveWeapon.WeaponLevel == emitters, "Menu planet must be invulnerable without resetting emitters.");
        Require(EnemyController.Instance.Kills == 0 && SaveFileCodec.Encode(SaveGameController.GetSnapshot()) == before, "Preview changed saved progress.");
        var rotation = game.ActivePlanet.transform.rotation;
        yield return new WaitForSecondsRealtime(.15f);
        if (Mathf.Abs(game.ActivePlanet.rotationSpeed) > 0) Require(Quaternion.Angle(rotation, game.ActivePlanet.transform.rotation) > 0, "Preview planet should move.");
        EnemyController.Instance.enabled = true; Set(EnemyController.Instance, "spawnCountdown", 0f);
        yield return null; yield return null;
        Require(EnemyController.Instance.Enemies.Count > 0, "Preview must spawn enemies.");
    }

    private static IEnumerator ShopTransitions()
    {
        yield return Load(ProjectValidation.MenuScene);
        SaveGameController.CreditCoins(1000000);
        for (int cycle = 0; cycle < 3; cycle++)
        {
            var game = GameController.Instance; int menuInstance = game.GetInstanceID();
            UIController.Instance.Init(); UIController.Instance.Init(); UIMainMenu.Instance.btnShop.onClick.Invoke();
            Require(game.State == GameController.GameState.Shop && Time.timeScale == 0 && !game.IsSimulationRunning, "Shop must freeze preview.");
            var shop = UIShopMenu.Instance;
            foreach (var tab in new[] { shop.btnTabPlanets, shop.btnTabWeapons, shop.btnTabEnemyTypes, shop.btnTabBackgrounds })
            {
                tab.onClick.Invoke(); int clicks = 0;
                while (shop.btnRight.interactable && clicks < 36) { shop.btnRight.onClick.Invoke(); clicks++; }
                Require(!shop.btnRight.interactable && clicks < 36, "Right navigation must terminate."); clicks = 0;
                while (shop.btnLeft.interactable && clicks < 36) { shop.btnLeft.onClick.Invoke(); clicks++; }
                Require(!shop.btnLeft.interactable && clicks < 36, "Left navigation must terminate.");
            }
            shop.btnTabWeapons.onClick.Invoke();
            int resetClicks = 0;
            while (shop.btnLeft.interactable && resetClicks++ < 36) shop.btnLeft.onClick.Invoke();
            Require(!shop.btnLeft.interactable, "Cannot reset weapon selection to first item.");
            shop.btnRight.onClick.Invoke();
            var item = game.weaponPrefabs[1].GetComponent<Weapon>();
            int oldLevel = item.Level; bool unlocked = item.IsUnlocked;
            long coins = SaveGameController.Data.Coins, cost = item.GetPurchaseCost(SaveGameController.Data);
            if (cycle == 0)
            {
                var disk = new FileSaveStore(ProjectValidation.TestSaveDirectory); Directory.CreateDirectory(disk.TemporaryPath);
                try
                {
                    shop.btnBuyUpgrade.onClick.Invoke();
                    Require(SaveGameController.Data.Coins == coins && item.IsUnlocked == unlocked && item.Level == oldLevel && SaveGameController.LastError != null, "Failed shop write changed progress or hid error.");
                }
                finally { Directory.Delete(disk.TemporaryPath); }
            }
            shop.btnBuyUpgrade.onClick.Invoke();
            Require(SaveGameController.Data.Coins == coins - cost && item.Level == (unlocked ? oldLevel + 1 : 1), "One click must apply one purchase/upgrade even after repeated Init.");
            shop.btnStateSelect.onClick.Invoke(); Require(SaveGameController.Data.GetActiveId(item.entityType) == item.id, "Activate button did not persist selection.");
            Require(UIMainMenu.Instance.txtShopCoins.text == Utilities.NumberToString(SaveGameController.Data.Coins), "Shop coin display stale.");
            shop.btnClose.onClick.Invoke(); var snapshot = SaveGameController.GetSnapshot();
            UIMainMenu.Instance.btnPlay.onClick.Invoke(); yield return WaitScene(ProjectValidation.GameScene);
            game = GameController.Instance;
            Require(game.GetInstanceID() != menuInstance && game.IsPlaying && game.ActiveWeapon.id == item.id && game.ActiveWeapon.Level == item.Level, "Actual scene transition failed or lost upgraded weapon.");
            Require(SaveGameController.Data.Coins == snapshot.Coins && SaveGameController.Data.GetActiveId(item.entityType) == item.id, "Scene transition lost save session.");
            if (AudioController.Instance.ingameMusic) Require(Get<AudioSource>(AudioController.Instance, "musicSource").clip == AudioController.Instance.ingameMusic, "Gameplay music did not switch with scene.");
            game.Pause(); UIPauseMenu.Instance.btnBackToMainMenu.onClick.Invoke(); yield return WaitScene(ProjectValidation.MenuScene, false);
            Require(GameController.Instance.State == GameController.GameState.MainMenu && Time.timeScale == 1 && !UIPauseMenu.Instance, "Return must replace scene and reset pause.");
            Require(!UIController.Instance.splashScreen || !UIController.Instance.splashScreen.activeSelf, "Returning must not repeat splash.");
            if (AudioController.Instance.mainMenuMusic) Require(Get<AudioSource>(AudioController.Instance, "musicSource").clip == AudioController.Instance.mainMenuMusic, "Menu music did not switch with scene.");
        }
    }

    private static IEnumerator AllCatalog()
    {
        yield return Load(ProjectValidation.MenuScene); SaveGameController.CreditCoins(1000000);
        var game = GameController.Instance; game.OpenShop();
        foreach (var category in ProjectDataValidation.Categories())
            foreach (var prefab in game.GetCategoryItems(category))
            {
                var item = prefab.GetComponent<IngameEntity>(); string definition = JsonUtility.ToJson(item);
                if (!item.IsUnlocked) Require(game.PurchaseItem(item), "Catalog purchase failed: " + item.name);
                while (item.Level < item.maxEntityLevel && item.HasUpgrades)
                {
                    int previousLevel = item.Level;
                    Require(game.PurchaseItem(item), "Catalog upgrade failed: " + item.name);
                    Require(item.Level == previousLevel + 1, "Upgrade failed to advance exactly one level: " + item.name);
                }
                Require(game.SelectItem(item) && game.GetActiveItem(category) == item, "Catalog item cannot activate: " + item.name);
                Require(JsonUtility.ToJson(item) == definition, "Purchase/activation mutated prefab: " + item.name);
                yield return null;
                if (category == IngameEntity.eEntityType.Planet) Require(game.ActivePlanet.id == item.id && game.ActivePlanet.GetComponent<Collider2D>(), "Planet instance missing.");
                if (category == IngameEntity.eEntityType.Weapon) Require(game.ActiveWeapon.id == item.id && game.ActiveWeapon.normalLaserPrefab, "Weapon instance missing.");
            }
        game.CloseShop(); game.StartNewGame(); yield return WaitScene(ProjectValidation.GameScene); game = GameController.Instance;
        Require(game.CurrentLives == game.Stats.StartLives && game.CurrentLives <= game.MaxLives, "Maximum loadout start HP wrong.");
        Require(SaveFileCodec.TryDecode(SaveFileCodec.Encode(SaveGameController.GetSnapshot()), out var restored, out _) && restored.GetActiveId(IngameEntity.eEntityType.Weapon) == game.ActiveWeapon.id && restored.GetLevel(IngameEntity.eEntityType.Weapon, game.ActiveWeapon.id, game.ActiveWeapon.maxEntityLevel) == game.ActiveWeapon.Level, "Max-level loadout serialization failed.");
        var fragment = EnemyAt(new Vector2(0, 3), true);
        Require(Get<int>(fragment, "healthPoints") == Mathf.CeilToInt(game.Stats.EnemyHealth * .5f) && Get<int>(fragment, "damage") == Mathf.CeilToInt(game.Stats.EnemyDamage * .5f), "Max-level odd HP/damage did not round fragments upwards.");
        fragment.Despawn();
    }
}
