using System;
using UnityEngine;

public class ScoreController : MonoBehaviour
{
    public static ScoreController Instance { get; private set; }
    [Min(0f)] public float multiplier = 1f;
    [Min(0f)] public float minMultiplier = 0.5f;
    public long Score => Utilities.Round(total);
    public event Action<long> Changed;
    private double total;

    private void Awake() => Instance = this;

    public void ResetScore()
    {
        total = 0;
        Changed?.Invoke(Score);
    }

    public void AddScore(double amount)
    {
        var game = GameController.Instance;
        if (!game || !game.IsPlaying || amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount)) return;
        float factor = multiplier * game.GetAttribute(EntityAttribute.eAttributeType.ScoreMultiplier, 1f);
        if (game.CurrentLives == 1) factor *= game.GetAttribute(EntityAttribute.eAttributeType.ScoreBoostOnLowHP, 1f);
        total = Math.Min(long.MaxValue, total + amount * Mathf.Max(minMultiplier, factor));
        Changed?.Invoke(Score);
    }

    private void OnDestroy()
    {
        Changed = null;
        if (Instance == this) Instance = null;
    }
}
