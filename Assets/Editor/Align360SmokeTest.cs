using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Align360;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class Align360SmokeTest
{
    [MenuItem("Tools/ALIGN360/Run isolated Play mode smoke test")]
    public static void Run()
    {
        var app=UnityEngine.Object.FindFirstObjectByType<Align360App>();
        if(!EditorApplication.isPlaying || app==null) throw new Exception("Open SampleScene in Play mode first.");
        var field=typeof(Align360App).GetField("store",BindingFlags.Instance|BindingFlags.NonPublic);
        var original=field.GetValue(app);
        string path=Path.Combine(Application.temporaryCachePath,"align360-smoke-"+Guid.NewGuid().ToString("N")+".json");
        var isolated=new LocalStore(path); field.SetValue(app,isolated);
        try
        {
            Align360App.OpenPage("home");
            Complete(app,"ALIGN360",4,40);
            Check(isolated.Data.results.Count==1 && isolated.Data.results[0].Total==160,"40-question submission");
            Complete(app,"Pre-course",3,18); Complete(app,"Post-course",4,18);
            Check(isolated.Data.results[2].Total-isolated.Data.results[1].Total==18,"Pre/post comparison");
            Call(app,"StartNew","ALIGN360");Click(app,"5  Strongly agree");
            Check(new LocalStore(path).Data.draftAnswers[0]==5,"Saved draft round trip");
            Call(app,"Resume");Check(isolated.Data.draftAnswers[0]==5,"Resume draft");
            Call(app,"Journal");var input=app.GetComponentInChildren<InputField>();input.text="Smoke test reflection";Click(app,"Save reflection");
            Check(new LocalStore(path).Data.journal.Count==1,"Journal persistence");
            Call(app,"Exercises");Click(app,"Mark practiced");Check(isolated.Data.completedExercises.Count==1,"Exercise completion");
            Call(app,"Form",0,0);Click(app,"Remain calm while de-escalating the situation");Click(app,"Reflection before responding");
            Check(isolated.Data.journal.Count==3,"Scenario responses saved separately");
            var native=app.GetComponent<DontomykUI>();
            if(native!=null)
            {
                native.Splash();Check(native.CurrentScreen=="Splash","Native splash");
                native.MainMenu();native.Stats();Check(native.CurrentScreen=="Stats","Native stats");
                native.Journal("REFLECT");var editor=native.GetComponentsInChildren<InputField>().First(i=>i.name=="Editable reflection");editor.text="Native journal test";Click(app,"SAVE REFLECTION");
                Check(isolated.Data.journal.Count==4,"Native journal saved");
                native.Settings();Check(native.CurrentScreen=="Settings","Native settings");
                Check(!UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Any(c=>c.name=="Canvas" && c.renderMode!=RenderMode.WorldSpace),"Legacy screen Canvas disabled");
                var music=native.GetComponentsInChildren<Slider>().First(s=>s.name.Contains("Music"));music.value=.35f;
                Check(Math.Abs(new LocalStore(path).Data.musicVolume-.35f)<.001f,"Native music setting persistence");
                Click(app,"ON");Check(!isolated.Data.vibration,"Native vibration toggle");
                Click(app,"Small");Check(isolated.Data.textSize==0,"Native text size setting");
                native.StartDay();typeof(DontomykUI).GetMethod("ChooseWorkplaceAction",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(native,new object[]{1});
                Check(isolated.Data.activity[0].actions==1 && isolated.Data.journal.Count==5,"Native choice activity and journal");
                native.MainMenu();
            }
            Debug.Log("ALIGN360 PLAY SMOKE PASSED: 40-question submission, pre/post results, draft save/resume, journal, exercise completion and scenario responses, native navigation, settings and activity persistence. User data untouched.");
        }
        finally
        {
            field.SetValue(app,original);Call(app,"Home");var native=app.GetComponent<DontomykUI>();if(native!=null){native.MainMenu();typeof(DontomykUI).GetMethod("ApplyAudio",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(native,null);}
            foreach(string file in new[]{path,path+".bak",path+".tmp"}) if(File.Exists(file))File.Delete(file);
        }
    }
    private static void Complete(Align360App app,string phase,int rating,int count)
    {
        Call(app,"StartNew",phase);
        string[] labels={"", "Strongly disagree","Disagree","Neutral","Agree","Strongly agree"};
        for(int i=0;i<count;i++) { Click(app,rating+"  "+labels[rating]);Click(app,i==count-1?"Submit assessment":"Next"); }
    }
    private static void Click(Align360App app,string title)
    {
        foreach(var button in app.GetComponentsInChildren<Button>())
            if(button.gameObject.activeInHierarchy && button.GetComponentInChildren<Text>().text==title)
            { Check(button.interactable,"Button enabled: "+title);button.onClick.Invoke();return; }
        throw new Exception("Button not found: "+title);
    }
    private static void Call(Align360App app,string method,params object[] args)
    { typeof(Align360App).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,args); }
    private static void Check(bool condition,string label) { if(!condition)throw new Exception("Smoke test failed: "+label); }
}
