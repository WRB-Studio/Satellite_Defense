using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Type = EntityAttribute.eAttributeType;

public static partial class ProjectPlayValidation
{
    private static IEnumerator WeaponGates()
    {
        yield return Load(ProjectValidation.GameScene); QuietCombat();
        var weapon = GameController.Instance.ActiveWeapon; var enemies = EnemyController.Instance;
        weapon.UpgradeEmitters(); Require(weapon.WeaponLevel == 1, "Emitter bypassed interval gate.");
        for (int i = 0; i < enemies.weaponEmitterAddByKills + 2; i++) enemies.AddKill();
        Require(weapon.WeaponLevel == 1, "Ineligible kills unlocked emitter.");
        Set(weapon, "shotInterval", weapon.secondEmitterShotInterval + .001f); Require(!weapon.CanUpgradeEmitters, "Gate above boundary accepted.");
        Set(weapon, "shotInterval", weapon.secondEmitterShotInterval); Require(weapon.CanUpgradeEmitters, "Exact second boundary rejected.");
        for (int i = 0; i < enemies.weaponEmitterAddByKills - 1; i++) enemies.AddKill();
        Require(weapon.WeaponLevel == 1, "Old ineligible kills were banked.");
        enemies.AddKill(); Require(weapon.WeaponLevel == 2, "Eligible kill threshold failed.");
        weapon.UpgradeEmitters(); Require(weapon.WeaponLevel == 2, "Third emitter bypassed stricter gate.");
        Set(weapon, "shotInterval", weapon.thirdEmitterShotInterval + .001f); Require(!weapon.CanUpgradeEmitters, "Third gate above boundary accepted.");
        Set(weapon, "shotInterval", weapon.thirdEmitterShotInterval - .001f); Require(weapon.CanUpgradeEmitters, "Third gate below boundary rejected.");
        Set(weapon, "shotInterval", weapon.thirdEmitterShotInterval);
        Pickup(PowerUpController.Instance.itemShootUpgrade).Collect(); Require(weapon.WeaponLevel == 3, "Eligible pickup failed.");
        weapon.UpgradeEmitters(); Require(weapon.WeaponLevel == 3, "Emitter exceeded three.");
        weapon.ReduceShotInterval(float.MaxValue); Near(Get<float>(weapon, "shotInterval"), weapon.minShotInterval, "Shot interval floor");
        weapon.ReduceShotInterval(-1); Near(Get<float>(weapon, "shotInterval"), weapon.minShotInterval, "Negative reduction must not worsen interval");
        GameController.Instance.ActivePlanet.Hit(1);
        Require(weapon.WeaponLevel == 1 && Get<int>(enemies, "killsSinceUpgrade") == 0, "Planet hit did not reset emitters/counter.");
        Near(Get<float>(weapon, "shotInterval"), weapon.minShotInterval, "Planet hit must retain interval improvement");
        Set(weapon, "shotInterval", weapon.secondEmitterShotInterval + .1f);
        Pickup(PowerUpController.Instance.itemShootUpgrade).Collect(); Require(weapon.WeaponLevel == 1, "Old pickup bypassed gate.");
    }

    private static IEnumerator SplitSafety()
    {
        yield return Load(ProjectValidation.GameScene); QuietCombat();
        var controller = EnemyController.Instance; var game = GameController.Instance;
        Set(controller, "splitChance", 1f); Set(controller, "splitPieces", 3);
        var planet = game.ActivePlanet.GetComponent<Collider2D>(); Physics2D.SyncTransforms();
        float radius = planet.bounds.extents.y;
        var enemy = EnemyAt((Vector2)game.ActivePlanet.transform.position + Vector2.up * (radius + controller.minSplitDistanceFromPlanet));
        Require(!controller.TrySplit(enemy), "Near-surface split permitted.");
        float spread = enemy.transform.lossyScale.x * .75f;
        enemy.transform.position = game.ActivePlanet.transform.position + Vector3.up * (radius + controller.minSplitDistanceFromPlanet + spread + .1f);
        Physics2D.SyncTransforms();
        Require(controller.TrySplit(enemy) && controller.Enemies.Count >= 2 && controller.Enemies.Count <= 3, "Far split failed or count wrong.");
        foreach (var fragment in controller.Enemies)
        {
            Require(fragment.isSplitPiece && !controller.TrySplit(fragment), "Fragment split recursively.");
            Require(Get<int>(fragment, "healthPoints") == Math.Max(1, Mathf.CeilToInt(game.Stats.EnemyHealth * .5f)) && Get<int>(fragment, "damage") == Math.Max(1, Mathf.CeilToInt(game.Stats.EnemyDamage * .5f)), "Fragment HP/damage must halve with ceiling.");
            Vector2 point = fragment.transform.position;
            Require(Vector2.Distance(planet.ClosestPoint(point), point) >= controller.minSplitDistanceFromPlanet - .001f, "Fragment entered protected buffer.");
        }
        enemy.Despawn(); controller.RemoveAllEnemies();
        var alreadyFragment = EnemyAt(new Vector2(0, 3), true);
        Require(!controller.TrySplit(alreadyFragment), "Standalone fragment split."); alreadyFragment.Despawn();
        yield return null;
    }

