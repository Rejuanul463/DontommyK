using System;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    [Serializable]
    public class TaskData
    {
        public string taskID;
        public string title;
        public string description;

        public int requiredProgress;
        public int currentProgress;

        public int rewardXP;
        public int rewardCoins;

        public bool isActive;
        public bool isCompleted;

        public float Progress =>
            requiredProgress <= 0
                ? 1f
                : (float)currentProgress / requiredProgress;
    }

    [Header("Available Tasks")]
    [SerializeField]
    private List<TaskData> tasks = new List<TaskData>();

    public event Action<TaskData> OnTaskStarted;
    public event Action<TaskData> OnTaskUpdated;
    public event Action<TaskData> OnTaskCompleted;

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

    // -------------------------
    // TASK MANAGEMENT
    // -------------------------

    public void StartTask(string taskID)
    {
        TaskData task = GetTask(taskID);

        if (task == null)
        {
            Debug.LogWarning($"Task not found: {taskID}");
            return;
        }

        if (task.isCompleted)
        {
            Debug.Log($"Task already completed: {task.title}");
            return;
        }

        task.isActive = true;

        OnTaskStarted?.Invoke(task);

        Debug.Log($"Task Started: {task.title}");
    }

    public void UpdateTaskProgress(string taskID, int amount = 1)
    {
        TaskData task = GetTask(taskID);

        if (task == null || !task.isActive || task.isCompleted)
            return;

        task.currentProgress += amount;

        task.currentProgress =
            Mathf.Clamp(task.currentProgress, 0, task.requiredProgress);

        OnTaskUpdated?.Invoke(task);

        Debug.Log(
            $"Task Progress: {task.title} " +
            $"({task.currentProgress}/{task.requiredProgress})"
        );

        if (task.currentProgress >= task.requiredProgress)
        {
            CompleteTask(taskID);
        }
    }

    public void CompleteTask(string taskID)
    {
        TaskData task = GetTask(taskID);

        if (task == null || task.isCompleted)
            return;

        task.currentProgress = task.requiredProgress;
        task.isCompleted = true;
        task.isActive = false;

        GiveRewards(task);

        OnTaskCompleted?.Invoke(task);

        Debug.Log($"Task Completed: {task.title}");
    }

    // -------------------------
    // TASK QUERIES
    // -------------------------

    public TaskData GetTask(string taskID)
    {
        return tasks.Find(task => task.taskID == taskID);
    }

    public List<TaskData> GetActiveTasks()
    {
        return tasks.FindAll(task =>
            task.isActive && !task.isCompleted);
    }

    public List<TaskData> GetCompletedTasks()
    {
        return tasks.FindAll(task =>
            task.isCompleted);
    }

    public bool IsTaskCompleted(string taskID)
    {
        TaskData task = GetTask(taskID);

        return task != null && task.isCompleted;
    }

    // -------------------------
    // REWARDS
    // -------------------------

    private void GiveRewards(TaskData task)
    {
        Debug.Log(
            $"Rewards Received → " +
            $"XP: {task.rewardXP}, " +
            $"Coins: {task.rewardCoins}"
        );

        // Example:
        // PlayerData.AddXP(task.rewardXP);
        // CurrencyManager.AddCoins(task.rewardCoins);
    }

    // -------------------------
    // DEBUG / DEMO
    // -------------------------

    [ContextMenu("Create Demo Tasks")]
    private void CreateDemoTasks()
    {
        tasks.Clear();

        tasks.Add(new TaskData
        {
            taskID = "TASK_001",
            title = "First Mission",
            description = "Reach the village entrance.",
            requiredProgress = 1,
            rewardXP = 100,
            rewardCoins = 50
        });

        tasks.Add(new TaskData
        {
            taskID = "TASK_002",
            title = "Collect Supplies",
            description = "Collect 5 supplies.",
            requiredProgress = 5,
            rewardXP = 250,
            rewardCoins = 100
        });

        tasks.Add(new TaskData
        {
            taskID = "TASK_003",
            title = "Defeat Enemies",
            description = "Defeat 10 enemies.",
            requiredProgress = 10,
            rewardXP = 500,
            rewardCoins = 250
        });

        Debug.Log("Demo tasks created.");
    }
}