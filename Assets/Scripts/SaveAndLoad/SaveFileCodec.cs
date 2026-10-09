using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SaveFileCodec
{
    private const string Format = "satellite-defense";
    private const int Version = 1;

    [Serializable]
    private class Document
    {
        public string format;
        public int version;
        public string payload;
        public string checksum;
    }

    public static string Encode(Savegame data)
    {
        if (!data.IsValid()) throw new ArgumentException("Invalid save data.", nameof(data));
        string payload = JsonUtility.ToJson(data);
        return JsonUtility.ToJson(new Document
        {
            format = Format,
            version = Version,
            payload = payload,
            checksum = Checksum(payload)
        });
    }

    public static bool TryDecode(string json, out Savegame data, out bool unsupportedVersion)
    {
        data = null;
        unsupportedVersion = false;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            var document = JsonUtility.FromJson<Document>(json);
            if (document == null || document.format != Format) return false;
            unsupportedVersion = document.version > Version;
            if (document.version != Version || string.IsNullOrEmpty(document.payload) ||
                !string.Equals(document.checksum, Checksum(document.payload), StringComparison.Ordinal)) return false;

            // Missing fields must not become a valid empty save through field initializers.
            var candidate = new Savegame
            {
                version = 0, bestScore = -1, premiumCoins = -1,
                activePlanetID = 0, activeWeaponID = 0, activeEnemyTypeID = 0, activeBackgroundID = 0,
                unlockedPlanetIDs = null, unlockedWeaponIDs = null, unlockedEnemyTypeIDs = null, unlockedBackgroundIDs = null,
                planetLevels = null, weaponLevels = null, enemyTypeLevels = null, backgroundLevels = null
            };
            JsonUtility.FromJsonOverwrite(document.payload, candidate);
            unsupportedVersion = candidate.version > Savegame.CurrentVersion;
            if (!candidate.IsValid()) return false;
            data = candidate;
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    private static string Checksum(string payload)
    {
        // Integrity check for damaged files, not encryption or cheat protection.
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}