    private static IEnumerator Difficulty()
    {
        yield return Load(ProjectValidation.GameScene); QuietCombat(); var controller = EnemyController.Instance;
        Set(controller, "spawnCount", 0); Near(controller.EnemySpeedMultiplier, 1, "Initial speed ramp");
        var enemy = EnemyAt(new Vector2(0, 3)); float speed = Get<float>(enemy, "moveSpeed");
        Set(controller, "spawnCount", controller.spawnsToMaxDifficulty); Near(controller.EnemySpeedMultiplier, 1 + controller.maxSpeedIncrease, "Final speed ramp");
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Near(enemy.GetComponent<Rigidbody2D>().linearVelocity.magnitude, speed * (1 + controller.maxSpeedIncrease), "Live enemy ignores ramp", .01f);
        Set(controller, "spawnCount", controller.spawnsToMaxDifficulty * 10); Near(controller.EnemySpeedMultiplier, 1 + controller.maxSpeedIncrease, "Speed ramp cap"); enemy.Despawn();
    }

    private static IEnumerator ProjectilePhysics()
    {
        yield return Load(ProjectValidation.GameScene); QuietCombat(); var game = GameController.Instance;
        var enemy = EnemyAt(new Vector2(0, 3));
        Set(enemy, "healthPoints", 10); Set(enemy, "moveSpeed", 0f); enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        var bullet = Object.Instantiate(game.ActiveWeapon.normalLaserPrefab, new Vector3(0, 2, 0), Quaternion.identity, game.EffectsRoot).GetComponent<Bullet>();
        bullet.Init(game.ActiveWeapon, Vector2.up, false, Color.white, false, 3, 5);
        yield return new WaitForSecondsRealtime(.5f);
        Require(Get<int>(enemy, "healthPoints") == 7, "Real physics contact did not apply supplied damage exactly once.");
        enemy.Hit(100, enemy.transform.position); int kills = EnemyController.Instance.Kills; long score = ScoreController.Instance.Score;
        enemy.Hit(100, enemy.transform.position); Require(EnemyController.Instance.Kills == kills && ScoreController.Instance.Score == score, "Repeated lethal hit duplicated rewards.");
        yield return null;
        var expiring = Object.Instantiate(game.ActiveWeapon.normalLaserPrefab, new Vector3(0, 3, 0), Quaternion.identity, game.EffectsRoot).GetComponent<Bullet>();
        expiring.lifeTime = .1f; expiring.Init(game.ActiveWeapon, Vector2.up, false, Color.white, false, 1, .1f);
        game.Pause(); yield return new WaitForSecondsRealtime(.2f); Require(expiring && expiring.gameObject.activeSelf, "Paused bullet expired.");
        game.Resume(); yield return new WaitForSecondsRealtime(.2f); Require(!expiring || !expiring.gameObject.activeSelf, "Bullet lifetime did not expire.");
        game.ActiveWeapon.ReduceShotInterval(float.MaxValue); game.ActiveWeapon.UpgradeEmitters(); game.ActiveWeapon.UpgradeEmitters();
        game.ActiveWeapon.ClearBullets(); Call(game.ActiveWeapon, "Fire"); Require(Get<List<Bullet>>(game.ActiveWeapon, "bullets").Count == 3, "Normal volley missing emitters.");
        game.ActiveWeapon.ClearBullets(); game.ActiveWeapon.ActivateJumpLaser(); Call(game.ActiveWeapon, "Fire");
        var jump = Get<List<Bullet>>(game.ActiveWeapon, "bullets").Single(); Require(Get<int>(jump, "hitsRemaining") == jump.maxLaserJumpHits + 1, "Jump hit budget wrong.");
        game.ActiveWeapon.ClearBullets(); yield return null; Require(Get<List<Bullet>>(game.ActiveWeapon, "bullets").Count == 0, "Bullet cleanup retained ownership references.");
        var targets = new List<Enemy>();
        for (int i = 0; i < 4; i++)
        {
            var target = EnemyAt(new Vector2(0, 2.5f + i * .6f));
            Set(target, "healthPoints", 10); Set(target, "moveSpeed", 0f);
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            targets.Add(target);
        }
        var chain = Object.Instantiate(game.ActiveWeapon.jumpLaserPrefab, new Vector3(0, 1.5f, 0), Quaternion.identity, game.EffectsRoot).GetComponent<Bullet>();
        chain.Init(game.ActiveWeapon, Vector2.up, true, Color.white, false, 3, 5);
        int budget = chain.maxLaserJumpHits + 1;
        yield return new WaitForSecondsRealtime(1f);
        Require(targets.Count(target => Get<int>(target, "healthPoints") == 7) == Math.Min(budget, targets.Count), "Jump laser did not hit its allowed distinct targets once each.");
        Require(targets.All(target => Get<int>(target, "healthPoints") == 7 || Get<int>(target, "healthPoints") == 10), "Jump laser hit one target repeatedly.");
        foreach (var target in targets) target.Despawn();
    }

