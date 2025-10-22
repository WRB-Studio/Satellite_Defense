using UnityEngine;

[System.Serializable]
public class EntityAttribute
{
    public enum eAttributeType
    {
        None,

        //Planet
        PlanetStartHP,
        PlanetMaxHP,
        PlanetRevive,
        PlanetExplosionOnHit,

        //Weapon
        WeaponRotationSpeed,
        WeaponFireRate,
        WeaponProjectileSpeed,
        WeaponDamage,

        //Enemy
        EnemyHP,
        EnemySpeed,
        EnemyDamage,
        EnemySplitCount,
        EnemySplitChance,

        //General
        CoinChance,
        ScoreMultiplier,
        EnemySpawnRate,
        BonusCoinValue,
        ScoreBoostOnLowHP,
    }

    public eAttributeType attributeType = eAttributeType.None;

    public float initialValue = 1f;
    public float attributeIncrement = 2f;

    public float GetAttributeEffect(int level) => initialValue + ((level - 1) * attributeIncrement);

    public string GetAttributeEffectString(int level)
    {
        float effect = GetAttributeEffect(level);
        if (effect % 1 != 0) effect = Mathf.Round(effect * 100f) / 100f;

        if (effect < 0f) return effect.ToString();

        string prefix = "";
        
        switch (attributeType)
        {
            // --- Planet ---
            case eAttributeType.PlanetStartHP:
                prefix = "+";
                break;
            case eAttributeType.PlanetMaxHP:
                prefix = "+";
                break;
            case eAttributeType.PlanetRevive:
                prefix = "";
                break;
            case eAttributeType.PlanetExplosionOnHit:
                prefix = "";
                break;

            // --- Weapon ---
            case eAttributeType.WeaponRotationSpeed:
                prefix = "+";
                break;
            case eAttributeType.WeaponFireRate:
                prefix = "+";
                break;
            case eAttributeType.WeaponProjectileSpeed:
                prefix = "+";
                break;
            case eAttributeType.WeaponDamage:
                prefix = "+";
                break;

            // --- Enemy ---
            case eAttributeType.EnemyHP:
                prefix = "+";
                break;
            case eAttributeType.EnemySpeed:
                prefix = "+";
                break;
            case eAttributeType.EnemyDamage:
                prefix = "+";
                break;
            case eAttributeType.EnemySplitCount:
                prefix = "+";
                break;
            case eAttributeType.EnemySplitChance:
                prefix = "+";
                break;

            // --- General ---
            case eAttributeType.CoinChance:
                prefix = "+";
                break;
            case eAttributeType.ScoreMultiplier:
                prefix = "x";
                break;
            case eAttributeType.EnemySpawnRate:
                prefix = "+";
                break;
            case eAttributeType.BonusCoinValue:
                prefix = "+";
                break;
            case eAttributeType.ScoreBoostOnLowHP:
                prefix = "x";
                break;

            // --- None / Default ---
            case eAttributeType.None:
            default:
                prefix = "ERROR GetUpgradeEffectString";
                break;
        }

        return prefix + effect.ToString();
    }

