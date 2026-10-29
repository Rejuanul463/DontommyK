using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BackendSystem : MonoBehaviour
{
    public static BackendSystem Instance { get; private set; }

    [Header("Backend Configuration")]
    [SerializeField] private string apiBaseUrl = "https://api.example-game.com";
    [SerializeField] private bool useMockBackend = true;

    [Header("Player Session")]
    [SerializeField] private string playerID;
    [SerializeField] private string authToken;
    [SerializeField] private bool isLoggedIn;

    [Header("Player Data")]
    [SerializeField] private int playerLevel;
    [SerializeField] private int coins;

    public bool IsLoggedIn => isLoggedIn;
    public string PlayerID => playerID;
    public int PlayerLevel => playerLevel;
    public int Coins => coins;

    public event Action OnLoginCompleted;
    public event Action OnDataLoaded;
    public event Action OnDataSaved;
    public event Action<string> OnBackendError;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --------------------------------
    // AUTHENTICATION
    // --------------------------------

    public void Login(string username, string password)
    {
        if (useMockBackend)
        {
            MockLogin();
            return;
        }

        StartCoroutine(LoginRequest(username, password));
    }

    private IEnumerator LoginRequest(
        string username,
        string password)
    {
        LoginRequestData requestData = new LoginRequestData
        {
            username = username,
            password = password
        };

        string json = JsonUtility.ToJson(requestData);

        using UnityWebRequest request =
            new UnityWebRequest(
                $"{apiBaseUrl}/auth/login",
                "POST"
            );

        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler =
            new UploadHandlerRaw(body);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            HandleError(request.error);
            yield break;
        }

        LoginResponse response =
            JsonUtility.FromJson<LoginResponse>(
                request.downloadHandler.text
            );

        playerID = response.playerID;
        authToken = response.token;
        isLoggedIn = true;

        OnLoginCompleted?.Invoke();

        LoadPlayerData();
    }

    // --------------------------------
    // PLAYER DATA
    // --------------------------------

    public void LoadPlayerData()
    {
        if (useMockBackend)
        {
            MockLoadPlayerData();
            return;
        }

        StartCoroutine(
            SendGetRequest(
                "/player/data",
                HandlePlayerData
            )
        );
    }

    private void HandlePlayerData(string json)
    {
        PlayerData data =
            JsonUtility.FromJson<PlayerData>(json);

        playerLevel = data.level;
        coins = data.coins;

        OnDataLoaded?.Invoke();
    }

    public void SavePlayerData()
    {
        if (!isLoggedIn)
            return;

        if (useMockBackend)
        {
            MockSavePlayerData();
            return;
        }

        StartCoroutine(SaveDataRequest());
    }

    private IEnumerator SaveDataRequest()
    {
        PlayerData data = new PlayerData
        {
            level = playerLevel,
            coins = coins
        };

        string json = JsonUtility.ToJson(data);

        using UnityWebRequest request =
            new UnityWebRequest(
                $"{apiBaseUrl}/player/data",
                "POST"
            );

        request.uploadHandler =
            new UploadHandlerRaw(
                System.Text.Encoding.UTF8.GetBytes(json)
            );

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        request.SetRequestHeader(
            "Authorization",
            $"Bearer {authToken}"
        );

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            HandleError(request.error);
            yield break;
        }

        OnDataSaved?.Invoke();

        Debug.Log("Player data saved.");
    }

    // --------------------------------
    // INVENTORY
    // --------------------------------

    public void AddItem(string itemID, int amount)
    {
        Debug.Log(
            $"Backend Inventory → Added {amount}x {itemID}"
        );

        // Send inventory update to backend
    }

    public void RemoveItem(string itemID, int amount)
    {
        Debug.Log(
            $"Backend Inventory → Removed {amount}x {itemID}"
        );

        // Send inventory update to backend
    }

    // --------------------------------
    // CURRENCY
    // --------------------------------

    public void AddCoins(int amount)
    {
        coins += amount;

        Debug.Log(
            $"Coins Added: {amount} | Total: {coins}"
        );

        SavePlayerData();
    }

    public bool SpendCoins(int amount)
    {
        if (coins < amount)
            return false;

        coins -= amount;

        SavePlayerData();

        return true;
    }

    // --------------------------------
    // HTTP HELPERS
    // --------------------------------

    private IEnumerator SendGetRequest(
        string endpoint,
        Action<string> callback)
    {
        using UnityWebRequest request =
            UnityWebRequest.Get(
                $"{apiBaseUrl}{endpoint}"
            );

        request.SetRequestHeader(
            "Authorization",
            $"Bearer {authToken}"
        );

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            HandleError(request.error);
            yield break;
        }

        callback?.Invoke(
            request.downloadHandler.text
        );
    }

    private void HandleError(string error)
    {
        Debug.LogError(
            $"Backend Error: {error}"
        );

        OnBackendError?.Invoke(error);
    }

    // --------------------------------
    // MOCK BACKEND
    // --------------------------------

    private void MockLogin()
    {
        playerID = "PLAYER_001";
        authToken = "DEMO_AUTH_TOKEN";

        isLoggedIn = true;

        Debug.Log(
            $"Mock Login Successful: {playerID}"
        );

        OnLoginCompleted?.Invoke();

        MockLoadPlayerData();
    }

    private void MockLoadPlayerData()
    {
        playerLevel = 12;
        coins = 2500;

        Debug.Log(
            $"Player Data Loaded → " +
            $"Level: {playerLevel}, Coins: {coins}"
        );

        OnDataLoaded?.Invoke();
    }

    private void MockSavePlayerData()
    {
        Debug.Log(
            $"Player Data Saved → " +
            $"Level: {playerLevel}, Coins: {coins}"
        );

        OnDataSaved?.Invoke();
    }

    // --------------------------------
    // DATA CLASSES
    // --------------------------------

    [Serializable]
    private class LoginRequestData
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class LoginResponse
    {
        public string playerID;
        public string token;
    }

    [Serializable]
    private class PlayerData
    {
        public int level;
        public int coins;
    }
}