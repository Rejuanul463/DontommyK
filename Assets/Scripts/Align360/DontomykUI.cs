using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Align360
{
    // Rebuilt native uGUI front end. All controls, chart bars and journal text are live.
    public sealed class DontomykUI : MonoBehaviour
    {
        private Align360App app;
        private Align360Theme theme;
        private RectTransform safe,stage,page;
        private Font font,headingFont,journalFont;
        private Sprite paperSprite;
        private Texture2D paperTexture;
        private readonly Dictionary<string,Sprite> assets=new Dictionary<string,Sprite>();
        private Color gold => app==null || Data.accentIndex==0 ? new Color(1,.77f,.25f) : Data.accentIndex==1 ? new Color(.43f,.76f,.72f) : new Color(.7f,.59f,.8f);
        private readonly Color cream=new Color(.94f,.88f,.71f), navy=new Color(.025f,.045f,.08f), muted=new Color(.56f,.61f,.6f),green=new Color(.64f,.77f,.38f);
        private string screen="",statsTab="OVERVIEW",journalTab="DAILY";
        private DateTime journalDate=DateTime.Today;
        private int statsDays=7;
        private bool sessionActive;
        private float pendingSeconds;
        private Text clockText;
        public string CurrentScreen=>screen;
        private SaveData Data=>app.SavedData;
        private void Start()
        {
            app=GetComponent<Align360App>();theme=new Align360Theme();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");headingFont=Resources.Load<Font>("Dontomyk/HeadingFont");journalFont=Resources.Load<Font>("Dontomyk/JournalFont");
            foreach(var old in Resources.FindObjectsOfTypeAll<Canvas>())
                if(old.gameObject.scene==gameObject.scene && old.name=="Canvas" && old.renderMode!=RenderMode.WorldSpace)old.gameObject.SetActive(false);
            var character=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t.gameObject.scene==gameObject.scene && t.name=="DOnt_model");
            if(character!=null)character.gameObject.SetActive(true);
            var root=new GameObject("Dontomyk Native UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=100;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(402,874);scaler.matchWidthOrHeight=1;
            safe=Rect(root.transform,"Safe Area");stage=Rect(safe,"402 x 874 Design Stage");stage.anchorMin=stage.anchorMax=new Vector2(.5f,.5f);stage.sizeDelta=new Vector2(402,874);
            var officeClock=FindFirstObjectByType<ClockTiming>();
            if(officeClock!=null && officeClock.background!=null && officeClock.TimeLapsMaterials!=null && officeClock.TimeLapsMaterials.Length>0)
                officeClock.background.sharedMaterial=officeClock.TimeLapsMaterials[0];
            ApplyAudio();Splash();
        }
        private void Update()
        {
            if(safe==null)return;
            var area=Screen.safeArea;safe.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
            float scale=Mathf.Min(safe.rect.width/402,safe.rect.height/874);stage.localScale=Vector3.one*scale;
            if(sessionActive) {Today().focusSeconds+=Time.unscaledDeltaTime;pendingSeconds+=Time.unscaledDeltaTime;if(pendingSeconds>=15){app.Persist();pendingSeconds=0;}}
            if(clockText!=null)clockText.text=DateTime.Now.ToString("h:mm tt");
        }
        private ActivityDay Today()
        {
            string date=DateTime.Today.ToString("yyyy-MM-dd");var day=Data.activity.Find(d=>d.date==date);
            if(day==null){day=new ActivityDay{date=date};Data.activity.Add(day);}return day;
        }
        private void OnApplicationPause(bool paused){if(paused && app!=null)app.Persist();}
        private void OnApplicationQuit(){if(app!=null)app.Persist();}
        private void OnDestroy(){if(theme!=null)theme.Dispose();foreach(var a in assets.Values)if(a!=null)Destroy(a);if(paperSprite!=null)Destroy(paperSprite);if(paperTexture!=null)Destroy(paperTexture);}
        private RectTransform Rect(Transform parent,string name)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        private void Place(RectTransform rect,float x,float y,float w,float h)
        {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);}
        private Image Fill(Transform parent,string name,float x,float y,float w,float h,Color color)
        {var r=Rect(parent,name);Place(r,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=color;return image;}
        private Image Card(Transform parent,string name,float x,float y,float w,float h)
        {var image=Fill(parent,name,x,y,w,h,navy);theme.Card(image);return image;}
        private Sprite Asset(string name)
        {
            if(assets.TryGetValue(name,out var sprite))return sprite;
            var texture=Resources.Load<Texture2D>("Dontomyk/"+name);if(texture==null)return null;
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));assets[name]=sprite;return sprite;
        }
        private Image Picture(Transform parent,string asset,float x,float y,float w,float h)
        {var image=Fill(parent,asset,x,y,w,h,Color.white);image.sprite=Asset(asset);image.preserveAspect=true;image.raycastTarget=false;return image;}
        private Text Text(Transform parent,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.UpperLeft,bool bold=false)
        {
            var r=Rect(parent,value.Length>25?value.Substring(0,25):value);Place(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=(!bold && journalFont!=null && (parent.name=="Live content" || parent.name=="Editable reflection"))?journalFont:font;t.fontSize=size<=16?Mathf.Max(8,size+Data.textSize-1):size;t.color=color;t.text=value;t.alignment=align;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        private Button Button(Transform parent,string value,float x,float y,float w,float h,UnityEngine.Events.UnityAction click,bool primary=false)
        {
            var image=Fill(parent,value,x,y,w,h,navy);theme.Button(image,primary);var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;
            b.onClick.AddListener(()=>{if(Data.vibration && Application.isMobilePlatform)Handheld.Vibrate();click();});
            Text(image.transform,value,8,0,w-16,h,(h>=54 && w>250?26:value.Length<=2?14:w<=60?9:w<=110?10:Mathf.Max(10,Data.textSize+11)),primary?new Color(.17f,.12f,.06f):gold,TextAnchor.MiddleCenter,true);return b;
        }
        private void Clear(string name,bool dim=true)
        {
            screen=name;clockText=null;app.HideWorkflow();
            if(page!=null){page.gameObject.SetActive(false);Destroy(page.gameObject);}page=Rect(stage,name);
            if(dim)Fill(page,"Night shade",0,0,402,874,new Color(.01f,.02f,.05f,Data.darkMode?.68f:.24f));
        }
        private void Header(string title,string subtitle)
        {
            var heading=Text(page,title,38,45,326,44,32,gold,TextAnchor.MiddleCenter,true);if(headingFont!=null){heading.font=headingFont;heading.fontStyle=FontStyle.Normal;}
            Text(page,subtitle,38,87,326,24,10,cream,TextAnchor.MiddleCenter);
        }
        private void CloseButton(){Button(page,"×",349,119,25,25,MainMenu);}
        private void Footer()
        {
            var nav=Fill(page,"Bottom navigation",20,800,362,70,new Color(.02f,.04f,.075f,.85f));
            string[] icons={"journal","stats","settings"};UnityEngine.Events.UnityAction[] actions={()=>Journal(),()=>Stats(),()=>Settings()};
            for(int i=0;i<3;i++)
            {
                int j=i;var target=Fill(nav.transform,icons[i]+" navigation",i*120,0,120,70,Color.clear);var button=target.gameObject.AddComponent<Button>();button.targetGraphic=target;button.onClick.AddListener(actions[j]);
                Picture(target.transform,icons[i],39,3,42,42);Text(target.transform,icons[i].ToUpperInvariant(),5,49,110,18,12,gold,TextAnchor.MiddleCenter);
                if(i>0)Fill(nav.transform,"Gold divider",i*120,7,1,56,new Color(.6f,.46f,.2f,.5f));
            }
        }
        public void Splash()
        {
            sessionActive=false;Clear("Splash",false);var bg=Fill(page,"Night office",0,0,402,874,Color.white);bg.sprite=Asset("splash");
            Picture(page,"title",18,245,366,160);
            var play=Button(page,"PLAY",38,438,326,62,MainMenu,true);Picture(play.transform,"play",45,14,30,34);
            Text(page,"Your day. Your choices. Your balance.",30,523,342,24,13,cream,TextAnchor.MiddleCenter);
        }
        public void MainMenu()
        {
            if(sessionActive){sessionActive=false;app.Persist();}Clear("Main menu",false);
            Picture(page,"title",30,50,342,150);
            Button(page,"START DAY  →",39,626,324,58,StartDay,true);
            Button(page,"ALIGN360 ASSESSMENT",78,702,246,34,()=>app.ShowAssessment("ALIGN360"));
            if(Data.showTips)Text(page,"Your day. Your choices. Your balance.",30,740,342,22,12,cream,TextAnchor.MiddleCenter);
            Footer();
        }
        public void StartDay()
        {
            Clear("Workplace day",false);sessionActive=true;
            var day=Card(page,"Day",14,20,67,64);Text(day.transform,"DAY",0,4,67,20,12,gold,TextAnchor.MiddleCenter,true);Text(day.transform,Mathf.Max(1,Data.activity.Count).ToString(),0,24,67,32,25,cream,TextAnchor.MiddleCenter,true);
            clockText=Text(page,DateTime.Now.ToString("h:mm tt"),120,26,162,35,26,cream,TextAnchor.MiddleCenter,true);
            Button(page,"×",355,26,30,30,MainMenu);
            var scenario=Card(page,"Overtime event",14,477,374,100);Text(scenario.transform,"BOSS ASKED FOR OVERTIME",8,10,358,27,19,gold,TextAnchor.MiddleCenter,true);Text(scenario.transform,"The project is behind schedule. What will you do?",15,45,344,42,14,cream,TextAnchor.MiddleCenter);
            string[] titles={"STAY LATE","NEGOTIATE\nTOMORROW","GO HOME"};string[] icons={"focus","journal","heart"};Color[] colors={new Color(.19f,.65f,.6f),new Color(1,.73f,.12f),new Color(.91f,.31f,.25f)};
            for(int i=0;i<3;i++)
            {
                int choice=i;var card=Fill(page,titles[i],14+i*128,595,118,190,colors[i]);var b=card.gameObject.AddComponent<Button>();b.targetGraphic=card;b.onClick.AddListener(()=>ChooseWorkplaceAction(choice));
                Text(card.transform,titles[i],5,12,108,46,16,navy,TextAnchor.MiddleCenter,true);Picture(card.transform,icons[i],29,68,60,60);Text(card.transform,"CHOOSE  →",4,150,110,25,12,navy,TextAnchor.MiddleCenter,true);
            }
            Button(page,"COURSE SCENARIOS",76,813,250,36,()=>Align360App.OpenPage("scenarios"));
        }
        private void ChooseWorkplaceAction(int choice)
        {
            string[] actions={"Stay late: identify a realistic work limit and protect time to recover.","Negotiate tomorrow: clarify priorities and agree a realistic deadline.","Go home: communicate your boundaries respectfully and plan the next work period."};
            Today().actions++;Data.journal.Add(new JournalEntry{createdUtc=DateTime.UtcNow.ToString("o"),text="WORKPLACE CHOICE\n"+actions[choice]});sessionActive=false;app.Persist();
            Dialog("YOUR CHOICE",actions[choice]+"\n\nReflect on your reasons in your journal. Choices are not added to your assessment score.",()=>Journal(),"OPEN JOURNAL");
        }
        private string Duration(float seconds){int minutes=Mathf.FloorToInt(seconds/60);return (minutes/60)+"h "+(minutes%60)+"m";}
        public void Stats(string tab=null)
        {
            if(tab!=null)statsTab=tab;Clear("Stats");Header("STATS","Track your progress. Build your best self.");
            var body=Card(page,"Statistics panel",24,122,354,628);CloseButton();
            string[] tabs={"OVERVIEW","FOCUS","TASKS","BALANCE"};for(int i=0;i<4;i++){string selected=tabs[i];Button(body.transform,selected,10+i*84,18,82,27,()=>Stats(selected),statsTab==selected);}
            string[] icons={"check","focus","fire"};string[] names={"FOCUS TIME","TASKS DONE","DAYS ACTIVE"};
            float seconds=Data.activity.Sum(d=>d.focusSeconds);int count=Data.activity.Sum(d=>d.actions)+Data.completedExercises.Count;
            string[] values={Duration(seconds),count.ToString(),Data.activity.Count(d=>d.actions>0||d.focusSeconds>=60).ToString()};
            for(int i=0;i<3;i++){var tile=Card(body.transform,names[i],10+i*112,60,108,130);Picture(tile.transform,icons[i],34,10,40,40);Text(tile.transform,names[i],3,58,102,18,9,muted,TextAnchor.MiddleCenter);Text(tile.transform,values[i],0,79,108,30,23,i==1?new Color(.57f,.7f,.85f):green,TextAnchor.MiddleCenter,true);Text(tile.transform,"Saved activity",0,111,108,15,8,cream,TextAnchor.MiddleCenter);}
            var chart=Card(body.transform,"Activity graph",10,202,334,218);
            Text(chart.transform,statsTab=="TASKS"?"TASKS":"FOCUS TIME",12,10,145,22,12,cream);
            int[] ranges={7,28,364};string[] periods={"WEEK","MONTH","YEAR"};
            for(int i=0;i<3;i++){int days=ranges[i];var selector=Button(chart.transform,periods[i],177+i*48,9,46,20,()=>{statsDays=days;Stats();},statsDays==days);selector.GetComponentInChildren<Text>().fontSize=7;}
            var valuesByBin=new float[7];int stride=statsDays/7;var first=DateTime.Today.AddDays(-statsDays+1);
            if(statsDays==7){int offset=Data.weekStartsMonday?((int)DateTime.Today.DayOfWeek+6)%7:(int)DateTime.Today.DayOfWeek;first=DateTime.Today.AddDays(-offset);}
            for(int i=0;i<7;i++){DateTime from=first.AddDays(i*stride),until=from.AddDays(stride);valuesByBin[i]=Data.activity.Where(d=>DateTime.TryParse(d.date,out var date)&&date>=from&&date<until).Sum(d=>statsTab=="TASKS"?d.actions:d.focusSeconds/60);}
            float rangeTotal=valuesByBin.Sum();Text(chart.transform,statsTab=="TASKS"?rangeTotal.ToString("0"):Duration(rangeTotal*60),10,35,314,37,29,green,TextAnchor.MiddleCenter,true);
            if(statsTab=="BALANCE")Text(chart.transform,"Use reflection to review your work and recovery.\nNo balance score has been assigned.",24,95,286,75,14,cream,TextAnchor.MiddleCenter);
            else for(int i=0;i<7;i++)
            {
                DateTime date=first.AddDays(i*stride);float v=valuesByBin[i];float max=Mathf.Max(1,valuesByBin.Max());float height=v==0?2:Mathf.Clamp(v/max*78,3,78);
                Fill(chart.transform,"Bar "+date.ToString("yyyy-MM-dd"),22+i*44,174-height,20,height,green);
                Text(chart.transform,date.ToString(statsDays==7?"ddd":statsDays==28?"d MMM":"MMM").ToUpperInvariant(),10+i*44,184,43,15,8,muted,TextAnchor.MiddleCenter);
            }
            var achievements=Card(body.transform,"Achievements",10,434,334,136);Text(achievements.transform,"ACHIEVEMENTS",12,9,310,20,11,cream);
            Achievement(achievements.transform,"check","DEVELOPMENT PRACTICE",Data.completedExercises.Count+" / 5",36,green);
            Achievement(achievements.transform,"focus","ASSESSMENTS COMPLETED",Data.results.Count.ToString(),81,new Color(.5f,.67f,.8f));
            Button(body.transform,"VIEW ASSESSMENT RESULTS  →",14,582,326,32,()=>Align360App.OpenPage("stats"),true);
            var summary=Card(page,"Weekly summary",24,763,354,34);Text(summary.transform,"★  THIS WEEK'S SUMMARY",10,5,220,25,11,gold,TextAnchor.MiddleLeft,true);Button(summary.transform,"VIEW INSIGHTS",235,5,110,24,()=>Align360App.OpenPage("stats"),true);
            Footer();
        }
        private void Achievement(Transform parent,string icon,string title,string value,float y,Color color)
        {
            Picture(parent,icon,12,y,28,28);Text(parent,title,49,y,210,18,9,cream);Text(parent,value,255,y+2,65,23,14,color,TextAnchor.MiddleRight,true);Fill(parent,"Rule",49,y+24,270,1,new Color(.23f,.27f,.28f));
        }
        public void Journal(string tab=null)
        {
            if(tab!=null)journalTab=tab;Clear("Journal");Header("JOURNAL","Your day. Your choices. Your balance.");Button(page,"×",365,92,24,24,MainMenu);
            var calendar=Card(page,"Week calendar",44,122,334,65);
            int dayOffset=Data.weekStartsMonday?((int)journalDate.DayOfWeek+6)%7:(int)journalDate.DayOfWeek;DateTime start=journalDate.AddDays(-dayOffset);
            for(int i=0;i<7;i++){DateTime date=start.AddDays(i);Button(calendar.transform,date.ToString("ddd").Substring(0,1)+"\n"+date.Day,6+i*46,10,44,44,()=>{journalDate=date;Journal();},date.Date==journalDate.Date);}
            var cover=Fill(page,"Notebook cover",74,224,306,484,new Color(.29f,.18f,.07f));cover.rectTransform.localRotation=Quaternion.Euler(0,0,-2);
            var sheet=Fill(page,"Parchment notebook",79,215,294,480,Color.white);sheet.sprite=Paper();sheet.type=Image.Type.Sliced;
            for(int i=0;i<12;i++){Fill(sheet.transform,"Spiral shadow",0,16+i*37,13,5,new Color(.2f,.15f,.06f));Fill(sheet.transform,"Spiral binding",-8,13+i*37,21,3,new Color(.63f,.49f,.22f));}
            Text(sheet.transform,journalDate.ToString("dddd, MMM d, yyyy"),25,12,255,24,12,navy,TextAnchor.MiddleLeft,true);
            string[] tabs={"DAILY","STATS","FAVORITES","REFLECT"};string[] icons={"journal","chart","star","heart"};
            var side=Card(page,"Journal tabs",20,222,54,255);
            for(int i=0;i<4;i++){string selected=tabs[i];var b=Button(side.transform,"",2,4+i*61,50,58,()=>{if(selected=="STATS")Stats();else Journal(selected);});Picture(b.transform,icons[i],13,5,24,25);Text(b.transform,selected,0,36,50,15,7,gold,TextAnchor.MiddleCenter);}
            if(journalTab=="REFLECT")
            {
                Text(sheet.transform,"DAILY REFLECTION",25,52,250,25,13,navy,TextAnchor.MiddleLeft,true);
                var input=Input(sheet.transform,"What did you notice about your reactions today?",25,87,250,260);
                Button(sheet.transform,"SAVE REFLECTION",25,383,250,36,()=>{if(string.IsNullOrWhiteSpace(input.text))return;Data.journal.Add(new JournalEntry{createdUtc=DateTime.UtcNow.ToString("o"),text=input.text.Trim()});app.Persist();Journal("DAILY");},true);
            }
            else
            {
                var scroll=Scroll(sheet.transform,24,48,259,408,out var list);float y=0;
                var entries=Data.journal.Where(e=>journalTab=="FAVORITES"?Data.favorites.Contains(e.createdUtc):EntryDate(e.createdUtc).Date==journalDate.Date).Reverse().ToList();
                if(entries.Count==0)
                {
                    string[] headings={"FOCUS","DELEGATE","HELP","BALANCE","DAILY REFLECTION"};string[] hints={"What matters most today?","What could you share or clarify?","Who could you support?","How will you make time to recover?","What did you learn about your reactions?"};
                    for(int i=0;i<5;i++){Text(list,headings[i],0,y,248,20,12,navy,TextAnchor.MiddleLeft,true);Text(list,hints[i],0,y+23,248,44,12,new Color(.32f,.26f,.14f));Fill(list,"Paper rule",0,y+69,248,1,new Color(.65f,.54f,.34f,.5f));y+=77;}
                }
                else foreach(var entry in entries)
                {
                    var selected=entry;Text(list,EntryDate(entry.createdUtc).ToString("ddd, MMM d  h:mm tt"),0,y,206,20,10,navy,TextAnchor.MiddleLeft,true);
                    Button(list,Data.favorites.Contains(entry.createdUtc)?"★":"☆",220,y-2,28,25,()=>{if(Data.favorites.Contains(selected.createdUtc))Data.favorites.Remove(selected.createdUtc);else Data.favorites.Add(selected.createdUtc);app.Persist();Journal();});
                    float height=Mathf.Max(58,Mathf.Ceil(entry.text.Length/30f)*17);Text(list,entry.text,0,y+26,248,height,13,new Color(.24f,.18f,.1f));y+=height+41;Fill(list,"Paper rule",0,y-7,248,1,new Color(.65f,.54f,.34f,.5f));
                }
                list.sizeDelta=new Vector2(0,Mathf.Max(408,y));
            }
            var daily=Card(page,"Daily score",44,724,334,64);Text(daily.transform,"DAILY SCORE",12,10,245,21,14,gold,TextAnchor.MiddleLeft,true);
            var latest=Data.results.FindLast(r=>EntryDate(r.completedUtc).Date==journalDate.Date);Text(daily.transform,latest==null?"No assessment completed on this day":"Based on your assessment today",12,33,245,20,10,cream);Text(daily.transform,latest==null?"—":latest.Total.ToString(),267,8,57,45,27,gold,TextAnchor.MiddleCenter,true);
            Button(page,"+  WRITE A REFLECTION",95,665,260,31,()=>Journal("REFLECT"),true);Footer();
        }
        private Sprite Paper()
        {
            if(paperSprite!=null)return paperSprite;
            paperTexture=new Texture2D(128,256,TextureFormat.RGBA32,false);paperTexture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[128*256];
            for(int y=0;y<256;y++)for(int x=0;x<128;x++)
            {
                float grain=(Mathf.PerlinNoise(x*.4f,y*.4f)-.5f)*.055f+(Mathf.PerlinNoise(x*.04f,y*.04f)-.5f)*.07f;
                float dx=Mathf.Min(x,127-x),dy=Mathf.Min(y,255-y);float alpha=dx<12 && dy<12 && (dx-12)*(dx-12)+(dy-12)*(dy-12)>144?0:1;
                pixels[y*128+x]=new Color(.9f+grain,.79f+grain,.57f+grain,alpha);
            }
            paperTexture.SetPixels(pixels);paperTexture.Apply();paperSprite=Sprite.Create(paperTexture,new Rect(0,0,128,256),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(14,14,14,14));return paperSprite;
        }
        private DateTime EntryDate(string value){return DateTime.TryParse(value,null,System.Globalization.DateTimeStyles.RoundtripKind,out var date)?date.ToLocalTime():DateTime.Today;}
        private RectTransform Scroll(Transform parent,float x,float y,float w,float h,out RectTransform list)
        {
            var viewport=Fill(parent,"Scroll viewport",x,y,w,h,Color.clear).rectTransform;viewport.gameObject.AddComponent<RectMask2D>();
            list=Rect(viewport,"Live content");list.anchorMin=new Vector2(0,1);list.anchorMax=Vector2.one;list.pivot=new Vector2(.5f,1);list.sizeDelta=new Vector2(0,h);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=list;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=22;return viewport;
        }
        private InputField Input(Transform parent,string placeholder,float x,float y,float w,float h)
        {
            var box=Fill(parent,"Editable reflection",x,y,w,h,new Color(1,1,1,.13f));var input=box.gameObject.AddComponent<InputField>();
            var text=Text(box.transform,"",8,8,w-16,h-16,14,navy);input.textComponent=text;input.lineType=InputField.LineType.MultiLineNewline;input.characterLimit=8000;
            var hint=Text(box.transform,placeholder,8,8,w-16,h-16,13,new Color(.34f,.29f,.2f,.7f));hint.fontStyle=FontStyle.Italic;input.placeholder=hint;return input;
        }
        public void Settings()
        {
            Clear("Settings");Header("SETTINGS","Your day. Your choices. Your balance.");
            var body=Card(page,"Settings panel",24,122,354,628);CloseButton();
            Section(body.transform,"AUDIO",12);Volume(body.transform,"♫  Music",Data.musicVolume,42,v=>{Data.musicVolume=v;ApplyAudio();});Volume(body.transform,"♪  SFX",Data.sfxVolume,79,v=>{Data.sfxVolume=v;ApplyAudio();});
            Toggle(body.transform,"Vibration",Data.vibration,117,()=>{Data.vibration=!Data.vibration;app.Persist();Settings();});
            Section(body.transform,"DISPLAY",159);Toggle(body.transform,"Dark Mode",Data.darkMode,188,()=>{Data.darkMode=!Data.darkMode;app.Persist();Settings();});
            Text(body.transform,"Aa   Text Size",15,229,130,24,12,cream);string[] sizes={"Small","Medium","Large"};for(int i=0;i<3;i++){int value=i;Button(body.transform,sizes[i],157+i*58,226,55,26,()=>{Data.textSize=value;app.Persist();Settings();},Data.textSize==i);}
            Text(body.transform,"◉   Color Theme",15,268,140,25,12,cream);Color[] accents={gold,new Color(.43f,.76f,.72f),new Color(.7f,.59f,.8f)};
            for(int i=0;i<3;i++){int value=i;var b=Button(body.transform,Data.accentIndex==i?"●":"○",187+i*48,264,36,28,()=>{Data.accentIndex=value;app.Persist();Settings();});b.GetComponentInChildren<Text>().color=accents[i];}
            Section(body.transform,"GAMEPLAY",310);
            Text(body.transform,"Daily Reminder",15,342,170,24,12,cream);Button(body.transform,Data.reminderTime,240,339,96,27,Reminder);
            Text(body.transform,"Start Week On",15,379,170,24,12,cream);Button(body.transform,Data.weekStartsMonday?"Monday":"Sunday",240,376,96,27,()=>{Data.weekStartsMonday=!Data.weekStartsMonday;app.Persist();Settings();});
            Toggle(body.transform,"Show Motivational Tips",Data.showTips,416,()=>{Data.showTips=!Data.showTips;app.Persist();Settings();});
            Section(body.transform,"ABOUT",458);
            string[] names={"How to play","Privacy Policy","Terms of Service","Rate Us"};for(int i=0;i<4;i++){string title=names[i];Button(body.transform,title,14,488+i*30,324,27,()=>About(title));}
            Button(page,"↻ RESET PROGRESS",24,765,174,31,()=>Dialog("RESET PROGRESS?","Delete your saved assessments, reflections and activity on this device? This cannot be undone. Export your data first if you need a copy.",ResetProgress,"DELETE PROGRESS"));
            Button(page,"SIGN OUT  →",207,765,171,31,()=>Dialog("RETURN TO TITLE","This project uses local saved data. Returning to the title keeps your progress on this device.",Splash,"RETURN TO TITLE"),true);Footer();
        }
        private void Reminder()
        {
            Clear("Practice time");var panel=Card(page,"Practice time",25,250,352,330);
            Text(panel.transform,"DAILY REMINDER",15,25,322,38,24,gold,TextAnchor.MiddleCenter,true);
            Text(panel.transform,"Set your preferred practice time (24-hour HH:mm). For a notification, add this time in your phone's reminders.",22,80,308,85,14,cream,TextAnchor.MiddleCenter);
            var input=Input(panel.transform,"HH:mm",75,175,202,42);input.lineType=InputField.LineType.SingleLine;input.text=Data.reminderTime;input.characterLimit=5;
            var feedback=Text(panel.transform,"",22,222,308,24,12,cream,TextAnchor.MiddleCenter);
            Button(panel.transform,"SAVE TIME",22,266,308,38,()=>{if(!DateTime.TryParseExact(input.text,"HH:mm",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var time)){feedback.text="Enter a time such as 09:00 or 18:30.";return;}Data.reminderTime=time.ToString("HH:mm");app.Persist();Settings();},true);
        }
        private void Section(Transform parent,string title,float y){Text(parent,title,13,y,326,22,12,gold,TextAnchor.MiddleLeft,true);Fill(parent,"Section rule",13,y+23,326,1,new Color(.25f,.25f,.21f));}
        private void Volume(Transform parent,string name,float value,float y,UnityEngine.Events.UnityAction<float> change)
        {
            Text(parent,name,15,y,132,24,12,cream);
            var hit=Fill(parent,name+" slider",158,y-3,169,28,Color.clear);var track=Fill(hit.transform,"Track",0,10,169,6,new Color(.27f,.28f,.24f));var slider=hit.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;
            var fill=Rect(track.transform,"Gold fill");var graphic=fill.gameObject.AddComponent<Image>();graphic.color=gold;slider.fillRect=fill;
            var handle=Rect(track.transform,"Handle");handle.sizeDelta=new Vector2(11,15);var knob=handle.gameObject.AddComponent<Image>();knob.color=gold;slider.handleRect=handle;slider.targetGraphic=knob;slider.value=value;
            slider.onValueChanged.AddListener(change);slider.onValueChanged.AddListener(v=>app.Persist());
        }
        private void Toggle(Transform parent,string name,bool value,float y,UnityEngine.Events.UnityAction action)
        {Text(parent,name,15,y,240,24,12,cream);Button(parent,value?"ON":"OFF",283,y-2,53,25,action,value);}
        private void ApplyAudio()
        {
            foreach(var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))source.volume=source.loop?Data.musicVolume:Data.sfxVolume;
        }
        private void About(string title)
        {
            string text=title=="How to play"?"Start your day, consider workplace choices, and reflect in your journal. Complete an ALIGN360 assessment and course baseline to track development. Assessment scores use your answers only.":title=="Privacy Policy"?"Assessment answers, reflections and activity are saved on this device. They are not uploaded by this app. You can export or delete local data in the assessment workspace. A client privacy policy has not yet been supplied.":title=="Terms of Service"?"A client terms document has not yet been supplied. This self-assessment is for professional development and is not a clinical or disciplinary instrument.":"No published store listing is connected yet.";
            Dialog(title.ToUpperInvariant(),text,Settings,"BACK");
        }
        private void ResetProgress()
        {
            Data.results.Clear();Data.journal.Clear();Data.activity.Clear();Data.favorites.Clear();Data.completedExercises.Clear();Data.draftAnswers=null;Data.draftPhase=null;Data.draftIndex=0;app.Persist();
            string backup=System.IO.Path.Combine(Application.persistentDataPath,"align360.json.bak");if(System.IO.File.Exists(backup))System.IO.File.Delete(backup);Settings();
        }
        private void Dialog(string title,string message,UnityEngine.Events.UnityAction confirm,string label)
        {
            Clear("Dialog");var panel=Card(page,title,25,240,352,350);Text(panel.transform,title,18,24,316,36,23,gold,TextAnchor.MiddleCenter,true);Text(panel.transform,message,24,78,304,166,14,cream,TextAnchor.MiddleCenter);
            Button(panel.transform,label,22,265,308,38,confirm,true);Button(panel.transform,"CANCEL",22,310,308,27,MainMenu);
        }
    }
}