    public string GetDescription()
    {
        switch (attributeType)
        {
            case eAttributeType.PlanetStartHP:
                return initialValue > 0 ? "Planet starts with more lives." : "Planet starts with fewer lives.";

            case eAttributeType.PlanetMaxHP:
                return initialValue > 0 ? "Planet's max lives increased." : "Planet's max lives reduced.";

            case eAttributeType.PlanetRevive:
                return "Planet revives once after destruction."; // neutral

            case eAttributeType.PlanetExplosionOnHit:
                return "On hit, an explosion kills all enemies in range."; // neutral


            case eAttributeType.WeaponRotationSpeed:
                return initialValue > 0 ? "Faster weapon rotation." : "Slower weapon rotation.";

            case eAttributeType.WeaponFireRate:
                return initialValue > 0 ? "Faster weapon fire rate." : "Slower weapon fire rate.";

            case eAttributeType.WeaponProjectileSpeed:
                return initialValue > 0 ? "Faster projectile speed." : "Slower projectile speed.";

            case eAttributeType.WeaponDamage:
                return initialValue > 0 ? "Higher weapon damage." : "Lower weapon damage.";


            case eAttributeType.EnemyHP:
                return initialValue > 0 ? "Enemies have more HP." : "Enemies have less HP.";

            case eAttributeType.EnemySpeed:
                return initialValue > 0 ? "Enemies move faster." : "Enemies move slower.";

            case eAttributeType.EnemyDamage:
                return initialValue > 0 ? "Enemies deal more damage." : "Enemies deal less damage.";

            case eAttributeType.EnemySplitCount:
                return initialValue > 0 ? "Enemies can split into more units." : "Enemies can split into fewer units.";

            case eAttributeType.EnemySplitChance:
                return initialValue > 0 ? "Enemies split more often." : "Enemies split less often.";


            case eAttributeType.CoinChance:
                return initialValue > 0 ? "Higher coin drop chance." : "Lower coin drop chance.";

            case eAttributeType.ScoreMultiplier:
                return initialValue > 0 ? "Higher score multiplier." : "Lower score multiplier.";

            case eAttributeType.EnemySpawnRate:
                return initialValue > 0 ? "Higher enemy spawn rate." : "Lower enemy spawn rate.";

            case eAttributeType.BonusCoinValue:
                return initialValue > 0 ? "Coins are worth more." : "Coins are worth less.";

            case eAttributeType.ScoreBoostOnLowHP:
                return initialValue > 0 ? "Extra score on low HP." : "Less score on low HP.";

            default:
                return "No description.";
        }
    }

    public string GetUpgradeName()
    {
        switch (attributeType)
        {
            // Planet
            case eAttributeType.PlanetStartHP: return "Start HP";
            case eAttributeType.PlanetMaxHP: return "Max HP";
            case eAttributeType.PlanetRevive: return "Revive";
            case eAttributeType.PlanetExplosionOnHit: return "On-Hit Explosion";

            // Weapon
            case eAttributeType.WeaponRotationSpeed: return "Weapon Rotation";
            case eAttributeType.WeaponFireRate: return "Fire Rate";
            case eAttributeType.WeaponProjectileSpeed: return "Projectile Speed";
            case eAttributeType.WeaponDamage: return "Weapon Damage";

            // Enemy
            case eAttributeType.EnemyHP: return "Enemy HP";
            case eAttributeType.EnemySpeed: return "Enemy Speed";
            case eAttributeType.EnemyDamage: return "Enemy Damage";
            case eAttributeType.EnemySplitCount: return "Enemy Split Count";
            case eAttributeType.EnemySplitChance: return "Enemy Split Chance";

            // General
            case eAttributeType.CoinChance: return "Coin Drop Chance";
            case eAttributeType.ScoreMultiplier: return "Score Multiplier";
            case eAttributeType.EnemySpawnRate: return "Enemy Spawn Rate";
            case eAttributeType.BonusCoinValue: return "Coin Value";
            case eAttributeType.ScoreBoostOnLowHP: return "Low HP Score";

            default: return "None";
        }
    }

    public static string GetAttributeName(eAttributeType eUpgradeType)
    {
        switch (eUpgradeType)
        {
            // Planet
            case eAttributeType.PlanetStartHP: return "Start HP";
            case eAttributeType.PlanetMaxHP: return "Max HP";
            case eAttributeType.PlanetRevive: return "Revive";
            case eAttributeType.PlanetExplosionOnHit: return "On-Hit Explosion";

            // Weapon
            case eAttributeType.WeaponRotationSpeed: return "Weapon Rotation";
            case eAttributeType.WeaponFireRate: return "Fire Rate";
            case eAttributeType.WeaponProjectileSpeed: return "Projectile Speed";
            case eAttributeType.WeaponDamage: return "Weapon Damage";

            // Enemy
            case eAttributeType.EnemyHP: return "Enemy HP";
            case eAttributeType.EnemySpeed: return "Enemy Speed";
            case eAttributeType.EnemyDamage: return "Enemy Damage";
            case eAttributeType.EnemySplitCount: return "Enemy Split Count";
            case eAttributeType.EnemySplitChance: return "Enemy Split Chance";

            // General
            case eAttributeType.CoinChance: return "Coin Drop Chance";
            case eAttributeType.ScoreMultiplier: return "Score Multiplier";
            case eAttributeType.EnemySpawnRate: return "Enemy Spawn Rate";
            case eAttributeType.BonusCoinValue: return "Coin Value";
            case eAttributeType.ScoreBoostOnLowHP: return "Low HP Score";

            default: return "None";
        }
    }

}