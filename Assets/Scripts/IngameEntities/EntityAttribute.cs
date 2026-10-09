using UnityEngine;

[System.Serializable]
public class EntityAttribute
{
    // Numeric values are serialized in prefabs and attribute backups.
    public enum eAttributeType
    {
        None, PlanetStartHP, PlanetMaxHP, PlanetRevive, PlanetExplosionOnHit,
        WeaponRotationSpeed, WeaponFireRate, WeaponProjectileSpeed, WeaponDamage,
        EnemyHP, EnemySpeed, EnemyDamage, EnemySplitCount, EnemySplitChance,
        CoinChance, ScoreMultiplier, EnemySpawnRate, BonusCoinValue, ScoreBoostOnLowHP
    }

    public eAttributeType attributeType;
    public float initialValue = 1f;
    public float attributeIncrement = 2f;

    public float GetAttributeEffect(int level) => initialValue + (Mathf.Max(1, level) - 1) * attributeIncrement;
    public static bool IsMultiplier(eAttributeType type) => type == eAttributeType.ScoreMultiplier || type == eAttributeType.ScoreBoostOnLowHP;

    public string GetAttributeEffectString(int level)
    {
        float value = GetAttributeEffect(level);
        return attributeType switch
        {
            eAttributeType.PlanetRevive or eAttributeType.PlanetExplosionOnHit => value > 0 ? "Active" : "Inactive",
            eAttributeType.PlanetStartHP or eAttributeType.PlanetMaxHP or eAttributeType.EnemyHP => $"{Utilities.Round(value)} HP",
            eAttributeType.WeaponDamage or eAttributeType.EnemyDamage => $"{Utilities.Round(value)} DMG",
            eAttributeType.WeaponRotationSpeed => $"{value:0.##} °/s",
            eAttributeType.WeaponFireRate => $"{value:0.##} s/shot",
            eAttributeType.EnemySpawnRate => $"{value:0.##} s/spawn",
            eAttributeType.WeaponProjectileSpeed or eAttributeType.EnemySpeed => $"{value:0.##} u/s",
            eAttributeType.EnemySplitChance => $"{value * 100f:0.##}%",
            eAttributeType.CoinChance => $"{value * 100f:+0.##;-0.##;0}%",
            eAttributeType.ScoreMultiplier or eAttributeType.ScoreBoostOnLowHP => $"×{value:0.##}",
            eAttributeType.EnemySplitCount => $"{Utilities.Round(value)}",
            eAttributeType.BonusCoinValue => $"{Utilities.Round(value):+0;-0;0} coins",
            _ => value.ToString("0.##")
        };
    }

    public string GetUpgradeName() => GetAttributeName(attributeType);

    public static string GetAttributeName(eAttributeType type) => type switch
    {
        eAttributeType.PlanetStartHP => "Start HP",
        eAttributeType.PlanetMaxHP => "Max HP",
        eAttributeType.PlanetRevive => "Revive",
        eAttributeType.PlanetExplosionOnHit => "On-Hit Explosion",
        eAttributeType.WeaponRotationSpeed => "Weapon Rotation",
        eAttributeType.WeaponFireRate => "Shot Interval",
        eAttributeType.WeaponProjectileSpeed => "Projectile Speed",
        eAttributeType.WeaponDamage => "Weapon Damage",
        eAttributeType.EnemyHP => "Enemy HP",
        eAttributeType.EnemySpeed => "Enemy Speed",
        eAttributeType.EnemyDamage => "Enemy Damage",
        eAttributeType.EnemySplitCount => "Max Split Pieces",
        eAttributeType.EnemySplitChance => "Split Chance",
        eAttributeType.CoinChance => "Extra Coin Chance",
        eAttributeType.ScoreMultiplier => "Score Multiplier",
        eAttributeType.EnemySpawnRate => "Spawn Interval",
        eAttributeType.BonusCoinValue => "Extra Coin Value",
        eAttributeType.ScoreBoostOnLowHP => "Low HP Multiplier",
        _ => "None"
    };

    public string GetDescription() => attributeType switch
    {
        eAttributeType.PlanetStartHP => "Lives at the start of a round, limited by max HP.",
        eAttributeType.PlanetMaxHP => "Maximum lives the planet can hold.",
        eAttributeType.PlanetRevive => "Revives the planet once per round.",
        eAttributeType.PlanetExplosionOnHit => "An impulse wave destroys nearby enemies when hit.",
        eAttributeType.WeaponRotationSpeed => "Maximum aiming speed in degrees per second.",
        eAttributeType.WeaponFireRate => "Time between shots. Lower is faster.",
        eAttributeType.WeaponProjectileSpeed => "Projectile movement speed.",
        eAttributeType.WeaponDamage => "Damage dealt by each projectile.",
        eAttributeType.EnemyHP => "Damage needed to destroy an enemy.",
        eAttributeType.EnemySpeed => "Enemy movement speed before random variation.",
        eAttributeType.EnemyDamage => "Lives lost on impact.",
        eAttributeType.EnemySplitCount => "Maximum pieces created by a splitting enemy.",
        eAttributeType.EnemySplitChance => "Chance for a destroyed enemy to split.",
        eAttributeType.CoinChance => "Added to the base 15% coin chance when an item drops.",
        eAttributeType.ScoreMultiplier => "Multiplies earned score. Equipped multipliers combine.",
        eAttributeType.EnemySpawnRate => "Initial time between enemies. Lower is harder.",
        eAttributeType.BonusCoinValue => "Additional coins granted by a coin pickup.",
        eAttributeType.ScoreBoostOnLowHP => "Additional score multiplier while only one life remains.",
        _ => ""
    };
}
