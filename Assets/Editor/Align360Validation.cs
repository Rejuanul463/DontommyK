using System;
using Align360;
using UnityEditor;
using UnityEngine;

public static class Align360Validation
{
    [MenuItem("Tools/ALIGN360/Validate assessment data and scoring")]
    public static void Run()
    {
        var bank=JsonUtility.FromJson<QuestionBank>(Resources.Load<TextAsset>("Align360/assessment").text);
        Check(bank.questions.Length==40 && bank.categories.Length==5,"ALIGN360 question count");
        Check(bank.courseQuestions.Length==18 && bank.courseCategories.Length==6,"Course question count");
        foreach(string q in bank.questions) Check(!string.IsNullOrWhiteSpace(q),"Empty question");
        foreach(string q in bank.courseQuestions) Check(!string.IsNullOrWhiteSpace(q),"Empty course question");
        int[] main=new int[40];for(int i=0;i<40;i++) main[i]=5;
        var result=new AssessmentResult{phase="ALIGN360",answers=main};
        Check(result.Total==200 && result.CategoryScore(4,8)==40,"Maximum scoring");
        for(int i=0;i<40;i++) main[i]=1;
        Check(result.Total==40 && result.CategoryScore(0,8)==8,"Minimum scoring");
        Check(Scoring.Valid(main,40),"Valid answers");main[2]=0;Check(!Scoring.Valid(main,40),"Missing answer rejection");main[2]=6;Check(!Scoring.Valid(main,40),"Invalid rating rejection");
        Check(!Scoring.Valid(new int[18],40),"Wrong length rejection");
        int[] boundaries={79,80,119,120,159,160,200};
        string[] expected={"Significant Development Opportunity","Developing Awareness and Behavioral Regulation","Developing Awareness and Behavioral Regulation","Moderate Alignment and Self-Awareness","Moderate Alignment and Self-Awareness","High Organizational Alignment and Emotional Awareness","High Organizational Alignment and Emotional Awareness"};
        for(int i=0;i<boundaries.Length;i++)Check(Scoring.Band(boundaries[i],false)==expected[i],"ALIGN360 boundary "+boundaries[i]);
        int[] course={44,45,59,60,74,75,90};string[] bands={"High Development Opportunity","Needs Improvement","Needs Improvement","Moderate Effectiveness","Moderate Effectiveness","High Emotional and Professional Effectiveness","High Emotional and Professional Effectiveness"};
        for(int i=0;i<course.Length;i++)Check(Scoring.Band(course[i],true)==bands[i],"Course boundary "+course[i]);
        var save=new SaveData();save.results.Add(new AssessmentResult{id="test",phase="Pre-course",answers=new[]{1,2,3,4,5,1,2,3,4,5,1,2,3,4,5,1,2,3}});
        save.journal.Add(new JournalEntry{text="Round trip reflection",createdUtc="test"});
        var restored=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
        Check(restored.results[0].Total==51 && restored.journal[0].text=="Round trip reflection","Save serialization");
        Debug.Log("ALIGN360 VALIDATION PASSED: question bank, minimum/maximum scores, all interpretation boundaries, answer validation and save serialization.");
    }
    private static void Check(bool condition,string name) { if(!condition) throw new Exception("ALIGN360 validation failed: "+name); }
}
