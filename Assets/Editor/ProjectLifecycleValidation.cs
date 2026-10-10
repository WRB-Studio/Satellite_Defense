using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public static partial class ProjectPlayValidation
{
    private static IEnumerator PauseLifecycle()
    {
        yield return Load(ProjectValidation.GameScene); QuietCombat();
        var game = GameController.Instance; game.ActiveWeapon.ActivateJumpLaser();
        var rotation = game.ActivePlanet.transform.rotation;
        var item = Pickup(PowerUpController.Instance.itemFireRate);
        float lifetime = Get<float>(item, "remainingLife"), jump = Get<float>(game.ActiveWeapon, "jumpLaserRemaining");
        SaveGameController.CreditCoins(3); game.Pause(); yield return new WaitForSecondsRealtime(.2f);
        Require(game.State == GameController.GameState.Paused && Time.timeScale == 0, "Pause state wrong.");
        Near(Quaternion.Angle(rotation, game.ActivePlanet.transform.rotation), 0, "Paused rotation");
        Near(Get<float>(item, "remainingLife"), lifetime, "Paused pickup lifetime"); Near(Get<float>(game.ActiveWeapon, "jumpLaserRemaining"), jump, "Paused jump timer");
        Require(!SaveGameController.HasPendingChanges && new FileSaveStore(ProjectValidation.TestSaveDirectory).Load().Data.Coins == SaveGameController.Data.Coins, "Pause did not flush rewards.");
        UIPauseMenu.Instance.btnContinue.onClick.Invoke(); Require(game.IsPlaying && Time.timeScale == 1, "Continue failed.");
        foreach (string callback in new[] { "OnApplicationPause", "OnApplicationFocus" })
        {
            SaveGameController.CreditCoins(2); game.SendMessage(callback, callback == "OnApplicationPause");
            Require(game.State == GameController.GameState.Paused && !SaveGameController.HasPendingChanges, "Lifecycle callback did not pause/flush: " + callback);
            game.Resume();
        }
        yield return new WaitForSecondsRealtime(.1f); Require(Get<float>(game.ActiveWeapon, "jumpLaserRemaining") < jump, "Timer must resume.");
    }

    private static IEnumerator RewardsReplay()
    {
        yield return Load(ProjectValidation.GameScene); QuietCombat(); var game = GameController.Instance;
        foreach (double amount in new[] { 0d, 1d, 250d })
        {
            var weapon = game.ActiveWeapon;
            weapon.ReduceShotInterval(float.MaxValue); weapon.UpgradeEmitters(); weapon.UpgradeEmitters(); weapon.ActivateJumpLaser();
            Call(weapon, "Fire");
            PowerUpController.Instance.dropChance = 1;
            PowerUpController.Instance.SpawnRandomItem(Vector2.zero);
            ScoreController.Instance.AddScore(amount); long score = ScoreController.Instance.Score, before = SaveGameController.Data.Coins;
            game.EndRound();
            long expected = score == 0 ? 0 : Math.Max(1, score / Math.Max(1, PremiumCoinController.Instance.premiumCoinsPerScore));
            Require(game.State == GameController.GameState.GameOver && SaveGameController.Data.Coins == before + expected, "Round payout/minimum wrong.");
            var disk = new FileSaveStore(ProjectValidation.TestSaveDirectory).Load();
            Require(disk.Data.Coins == SaveGameController.Data.Coins && disk.Data.BestScore == SaveGameController.Data.BestScore, "Round result not committed before animations.");
            game.EndRound(); Require(SaveGameController.Data.Coins == before + expected, "Repeated game-over paid twice.");
            Require(Get<System.Collections.Generic.List<Bullet>>(weapon, "bullets").Count == 0 && PowerUpController.Instance.Items.Count == 0, "Game over retained bullets/pickups.");
            UIPauseMenu.Instance.btnReplay.onClick.Invoke(); yield return null; QuietCombat();
            Require(game.IsPlaying && ScoreController.Instance.Score == 0 && game.ActiveWeapon.WeaponLevel == 1 && !game.ActiveWeapon.IsJumpLaserActive && EnemyController.Instance.Kills == 0 && PowerUpController.Instance.Items.Count == 0, "Replay retained round progress.");
        }
    }

    private static IEnumerator Pickups()
    {
        yield return Load(ProjectValidation.GameScene); QuietCombat();
        var game = GameController.Instance; var items = PowerUpController.Instance;
        game.ChangeLife(-1, false); int beforeHp = game.CurrentLives;
        var heart = Pickup(items.itemHeart); heart.Collect(); heart.Collect();
        Require(game.CurrentLives == Math.Min(game.MaxLives, beforeHp + Math.Max(1, Mathf.CeilToInt(game.MaxLives * .2f))), "Heart must heal once and clamp.");
        var coin = Pickup(items.itemCoin); long before = SaveGameController.Data.Coins; int value = coin.addPremiumCoins;
        coin.Collect(); coin.Collect(); Require(SaveGameController.Data.Coins == before + Math.Max(1, value + game.Stats.BonusCoinValue), "Coin paid twice or ignored bonus.");
        float interval = Get<float>(game.ActiveWeapon, "shotInterval"); var fire = Pickup(items.itemFireRate); float reduction = fire.shotIntervalReduction; fire.Collect();
        Near(Get<float>(game.ActiveWeapon, "shotInterval"), Math.Max(game.ActiveWeapon.minShotInterval, interval - reduction), "Fire pickup amount");
        Pickup(items.itemJumpLaser).Collect(); Require(game.ActiveWeapon.IsJumpLaserActive, "Jump pickup inactive.");
        game.Pause(); var pausedCoin = Pickup(items.itemCoin); before = SaveGameController.Data.Coins;
        pausedCoin.Collect(); Require(SaveGameController.Data.Coins == before && pausedCoin.gameObject.activeSelf, "Paused pickup changed progress.");
        game.Resume(); pausedCoin.Collect(); Require(SaveGameController.Data.Coins > before, "Pickup cannot be collected after resume.");
    }

    private static IEnumerator DropLifecycle()
    {
        yield return Load(ProjectValidation.GameScene); QuietCombat(); var controller = PowerUpController.Instance;
        controller.dropChance = 1; controller.itemLifeTime = .15f; controller.SpawnRandomItem(Vector2.zero);
        Require(controller.Items.Count == 1, "Guaranteed eligible drop missing.");
        Require(controller.Items[0].itemType != PowerUp.enumItemType.shootUpgrade, "Ineligible emitter pickup offered.");
        controller.SpawnRandomItem(Vector2.zero); Require(controller.Items.Count == 1, "Drop cooldown bypassed.");
        float cooldown = Get<float>(controller, "dropCooldown");
        GameController.Instance.Pause(); yield return new WaitForSecondsRealtime(.2f);
        Require(controller.Items.Count == 1, "Paused item expired."); Near(Get<float>(controller, "dropCooldown"), cooldown, "Drop cooldown must pause");
        GameController.Instance.Resume(); yield return new WaitForSecondsRealtime(.25f); Require(controller.Items.Count == 0, "Expired drop did not unregister.");
        controller.RemoveAllItems(); controller.SpawnRandomItem(Vector2.zero); Require(controller.Items.Count == 1, "Round-clear did not reset cooldown.");
        Set(controller, "dropCooldown", 0f); controller.SpawnRandomItem(new Vector2(100, 100)); Require(controller.Items.Count == 1, "Offscreen drop spawned.");
        controller.RemoveAllItems();
        controller.itemCoin = controller.itemHeart = controller.itemShootUpgrade = controller.itemJumpLaser = null;
        controller.SpawnRandomItem(Vector2.zero); Require(controller.Items.Count == 1 && controller.Items[0].itemType == PowerUp.enumItemType.fireRate, "Single candidate selection wrong.");
        Set(controller, "dropCooldown", 0f); controller.SpawnRandomItem(Vector2.zero); Require(controller.Items.Count == 1, "Duplicate item type spawned.");
    }

    private static IEnumerator JoystickInput()
    {
        yield return Load(ProjectValidation.GameScene); QuietCombat();
        var game = GameController.Instance; game.enableJoystickControll = true; game.StartNewGame(); yield return null; QuietCombat();
        var stick = game.joystick;
        var background = Get<RectTransform>(stick, "background");
        var canvas = stick.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var center = RectTransformUtility.WorldToScreenPoint(camera, background.position);
        var pointer = new PointerEventData(EventSystem.current) { pointerId = 7, position = center + Vector2.right * Mathf.Max(50, background.rect.width * canvas.scaleFactor) };
        stick.OnPointerDown(pointer); Require(stick.IsPressed && stick.Direction.magnitude <= 1.001f, "Joystick press/normalization failed.");
        // Dynamic/floating joysticks recenter on pointer-down; aiming starts on drag.
        pointer.position = RectTransformUtility.WorldToScreenPoint(camera, background.position) + Vector2.right * Mathf.Max(50, background.rect.width * canvas.scaleFactor * .4f);
        stick.OnDrag(pointer);
        Require(stick.Direction.sqrMagnitude > .001f && stick.Direction.magnitude <= 1.001f, "Owning pointer drag produced no normalized direction.");
        stick.OnPointerUp(new PointerEventData(EventSystem.current) { pointerId = 8 }); Require(stick.IsPressed, "Other finger released joystick.");
        var direction = stick.Direction; stick.OnDrag(new PointerEventData(EventSystem.current) { pointerId = 8, position = Vector2.zero });
        Require(stick.Direction == direction, "Other finger moved joystick.");
        var rotation = game.ActiveWeapon.transform.rotation;
        float simulationStart = Time.fixedTime;
        for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
        float turned = Quaternion.Angle(rotation, game.ActiveWeapon.transform.rotation);
        float elapsed = Time.fixedTime - simulationStart;
        Require(turned > .1f && turned <= game.Stats.WeaponRotationSpeed * (elapsed + Time.fixedDeltaTime) + .1f,
            $"Aiming missing or exceeding configured rotation speed: direction {direction}, turned {turned}, simulated seconds {elapsed}, speed {game.Stats.WeaponRotationSpeed}.");
        Require(Get<System.Collections.Generic.List<Bullet>>(game.ActiveWeapon, "bullets").Count > 0, "Pressed joystick did not fire.");
        stick.OnPointerUp(pointer); Require(!stick.IsPressed && stick.Direction == Vector2.zero, "Owning pointer release failed.");
        stick.OnPointerDown(pointer);
        game.Pause(); Require(!stick.IsPressed && stick.Direction == Vector2.zero, "Hiding joystick must clear input.");
        game.Resume(); yield return null; Require(!stick.IsPressed, "Resuming restored stale pointer.");
    }

    private static IEnumerator UiState()
    {
        yield return Load(ProjectValidation.MenuScene); var game = GameController.Instance; var shop = UIShopMenu.Instance;
        var panel = (RectTransform)shop.shopMenuPanel.transform; Vector2 size = panel.sizeDelta, anchored = panel.anchoredPosition;
        game.OpenShop();
        foreach (var tab in new[] { shop.btnTabPlanets, shop.btnTabWeapons, shop.btnTabEnemyTypes, shop.btnTabBackgrounds })
        {
            tab.onClick.Invoke(); yield return null;
            Require(panel.sizeDelta == size && panel.anchoredPosition == anchored, "Shop category changed authored geometry.");
            Require(!shop.btnLeft.interactable && shop.btnClose.gameObject.activeInHierarchy, "Shop boundary/close unavailable.");
        }
        Require(shop.txtPurchaseAction && !string.IsNullOrWhiteSpace(shop.txtPurchaseAction.text), "Purchase/upgrade caption missing.");
        game.CloseShop(); game.StartNewGame(); yield return WaitScene(ProjectValidation.GameScene);
        var pause = UIPauseMenu.Instance; pause.txtAvailableCoins = null; pause.Init(); Require(pause.txtAvailableCoins, "Inactive pause coin text fallback failed.");
        SaveGameController.SetCoinsForEditor(1234); Require(pause.txtAvailableCoins.text == Utilities.NumberToString(1234), "Pause coins did not refresh.");
        pause.txtAvailableCoins = null; PremiumCoinController.Instance.Init(); Require(GameController.Instance.IsPlaying, "Missing optional coin text interrupted gameplay.");
    }

    private static IEnumerator BlockedSave()
    {
        Directory.CreateDirectory(ProjectValidation.TestSaveDirectory); var disk = new FileSaveStore(ProjectValidation.TestSaveDirectory);
        File.WriteAllText(disk.SavePath, "{broken"); File.WriteAllText(disk.BackupPath, "{broken");
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(ProjectValidation.GameScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
        yield return WaitScene(ProjectValidation.MenuScene);
        Require(!SaveGameController.CanSave && !UIMainMenu.Instance.btnPlay.interactable && !string.IsNullOrEmpty(SaveGameController.LastError), "Blocked save did not prevent gameplay/explain failure.");
        Require(File.ReadAllText(disk.SavePath) == "{broken" && File.ReadAllText(disk.BackupPath) == "{broken", "Blocked files overwritten.");
    }

    private static IEnumerator AudioAndToasts()
    {
        yield return Load(ProjectValidation.MenuScene);
        var audio = AudioController.Instance;
        var clip = AudioClip.Create("Silent validation fixture", 44100, 1, 44100, false);
        try
        {
            audio.maxSoundSources = 4;
            for (int i = 0; i < 50; i++) AudioController.PlaySound(clip, pitch: 100);
            var sources = Get<System.Collections.Generic.List<AudioSource>>(audio, "soundSources");
            Require(sources.Count > 0 && sources.Count <= audio.maxSoundSources, "Audio pool grew past its cap.");
            Require(sources.Any(source => Mathf.Approximately(source.pitch, 3)), "Upper pitch bound missing.");
            AudioController.PlaySound(clip, pitch: -10);
            Require(sources.Any(source => Mathf.Approximately(source.pitch, .1f)), "Lower pitch bound missing.");
            int count = sources.Count; AudioController.PlaySound(null);
            Require(sources.Count == count, "Null sound allocated a source.");
        }
        finally
        {
            foreach (var source in audio.GetComponentsInChildren<AudioSource>()) source.Stop();
            UnityEngine.Object.Destroy(clip);
        }
        var toast = UIToastMessage.Instance; toast.Init(); toast.fadeDuration = .02f;
        GameController.Instance.OpenShop();
        toast.ShowToast("First validation message", .1f); toast.ShowToast("First validation message", .1f);
        toast.ShowToast("Second validation message", .1f); toast.ShowToast("Second validation message", .1f);
        var queue = Get<System.Collections.Generic.Queue<(string message, float duration)>>(toast, "queue");
        Require(queue.Count == 1 && toast.txtMessage.text == "First validation message", "Toast duplication/ordering wrong.");
        float deadline = Time.realtimeSinceStartup + 3;
        while (toast.toastPanel.activeSelf && Time.realtimeSinceStartup < deadline) yield return null;
        Require(!toast.toastPanel.activeSelf && queue.Count == 0 && Time.timeScale == 0, "Toast queue must finish with unscaled time while shop is paused.");
    }

    private static IEnumerator ExitGuard()
    {
        yield return Load(ProjectValidation.MenuScene);
        SaveGameController.CreditCoins(5); var disk = new FileSaveStore(ProjectValidation.TestSaveDirectory);
        Directory.CreateDirectory(disk.TemporaryPath);
        try
        {
            UIMainMenu.Instance.ExitGame();
            Require(Application.isPlaying && SaveGameController.HasPendingChanges && SaveGameController.Data.Coins == 5 && !string.IsNullOrEmpty(SaveGameController.LastError), "Exit guard failed to retain unsaved progression.");
        }
        finally { Directory.Delete(disk.TemporaryPath); }
        GameController.Instance.SendMessage("OnApplicationQuit");
        Require(!SaveGameController.HasPendingChanges && new FileSaveStore(ProjectValidation.TestSaveDirectory).Load().Data.Coins == 5, "Shutdown callback did not flush recovered storage.");
    }
}
