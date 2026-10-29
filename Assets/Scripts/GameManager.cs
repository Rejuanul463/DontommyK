using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Loading,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    [Header("Game Settings")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int startingScore = 0;

    [Header("Runtime Data")]
    [SerializeField] private GameState currentState;
    [SerializeField] private int score;
    [SerializeField] private int lives;

    // Events
    public event Action<int> OnScoreChanged;
    public event Action<int> OnLivesChanged;
    public event Action<GameState> OnGameStateChanged;

    public GameState CurrentState => currentState;
    public int Score => score;
    public int Lives => lives;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeGame();
    }

    private void InitializeGame()
    {
        score = startingScore;
        lives = startingLives;

        ChangeState(GameState.Loading);

        Debug.Log("[GameManager] Game initialized.");
    }

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        score = startingScore;
        lives = startingLives;

        OnScoreChanged?.Invoke(score);
        OnLivesChanged?.Invoke(lives);

        ChangeState(GameState.Playing);

        Debug.Log("[GameManager] Game Started.");
    }

    public void AddScore(int amount)
    {
        if (currentState != GameState.Playing)
            return;

        score += amount;

        OnScoreChanged?.Invoke(score);

        Debug.Log($"[GameManager] Score: {score}");
    }

    public void LoseLife()
    {
        if (currentState != GameState.Playing)
            return;

        lives--;

        OnLivesChanged?.Invoke(lives);

        if (lives <= 0)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        ChangeState(GameState.GameOver);

        Debug.Log("[GameManager] Game Over.");
    }

    public void CompleteLevel()
    {
        ChangeState(GameState.Victory);

        Debug.Log("[GameManager] Level Completed.");
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing)
            return;

        Time.timeScale = 0f;

        ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused)
            return;

        Time.timeScale = 1f;

        ChangeState(GameState.Playing);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void LoadLevel(int buildIndex)
    {
        Time.timeScale = 1f;

        ChangeState(GameState.Loading);

        SceneManager.LoadScene(buildIndex);
    }

    private void ChangeState(GameState newState)
    {
        currentState = newState;

        OnGameStateChanged?.Invoke(currentState);

        Debug.Log($"[GameManager] State changed to: {currentState}");
    }
}