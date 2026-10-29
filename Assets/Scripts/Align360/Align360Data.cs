using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Align360
{
    [Serializable] public class QuestionBank
    {
        public string[] categories, questions, courseCategories, courseQuestions;
    }
    [Serializable] public class AssessmentResult
    {
        public string id, phase, completedUtc;
        public int[] answers;
        public int Total { get { int sum = 0; foreach (int value in answers) sum += value; return sum; } }
        public int CategoryScore(int category, int size)
        {
            int sum = 0;
            for (int i = category * size; i < (category + 1) * size; i++) sum += answers[i];
            return sum;
        }
    }
    [Serializable] public class JournalEntry { public string createdUtc, text; }
    [Serializable] public class ActivityDay { public string date; public float focusSeconds; public int actions; }
    [Serializable] public class SaveData
    {
        public List<AssessmentResult> results = new List<AssessmentResult>();
        public List<JournalEntry> journal = new List<JournalEntry>();
        public List<string> completedExercises = new List<string>();
        public List<ActivityDay> activity = new List<ActivityDay>();
        public List<string> favorites = new List<string>();
        public float musicVolume = .7f, sfxVolume = .7f;
        public bool vibration = true, darkMode = true, showTips = true, weekStartsMonday = true;
        public int textSize = 1, accentIndex;
        public string reminderTime = "09:00";
        public string draftPhase;
        public int[] draftAnswers;
        public int draftIndex;
    }
    public static class Scoring
    {
        public static string Band(int total, bool course)
        {
            if (course) return total >= 75 ? "High Emotional and Professional Effectiveness" : total >= 60 ? "Moderate Effectiveness" : total >= 45 ? "Needs Improvement" : "High Development Opportunity";
            return total >= 160 ? "High Organizational Alignment and Emotional Awareness" : total >= 120 ? "Moderate Alignment and Self-Awareness" : total >= 80 ? "Developing Awareness and Behavioral Regulation" : "Significant Development Opportunity";
        }
        public static bool Valid(int[] answers, int count)
        {
            if (answers == null || answers.Length != count) return false;
            foreach (int value in answers) if (value < 1 || value > 5) return false;
            return true;
        }
    }
    public class LocalStore
    {
        public SaveData Data { get; private set; }
        public string Path { get; private set; }
        public LocalStore(string storagePath = null)
        {
            Path = storagePath ?? System.IO.Path.Combine(Application.persistentDataPath, "align360.json");
            Data = new SaveData();
            if (!File.Exists(Path)) return;
            try
            {
                Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path)) ?? new SaveData();
                if (Data.results == null) Data.results = new List<AssessmentResult>();
                if (Data.journal == null) Data.journal = new List<JournalEntry>();
                if (Data.completedExercises == null) Data.completedExercises = new List<string>();
                if(Data.activity==null)Data.activity=new List<ActivityDay>();
                if(Data.favorites==null)Data.favorites=new List<string>();
                Data.results.RemoveAll(r => r == null || !Scoring.Valid(r.answers, r.phase == "ALIGN360" ? 40 : 18));
            }
            catch (Exception e) { Debug.LogWarning("ALIGN360 save could not be loaded: " + e.Message); }
        }
        public void Save()
        {
            string temp = Path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
            if (File.Exists(Path)) File.Copy(Path, Path + ".bak", true);
            File.Copy(temp, Path, true);
            File.Delete(temp);
        }
    }
}
