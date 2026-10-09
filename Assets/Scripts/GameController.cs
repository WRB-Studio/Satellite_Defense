using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public enum GameState { MainMenu, Shop, Playing, Paused, GameOver }
    public static GameController Instance { get; private set; }
    public GameState State { get; private set; } = GameState.MainMenu;
    public bool IsPlaying => State == GameState.Playing;
    public bool IsSimulationRunning => State == GameState.Playing || State == GameState.MainMenu;
    public bool IsInitialized { get; private set; }

    [Header("Lives")]
    public Image imgLive;
    [Min(1)] public int startLifes = 3;
    [Min(1)] public int maxLives = 5;
    public Transform imgLiveParent;
    public int CurrentLives { get; private set; }

    [Header("Shop catalog")]
    public GameObject[] planetPrefabs;
    public GameObject[] weaponPrefabs;
    public GameObject[] backgroundPrefabs;
    public GameObject[] enemyTypePrefabs;

    [Header("World")]
    public Transform planetParent;
    public Transform weaponParent;
    public Transform backgroundParent;
    public GameObject star;

    [Header("Input")]
    public bool enableJoystickControll;
    public GameObject joystickGO;
    public Joystick joystick;

    public Planet ActivePlanet { get; private set; }
    public Weapon ActiveWeapon { get; private set; }
    public Background ActiveBackground { get; private set; }
    public Transform EffectsRoot { get; private set; }
    public Camera GameCamera { get; private set; }
    public long LastRoundScore { get; private set; }
    public long LastRoundCoins { get; private set; }
    public bool LastRoundWasBest { get; private set; }

    private readonly Dictionary<IngameEntity.eEntityType, IngameEntity> activeItems = new();
    private readonly Dictionary<EntityAttribute.eAttributeType, float> attributes = new();
    private Coroutine starRoutine;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        GameCamera = Camera.main;
        EffectsRoot = new GameObject("Effects").transform;
        EffectsRoot.SetParent(transform);
    }

    private void Start() => Init();

    public void Init()
    {
        if (IsInitialized) return;
        SaveGameController.Load();
        UIController.Instance.Init();
        PremiumCoinController.Instance.Init();
        IsInitialized = true;
        ReturnToMainMenu();
        SaveGameController.Save();
        UIController.Instance.FadeOutSplashScreen();
    }

    private void Update()
    {
        if (!IsInitialized) return;
        SaveGameController.Tick(Time.unscaledDeltaTime);
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        switch (State)
        {
            case GameState.Playing: Pause(); break;
            case GameState.Paused: Resume(); break;
            case GameState.Shop: CloseShop(); break;
            case GameState.GameOver: ReturnToMainMenu(); break;
            case GameState.MainMenu: UIMainMenu.Instance.ExitGame(); break;
        }
    }

    private void SetState(GameState state)
    {
        State = state;
        Time.timeScale = state == GameState.Paused || state == GameState.Shop ? 0f : 1f;
        if (joystickGO) joystickGO.SetActive(enableJoystickControll && IsSimulationRunning);
    }

    public void StartNewGame()
    {
        if (!SaveGameController.CanSave)
        {
            UIController.Instance.ShowSaveMessage(SaveGameController.LastError);
            return;
        }
        SaveGameController.Save();
        SetState(GameState.Playing);
        RebuildLoadout();
        ScoreController.Instance.ResetScore();
        ResetLives();
        UIController.Instance.ShowMenu(UIController.eMenuType.IngameMenu);
        AudioController.PlayMusic(AudioController.Instance.ingameMusic);
    }

    public void ReturnToMainMenu()
    {
        SaveGameController.Save();
        SetState(GameState.MainMenu);
        RebuildLoadout();
        ScoreController.Instance.ResetScore();
        ResetLives();
        UIController.Instance.ShowMenu(UIController.eMenuType.MainMenu);
        AudioController.PlayMusic(AudioController.Instance.mainMenuMusic);
    }

    public void OpenShop()
    {
        if (State != GameState.MainMenu) return;
        SetState(GameState.Shop);
        UIController.Instance.ShowMenu(UIController.eMenuType.Shop);
    }

    public void CloseShop()
    {
        if (State != GameState.Shop) return;
        SetState(GameState.MainMenu);
        UIController.Instance.ShowMenu(UIController.eMenuType.MainMenu);
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        SetState(GameState.Paused);
        SaveGameController.Save();
        UIController.Instance.ShowMenu(UIController.eMenuType.PauseMenu);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        SetState(GameState.Playing);
        UIController.Instance.ShowMenu(UIController.eMenuType.IngameMenu);
    }

    public void EndRound()
    {
        if (!IsPlaying) return;
        SetState(GameState.GameOver);
        LastRoundScore = ScoreController.Instance.Score;
        LastRoundCoins = LastRoundScore / System.Math.Max(1, PremiumCoinController.Instance.premiumCoinsPerScore);
        LastRoundWasBest = LastRoundScore > SaveGameController.Data.BestScore;
        SaveGameController.RecordRound(LastRoundScore, LastRoundCoins);

        EnemyController.Instance.RemoveAllEnemies();
        PowerUpController.Instance.RemoveAllItems();
        if (ActiveWeapon) ActiveWeapon.DestroyWeapon();
        UIController.Instance.ShowMenu(UIController.eMenuType.GameOverMenu);
    }

    public GameObject[] GetCategoryItems(IngameEntity.eEntityType category) => category switch
    {
        IngameEntity.eEntityType.Planet => planetPrefabs,
        IngameEntity.eEntityType.Weapon => weaponPrefabs,
        IngameEntity.eEntityType.Background => backgroundPrefabs,
        IngameEntity.eEntityType.Enemy => enemyTypePrefabs,
        _ => System.Array.Empty<GameObject>()
    };

    public IngameEntity GetActiveItem(IngameEntity.eEntityType category) =>
        activeItems.TryGetValue(category, out var item) ? item : null;

    public bool SelectItem(IngameEntity item)
    {
        if (State != GameState.Shop || !IsCatalogItem(item) || !SaveGameController.Select(item)) return false;
        RebuildLoadout();
        return true;
    }

    public bool PurchaseItem(IngameEntity item)
    {
        if (State != GameState.Shop || !IsCatalogItem(item) || !SaveGameController.TryPurchase(item)) return false;
        RebuildLoadout();
        return true;
    }

    private bool IsCatalogItem(IngameEntity item) =>
        item && System.Array.IndexOf(GetCategoryItems(item.entityType), item.gameObject) >= 0;

    private void RebuildLoadout()
    {
        ClearWorld();
        activeItems.Clear();
        foreach (var category in new[] { IngameEntity.eEntityType.Planet, IngameEntity.eEntityType.Weapon,
                     IngameEntity.eEntityType.Background, IngameEntity.eEntityType.Enemy })
            activeItems.Add(category, SaveGameController.ResolveActiveItem(category, GetCategoryItems(category)));

        attributes.Clear();
        foreach (var item in activeItems.Values)
        {
            foreach (var attribute in item.attribute)
            {
                if (attribute == null || attribute.attributeType == EntityAttribute.eAttributeType.None) continue;
                var type = attribute.attributeType;
                bool multiply = EntityAttribute.IsMultiplier(type);
                float previous = GetAttribute(type, multiply ? 1f : 0f);
                float value = attribute.GetAttributeEffect(item.Level);
                attributes[type] = multiply ? previous * Mathf.Max(0f, value) : previous + value;
            }
        }

        ActivePlanet = Instantiate(GetActiveItem(IngameEntity.eEntityType.Planet).gameObject, planetParent).GetComponent<Planet>();
        ActiveWeapon = Instantiate(GetActiveItem(IngameEntity.eEntityType.Weapon).gameObject, weaponParent).GetComponent<Weapon>();
        ActiveBackground = Instantiate(GetActiveItem(IngameEntity.eEntityType.Background).gameObject, backgroundParent).GetComponent<Background>();
        ActivePlanet.Init();
        ActiveWeapon.Init();
        EnemyController.Instance.Init((EnemyType)GetActiveItem(IngameEntity.eEntityType.Enemy));
        if (star) starRoutine = StartCoroutine(RandomStarBlink());
    }

    private void ClearWorld()
    {
        PremiumCoinController.Instance.ClearPopups();
        if (starRoutine != null) StopCoroutine(starRoutine);
        starRoutine = null;
        EnemyController.Instance.RemoveAllEnemies();
        PowerUpController.Instance.RemoveAllItems();
        if (ActiveWeapon) ActiveWeapon.ClearBullets();
        RemoveObject(ActivePlanet ? ActivePlanet.gameObject : null);
        RemoveObject(ActiveWeapon ? ActiveWeapon.gameObject : null);
        RemoveObject(ActiveBackground ? ActiveBackground.gameObject : null);
        for (int i = EffectsRoot.childCount - 1; i >= 0; i--) RemoveObject(EffectsRoot.GetChild(i).gameObject);
    }

    private static void RemoveObject(GameObject instance)
    {
        if (!instance) return;
        instance.SetActive(false);
        Destroy(instance);
    }

    public float GetAttribute(EntityAttribute.eAttributeType type, float fallback = 0f) =>
        attributes.TryGetValue(type, out float value) ? value : fallback;

    public bool HasAbility(EntityAttribute.eAttributeType type) => GetAttribute(type) > 0f;
    public int MaxLives => Mathf.Max(1, Utilities.Round(GetAttribute(EntityAttribute.eAttributeType.PlanetMaxHP, maxLives)));

    public void ResetLives()
    {
        CurrentLives = Mathf.Clamp(Utilities.Round(GetAttribute(EntityAttribute.eAttributeType.PlanetStartHP, startLifes)), 1, MaxLives);
        UIIngameHud.Instance.SetLives(CurrentLives);
    }

    public void ChangeLife(int amount, bool playSound = true)
    {
        int previous = CurrentLives;
        CurrentLives = (int)System.Math.Clamp((long)CurrentLives + amount, 0, MaxLives);
        if (CurrentLives == previous) return;
        UIIngameHud.Instance.SetLives(CurrentLives);
        if (playSound && CurrentLives > previous) AudioController.PlaySound(AudioController.Instance.soundAddLive);
    }

    public void ResetWeaponProgress()
    {
        EnemyController.Instance.ResetWeaponProgress();
        if (ActiveWeapon) ActiveWeapon.ResetWeaponLevel();
    }

    public GameObject SpawnEffect(GameObject prefab, Vector3 position, float lifetime)
    {
        if (!prefab) return null;
        var instance = Instantiate(prefab, position, prefab.transform.rotation, EffectsRoot);
        Destroy(instance, lifetime);
        return instance;
    }

    private IEnumerator RandomStarBlink()
    {
        while (true)
        {
            int count = Random.Range(1, 20);
            for (int i = 0; i < count; i++)
            {
                if (!ActiveBackground) yield break;
                if (IsSimulationRunning)
                {
                    Vector3 position = GameCamera.ViewportToWorldPoint(new Vector3(Random.value, Random.value, -GameCamera.transform.position.z));
                    var instance = Instantiate(star, position, star.transform.rotation, ActiveBackground.transform);
                    Destroy(instance, 1f);
                }
                yield return new WaitForSeconds(Random.Range(0.01f, 0.06f));
            }
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (!IsInitialized || !paused) return;
        Pause();
        SaveGameController.Save();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!IsInitialized || focused) return;
        Pause();
        SaveGameController.Save();
    }

    private void OnApplicationQuit() => SaveGameController.Save();

    private void OnDestroy()
    {
        if (Instance != this) return;
        SaveGameController.Save();
        Time.timeScale = 1f;
        Instance = null;
    }
}
