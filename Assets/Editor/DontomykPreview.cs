using System;
using System.IO;
using Align360;
using UnityEditor;
using UnityEngine;

public static class DontomykPreview
{
    private static DontomykUI UI()
    {
        var ui=UnityEngine.Object.FindFirstObjectByType<DontomykUI>();
        if(!EditorApplication.isPlaying || ui==null)throw new InvalidOperationException("Press Play in SampleScene before previewing the native UI.");
        return ui;
    }
    [MenuItem("Tools/Dontomyk/Preview/Splash")] public static void Splash(){UI().Splash();}
    [MenuItem("Tools/Dontomyk/Preview/Main Menu")] public static void Main(){UI().MainMenu();}
    [MenuItem("Tools/Dontomyk/Preview/Stats")] public static void Stats(){UI().Stats();}
    [MenuItem("Tools/Dontomyk/Preview/Journal")] public static void Journal(){UI().Journal();}
    [MenuItem("Tools/Dontomyk/Preview/Settings")] public static void Settings(){UI().Settings();}
    [MenuItem("Tools/Dontomyk/Capture Game View")]
    public static void Capture()
    {
        var ui=UI();string path=Path.Combine(Application.temporaryCachePath,"Dontomyk-"+ui.CurrentScreen.Replace(" ","-")+".png");
        ScreenCapture.CaptureScreenshot(path);Debug.Log("Dontomyk screenshot requested: "+path);
    }
}