    private static IEnumerator PlanetAbilities()
    {
        yield return Equip(item => item is Planet && item.GetAttributeByType(Type.PlanetRevive)?.GetAttributeEffect(1) > 0);
        var game = GameController.Instance; game.ActivePlanet.Hit(game.MaxLives);
        Require(game.IsPlaying && game.CurrentLives == game.Stats.StartLives, "First lethal hit must revive.");
        game.ActivePlanet.Hit(game.MaxLives); Require(game.State == GameController.GameState.GameOver, "Second lethal hit must end round.");
        yield return Equip(item => item is Planet && item.GetAttributeByType(Type.PlanetExplosionOnHit)?.GetAttributeEffect(1) > 0);
        game = GameController.Instance; game.ChangeLife(int.MaxValue, false); game.ActivePlanet.Hit(1);
        var wave = Get<GameObject>(game.ActivePlanet, "currentImpulseWave"); Require(wave, "Impulse ability did not spawn wave.");
        game.ActivePlanet.Hit(1); Require(Get<GameObject>(game.ActivePlanet, "currentImpulseWave") == wave, "Cooldown allowed duplicate wave.");
        float timer = Get<float>(game.ActivePlanet, "impulseCooldownRemaining");
        game.Pause(); yield return new WaitForSecondsRealtime(.1f); Near(Get<float>(game.ActivePlanet, "impulseCooldownRemaining"), timer, "Impulse timer must pause"); game.Resume();
    }

    private static IEnumerator ScoreThreshold()
    {
        yield return Equip(item => item is Planet && item.GetAttributeByType(Type.ScoreBoostOnLowHP)?.GetAttributeEffect(1) > 1);
        var game = GameController.Instance; int threshold = game.Stats.LowHealthThreshold;
        game.ChangeLife(int.MaxValue, false); game.ChangeLife(-(game.CurrentLives - Math.Min(game.MaxLives, threshold + 1)), false);
        ScoreController.Instance.ResetScore(); ScoreController.Instance.AddScore(100);
        long normal = Utilities.Round(100d * ScoreController.Instance.multiplier * game.Stats.ScoreMultiplier);
        Require(ScoreController.Instance.Score == normal, "Low-HP bonus above threshold.");
        game.ChangeLife(-1, false); ScoreController.Instance.ResetScore(); ScoreController.Instance.AddScore(100);
        Require(ScoreController.Instance.Score == Utilities.Round(100d * ScoreController.Instance.multiplier * game.Stats.ScoreMultiplier * game.Stats.LowHealthScoreMultiplier), "Low-HP bonus missing at threshold.");
        long score = ScoreController.Instance.Score; ScoreController.Instance.AddScore(double.NaN); ScoreController.Instance.AddScore(-1); ScoreController.Instance.AddScore(double.PositiveInfinity);
        Require(ScoreController.Instance.Score == score, "Invalid score altered total.");
    }
}
