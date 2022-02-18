// code reference: http://answers.unity3d.com/questions/894995/how-to-saveload-with-google-play-services.html		
// you need to import https://github.com/playgameservices/play-games-plugin-for-unity
using UnityEngine;
using System;
using System.Collections;
//gpg
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
//for encoding
using System.Text;
//for extra save ui
using UnityEngine.SocialPlatforms;
//for text, remove
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class PlayCloudDataManager : MonoBehaviour
{
    private static PlayCloudDataManager instance;

    public static PlayCloudDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PlayCloudDataManager>();

                if (instance == null)
                {
                    instance = new GameObject("PlayGameCloudData").AddComponent<PlayCloudDataManager>();
                }
            }

            return instance;
        }
    }

    public bool isProcessing
    {
        get;
        private set;
    }

    private Savegame curSavegame;
    public Savegame CurSavegame
    {
        get 
        {
            if (curSavegame == null)
                curSavegame = new Savegame();

            return curSavegame;
        }
        private set 
        {
            curSavegame = value;
        }
    }

    public string generalLeaderboardID;

    private const string m_saveFileName = "SatDefSavegame";

    public bool isAuthenticated
    {
        get
        {
            return Social.localUser.authenticated;
        }
    }

    private bool showNote = true;

    private GameHandler ghScrp;



    private void Awake()
    {
        ghScrp = GameObject.Find("GameHandler").GetComponent<GameHandler>();

        InitiatePlayGames();
        Login();
    }
    
    private void InitiatePlayGames()
    {
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
        .EnableSavedGames()// enables saving game progress.
        .Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.DebugLogEnabled = false;// recommended for debugging:
        PlayGamesPlatform.Activate();// Activate the Google Play Games platform
    }
    
    

    public void Login(bool initGame = true)
    {
        Social.localUser.Authenticate((bool success) =>
        {
            if (success)
            {
                Debug.Log("Login successfully!");
                ((PlayGamesPlatform)Social.Active).SetGravityForPopups(Gravity.TOP);
                if(initGame)
                    LoadFromCloud();
            }
            else
            {
                Debug.Log("Login failed!");
                if (!showNote)
                {
                    GameObject.Find("QuickInfo").GetComponent<QuickInfo>()
                    .showQuickInfo("Note that you cannot unlock achievements or save game results without an active google play account.", 5);
                    showNote = false;
                }

                if(initGame)
                    ghScrp.init();
            }
        });
    }

    private void ProcessCloudData(byte[] cloudData)
    {
        if (cloudData == null)
        {
            Debug.Log("No Data saved to the cloud");
            CurSavegame = new Savegame();
            return;
        }

        CurSavegame = bytesSavegame(cloudData);
    }


    public int getDaysSinceLastPlayed()
    {
        int days = -1;
        ((PlayGamesLocalUser)Social.localUser).GetStats((rc, stats) =>
        {
            // -1 means cached stats, 0 is succeess
            // see  CommonStatusCodes for all values.
            if (rc <= 0 && stats.HasDaysSinceLastPlayed())
            {
                days = stats.DaysSinceLastPlayed;
                //Debug.Log("It has been " + stats.DaysSinceLastPlayed + " days");
            }
        });

        return days;
    }
    
    public void unlockAchievement(string achivementID)
    {
        if (!isAuthenticated)
            return;

        Social.ReportProgress(achivementID, 100.0f, (bool success) => {
            if (success)
            {
                Debug.Log("Achivement with ID: " + achivementID + " unlocked.");
            }
            else
            {
                Debug.Log("Achivement with ID: " + achivementID + " failed to unlock.");
            }
        });
    }

    public void showAchivementsUI()
    {
        if (isAuthenticated)
        {
            // show achievements UI
            Social.ShowAchievementsUI();
        }
    }

    public void postScoreToLeaderBoard(long score)
    {
        if (generalLeaderboardID == null)
            Debug.Log("There is no leaderboard ID.");

        // post score 12345 to leaderboard ID "Cfji293fjsie_QA")
        Social.ReportScore(score, generalLeaderboardID, (bool success) => {
            if (success)
            {
                Debug.Log("Score successfully uploaded.");
            }
            else
            {
                Debug.Log("Score upload failed.");
            }
        });
    }

    public void showLeaderboardUI(string specificLeaderboardID = null)
    {
        if (specificLeaderboardID == null)
        {
            // show leaderboard UI
            Social.ShowLeaderboardUI();
        }
        else
        {
            // show specific leaderboard UI
            PlayGamesPlatform.Instance.ShowLeaderboardUI(specificLeaderboardID);
        }
    }



    //Load----------------------------------------------------
    public void LoadFromCloud()
    {
        if (isAuthenticated && !isProcessing)
        {
            StartCoroutine(LoadFromCloudRoutin());
        }else if(!isAuthenticated)
        {
            ghScrp.init();
        }
    }

    private IEnumerator LoadFromCloudRoutin()
    {
        if (ghScrp.removeSavegame)
        {
            deleteCloudData();
            while (isProcessing)
                yield return null;
        }
        
        isProcessing = true;
        Debug.Log("Loading game progress from the cloud.");

        ((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(
            m_saveFileName, //name of file.
            DataSource.ReadCacheOrNetwork,
            ConflictResolutionStrategy.UseLongestPlaytime,
            OnFileOpenToLoad);

        while (isProcessing)
        {
            yield return null;
        }

        SaveSystem.loadAll();

        ghScrp.init();
    }

    private void OnFileOpenToLoad(SavedGameRequestStatus status, ISavedGameMetadata metaData)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            ((PlayGamesPlatform)Social.Active).SavedGame.ReadBinaryData(metaData, OnGameLoad);
        }
        else
        {
            Debug.LogWarning("Error opening Saved Game" + status);
        }
    }

    private void OnGameLoad(SavedGameRequestStatus status, byte[] bytes)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            ProcessCloudData(bytes);
        }
        else
        {
            Debug.LogWarning("Error Saving" + status);
        }

        isProcessing = false;
    }
    


    //Save----------------------------------------------------
    public void SaveToCloud()
    {
        if (isAuthenticated)
        {
            isProcessing = true;
            ((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(m_saveFileName, 
                                                                                             DataSource.ReadCacheOrNetwork, 
                                                                                             ConflictResolutionStrategy.UseLongestPlaytime, 
                                                                                             OnFileOpenToSave);
        }
        else
        {
            Login(false);
        }
    }

    private void OnFileOpenToSave(SavedGameRequestStatus status, ISavedGameMetadata metaData)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            byte[] data = savegameToBytes(CurSavegame);

            SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();

            SavedGameMetadataUpdate updatedMetadata = builder.Build();

            ((PlayGamesPlatform)Social.Active).SavedGame.CommitUpdate(metaData, updatedMetadata, data, OnGameSave);
        }
        else
        {
            Debug.LogWarning("Error opening Saved Game" + status);
        }
    }
    
    private void OnGameSave(SavedGameRequestStatus status, ISavedGameMetadata metaData)
    {
        if (status != SavedGameRequestStatus.Success)
        {
            Debug.LogWarning("Error Saving" + status);
        }

        isProcessing = false;
    }



    //Delete----------------------------------------------------
    public void deleteCloudData()
    {
        if (isAuthenticated && !isProcessing)
        {
            isProcessing = true;
            Debug.Log("Delete savegame in progress.");

            ((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(
                m_saveFileName, //name of file.
                DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime,
                deleteSavedGame);
        }
        else if (!isAuthenticated)
        {
            Debug.Log("Can't remove data. Login not authentificated!");
        }
    }
    
    private void deleteSavedGame(SavedGameRequestStatus status, ISavedGameMetadata game)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            savedGameClient.Delete(game);
        }
        else
        {
            Debug.Log("Savegame delete failed!");
        }

        isProcessing = false;
    }



    private byte[] savegameToBytes(Savegame savegame)
    {
        if (savegame == null)
            return null;
        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            bf.Serialize(ms, savegame);
            return ms.ToArray();
        }
    }

    private Savegame bytesSavegame(byte[] bytes)
    {
        if (bytes == null)
            return new Savegame();

        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                Savegame savegame = bf.Deserialize(ms) as Savegame;
                return savegame;
            }
        }catch
        {
            Debug.Log("Bytes to savegame failed!");
            return new Savegame();
        }
    }
}
