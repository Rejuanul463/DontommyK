using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Align360
{
    // Adds the assessment workspace without rewriting the authored office scene.
    public class Align360App : MonoBehaviour
    {
        private QuestionBank bank;
        private LocalStore store;
        private Canvas canvas;
        private RectTransform window, content, safeRoot;
        private Rect lastSafeArea;
        private Sprite brandSprite;
        private Font font;
        private Align360Theme theme;
        private string phase, journalDraft = "", status = "";
        private int[] answers;
        private int index;
        private readonly Color gold = new Color(.92f, .73f, .31f);
        private readonly Color ink = new Color(.025f, .045f, .085f, .96f);
        private static readonly string[] ExerciseTitles = { "Emotional trigger journal", "Mission and role reflection", "Pause before responding", "Reflective listening", "Accountability and feedback" };
        private static readonly string[] ExerciseSteps = {
            "Recall one difficult interaction today. Name the emotion and trigger. Separate what happened from what you assumed. Write one response you would choose next time.",
            "Choose one organizational value. Describe how your role supports it. Identify one action tomorrow that demonstrates that value while respecting professional boundaries.",
            "Pause during a stressful moment. Take three slow, comfortable breaths. Name the emotion. Consider your professional goal, then choose a constructive response. Record what changed.",
            "In your next conversation, listen without interrupting. Summarize the other person's point and ask whether you understood correctly before sharing your view. Reflect on the outcome.",
            "Recall feedback you received. Identify one useful observation, one action you own, and one measurable improvement to practice this week. Request feedback on that action." };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene" || FindFirstObjectByType<Align360App>() != null) return;
            new GameObject("ALIGN360 Workspace").AddComponent<Align360App>();
        }
        private void Start()
        {
            theme = new Align360Theme();
            bank = JsonUtility.FromJson<QuestionBank>(Resources.Load<TextAsset>("Align360/assessment").text);
            store = new LocalStore();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("ALIGN360 Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1100, 800); scaler.matchWidthOrHeight = .5f;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                events.AddComponent<InputSystemUIInputModule>();
#else
                events.AddComponent<StandaloneInputModule>();
#endif
            }
            safeRoot = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(root.transform,false); ApplySafeArea();
            window = Panel(safeRoot, "Workspace", ink);
            window.anchorMin = Vector2.zero; window.anchorMax = Vector2.one; window.offsetMin = new Vector2(12,12); window.offsetMax = new Vector2(-12,-12);
            theme.Card(window.GetComponent<Image>());
            var header = Panel(window, "Header", new Color(0,0,0,0));
            header.anchorMin = new Vector2(0,1); header.anchorMax = Vector2.one; header.pivot = new Vector2(.5f,1); header.sizeDelta = new Vector2(0,70); header.anchoredPosition = Vector2.zero;
            var title = Label(header, "ALIGN360", 26); title.fontStyle=FontStyle.Bold;title.color=theme.Gold;title.alignment=TextAnchor.MiddleLeft; title.GetComponent<RectTransform>().offsetMin = new Vector2(20,0); title.GetComponent<RectTransform>().offsetMax = new Vector2(-160,0);
            var close = Button(header, "Office", () => window.gameObject.SetActive(false));
            var cr = close.GetComponent<RectTransform>(); cr.anchorMin = cr.anchorMax = new Vector2(1,.5f); cr.pivot = new Vector2(1,.5f); cr.sizeDelta = new Vector2(120,44); cr.anchoredPosition = new Vector2(-14,0);
            var viewport = Panel(window, "Scroll", Color.clear); viewport.offsetMin = new Vector2(16,86); viewport.offsetMax = new Vector2(-20,-82);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Panel(viewport, "Content", Color.clear); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1); content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 14; layout.padding = new RectOffset(12,12,12,30); layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            var nav=Panel(window,"Navigation",Color.clear);
            nav.anchorMin=Vector2.zero;nav.anchorMax=new Vector2(1,0);nav.pivot=new Vector2(.5f,0);nav.sizeDelta=new Vector2(-24,64);nav.anchoredPosition=new Vector2(0,12);
            var row=nav.gameObject.AddComponent<HorizontalLayoutGroup>();row.spacing=8;row.childControlWidth=true;row.childForceExpandWidth=true;row.childControlHeight=true;
            Button(nav,"HOME",Home);Button(nav,"JOURNAL",Journal);Button(nav,"STATS",History);Button(nav,"OFFICE",()=>window.gameObject.SetActive(false));
            Home();
            window.gameObject.SetActive(false);
            gameObject.AddComponent<DontomykUI>();
        }
        public SaveData SavedData => store.Data;
        public void Persist() { Save(); }
        public void ShowAssessment(string selectedPhase) { window.gameObject.SetActive(true);Begin(selectedPhase); }
        public void HideWorkflow() { window.gameObject.SetActive(false); }

        private void Update() { if(safeRoot!=null && lastSafeArea!=Screen.safeArea) ApplySafeArea(); }
        private void ApplySafeArea()
        {
            lastSafeArea=Screen.safeArea;
            safeRoot.anchorMin=new Vector2(lastSafeArea.xMin/Screen.width,lastSafeArea.yMin/Screen.height);
            safeRoot.anchorMax=new Vector2(lastSafeArea.xMax/Screen.width,lastSafeArea.yMax/Screen.height);
            safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
        }
        public static bool OpenPage(string page)
        {
            var app=FindFirstObjectByType<Align360App>();
            if(app==null || app.window==null) return false;
            app.window.gameObject.SetActive(true);
            if(page=="journal") app.Journal(); else if(page=="stats") app.History(); else if(page=="scenarios") app.Form(0,0); else app.Home();
            return true;
        }
        private void OnDestroy() { if(brandSprite!=null) Destroy(brandSprite); if(theme!=null)theme.Dispose(); }
        private RectTransform Panel(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent,false);
            obj.GetComponent<Image>().color = color;
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private Text Label(Transform parent, string text, int size = 20)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent,false);
            var t = obj.GetComponent<Text>(); t.font = font; t.fontSize = size; t.color = theme.Cream; t.text = text; t.supportRichText = false;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var r = obj.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            return t;
        }
        private Button Button(Transform parent, string text, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(parent,text,new Color(.19f,.22f,.27f));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.onClick.AddListener(action);
            bool primary=text=="Next" || text=="Submit assessment" || text.StartsWith("Start ALIGN360") || text.StartsWith("Resume saved") || text=="Save reflection" || text=="Open ALIGN360";
            theme.Button(rect.GetComponent<Image>(),primary);
            var label = Label(rect,text); label.alignment = TextAnchor.MiddleCenter; label.color = primary?theme.PaperInk:theme.Gold;
            label.fontStyle=FontStyle.Bold;label.fontSize=18;
            label.GetComponent<RectTransform>().offsetMin=new Vector2(14,4);label.GetComponent<RectTransform>().offsetMax=new Vector2(-14,-4);
            var colors=button.colors;colors.highlightedColor=new Color(1, .93f,.72f);colors.pressedColor=new Color(.75f,.65f,.42f);colors.disabledColor=new Color(.45f,.45f,.45f);button.colors=colors;
            var le = rect.gameObject.AddComponent<LayoutElement>(); le.minHeight = 52; le.preferredHeight = 56;
            return button;
        }
        private void Clear()
        {
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            content.anchoredPosition = Vector2.zero;
        }
        private void Text(string text, int size = 20)
        {
            var t = Label(content,text,size); t.color=size>=24?theme.Gold:(size<=17?theme.Muted:theme.Cream);if(size>=24)t.fontStyle=FontStyle.Bold;var le = t.gameObject.AddComponent<LayoutElement>(); le.minHeight = size + 12;

        }
        private void Progress(float fraction)
        {
            var track=Panel(content,"Progress",new Color(.12f,.17f,.2f));track.gameObject.AddComponent<LayoutElement>().preferredHeight=9;
            var fill=Panel(track,"Fill",new Color(.62f,.76f,.38f));fill.anchorMax=new Vector2(Mathf.Clamp01(fraction),1);fill.offsetMin=fill.offsetMax=Vector2.zero;
        }
        private void PaperEntry(string date,string body)
        {
            var card=Panel(content,"Reflection page",new Color(.91f,.82f,.63f));
            var layout=card.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(22,18,18,18);layout.spacing=10;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
            var heading=Label(card,date,17);heading.color=theme.PaperInk;heading.fontStyle=FontStyle.Bold;
            var text=Label(card,body);text.color=theme.PaperInk;
        }
        private void Action(string text, UnityEngine.Events.UnityAction action) { Button(content,text,action); }
        private void Save()
        {
            try { store.Save(); status = "Saved on this device."; }
            catch (Exception e) { status = "Could not save: " + e.Message; Debug.LogWarning(status); }
        }
        private void Home()
        {
            Clear();
            var texture=Resources.Load<Texture2D>("Align360/logo");
            if(texture!=null) { var logo=Panel(content,"Excellent Strategies logo",Color.white);var image=logo.GetComponent<Image>();if(brandSprite==null) brandSprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));image.sprite=brandSprite;image.preserveAspect=true;logo.gameObject.AddComponent<LayoutElement>().preferredHeight=100; }
            Text("YOUR DEVELOPMENT",28);
            Text("Reflect. Practice. Track your progress.",17);
            Text("Professional development only. This self-assessment is not a clinical evaluation or a tool for disciplinary decisions.");
            Text("Answers and journal entries are stored locally on this device. No account or AI service is connected.",17);
            if (store.Data.draftAnswers != null) Action("Resume saved assessment", Resume);
            Action("Start ALIGN360 assessment · 40 questions", () => Begin("ALIGN360"));
            Action("Pre-course baseline · 18 questions", () => Begin("Pre-course"));
            Action("Post-course assessment · 18 questions", () => Begin("Post-course"));
            Action("30-day follow-up · 18 questions", () => Begin("30-day"));
            Action("Results and course progress", History); Action("Development exercises", Exercises); Action("Reflection journal", Journal);
            Action("Course scenarios and optional surveys", Forms);
            Action("Local data and export", DataScreen);
            if (!string.IsNullOrEmpty(status)) Text(status,17);
        }
        private void Begin(string selectedPhase)
        {
            if (store.Data.draftAnswers != null)
            {
                Clear(); Text("Replace the unfinished assessment?",28); Text("Your saved draft will be replaced. Completed results are retained.");
                Action("Keep draft and return",Home); Action("Replace draft",()=>StartNew(selectedPhase)); return;
            }
            StartNew(selectedPhase);
        }
        private void StartNew(string selectedPhase)
        {
            phase = selectedPhase; answers = new int[phase == "ALIGN360" ? 40 : 18]; index = 0; SaveDraft(); Question();
        }
        private void Resume()
        {
            phase = store.Data.draftPhase; int count = phase == "ALIGN360" ? 40 : 18;
            if (store.Data.draftAnswers == null || store.Data.draftAnswers.Length != count) { store.Data.draftAnswers = null; Home(); return; }
            answers = store.Data.draftAnswers; index = Mathf.Clamp(store.Data.draftIndex,0,count-1); Question();
        }
        private void SaveDraft() { store.Data.draftPhase = phase; store.Data.draftAnswers = answers; store.Data.draftIndex = index; Save(); }
        private void Question()
        {
            Clear(); bool main = phase == "ALIGN360"; int size = main ? 8 : 3;
            Text(phase + " · " + (index+1) + " / " + answers.Length,26);
            Progress((float)(index+1)/answers.Length);
            Text((main ? bank.categories : bank.courseCategories)[index/size],22);
            Text((main ? bank.questions : bank.courseQuestions)[index],28);
            Text("Choose based on your typical workplace experiences. All questions are required.",17);
            string[] ratings = { "Strongly disagree", "Disagree", "Neutral", "Agree", "Strongly agree" };
            for (int i=1;i<=5;i++) { int value=i; Action((answers[index]==i ? "✓ " : "") + i + "  " + ratings[i-1],()=>{answers[index]=value;SaveDraft();Question();}); }
            if(index>0) Action("Previous",()=>{index--;SaveDraft();Question();});
            var next = Button(content,index==answers.Length-1 ? "Submit assessment" : "Next",()=>{
                if(index<answers.Length-1) { index++;SaveDraft();Question(); } else Submit();
            }); next.interactable = answers[index]>=1 && answers[index]<=5;
            Action("Save draft and return",()=>{SaveDraft();Home();});
        }
        private void Submit()
        {
            if(!Scoring.Valid(answers,phase=="ALIGN360" ? 40 : 18)) { index=Array.FindIndex(answers,a=>a<1||a>5);Question();return; }
            var result = new AssessmentResult { id=Guid.NewGuid().ToString("N"), phase=phase, completedUtc=DateTime.UtcNow.ToString("o"), answers=(int[])answers.Clone() };
            store.Data.results.Add(result);store.Data.draftAnswers=null;Save();Results(result);
        }
        private void Results(AssessmentResult result)
        {
            Clear(); bool main=result.phase=="ALIGN360";Text(result.phase+" results",30);
            Text(result.Total+" / "+(main?200:90)+" · "+Scoring.Band(result.Total,!main),24);
            var categories=main?bank.categories:bank.courseCategories; int size=main?8:3;
            for(int i=0;i<categories.Length;i++) { Text(categories[i]+": "+result.CategoryScore(i,size)+" / "+(size*5));Progress((float)result.CategoryScore(i,size)/(size*5)); }
            Text("Category thresholds were not specified by the client; category totals are shown without high/low labels. Self-ratings describe development opportunities, not diagnoses.",17);
            if(!main)
            {
                AssessmentResult baseline=null;
                foreach(var r in store.Data.results) { if(r==result) break; if(r.phase=="Pre-course") baseline=r; }
                if(baseline!=null && result.phase!="Pre-course")
                {
                    int delta=result.Total-baseline.Total;Text("Change from preceding baseline: "+(delta>=0?"+":"")+delta+" points out of 90",24);
                    for(int i=0;i<categories.Length;i++) Text(categories[i]+" change: "+(result.CategoryScore(i,3)-baseline.CategoryScore(i,3)));
                }
                else Text("Complete a pre-course baseline before a later assessment to compare progress.",17);
            }
            Action("Development exercises",Exercises);Action("All results",History);Action("Home",Home);
        }
        private void History()
        {
            Clear();Text("STATS",30);Text("Track your progress. Build your best self.",17);
            if(store.Data.results.Count>0) { var latest=store.Data.results[store.Data.results.Count-1];Text(latest.Total+" / "+(latest.phase=="ALIGN360"?200:90),36);Text(latest.phase+" · latest completed assessment",17); }
            if(store.Data.results.Count==0) Text("No completed assessments yet.");
            for(int i=store.Data.results.Count-1;i>=0;i--) { var r=store.Data.results[i];Action(r.phase+" · "+r.completedUtc.Substring(0,10)+" · "+r.Total,()=>Results(r)); }
            Action("Home",Home);
        }
        private void Exercises()
        {
            Clear();Text("Your development pathway",30);Text("Weeks 1–2: awareness and triggers. Weeks 3–4: regulation and stress. Weeks 5–6: communication. Weeks 7–8: alignment and leadership. Continue practicing afterward.");
            var latest=store.Data.results.FindLast(r=>r.phase=="ALIGN360");
            int priority=-1;
            if(latest!=null) { priority=0;for(int i=1;i<5;i++) if(latest.CategoryScore(i,8)<latest.CategoryScore(priority,8)) priority=i;Text("Suggested starting point: "+bank.categories[priority]+" (your lowest category total). This is a rule-based suggestion.",22); }
            for(int i=0;i<5;i++)
            {
                int exercise=i;string id="exercise-"+i;Text((i==priority?"Suggested · ":"")+ExerciseTitles[i],24);Text(ExerciseSteps[i]);
                bool done=store.Data.completedExercises.Contains(id);
                Action(done?"Completed · mark for more practice":"Mark practiced",()=>{if(done)store.Data.completedExercises.Remove(id);else store.Data.completedExercises.Add(id);Save();Exercises();});
            }
            Action("Write a reflection",Journal);Action("Home",Home);
        }
        private void Journal()
        {
            Clear();Text("JOURNAL",30);Text(DateTime.Now.ToString("dddd, MMMM d, yyyy"),17);Text("What triggered your reaction? What did you assume? What constructive response would you practice next time?");
            var box=Panel(content,"Journal input",new Color(.9f,.8f,.59f));var le=box.gameObject.AddComponent<LayoutElement>();le.preferredHeight=180;
            var input=box.gameObject.AddComponent<InputField>();var text=Label(box,journalDraft);text.color=theme.PaperInk;text.GetComponent<RectTransform>().offsetMin=new Vector2(12,12);text.GetComponent<RectTransform>().offsetMax=new Vector2(-12,-12);
            input.textComponent=text;input.lineType=InputField.LineType.MultiLineNewline;input.characterLimit=8000;input.text=journalDraft;input.onValueChanged.AddListener(v=>journalDraft=v);
            Action("Save reflection",()=>{if(string.IsNullOrWhiteSpace(journalDraft))return;store.Data.journal.Add(new JournalEntry{createdUtc=DateTime.UtcNow.ToString("o"),text=journalDraft.Trim()});journalDraft="";Save();Journal();});
            for(int i=store.Data.journal.Count-1;i>=0;i--) { var e=store.Data.journal[i];PaperEntry(e.createdUtc.Substring(0,10),e.text); }
            Action("Home",Home);
        }
        private static readonly string[][] FormPrompts = {
            new[] { "A citizen becomes verbally aggressive during a public-service interaction. How would you most likely respond?", "A coworker criticizes your work publicly during a meeting. What is your first internal reaction?" },
            new[] { "Workplace stress", "Emotional reactions", "Miscommunication", "Interpersonal conflict", "Anxiety under pressure", "Defensive communication" },
            new[] { "What emotional trigger are you now better able to recognize?", "What strategy will you use to prevent emotional hijacking?", "How has your understanding of perception changed?", "What workplace behavior will you intentionally improve?", "Which course concept will most improve your professional effectiveness?" },
            new[] { "I can better manage emotional reactions at work.", "I can identify emotional hijacking more quickly.", "I communicate more intentionally under stress.", "I understand how perception influences conflict.", "I can better regulate anger and anxiety.", "I can apply self-awareness strategies professionally." },
            new[] { "Have you applied the course techniques in the workplace?", "Has your emotional regulation improved?", "Have workplace conflicts decreased?", "Has your communication improved?", "Have stress-management techniques been helpful?" },
            new[] { "Professional communication", "Emotional control", "Team interactions", "Responsiveness to feedback", "Workplace professionalism", "Conflict management" },
            new[] { "What emotional triggers most commonly affect your workplace interactions?", "In what situations do you find it most difficult to maintain professional emotional control?", "What personal beliefs or fears may sometimes influence your workplace behavior?", "What strategies help you remain aligned with professional expectations under pressure?", "What professional behaviors would you most like to improve?" }
        };
        private static readonly string[] FormNames = { "Workplace scenarios", "Organizational impact", "Knowledge application", "Behavioral application", "30-day follow-up reflections", "Supervisor observations", "Optional ALIGN360 reflections" };
        private void Forms()
        {
            Clear();Text("Course activities and optional surveys",30);Text("These responses are saved separately and do not change your 200-point or 90-point assessment score. No scenario weights were supplied.");
            for(int i=0;i<FormNames.Length;i++) { int type=i;Action(FormNames[i],()=>Form(type,0)); }
            Action("View saved responses in journal",Journal);Action("Home",Home);
        }
        private void Form(int type,int question)
        {
            Clear();Text(FormNames[type]+" · "+(question+1)+" / "+FormPrompts[type].Length,26);Text(FormPrompts[type][question],24);
            System.Action<string> record=value=>{store.Data.journal.Add(new JournalEntry{createdUtc=DateTime.UtcNow.ToString("o"),text=FormNames[type]+" — "+FormPrompts[type][question]+"\n"+value});Save();if(question+1<FormPrompts[type].Length)Form(type,question+1);else Forms();};
            string[] choices=null;
            if(type==0) choices=question==0?new[]{"Become defensive","Match their emotional intensity","Remain calm while de-escalating the situation","End the interaction immediately"}:new[]{"Anger","Embarrassment","Reflection before responding","Withdrawal"};
            if(type==1) choices=new[]{"Low","Moderate","High"};
            if(type==3) choices=new[]{"1 — Strongly disagree","2 — Disagree","3 — Neutral","4 — Agree","5 — Strongly agree"};
            if(type==5) choices=new[]{"Improved","No Change","Declined"};
            if(choices!=null) foreach(var choice in choices) { string value=choice;Action(value,()=>record(value)); }
            else
            {
                var box=Panel(content,"Response",new Color(.15f,.18f,.22f));box.gameObject.AddComponent<LayoutElement>().preferredHeight=160;
                var input=box.gameObject.AddComponent<InputField>();input.textComponent=Label(box,"");input.lineType=InputField.LineType.MultiLineNewline;input.characterLimit=8000;
                Action("Save response and continue",()=>{if(!string.IsNullOrWhiteSpace(input.text))record(input.text.Trim());});
            }
            Action("Return to activities",Forms);
        }
        private void DataScreen()
        {
            Clear();Text("Your local data",30);Text("Data is stored as a local JSON file on this device. Anyone with access to that file can read it. Export includes assessment answers and journal entries.");
            Action("Export JSON copy",()=>{try { string path=Path.Combine(Application.persistentDataPath,"align360-export-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".json");File.WriteAllText(path,JsonUtility.ToJson(store.Data,true));status="Exported to "+path;}catch(Exception e){status="Export failed: "+e.Message;}DataScreen();});
            Text(status,17);
            Action("Delete local ALIGN360 progress…",()=>{Clear();Text("Delete assessments, drafts, exercise progress and journal?",28);Text("Exported copies are retained. This cannot be undone.");Action("Cancel",DataScreen);Action("Delete local progress",()=>{try{if(File.Exists(store.Path))File.Delete(store.Path);if(File.Exists(store.Path+".bak"))File.Delete(store.Path+".bak");store=new LocalStore();journalDraft="";status="Local progress deleted.";Home();}catch(Exception e){status="Delete failed: "+e.Message;DataScreen();}});});
            Action("Home",Home);
        }
    }
}
