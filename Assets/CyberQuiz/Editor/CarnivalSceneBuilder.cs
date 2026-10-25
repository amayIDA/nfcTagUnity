using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
namespace CyberQuiz.Editor
{
    [InitializeOnLoad]public static class CarnivalSceneBuilder
    {
        const string Target="Assets/CyberQuiz/CyberCarnivalQuiz.unity";
        static Font font;
        static string Art="Assets/CyberQuiz/Art/";
        static CarnivalSceneBuilder(){EditorApplication.delayCall+=AutoBuild;}
        static void AutoBuild(){if(!File.Exists(Target)&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling)Build();}
        static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        static void ConfigureArt()
        {
            foreach(var file in Directory.GetFiles(Art,"*.png"))
            {
                var importer=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(importer==null)continue;
                if(importer.textureType!=TextureImporterType.Sprite||importer.maxTextureSize!=4096){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();}
            }
        }
        [MenuItem("Cyber Quiz/Open Editable Carnival Scene")]
        public static void Open(){if(!File.Exists(Target))Build();else if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Target);}
        [MenuItem("Cyber Quiz/Build Editable Carnival Scene")]
        public static void Build()
        {
            if(File.Exists(Target)){Debug.Log("Carnival scene already exists; open it to edit Canvas objects.");return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            ConfigureArt();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var camera=new GameObject("UI Camera",typeof(Camera));camera.tag="MainCamera";camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.GetComponent<Camera>().backgroundColor=new Color(.005f,.02f,.08f);camera.GetComponent<Camera>().cullingMask=0;
            var canvas=new GameObject("Cyber Carnival Canvas — EDIT ALL UI HERE",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var quiz=canvas.AddComponent<CarnivalQuiz>();
            Pic("Figma Background",canvas.transform,0,0,1920,1080,"FigmaBackground");
            Pic("Department logo — original Figma asset",canvas.transform,592.75f,64.5f,734.5f,176,"FigmaDepartmentLogo");
            quiz.splash=Screen("01 — Splash",canvas.transform);
            Pic("Tunnel",quiz.splash.transform,0,0,1920,1080,"Tunnel");
            Pic("Cyber Carnival artwork",quiz.splash.transform,0,0,1920,1080,"CarnivalTitle");
            Pic("Department Logo",quiz.splash.transform,535,52,850,212,"FigmaDepartmentLogo");
            Panel("Idle panel",quiz.splash.transform,240,825,1440,190);
            Label("DATA CLASSIFICATION · LEADERBOARD",quiz.splash.transform,350,847,1220,36,28);
            MakeButton("START",quiz.splash.transform,600,910,330,62,quiz.OpenInstructions);
            MakeButton("LEADERBOARD",quiz.splash.transform,970,910,350,62,quiz.OpenLeaderboard);
            quiz.instructions=Screen("02 — Instructions and Player Name",canvas.transform);
            Panel("Instructions panel",quiz.instructions.transform,160,325,1600,450);
            Label("HOW TO PLAY",quiz.instructions.transform,240,365,1440,42,36);
            Label("1. Read each of the 10 questions.\n2. Tap the OPEN, SECRET, CONFIDENTIAL or SENSITIVE cube.\n3. Most correct answers wins. Fastest answer time breaks ties.",quiz.instructions.transform,350,430,1220,150,30);
            var input=Rect("Player Name field",quiz.instructions.transform,650,600,620,64);var image=input.AddComponent<Image>();image.color=new Color(.015f,.08f,.2f,.9f);
            var field=input.AddComponent<InputField>();field.targetGraphic=image;field.characterLimit=24;
            field.textComponent=Label("",input.transform,20,0,580,64,28);var placeholder=Label("Player Name",input.transform,20,0,580,64,28);placeholder.color=new Color(.7f,.83f,1,.7f);field.placeholder=placeholder;quiz.playerName=field;
            MakeButton("START",quiz.instructions.transform,790,687,340,60,quiz.StartQuiz);
            quiz.hud=Rect("Player HUD",canvas.transform,600,850,720,190);
            Pic("Figma HUD artwork",quiz.hud.transform,0,0,720,190,"FigmaHud");
            quiz.hudName=Label("Player Name",quiz.hud.transform,160,26,400,38,25);
            quiz.hudTime=Label("Time : 0.0 sec",quiz.hud.transform,65,94,280,40,24);
            quiz.hudCorrect=Label("Correct Answers : 0",quiz.hud.transform,350,94,310,40,24);
            string[] titles={"An unexpected email asks you to sign in urgently using a link. What should you do?","Which is the strongest password practice?","What does multi-factor authentication (MFA) add?","You find an unknown USB drive in the office. What is the safest action?","A computer displays a ransomware demand. What should you do first?"};
            string[][] answers={new[]{"Open the link immediately","Reply with your password","Verify through an official channel","Forward it to all colleagues","Download the attachment","Ignore all security alerts"},new[]{"Reuse one complex password","Use your birthday","Share passwords with your team","Use unique long passwords and a manager","Write passwords on your monitor","Use password123 with a symbol"},new[]{"Another verification factor","A guarantee against every attack","Automatic password sharing","A replacement for updates","Public access to your account","A faster internet connection"},new[]{"Plug it into your work PC","Open files to find the owner","Give it to a friend to try","Disable antivirus first","Hand it to IT or security","Use a shared computer"},new[]{"Pay immediately","Keep using the computer","Email the infected files","Disable all backups","Try tools from pop-ups","Disconnect and notify IT"}};
            string[] explanations={"Verify suspicious requests through a known official channel. Report suspected phishing.","Use a long, unique password for every account. A password manager helps keep them secure.","MFA adds another verification factor, such as possession of a security key.","Unknown USB devices can carry malware. Hand the device to your IT or security team.","Disconnect the affected device from networks and notify your IT or incident response team."};
            string[] tags={"2435786112","2435817104","2435882544","2435920016","2435985280","2436017536"};int[] correct={2,3,0,4,5};quiz.questions=new CarnivalQuiz.QuestionView[5];
            for(int q=0;q<5;q++)
            {
                var screen=Screen("0"+(q+3)+" — Question "+(q+1)+" — EDIT TEXT AND OPTIONS",canvas.transform);
                Panel("Figma Question Panel",screen.transform,150,315,1620,430);
                Label("QUESTION "+(q+1)+" OF 5",screen.transform,250,354,1420,42,32);
                var title=Label(titles[q],screen.transform,260,405,1400,78,30);
                var view=new CarnivalQuiz.QuestionView{panel=screen,questionText=title,correctOption=correct[q],explanation=explanations[q],options=new CarnivalOption[6]};
                for(int i=0;i<6;i++)
                {
                    float x=240+(i%3)*485,y=510+(i/3)*94;
                    var button=MakeButton(((char)('A'+i))+"   "+answers[q][i],screen.transform,x,y,465,78,null);
                    button.gameObject.name="Option "+(char)('A'+i)+" — NFC "+tags[i]+(i==correct[q]?" — CORRECT":"");
                    var option=button.gameObject.AddComponent<CarnivalOption>();option.button=button;option.surface=button.GetComponent<Image>();option.optionIndex=i;option.nfcValue=tags[i];view.options[i]=option;
                }
                quiz.questions[q]=view;
            }
            quiz.feedback=Screen("08 — Answer Feedback",canvas.transform);
            var tint=Rect("Correct or incorrect colour wash",quiz.feedback.transform,0,0,1920,1080).AddComponent<Image>();tint.color=Color.clear;tint.raycastTarget=false;quiz.feedbackTint=tint;
            Panel("Figma Feedback Panel",quiz.feedback.transform,150,315,1620,387);
            quiz.feedbackTitle=Label("CORRECT!",quiz.feedback.transform,260,400,1400,60,44);
            quiz.feedbackBody=Label("Answer explanation placeholder",quiz.feedback.transform,300,478,1320,135,31);
            quiz.results=Screen("09 — Result",canvas.transform);Panel("Figma Result Panel",quiz.results.transform,150,315,1620,420);
            quiz.resultBody=Label("Thank you!\nYou got 0 out of 5 correct.\nTime : 0.0 sec",quiz.results.transform,260,370,1400,220,42);
            MakeButton("LEADERBOARD",quiz.results.transform,700,630,520,62,quiz.OpenLeaderboard);
            quiz.leaderboard=Screen("10 — Leaderboard",canvas.transform);Panel("Leaderboard neon panel",quiz.leaderboard.transform,285,205,1350,800);
            Label("LEADERBOARD · DATA CLASSIFICATION",quiz.leaderboard.transform,360,300,1200,60,38);
            float[] cols={365,560,1030,1320};float[] widths={170,460,260,200};string[] headers={"RANK","PLAYER","CORRECT / 5","TIME"};
            for(int c=0;c<4;c++)Label(headers[c],quiz.leaderboard.transform,cols[c],430,widths[c],50,28);
            quiz.rows=new CarnivalQuiz.LeaderboardRow[5];
            for(int i=0;i<5;i++)
            {
                var texts=new Text[4];for(int c=0;c<4;c++)texts[c]=Label(c==1&&i==0?"Awaiting first agent":"—",quiz.leaderboard.transform,cols[c],515+i*64,widths[c],48,29);
                quiz.rows[i]=new CarnivalQuiz.LeaderboardRow{rank=texts[0],player=texts[1],correct=texts[2],time=texts[3]};
            }
            MakeButton("PLAY AGAIN",quiz.leaderboard.transform,735,884,450,62,quiz.ReturnHome);
            quiz.readerStatus=Label("",canvas.transform,320,777,1280,45,22);quiz.readerStatus.name="Reader status — only visible for unknown tag";
            var confetti=Rect("Correct Answer Effects — prebuilt pieces",canvas.transform,0,0,1920,1080);quiz.celebrationPieces=new RectTransform[36];
            for(int i=0;i<36;i++)
            {
                var piece=Rect("Spark "+i,confetti.transform,960,540,8+i%3*3,5+i%4*3);var pi=piece.AddComponent<Image>();pi.color=i%3==0?new Color(1,.79f,.3f):i%3==1?new Color(.2f,1,.85f):new Color(.4f,.7f,1);pi.raycastTarget=false;
                var rect=piece.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;quiz.celebrationPieces[i]=rect;piece.SetActive(false);
            }
            foreach(var screen in quiz.questions)screen.panel.gameObject.SetActive(false);
            foreach(var screen in new[]{quiz.instructions,quiz.feedback,quiz.results,quiz.leaderboard})screen.gameObject.SetActive(false);quiz.hud.SetActive(false);
            EditorSceneManager.SaveScene(scene,Target);
            var build=EditorBuildSettings.scenes.Where(s=>s.path!=Target).ToList();build.Insert(0,new EditorBuildSettingsScene(Target,true));EditorBuildSettings.scenes=build.ToArray();
            bool safe=true;for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i)!=scene&&SceneManager.GetSceneAt(i).isDirty)safe=false;
            if(safe){for(int i=SceneManager.sceneCount-1;i>=0;i--){var other=SceneManager.GetSceneAt(i);if(other!=scene)EditorSceneManager.CloseScene(other,true);}}
            else{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(old);}
            File.WriteAllText("Assets/CyberQuiz/CarnivalSetupStatus.txt","Editable Carnival scene baked and saved. All Canvas objects, five questions, thirty NFC option buttons, effects and leaderboard rows exist in the scene. No runtime UI spawning.");AssetDatabase.Refresh();Debug.Log("Carnival Canvas scene baked successfully.");
        }
        static GameObject Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x+w/2,-y-h/2);return go;
        }
        static CanvasGroup Screen(string name,Transform parent){var go=Rect(name,parent,0,0,1920,1080);return go.AddComponent<CanvasGroup>();}
        static Image Pic(string name,Transform parent,float x,float y,float w,float h,string sprite){var go=Rect(name,parent,x,y,w,h);var im=go.AddComponent<Image>();im.sprite=Sprite(sprite);im.raycastTarget=false;return im;}
        static void Panel(string name,Transform parent,float x,float y,float w,float h){Pic(name,parent,x,y,w,h,"FigmaQuestionPanel");}
        static Text Label(string text,Transform parent,float x,float y,float w,float h,int size)
        {
            var go=Rect(string.IsNullOrEmpty(text)?"Text placeholder":text.Replace('\n',' '),parent,x,y,w,h);var t=go.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.supportRichText=false;return t;
        }
        static Button MakeButton(string text,Transform parent,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {
            var go=Rect(text,parent,x,y,w,h);var im=go.AddComponent<Image>();im.color=new Color(.025f,.13f,.32f,.82f);var outline=go.AddComponent<Outline>();outline.effectColor=new Color(.1f,.8f,1,.6f);outline.effectDistance=new Vector2(1.2f,-1.2f);
            var button=go.AddComponent<Button>();button.targetGraphic=im;var colors=button.colors;colors.highlightedColor=new Color(.4f,.85f,1);colors.pressedColor=new Color(.25f,.65f,.9f);button.colors=colors;
            if(action!=null)UnityEventTools.AddPersistentListener(button.onClick,action);
            var label=Label(text,go.transform,16,6,w-32,h-12,25);label.resizeTextForBestFit=true;label.resizeTextMinSize=19;label.resizeTextMaxSize=25;return button;
        }
    }
}
