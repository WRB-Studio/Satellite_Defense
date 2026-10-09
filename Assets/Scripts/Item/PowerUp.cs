using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum enumItemType { hearth, fireRate, shootUpgrade, coin, jumpLaser }
    public enumItemType itemType;
    [Min(0f)] public float fireRateUpgradeHeight = 0.1f;
    [Min(0)] public long scorePerCoin = 2500;
    [Min(1)] public int addPremiumCoins = 1;
    public AudioClip soundFireRate;
    public AudioClip soundShootUpgrade;
    public AudioClip soundCoin;
    public AudioClip soundJumpLaser;

    private float remainingLife;
    private bool removed;

    public void Init(float lifetime) => remainingLife = Mathf.Max(0f, lifetime);

    private void Update()
    {
        if (removed || !GameController.Instance.IsPlaying) return;
        remainingLife -= Time.deltaTime;
        if (remainingLife <= 0f) Despawn();
    }

    public void Collect()
    {
        var game = GameController.Instance;
        if (removed || !game.IsPlaying) return;
        removed = true;
        switch (itemType)
        {
            case enumItemType.hearth:
                game.ChangeLife(1);
                break;
            case enumItemType.fireRate:
                AudioController.PlaySound(soundFireRate);
                game.ActiveWeapon.FireRateUpgrade(fireRateUpgradeHeight);
                break;
            case enumItemType.shootUpgrade:
                AudioController.PlaySound(soundShootUpgrade);
                game.ActiveWeapon.UpgradeEmitters();
                break;
            case enumItemType.coin:
                AudioController.PlaySound(soundCoin);
                ScoreController.Instance.AddScore(scorePerCoin);
                PremiumCoinController.Instance.CollectCoin(addPremiumCoins, transform.position);
                break;
            case enumItemType.jumpLaser:
                AudioController.PlaySound(soundJumpLaser);
                game.ActiveWeapon.ActivateJumpLaser();
                break;
        }
        Despawn();
    }

    public void Despawn()
    {
        removed = true;
        if (PowerUpController.Instance) PowerUpController.Instance.Unregister(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (PowerUpController.Instance) PowerUpController.Instance.Unregister(this);
    }
}
