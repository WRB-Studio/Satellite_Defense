using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class IngameEntityAttributeEditor : EditorWindow
{
    // ---------- Hardcoded Paths (ggf. anpassen) ----------
    const string PATH_PLANETS = "Assets/Prefabs/IngameEntities/Planets";
    const string PATH_WEAPONS = "Assets/Prefabs/IngameEntities/Weapons";
    const string PATH_ENEMIES = "Assets/Prefabs/IngameEntities/Enemies";
    const string PATH_BACKGROUNDS = "Assets/Prefabs/IngameEntities/Backgrounds";

    IngameEntity.eEntityType selectedCat = IngameEntity.eEntityType.Planet;

    // Liste aus Prefab + IngameEntity-Komponente
    readonly List<(GameObject prefab, IngameEntity info)> entries = new();

    private IngameEntity targetEntity;
    private Vector2 listScroll;
    private Vector2 scroll;
    private int selectedIndex = -1;


    [MenuItem("Tools/Attribute/IngameEntity Attribute Editor")]
    public static void Open() => GetWindow<IngameEntityAttributeEditor>("Entity Attributes Editor");


    private void OnEnable() => LoadCategory(selectedCat);

    private void OnGUI()
    {
        Header();

        // --- Kategorie-Buttons ---
        EditorGUILayout.BeginHorizontal();
        if (CategoryButton("Planet", IngameEntity.eEntityType.Planet)) LoadCategory(IngameEntity.eEntityType.Planet);
        if (CategoryButton("Weapon", IngameEntity.eEntityType.Weapon)) LoadCategory(IngameEntity.eEntityType.Weapon);
        if (CategoryButton("Enemy", IngameEntity.eEntityType.Enemy)) LoadCategory(IngameEntity.eEntityType.Enemy);
        if (CategoryButton("Background", IngameEntity.eEntityType.Background)) LoadCategory(IngameEntity.eEntityType.Background);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        DrawEntityList();

        EditorGUILayout.Space(8);
        if (targetEntity == null)
        {
            EditorGUILayout.HelpBox("Wähle oben eine Kategorie und dann ein Entity aus der Liste.", MessageType.Info);
            DrawJsonSection(); // trotzdem Backup/Restore sichtbar lassen
            return;
        }

        // --- Detailbereich ---
        DrawEntityInfo();
        EditorGUILayout.Space(6);
        DrawAttributeList();
        EditorGUILayout.Space(10);
        DrawJsonSection();
    }


    // ------- UI Teile -------
    private void Header()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            var newRef = (IngameEntity)EditorGUILayout.ObjectField(
                new GUIContent("IngameEntity", "Scene-Objekt oder Prefab mit IngameEntity"),
                targetEntity, typeof(IngameEntity), true);

            if (newRef != targetEntity)
            {
                targetEntity = newRef;
                selectedIndex = -1;
            }
        }
    }

    bool CategoryButton(string label, IngameEntity.eEntityType cat)
    {
        bool pressed = GUILayout.Toggle(selectedCat == cat, label, "Button", GUILayout.Height(22));
        bool changed = pressed && selectedCat != cat;
        if (changed) selectedCat = cat;
        return changed;
    }

    void LoadCategory(IngameEntity.eEntityType cat)
    {
        entries.Clear();

        string path = cat switch
        {
            IngameEntity.eEntityType.Planet => PATH_PLANETS,
            IngameEntity.eEntityType.Weapon => PATH_WEAPONS,
            IngameEntity.eEntityType.Enemy => PATH_ENEMIES,
            IngameEntity.eEntityType.Background => PATH_BACKGROUNDS,
            _ => "Assets"
        };

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
        foreach (var guid in guids)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (!prefab) continue;

            var info = prefab.GetComponent<IngameEntity>();
            if (!info) continue;

            entries.Add((prefab, info));
        }

        // Stabil: nach ID sortieren
        entries.Sort((a, b) => a.info.id.CompareTo(b.info.id));
    }

    void DrawEntityList()
    {
        // Spaltenbreiten
        const float COL_ID = 30f;
        const float COL_ENTITY = 80f;
        const float COL_ITEMNAME = 150f;
        const float COL_BTN = 80f;
        const float SEP_W = 1f;

        // Styles
        var clipLabel = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Clip };
        float rowHeight = EditorGUIUtility.singleLineHeight;

        // Helper
        void VLine(float height, bool visible = true)
        {
            var col = Color.white;
            if (!visible) col.a = 0f;
            var r = GUILayoutUtility.GetRect(SEP_W, height, GUILayout.Width(SEP_W));
            EditorGUI.DrawRect(r, col);
        }

        void HLine(float thick = 1f, bool visible = true)
        {
            var col = Color.white;
            if (!visible) col.a = 0f;
            var r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(thick), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, col);
        }

        EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("HelpBox"))
        {
            // Header
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("ID", GUILayout.Width(COL_ID + 4f));
                VLine(rowHeight);

                GUILayout.Label("Entity", GUILayout.Width(COL_ENTITY));
                VLine(rowHeight);

                GUILayout.Label("Item Name", GUILayout.Width(COL_ITEMNAME));
                VLine(rowHeight);

                GUILayout.Label("Select", GUILayout.Width(COL_BTN));
            }
            HLine(1f);

            // Liste
            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.MinHeight(140), GUILayout.MaxHeight(260));
            for (int i = 0; i < entries.Count; i++)
            {
                var (prefab, info) = entries[i];

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(info.id.ToString(), GUILayout.Width(COL_ID));
                    VLine(rowHeight);

                    GUILayout.Label(prefab != null ? prefab.name : "<missing>", GUILayout.Width(COL_ENTITY));
                    VLine(rowHeight);

                    GUILayout.Label(string.IsNullOrWhiteSpace(info.itemName) ? "<unnamed>" : info.itemName, GUILayout.Width(COL_ITEMNAME));
                    VLine(rowHeight);

                    if (GUILayout.Button("Select", GUILayout.Width(COL_BTN)))
                    {
                        targetEntity = info;
                        selectedIndex = -1;
                        Selection.activeObject = prefab;
                    }
                }

                if (i < entries.Count - 1)
                    HLine(1f, false);
            }
            EditorGUILayout.EndScrollView();
        }
    }


    // ------- Detail/Editor-Abschnitte -------
    private void DrawEntityInfo()
    {
        const float RIGHT_W = 200f;   // right Column Width
        const float PREVIEW = 180f;   // preview size (square)

        using (new EditorGUILayout.HorizontalScope())
        {
            // =========================
            // Left Column (Form-Fields)
            // =========================
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(new GUIContent("Entity ID"), targetEntity.id);
                    EditorGUILayout.TextField(new GUIContent("Entity Type"), targetEntity.entityType.ToString());
                }

                EditorGUI.BeginChangeCheck();
                var newName = EditorGUILayout.TextField(new GUIContent("Entity Name"), targetEntity.itemName);
                var newCost = EditorGUILayout.LongField("Entity cost", targetEntity.cost);

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Level Einstellungen", EditorStyles.boldLabel);
                var newLevel = EditorGUILayout.IntField("Max Level", targetEntity.maxEntityLevel);
                var newAttributeBaseCost = EditorGUILayout.IntField("Upgrade Base Cost", targetEntity.upgradeBaseCost);
                var newUpgradeCostMultiplier = EditorGUILayout.FloatField("Upgrade Cost multiplier", targetEntity.upgradeCostMultiplier);

                EditorGUILayout.Space(8);
                string costPerLevel = "Cost per Level:\n";
                for (int j = 1; j <= targetEntity.maxEntityLevel; j++)
                    costPerLevel += $"\tLvL {j} = {targetEntity.GetAttributeCostByLevel(j):0.###}\n";
                EditorGUILayout.HelpBox(costPerLevel, MessageType.None);

                if (EditorGUI.EndChangeCheck())
                {
                    RecordAnd(() =>
                    {
                        targetEntity.itemName = newName;
                        targetEntity.cost = newCost;
                        targetEntity.maxEntityLevel = newLevel;
                        targetEntity.upgradeBaseCost = newAttributeBaseCost;
                        targetEntity.upgradeCostMultiplier = newUpgradeCostMultiplier;
                    }, "Edit Attribute");
                }
            }

            // Space between columns
            GUILayout.Space(8);

            // =========================
            // Right Column (Sprite-Preview)
            // =========================
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(RIGHT_W)))
            {
                GUILayout.Label("Sprite Preview", EditorStyles.boldLabel);

                SpriteRenderer sr = null;

                switch (targetEntity.entityType)
                {
                    case IngameEntity.eEntityType.None:
                        break;
                    case IngameEntity.eEntityType.Planet:
                        sr = targetEntity.GetComponent<SpriteRenderer>();
                        break;
                    case IngameEntity.eEntityType.Weapon:
                        sr = targetEntity.transform.Find("SatelliteModel").GetComponent<SpriteRenderer>();
                        break;
                    case IngameEntity.eEntityType.Enemy:
                        sr = targetEntity.GetComponent<EnemyType>().enemyPrefabs[0].GetComponent<SpriteRenderer>();
                        break;
                    case IngameEntity.eEntityType.Background:
                        sr = targetEntity.GetComponent<SpriteRenderer>();
                        break;
                    default:
                        break;
                }

                var sprite = sr != null ? sr.sprite : null;

                // Preview-Box
                var previewRect = GUILayoutUtility.GetRect(PREVIEW, PREVIEW,
                    GUILayout.Width(PREVIEW), GUILayout.Height(PREVIEW));

                // Backgroundframe
                EditorGUI.DrawRect(new Rect(previewRect.x - 1, previewRect.y - 1, PREVIEW + 2, PREVIEW + 2),
                    new Color(0, 0, 0, 0.4f));

                if (sprite != null && sprite.texture != null)
                {
                    // UVs aus dem Sprite zurecht schneiden
                    Texture2D tex = sprite.texture;
                    Rect tr = sprite.textureRect;
                    Rect uv = new Rect(tr.x / tex.width,
                                       tr.y / tex.height,
                                       tr.width / tex.width,
                                       tr.height / tex.height);

                    // Bild skalierend (fit) einpassen
                    float texAspect = tr.width / tr.height;
                    float boxAspect = PREVIEW / PREVIEW; // = 1, aber lassen zur Klarheit

                    Rect draw = previewRect;
                    if (texAspect > boxAspect)
                    {
                        // breiter als hoch
                        float h = PREVIEW / texAspect;
                        draw.y += (PREVIEW - h) * 0.5f;
                        draw.height = h;
                    }
                    else
                    {
                        // höher als breit
                        float w = PREVIEW * texAspect;
                        draw.x += (PREVIEW - w) * 0.5f;
                        draw.width = w;
                    }

                    GUI.DrawTextureWithTexCoords(draw, tex, uv, true);
                }
                else
                {
                    EditorGUI.HelpBox(previewRect, "Kein SpriteRenderer oder Sprite.", MessageType.Info);
                }
            }
        }
    }


    private void DrawAttributeList()
    {
        EditorGUILayout.LabelField($"Attributes ({targetEntity.attribute?.Count ?? 0})", EditorStyles.boldLabel);

        // Farbwahl für Box-Hintergrund (dezente Töne, pro/hell angepasst)
        Color BoxTint(EntityAttribute.eAttributeType type, float v)
        {
            switch (type)
            {
                // --------------------
                // Positiv = Vorteil
                // --------------------
                case EntityAttribute.eAttributeType.PlanetStartHP:
                case EntityAttribute.eAttributeType.PlanetMaxHP:
                case EntityAttribute.eAttributeType.WeaponRotationSpeed:
                case EntityAttribute.eAttributeType.WeaponFireRate:
                case EntityAttribute.eAttributeType.WeaponProjectileSpeed:
                case EntityAttribute.eAttributeType.WeaponDamage:
                case EntityAttribute.eAttributeType.CoinChance:
                case EntityAttribute.eAttributeType.ScoreMultiplier:
                case EntityAttribute.eAttributeType.BonusCoinValue:
                case EntityAttribute.eAttributeType.ScoreBoostOnLowHP:
                    if (v > 0f) return Color.green;
                    if (v < 0f) return Color.red;
                    return Color.white;

                // --------------------
                // Positiv = Nachteil
                // --------------------
                case EntityAttribute.eAttributeType.EnemyHP:
                case EntityAttribute.eAttributeType.EnemySpeed:
                case EntityAttribute.eAttributeType.EnemyDamage:
                case EntityAttribute.eAttributeType.EnemySpawnRate:
                case EntityAttribute.eAttributeType.EnemySplitCount:
                case EntityAttribute.eAttributeType.EnemySplitChance:
                    if (v > 0f) return Color.red;
                    if (v < 0f) return Color.green;
                    return Color.white;

                // --------------------
                // Neutrale Effekte
                // --------------------
                case EntityAttribute.eAttributeType.PlanetRevive:
                case EntityAttribute.eAttributeType.PlanetExplosionOnHit:
                case EntityAttribute.eAttributeType.None:
                default:
                    return Color.white;
            }
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        var list = targetEntity.attribute;
        if (list == null)
        {
            Undo.RecordObject(targetEntity, "Init Attributes");
            targetEntity.attribute = new List<EntityAttribute>();
            list = targetEntity.attribute;
            MarkDirty(targetEntity);
        }

        for (int i = 0; i < list.Count; i++)
        {
            var attribute = list[i];

            // --- getönte Box nur für den Rahmen, nicht für Child-Controls ---
            var prevBg = GUI.backgroundColor;
            var tint = BoxTint(attribute.attributeType, attribute.initialValue);
            bool neutral = Mathf.Approximately(attribute.initialValue, 0f);

            if (!neutral) GUI.backgroundColor = tint;
            EditorGUILayout.BeginVertical("box");
            if (!neutral) GUI.backgroundColor = prevBg; // sofort zurücksetzen → Kinder nicht einfärben

            using (new EditorGUILayout.HorizontalScope())
            {
                var label = $"{i:00} • {attribute.attributeType}";
                bool isSel = selectedIndex == i;
                if (GUILayout.Toggle(isSel, label, "Button")) selectedIndex = i;

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("▲", GUILayout.Width(26)) && i > 0) { Move(list, i, i - 1); selectedIndex = i - 1; }
                if (GUILayout.Button("▼", GUILayout.Width(26)) && i < list.Count - 1) { Move(list, i, i + 1); selectedIndex = i + 1; }
                if (GUILayout.Button("Dup", GUILayout.Width(40))) { InsertDuplicate(list, i); selectedIndex = i + 1; }
                if (GUILayout.Button("Del", GUILayout.Width(40)))
                {
                    if (EditorUtility.DisplayDialog("Löschen?", $"Attribute {attribute.attributeType} entfernen?", "Löschen", "Abbrechen"))
                    {
                        RecordAnd(() => list.RemoveAt(i), "Remove Attribute");
                        EditorGUILayout.EndVertical();
                        break;
                    }
                }
            }

            if (selectedIndex == i)
            {
                EditorGUI.BeginChangeCheck();

                var newType = (EntityAttribute.eAttributeType)EditorGUILayout.EnumPopup("Attribute Type", attribute.attributeType);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextField(new GUIContent("Description"), attribute.GetDescription());

                var newInit = EditorGUILayout.FloatField("Initial Value", attribute.initialValue);
                var newInc = EditorGUILayout.FloatField("Attribute Increment", attribute.attributeIncrement);

                string effectPerLevel = "Effect per Level:\n";
                for (int j = 1; j <= targetEntity.maxEntityLevel; j++)
                    effectPerLevel += $"\tLvL {j} = {attribute.GetAttributeEffect(j):0.###}\n";
                EditorGUILayout.HelpBox(effectPerLevel, MessageType.None);

                if (EditorGUI.EndChangeCheck())
                {
                    RecordAnd(() =>
                    {
                        attribute.attributeType = newType;
                        attribute.initialValue = newInit;
                        attribute.attributeIncrement = newInc;
                    }, "Edit Attribute");
                }
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Neue Attribute hinzufügen", GUILayout.Height(24)))
            {
                RecordAnd(() =>
                {
                    targetEntity.attribute.Add(new EntityAttribute());
                    selectedIndex = targetEntity.attribute.Count - 1;
                }, "Add Attribute");
            }

            if (GUILayout.Button("Alles leeren", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Alle Attribute löschen?",
                    $"Alle {targetEntity.attribute.Count} Attribute von Entity ID {targetEntity.id} entfernen?", "Ja", "Nein"))
                {
                    RecordAnd(() => targetEntity.attribute.Clear(), "Clear Attribute");
                    selectedIndex = -1;
                }
            }
        }
    }


    // ---------- JSON UI ----------
    private void DrawJsonSection()
    {
        EditorGUILayout.LabelField("Backup / Restore", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save ALL to JSON")) SaveAllToJson();
            if (GUILayout.Button("Load ALL from JSON")) LoadAllFromJson();
        }
    }


    // ---------- DTOs für JSON ----------
    [Serializable]
    private class AllEntitiesSave { public List<EntitySave> entities = new(); }

    [Serializable]
    private class EntitySave
    {
        public int entityId;
        public string entityType;            // Enum als String
        public string entityName;
        public long cost;
        public int maxEntityLevel;
        public int upgradeBaseCost;
        public float upgradeCostMultiplier;
        public List<EntityAttribute> attributes;
    }


    // ---------- Scannen aller Kategorien ----------
    private IEnumerable<(GameObject prefab, IngameEntity info)> EnumerateAllEntities()
    {
        var catPaths = new[]
        {
            (IngameEntity.eEntityType.Planet,     PATH_PLANETS),
            (IngameEntity.eEntityType.Weapon,     PATH_WEAPONS),
            (IngameEntity.eEntityType.Enemy,      PATH_ENEMIES),
            (IngameEntity.eEntityType.Background, PATH_BACKGROUNDS),
        };

        foreach (var (_, path) in catPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            foreach (var guid in guids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (!prefab) continue;
                var info = prefab.GetComponent<IngameEntity>();
                if (!info) continue;
                yield return (prefab, info);
            }
        }
    }


    // ---------- Save/Load: ALL (alle Prefabs) ----------
    private void SaveAllToJson()
    {
        var all = new AllEntitiesSave();

        foreach (var (_, info) in EnumerateAllEntities())
        {
            all.entities.Add(new EntitySave
            {
                entityId = info.id,
                entityType = info.entityType.ToString(),
                entityName = info.itemName ?? "",
                cost = info.cost,
                maxEntityLevel = info.maxEntityLevel,
                upgradeBaseCost = info.upgradeBaseCost,
                upgradeCostMultiplier = info.upgradeCostMultiplier,
                attributes = info.attribute != null ? new List<EntityAttribute>(info.attribute) : new List<EntityAttribute>()
            });
        }

        var json = JsonUtility.ToJson(all, true);
        var suggested = $"AllEntities_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var path = EditorUtility.SaveFilePanel("Save ALL Entities JSON", Application.dataPath, suggested, "json");
        if (string.IsNullOrEmpty(path)) return;

        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
        Debug.Log($"All entities exported: {path} ({all.entities.Count} entries)");
    }

    private void LoadAllFromJson()
    {
        var path = EditorUtility.OpenFilePanel("Load ALL Entities JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        var json = File.ReadAllText(path);
        var all = JsonUtility.FromJson<AllEntitiesSave>(json);
        if (all == null || all.entities == null || all.entities.Count == 0)
        {
            EditorUtility.DisplayDialog("Fehler", "JSON enthält keine gültigen Entities.", "OK");
            return;
        }

        // Map der vorhandenen Prefabs (ID+Type) -> Info
        var map = new Dictionary<(int id, IngameEntity.eEntityType type), IngameEntity>();
        foreach (var (_, info) in EnumerateAllEntities())
            map[(info.id, info.entityType)] = info;

        int updated = 0, missing = 0;

        foreach (var entity in all.entities)
        {
            if (!Enum.TryParse(entity.entityType, ignoreCase: true, out IngameEntity.eEntityType type))
            {
                Debug.LogWarning($"Unbekannter EntityType in JSON: {entity.entityType}");
                continue;
            }

            if (!map.TryGetValue((entity.entityId, type), out var info))
            {
                missing++;
                Debug.LogWarning($"Kein passendes Prefab gefunden für ID={entity.entityId}, Type={entity.entityType}");
                continue;
            }

            Undo.RecordObject(info, "Load ALL Entities JSON");
            info.itemName = entity.entityName ?? "";
            info.cost = entity.cost;
            info.maxEntityLevel = entity.maxEntityLevel;
            info.upgradeBaseCost = entity.upgradeBaseCost;
            info.upgradeCostMultiplier = entity.upgradeCostMultiplier;
            info.attribute = entity.attributes != null ? new List<EntityAttribute>(entity.attributes) : new List<EntityAttribute>();
            EditorUtility.SetDirty(info);
            updated++;
        }

        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Import abgeschlossen",
            $"Aktualisiert: {updated}\nNicht gefunden: {missing}", "OK");
    }

    // ---------- Helpers ----------
    private void RecordAnd(Action act, string undoName)
    {
        Undo.RecordObject(targetEntity, undoName);
        act?.Invoke();
        MarkDirty(targetEntity);
    }

    private void MarkDirty(UnityEngine.Object obj)
    {
        EditorUtility.SetDirty(obj);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void Move<T>(List<T> list, int from, int to)
    {
        if (from == to) return;
        RecordAnd(() =>
        {
            var tmp = list[from];
            list.RemoveAt(from);
            list.Insert(to, tmp);
        }, "Reorder Attributes");
    }

    private void InsertDuplicate(List<EntityAttribute> list, int index)
    {
        var src = list[index];
        var clone = JsonUtility.FromJson<EntityAttribute>(JsonUtility.ToJson(src));
        RecordAnd(() => list.Insert(index + 1, clone), "Duplicate attributes");
    }
}
