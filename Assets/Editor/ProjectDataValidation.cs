using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Type = EntityAttribute.eAttributeType;

public static class ProjectDataValidation
{
    internal sealed class FixtureScene : IDisposable
    {
        private readonly Scene previous = SceneManager.GetActiveScene();
        private readonly Scene fixture;
        public FixtureScene()
        {
            fixture = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(fixture);
        }
        public void Dispose()
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (fixture.IsValid() && fixture.isLoaded) EditorSceneManager.CloseScene(fixture, true);
        }
    }
    private static void Require(bool value, string message) => ProjectValidation.Require(value, message);
    private static void Near(float value, float expected, string message) => ProjectValidation.Near(value, expected, message);

    [Serializable] private class Envelope { public string format = "satellite-defense"; public int version = 1; public string payload, checksum; }
    [Serializable] private class Catalog { public Entry[] entities; }
    [Serializable] private class Entry
    {
        public int entityId, maxEntityLevel, upgradeBaseCost;
        public string entityType;
        public long cost;
        public float upgradeCostMultiplier;
        public AttributeEntry[] attributes;
    }
    [Serializable] private class AttributeEntry { public int attributeType; public float initialValue, attributeIncrement; }

    private static string EnvelopeFor(string payload, int version = 1)
    {
        using var hash = SHA256.Create();
        return JsonUtility.ToJson(new Envelope { version = version, payload = payload,
            checksum = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(payload))) });
    }

    public static void CheckCodec()
    {
        var save = new Savegame { premiumCoins = 75, bestScore = 123, activeWeaponID = 2 };
        save.unlockedWeaponIDs.Add(2);
        save.weaponLevels.Add(new EntityLevelEntry(2, 3));
        string encoded = SaveFileCodec.Encode(save);
        Require(SaveFileCodec.TryDecode(encoded, out var copy, out bool future) && !future && copy.Coins == 75 && copy.BestScore == 123 &&
            copy.GetActiveId(IngameEntity.eEntityType.Weapon) == 2 && copy.GetLevel(IngameEntity.eEntityType.Weapon, 2, 10) == 3, "Save round-trip lost progress.");
        foreach (string invalid in new[] { null, "", " ", "{broken", "{}", "[]", "null", JsonUtility.ToJson(save), encoded.Replace("satellite-defense", "other-game"), encoded.Replace("\\\"premiumCoins\\\":75", "\\\"premiumCoins\\\":76") })
            Require(!SaveFileCodec.TryDecode(invalid, out _, out _), "Invalid/checksum-modified document was accepted: " + invalid);
        Require(!SaveFileCodec.TryDecode(EnvelopeFor(JsonUtility.ToJson(save), 999), out _, out future) && future, "Future envelope must be recognized.");
        Require(!SaveFileCodec.TryDecode(EnvelopeFor("{}"), out _, out _), "Valid checksum must not make missing payload fields valid.");
        save.version = Savegame.CurrentVersion + 1;
        Require(!SaveFileCodec.TryDecode(EnvelopeFor(JsonUtility.ToJson(save)), out _, out future) && future, "Future payload must be recognized.");
        foreach (var invalid in new[] { new Savegame { premiumCoins = -1 }, new Savegame { bestScore = -1 },
                     new Savegame { weaponLevels = null }, new Savegame { activeWeaponID = 99 },
                     new Savegame { weaponLevels = new List<EntityLevelEntry> { null } },
                     new Savegame { weaponLevels = new List<EntityLevelEntry> { new(2, 0) } } })
            Require(!invalid.IsValid(), "Structurally invalid save was accepted.");
        foreach (var category in Categories())
        {
            var duplicate = new Savegame();
            duplicate.GetUnlockedIds(category).Add(1);
            Require(!duplicate.IsValid(), "Duplicate unlocked ID accepted: " + category);
            var levels = new Savegame();
            levels.GetLevels(category).Add(new EntityLevelEntry(2, 1));
            levels.GetLevels(category).Add(new EntityLevelEntry(2, 2));
            Require(!levels.IsValid(), "Duplicate level ID accepted: " + category);
        }
        save = new Savegame { premiumCoins = long.MaxValue - 2 };
        Require(save.CreditCoins(100) == 2 && save.Coins == long.MaxValue, "Coin overflow must saturate.");
        Require(save.CreditCoins(-10) == 0 && save.Coins == long.MaxValue, "Negative reward must not deduct.");
        var clone = save.Clone();
        clone.unlockedWeaponIDs.Add(999);
        clone.weaponLevels.Add(new EntityLevelEntry(999, 2));
        Require(!save.IsUnlocked(IngameEntity.eEntityType.Weapon, 999) && save.weaponLevels.Count == 0, "Clone must be deep.");
        Require(Utilities.Round(1.5f) == 2 && Utilities.Round(-1.5f) == -2 && Utilities.Round(double.NaN) == 0 &&
            Utilities.Round(double.PositiveInfinity) == long.MaxValue && Utilities.Round(float.PositiveInfinity) == int.MaxValue, "Rounding boundaries changed.");
    }

    public static void CheckShopRules()
    {
        using var fixtureScene = new FixtureScene();
        var fixture = new GameObject("Validation shop fixture");
        try
        {
            var item = fixture.AddComponent<IngameEntity>();
            item.id = 2; item.cost = 30; item.maxEntityLevel = 3; item.upgradeBaseCost = 20; item.upgradeCostMultiplier = 2;
            item.attribute.Add(new EntityAttribute { attributeType = Type.WeaponDamage, initialValue = 1, attributeIncrement = 1 });
            foreach (var category in Categories())
            {
                item.entityType = category;
                string definition = JsonUtility.ToJson(item);
                var save = new Savegame { premiumCoins = 90 };
                Require(!save.Select(category, 2), "Locked item selectable: " + category);
                Require(item.GetPurchaseCost(save) == 30 && item.CanPurchase(save), "Displayed purchase rules differ.");
                Require(save.TryPurchase(item) && save.Coins == 60 && save.GetLevel(category, 2, 3) == 1, "Purchase must unlock exactly once.");
                Require(save.Select(category, 2) && item.GetPurchaseCost(save) == 20, "Owned item must use upgrade price.");
                Require(save.TryPurchase(item) && save.Coins == 40 && save.GetLevel(category, 2, 3) == 2, "First upgrade failed.");
                Require(save.TryPurchase(item) && save.Coins == 0 && save.GetLevel(category, 2, 3) == 3, "Second upgrade failed.");
                save.premiumCoins = 100;
                Require(!item.CanPurchase(save) && !save.TryPurchase(item) && save.Coins == 100, "Max level was charged.");
                save = new Savegame { premiumCoins = 29 };
                string before = JsonUtility.ToJson(save);
                Require(!save.TryPurchase(item) && JsonUtility.ToJson(save) == before, "Insufficient funds changed data.");
                Require(JsonUtility.ToJson(item) == definition, "Shop transaction changed definition.");
                save = new Savegame { premiumCoins = 30 };
                Require(save.TryPurchase(item) && save.Coins == 0, "Exact funds should succeed.");
                save.GetLevels(category)[0].level = 999;
                Require(save.GetLevel(category, 2, 3) == 3, "Old excessive level must be capped for new definitions.");
            }
            item.entityType = IngameEntity.eEntityType.None;
            Require(!new Savegame { premiumCoins = 100 }.TryPurchase(item), "Invalid category can be bought.");
            item.entityType = IngameEntity.eEntityType.Weapon; item.id = 0;
            Require(!new Savegame { premiumCoins = 100 }.TryPurchase(item), "Invalid ID can be bought.");
            item.id = 2; item.attribute.Clear();
            var noUpgrades = new Savegame { premiumCoins = 100 };
            Require(noUpgrades.TryPurchase(item) && !item.CanPurchase(noUpgrades), "Items without attributes must only be bought once.");
            item.upgradeBaseCost = int.MaxValue; item.upgradeCostMultiplier = 2;
            Require(item.GetUpgradeCost(100) == int.MaxValue, "Upgrade overflow must saturate.");
        }
        finally { Object.DestroyImmediate(fixture); }
    }

    public static void CheckStats()
    {
        using var fixtureScene = new FixtureScene();
        var fixtures = new List<GameObject>();
        try
        {
            IngameEntity Item(int id, params (Type kind, float initial, float increment)[] attributes)
            {
                var go = new GameObject("Validation attribute fixture"); fixtures.Add(go);
                var item = go.AddComponent<IngameEntity>(); item.entityType = IngameEntity.eEntityType.Weapon; item.id = id; item.maxEntityLevel = 2;
                foreach (var attribute in attributes) item.attribute.Add(new EntityAttribute { attributeType = attribute.kind, initialValue = attribute.initial, attributeIncrement = attribute.increment });
                return item;
            }
            var first = Item(1, (Type.PlanetStartHP, 3, 2), (Type.PlanetMaxHP, 6, 1), (Type.WeaponDamage, 1, .5f),
                (Type.WeaponFireRate, 1.6f, -.1f), (Type.ScoreMultiplier, 1.5f, 0), (Type.ScoreBoostOnLowHP, 2, 0),
                (Type.EnemySplitChance, .4f, 0), (Type.CoinChance, .2f, 0), (Type.BonusCoinValue, 2, 0));
            var second = Item(2, (Type.ScoreMultiplier, 2, 0), (Type.ScoreBoostOnLowHP, 2, 0), (Type.EnemySplitChance, .4f, 0),
                (Type.CoinChance, .2f, 0), (Type.BonusCoinValue, 2, 0), (Type.WeaponProjectileSpeed, 20, 0),
                (Type.EnemyHP, 1.5f, 0), (Type.EnemyDamage, 1.5f, 0), (Type.EnemySplitCount, 8, 0), (Type.PlanetRevive, 1, 0));
            var progress = new Savegame(); progress.weaponLevels.Add(new EntityLevelEntry(1, 2));
            string before = JsonUtility.ToJson(first);
            var stats = new LoadoutStats(new[] { first, second }, progress, 3, 5);
            Require(stats.StartLives == 5 && stats.MaxLives == 7 && stats.LowHealthThreshold == 1, "HP aggregation or low-life threshold changed.");
            Require(stats.WeaponDamage == 2 && stats.EnemyHealth == 2 && stats.EnemyDamage == 2, "Half-value damage/health rounding changed.");
            Near(stats.ShotInterval, 1.5f, "Intervals must decrease with upgrades");
            Near(stats.ScoreMultiplier, 3, "Multipliers must multiply and cap");
            Near(stats.LowHealthScoreMultiplier, 2.5f, "Low-life factor cap");
            Near(stats.SplitChance, .5f, "Split chance adds and caps"); Near(stats.CoinChance, .45f, "Base coin chance adds and caps");
            Require(stats.BonusCoinValue == 3 && stats.MaxSplitPieces == 3 && stats.CanRevive, "Coin/piece/ability limits changed.");
            Near(stats.ProjectileSpeed, 10, "Projectile speed cap");
            Require(JsonUtility.ToJson(first) == before, "Loadout calculation mutated definition.");
            first.attribute[0].initialValue = 999;
            Require(stats.StartLives == 5, "Calculated snapshot must not change with later definition mutation.");
            var empty = new LoadoutStats(Array.Empty<IngameEntity>(), new Savegame(), 9, 4);
            Require(empty.StartLives == 4 && empty.MaxLives == 4 && empty.WeaponDamage == 1 && empty.EnemyHealth == 1, "Fallback/clamping changed.");
            Near(empty.CoinChance, .15f, "Default coin chance"); Near(empty.ScoreMultiplier, 1, "Default multiplier");
            var negative = Item(3, (Type.WeaponDamage, -2, 0), (Type.WeaponRotationSpeed, -10, 0), (Type.EnemySpeed, -1, 0), (Type.ScoreMultiplier, -1, 0));
            var safe = new LoadoutStats(new[] { negative }, new Savegame(), 3, 5);
            Require(safe.WeaponDamage == 1 && safe.WeaponRotationSpeed == 0 && safe.EnemySpeed > 0, "Invalid gameplay values need lower bounds.");
            Near(safe.ScoreMultiplier, .5f, "Negative multiplier lower bound");
            var oldCulture = CultureInfo.CurrentCulture;
            try
            {
                foreach (string culture in new[] { "de-DE", "en-US" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                    foreach (Type type in Enum.GetValues(typeof(Type)))
                    {
                        if (type == Type.None) continue;
                        var attribute = new EntityAttribute { attributeType = type, initialValue = 1.25f, attributeIncrement = 0 };
                        Require(!string.IsNullOrWhiteSpace(attribute.GetAttributeEffectString(1)) && !string.IsNullOrWhiteSpace(attribute.GetDescription()) && EntityAttribute.GetAttributeName(type) != "None", "Missing attribute display/description: " + type);
                    }
                }
            }
            finally { CultureInfo.CurrentCulture = oldCulture; }
        }
        finally { foreach (var fixture in fixtures) Object.DestroyImmediate(fixture); }
    }

    public static IngameEntity[] CatalogItems() => AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/IngameEntities" })
        .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)).GetComponent<IngameEntity>()).Where(item => item).ToArray();

    public static IngameEntity.eEntityType[] Categories() => new[] { IngameEntity.eEntityType.Planet, IngameEntity.eEntityType.Weapon, IngameEntity.eEntityType.Enemy, IngameEntity.eEntityType.Background };

    public static void CheckAssets()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs", "Assets/Joystick Pack/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab, "Cannot load prefab: " + path);
            CheckHierarchy(prefab, path);
        }
        var items = CatalogItems();
        Require(items.Length == 36, "Catalog must contain all 36 items.");
        var ids = new HashSet<(IngameEntity.eEntityType, int)>();
        var template = JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/Saves/IngameEntities/Entities_Template_v1.json"));
        Require(template?.entities?.Length == items.Length, "JSON template catalog count differs.");
        foreach (var item in items)
        {
            Require(item.id > 0 && item.entityType != IngameEntity.eEntityType.None && ids.Add((item.entityType, item.id)), "Invalid/duplicate ID: " + item.name);
            Require(item.maxEntityLevel > 0 && item.cost >= 0 && item.upgradeBaseCost >= 0 && item.upgradeCostMultiplier > 0 && !float.IsInfinity(item.upgradeCostMultiplier), "Invalid catalog price/level: " + item.name);
            var entry = template.entities.SingleOrDefault(value => value.entityId == item.id && value.entityType == item.entityType.ToString());
            Require(entry != null && entry.cost == item.cost && entry.maxEntityLevel == item.maxEntityLevel && entry.upgradeBaseCost == item.upgradeBaseCost, "JSON prices/levels differ: " + item.name);
            Near(entry.upgradeCostMultiplier, item.upgradeCostMultiplier, "JSON price growth: " + item.name);
            Require(item.attribute != null && entry.attributes != null && item.attribute.Count == entry.attributes.Length, "JSON attribute count differs: " + item.name);
            var attributes = new HashSet<Type>();
            foreach (var attribute in item.attribute)
            {
                Require(attribute != null && attribute.attributeType != Type.None && Enum.IsDefined(typeof(Type), attribute.attributeType) && attributes.Add(attribute.attributeType), "Invalid/duplicate attribute: " + item.name);
                var saved = entry.attributes.SingleOrDefault(value => value.attributeType == (int)attribute.attributeType);
                Require(saved != null, "JSON missing attribute: " + item.name);
                Near(saved.initialValue, attribute.initialValue, "JSON base: " + item.name); Near(saved.attributeIncrement, attribute.attributeIncrement, "JSON increment: " + item.name);
                for (int level = 1; level <= item.maxEntityLevel; level++)
                {
                    float value = attribute.GetAttributeEffect(level);
                    Require(!float.IsNaN(value) && !float.IsInfinity(value), "Nonfinite level value: " + item.name);
                }
            }
            if (item is Weapon weapon)
            {
                Require(weapon.normalLaserPrefab && weapon.jumpLaserPrefab && weapon.normalLaserPrefab.GetComponent<Bullet>() && weapon.jumpLaserPrefab.GetComponent<Bullet>(), "Weapon projectile reference missing: " + item.name);
                Require(weapon.minShotInterval > 0 && weapon.minShotInterval <= weapon.thirdEmitterShotInterval && weapon.thirdEmitterShotInterval < weapon.secondEmitterShotInterval, "Emitter gates unreachable: " + item.name);
                var group = item.transform.Find("LaserEmitterGrp"); Require(group, "Emitter group missing: " + item.name);
                for (int level = 1; level <= 3; level++) Require(group.Cast<Transform>().Count(t => t.name.Contains("LVL" + level)) == level, "Incorrect emitter count: " + item.name);
            }
            if (item is Planet) Require(item.GetComponent<Collider2D>(), "Planet collider missing: " + item.name);
            if (item is EnemyType enemies) Require(enemies.enemyPrefabs != null && enemies.enemyPrefabs.Length > 0 && enemies.enemyPrefabs.All(prefab => prefab && prefab.GetComponent<Enemy>() && prefab.GetComponent<Collider2D>()), "Enemy variants missing: " + item.name);
        }
        var weapons = items.Where(item => item.entityType == IngameEntity.eEntityType.Weapon).OrderBy(item => item.id).ToArray();
        for (int i = 1; i < weapons.Length; i++)
            foreach (var type in new[] { Type.WeaponDamage, Type.WeaponRotationSpeed, Type.WeaponProjectileSpeed, Type.WeaponFireRate })
                Near(weapons[i].GetAttributeByType(type).GetAttributeEffect(1), weapons[i - 1].GetAttributeByType(type).GetAttributeEffect(weapons[i - 1].maxEntityLevel), "New weapon must match predecessor's max: " + weapons[i].name);
    }

    internal static void CheckHierarchy(GameObject root, string label)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + label + "/" + transform.name);
            foreach (var component in transform.GetComponents<Component>())
            {
                if (!component) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Require(property.objectReferenceValue || property.objectReferenceInstanceIDValue == 0, "Broken reference: " + label + "/" + transform.name + "/" + property.propertyPath);
            }
        }
    }

    public static void CheckScenes()
    {
        var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        Require(enabled.Length >= 2 && enabled[0] == ProjectValidation.MenuScene && enabled.Contains(ProjectValidation.GameScene), "Build scene order must start with MainMenu and include Ingame.");
        var catalogs = new List<string>(); var scales = new List<Vector2>();
        foreach (string path in new[] { ProjectValidation.MenuScene, ProjectValidation.GameScene })
        {
            var scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var roots = scene.GetRootGameObjects();
                T[] Find<T>() where T : Component => roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
                var games = Find<GameController>(); Require(games.Length == 1, "Exactly one GameController required: " + path);
                var game = games[0]; bool gameplay = path == ProjectValidation.GameScene;
                Require(game.isGameplayScene == gameplay, "Scene mode flag differs: " + path);
                Require(Find<Camera>().Count(camera => camera.CompareTag("MainCamera")) == 1 && Find<EventSystem>().Length == 1 && Find<UIController>().Length == 1 && Find<PremiumCoinController>().Length == 1 && Find<AudioController>().Length == 1 && Find<UIToastMessage>().Length == 1 && Find<EnemyController>().Length == 1, "Scene controller/camera/event system duplicates or missing: " + path);
                Require((Find<UIMainMenu>().Length == 0) == gameplay && (Find<UIShopMenu>().Length == 0) == gameplay &&
                    (Find<UIIngameHud>().Length == 1) == gameplay && (Find<PowerUpController>().Length == 1) == gameplay && (Find<ScoreController>().Length == 1) == gameplay, "Menu/game responsibilities overlap: " + path);
                Require(game.planetParent && game.weaponParent && game.backgroundParent, "World parent reference missing: " + path);
                Require(Find<EnemyController>()[0].spawnParent && Find<UIToastMessage>()[0].toastPanel && Find<UIToastMessage>()[0].txtMessage, "Enemy/toast references missing: " + path);
                scales.Add(new Vector2(game.planetParent.lossyScale.x, game.weaponParent.lossyScale.x));
                foreach (var root in roots) CheckHierarchy(root, path);
                foreach (var category in Categories())
                {
                    var prefabs = game.GetCategoryItems(category);
                    int expected = category == IngameEntity.eEntityType.Enemy ? 4 : category == IngameEntity.eEntityType.Background ? 8 : 12;
                    Require(prefabs != null && prefabs.Length == expected && prefabs.All(prefab => prefab && prefab.GetComponent<IngameEntity>()?.entityType == category), "Scene catalog missing/wrong category: " + path);
                    catalogs.Add(category + ":" + string.Join(",", prefabs.Select(prefab => AssetDatabase.GetAssetPath(prefab))));
                }
                if (gameplay)
                {
                    Require(Find<UIPauseMenu>().Length == 1 && game.imgLive && game.imgLiveParent && game.joystick && game.joystickGO, "Gameplay HUD/input reference missing.");
                    var pause = Find<UIPauseMenu>()[0]; Require(pause.btnContinue && pause.btnReplay && pause.btnBackToMainMenu && pause.pauseMenuPanel, "Pause buttons missing.");
                    var hud = Find<UIIngameHud>()[0]; Require(hud.ingameHud && hud.txtScoreIngame && hud.btnPause, "HUD references missing.");
                    var drops = Find<PowerUpController>()[0]; Require(drops.spawnParent && drops.itemCoin && drops.itemHeart && drops.itemFireRate && drops.itemShootUpgrade && drops.itemJumpLaser, "Gameplay pickup references missing.");
                }
                else
                {
                    var menu = Find<UIMainMenu>()[0]; Require(menu.btnPlay && menu.btnShop && menu.btnExit && menu.btnPrivacyPolicy && menu.txtPremiumCoins && menu.txtShopCoins, "Main menu reference missing.");
                    var shop = Find<UIShopMenu>()[0]; Require(shop.btnClose && shop.btnLeft && shop.btnRight && shop.btnBuyUpgrade && shop.btnStateSelect && shop.attributeScroll && shop.attributeScroll.viewport && shop.attributePrefab, "Shop references missing.");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        for (int i = 0; i < 4; i++) Require(catalogs[i] == catalogs[i + 4], "Scene catalogs differ.");
        Near(scales[0].x, scales[1].x, "Planet scale must match in menu/game"); Near(scales[0].y, scales[1].y, "Satellite scale must match in menu/game");
    }
}
