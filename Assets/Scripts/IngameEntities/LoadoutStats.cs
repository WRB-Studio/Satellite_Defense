using System.Collections.Generic;
using UnityEngine;

// Calculated once when equipment changes. Prefabs and saved progress remain the source data.
public sealed class LoadoutStats
{
    private readonly Dictionary<EntityAttribute.eAttributeType, float> values = new();

    public int StartLives { get; }
    public int MaxLives { get; }
    public int LowHealthThreshold => Mathf.Max(1, MaxLives / 4);
    public bool CanRevive => GetValue(EntityAttribute.eAttributeType.PlanetRevive) > 0f;
    public bool HasImpulseWave => GetValue(EntityAttribute.eAttributeType.PlanetExplosionOnHit) > 0f;

    public float WeaponRotationSpeed => Mathf.Max(0f, GetValue(EntityAttribute.eAttributeType.WeaponRotationSpeed, 40f));
    public float ShotInterval => GetValue(EntityAttribute.eAttributeType.WeaponFireRate, 1f);
    public float ProjectileSpeed => Mathf.Clamp(GetValue(EntityAttribute.eAttributeType.WeaponProjectileSpeed, 1f), 0.1f, 10f);
    public int WeaponDamage => Mathf.Max(1, Utilities.Round(GetValue(EntityAttribute.eAttributeType.WeaponDamage, 1f)));

    public int EnemyHealth => Mathf.Max(1, Utilities.Round(GetValue(EntityAttribute.eAttributeType.EnemyHP, 1f)));
    public float EnemySpeed => Mathf.Max(0.01f, GetValue(EntityAttribute.eAttributeType.EnemySpeed, 1f));
    public int EnemyDamage => Mathf.Max(1, Utilities.Round(GetValue(EntityAttribute.eAttributeType.EnemyDamage, 1f)));
    public float SpawnInterval => GetValue(EntityAttribute.eAttributeType.EnemySpawnRate, 2f);
    public int MaxSplitPieces => Mathf.Clamp(Utilities.Round(GetValue(EntityAttribute.eAttributeType.EnemySplitCount, 2f)), 0, 3);
    public float SplitChance => Mathf.Clamp(GetValue(EntityAttribute.eAttributeType.EnemySplitChance), 0f, 0.5f);

    public float CoinChance => Mathf.Clamp(0.15f + GetValue(EntityAttribute.eAttributeType.CoinChance), 0f, 0.45f);
    public int BonusCoinValue => Mathf.Clamp(Utilities.Round(GetValue(EntityAttribute.eAttributeType.BonusCoinValue)), 0, 3);
    public float ScoreMultiplier => Mathf.Clamp(GetValue(EntityAttribute.eAttributeType.ScoreMultiplier, 1f), 0.5f, 3f);
    public float LowHealthScoreMultiplier => Mathf.Clamp(GetValue(EntityAttribute.eAttributeType.ScoreBoostOnLowHP, 1f), 1f, 2.5f);

    public LoadoutStats(IEnumerable<IngameEntity> items, IReadOnlySavegame progress, int defaultStartLives, int defaultMaxLives)
    {
        foreach (var item in items)
        {
            if (!item || item.attribute == null) continue;
            int level = progress.GetLevel(item.entityType, item.id, item.maxEntityLevel);
            foreach (var attribute in item.attribute)
            {
                if (attribute == null || attribute.attributeType == EntityAttribute.eAttributeType.None) continue;
                var type = attribute.attributeType;
                bool multiply = EntityAttribute.IsMultiplier(type);
                float previous = GetValue(type, multiply ? 1f : 0f);
                float value = attribute.GetAttributeEffect(level);
                values[type] = multiply ? previous * Mathf.Max(0f, value) : previous + value;
            }
        }

        MaxLives = Mathf.Max(1, Utilities.Round(GetValue(EntityAttribute.eAttributeType.PlanetMaxHP, defaultMaxLives)));
        StartLives = Mathf.Clamp(Utilities.Round(GetValue(EntityAttribute.eAttributeType.PlanetStartHP, defaultStartLives)), 1, MaxLives);
    }

    public float GetValue(EntityAttribute.eAttributeType type, float fallback = 0f) =>
        values.TryGetValue(type, out float value) ? value : fallback;
}
