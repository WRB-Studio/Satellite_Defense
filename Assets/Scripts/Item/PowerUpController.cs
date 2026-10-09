using System.Collections.Generic;
using UnityEngine;

public class PowerUpController : MonoBehaviour
{
    public static PowerUpController Instance { get; private set; }
    public Transform spawnParent;
    [Range(0f, 1f)] public float dropChance = 0.2f;
    [Min(0.1f)] public float itemLifeTime = 5f;
    public GameObject itemHeart;
    public GameObject itemCoin;
    public GameObject itemFireRate;
    public GameObject itemShootUpgrade;
    public GameObject itemJumpLaser;
    public IReadOnlyList<PowerUp> Items => items;

    private readonly List<PowerUp> items = new();
    private readonly List<(GameObject prefab, float weight)> candidates = new(5);
    private int itemLayer;

    private void Awake()
    {
        Instance = this;
        itemLayer = LayerMask.GetMask("Items");
    }

    private void Update()
    {
        if (!GameController.Instance.IsPlaying) return;
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began) CollectAt(touch.position);
            }
        }
        else if (Input.GetMouseButtonDown(0)) CollectAt(Input.mousePosition);
    }

    private void CollectAt(Vector2 screenPosition)
    {
        if (Utilities.IsPointerOverUI(screenPosition)) return;
        var collider = Physics2D.OverlapPoint(Utilities.ScreenToWorld(screenPosition), itemLayer);
        if (collider && collider.TryGetComponent<PowerUp>(out var item)) item.Collect();
    }

    public void SpawnRandomItem(Vector2 position)
    {
        var game = GameController.Instance;
        if (!game.IsPlaying || !Utilities.IsInsideViewWithPadding(position, 0.5f) || Random.value >= dropChance) return;
        var weapon = game.ActiveWeapon;
        candidates.Clear();
        AddCandidate(itemHeart, PowerUp.enumItemType.hearth, game.CurrentLives < game.MaxLives ? 0.4f : 0f);
        AddCandidate(itemFireRate, PowerUp.enumItemType.fireRate, weapon.CanUpgradeFireRate ? 0.35f : 0f);
        AddCandidate(itemShootUpgrade, PowerUp.enumItemType.shootUpgrade, weapon.CanUpgradeEmitters ? 0.15f : 0f);
        AddCandidate(itemJumpLaser, PowerUp.enumItemType.jumpLaser, weapon.IsJumpLaserActive ? 0f : 0.1f);

        // CoinChance is the absolute probability within a successful drop, plus its base chance.
        float coinChance = Mathf.Clamp01(0.15f + game.GetAttribute(EntityAttribute.eAttributeType.CoinChance));
        GameObject prefab = null;
        if (itemCoin && !HasItem(PowerUp.enumItemType.coin) && Random.value < coinChance) prefab = itemCoin;
        else
        {
            float total = 0f;
            foreach (var candidate in candidates) total += candidate.weight;
            if (total <= 0f) return;
            float roll = Random.value * total;
            foreach (var candidate in candidates)
            {
                roll -= candidate.weight;
                if (roll > 0f) continue;
                prefab = candidate.prefab;
                break;
            }
        }
        if (!prefab) return;
        AudioController.PlaySound(prefab == itemJumpLaser ? AudioController.Instance.soundPlanetDeath : AudioController.Instance.soundItemDrop);
        var item = Instantiate(prefab, position, prefab.transform.rotation, spawnParent).GetComponent<PowerUp>();
        items.Add(item);
        item.Init(itemLifeTime);
    }

    private void AddCandidate(GameObject prefab, PowerUp.enumItemType type, float weight)
    {
        if (prefab && weight > 0f && !HasItem(type)) candidates.Add((prefab, weight));
    }

    private bool HasItem(PowerUp.enumItemType type)
    {
        foreach (var item in items)
            if (item && item.itemType == type) return true;
        return false;
    }

    public void Unregister(PowerUp item) => items.Remove(item);

    public void RemoveAllItems()
    {
        while (items.Count > 0)
        {
            var item = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            if (item) item.Despawn();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
