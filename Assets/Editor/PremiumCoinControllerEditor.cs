using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PremiumCoinController))]
public class PremiumCoinControllerEditor : Editor
{
    private long targetCoins = 1000;
    private string message;
    private MessageType messageType;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Coins zum Testen", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Setzt und speichert den Coin-Stand im aktuellen Spielstand. Funktioniert auch außerhalb des Playmodes.", MessageType.Info);
        if (Application.isPlaying)
            EditorGUILayout.LabelField("Aktueller Stand", SaveGameController.Data.Coins.ToString());
        targetCoins = Math.Max(0L, EditorGUILayout.LongField("Neuer Coin-Stand", targetCoins));
        bool changingPlaymode = EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying;
        using (new EditorGUI.DisabledScope(changingPlaymode || EditorUtility.IsPersistent(target)))
        {
            if (GUILayout.Button("Coin-Stand setzen"))
            {
                bool saved = SaveGameController.SetCoinsForEditor(targetCoins);
                if (saved && Application.isPlaying && UIShopMenu.Instance)
                    UIShopMenu.Instance.RefreshForEditor();
                message = saved ? $"Coin-Stand auf {targetCoins} gesetzt und gespeichert." :
                    SaveGameController.LastError ?? "Der aktuelle Spielstand kann nicht gespeichert werden.";
                messageType = saved ? MessageType.Info : MessageType.Error;
            }
        }
        if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
    }
}
