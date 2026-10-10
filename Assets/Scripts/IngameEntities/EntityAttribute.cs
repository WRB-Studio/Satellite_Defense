using UnityEngine;

[System.Serializable]
public class EntityAttribute
{
    // Numeric values are serialized in prefabs and attribute backups.
    public enum eAttributeType
    {
        None = 0,
        PlanetStartHP = 1,
        PlanetMaxHP = 2,
        PlanetRevive = 3,
        PlanetExplosionOnHit = 4,
        WeaponRotationSpeed = 5,
        WeaponFireRate = 6,
        WeaponProjectileSpeed = 7,
        WeaponDamage = 8,
        EnemyHP = 9,
        EnemySpeed = 10,
        EnemyDamage = 11,
        EnemySplitCount = 12,
        EnemySplitChance = 13,
        CoinChance = 14,
        ScoreMultiplier = 15,
        EnemySpawnRate = 16,
        BonusCoinValue = 17,
        ScoreBoostOnLowHP = 18
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
        eAttributeType.PlanetExplosionOnHit => "An impulse wave destroys nearby enemies when hit, with a cooldown. No rewards for wave kills.",
        eAttributeType.WeaponRotationSpeed => "Maximum aiming speed in degrees per second.",
        eAttributeType.WeaponFireRate => "Time between shots. Lower is faster. Multiple-emitter upgrades only unlock at short intervals.",
        eAttributeType.WeaponProjectileSpeed => "Projectile movement speed.",
        eAttributeType.WeaponDamage => "Damage dealt by each projectile.",
        eAttributeType.EnemyHP => "Damage needed to destroy an enemy. Split pieces have half HP, rounded up.",
        eAttributeType.EnemySpeed => "Enemy movement speed before random variation.",
        eAttributeType.EnemyDamage => "Lives lost on impact. Split pieces deal half damage, rounded up.",
        eAttributeType.EnemySplitCount => "Maximum pieces created by a splitting enemy, limited to three.",
        eAttributeType.EnemySplitChance => "Chance for a destroyed enemy to split. Equipped bonuses add, limited to 50%.",
        eAttributeType.CoinChance => "Added to the base 15% coin chance within an eligible drop, limited to 45% total.",
        eAttributeType.ScoreMultiplier => "Multiplies earned score. Equipped multipliers combine, limited to x3.",
        eAttributeType.EnemySpawnRate => "Initial time between enemies. Gradually falls to half this interval, with a 0.45 second minimum.",
        eAttributeType.BonusCoinValue => "Additional coins granted by a coin pickup, limited to three extra coins.",
        eAttributeType.ScoreBoostOnLowHP => "Additional multiplier at 25% max lives or less, rounded down to at least one life. Combined bonus limited to x2.5.",
        _ => ""
    };
}
