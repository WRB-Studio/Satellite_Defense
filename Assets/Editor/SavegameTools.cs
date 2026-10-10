using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SavegameTools
{
    private const string DeleteMenu = "Tools/Spielstand/Aktuellen Spielstand löschen";

    [MenuItem(DeleteMenu)]
    private static void DeleteCurrentSave()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        string directory = Path.Combine(Application.persistentDataPath, "Saves");
        var store = new FileSaveStore(directory);
        if (!EditorUtility.DisplayDialog("Spielstand löschen?",
            "Coins, Käufe, Upgrades, Auswahl und Highscore werden gelöscht. " +
            "Auch das Backup und temporäre Speicherdateien werden entfernt.\n\n" +
            directory + "\n\nBeim nächsten Spielstart wird ein neuer Spielstand angelegt.",
            "Spielstand löschen", "Abbrechen")) return;

        try
        {
            SaveGameController.UnloadForEditor();
            if (!Directory.Exists(directory))
            {
                EditorUtility.DisplayDialog("Kein Spielstand vorhanden", "Es gibt noch keinen gespeicherten Spielstand.", "OK");
                return;
            }
            // Delete the primary last so an earlier failure leaves committed progress available.
            foreach (string path in new[] { store.TemporaryPath, store.BackupPath + ".tmp", store.BackupPath, store.SavePath })
                File.Delete(path);

            Debug.Log("Spielstand gelöscht: " + directory);
            EditorUtility.DisplayDialog("Spielstand gelöscht", "Der nächste Spielstart beginnt mit einem neuen Spielstand.", "OK");
        }
        catch (Exception error) when (FileSaveStore.IsStorageError(error))
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Löschen fehlgeschlagen",
                "Der Spielstand konnte nicht vollständig gelöscht werden.\n\n" + error.Message, "OK");
        }
    }

    [MenuItem(DeleteMenu, true)]
    private static bool CanDeleteCurrentSave() => !EditorApplication.isPlayingOrWillChangePlaymode;
}
