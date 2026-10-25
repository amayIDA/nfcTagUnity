using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CyberQuiz.Editor
{
    [InitializeOnLoad] public static class CarnivalDualSetup
    {
        const string Request="Temp/CarnivalDual.request";
        const string Report="Temp/CarnivalDualSetup.txt";
        static Font font;
        static CarnivalDualSetup(){EditorApplication.update+=Check;}
        static void Check()
        {
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(Request);
            try{Apply();File.WriteAllText(Report,"PASS: dual-camera scene saved");}
            catch(Exception e){File.WriteAllText(Report,"FAIL: "+e);Debug.LogException(e);}
        }
        [MenuItem("Cyber Quiz/Set Up Two Displays")]
        public static void Apply()
        {
            var q=UnityEngine.Object.FindAnyObjectByType<CarnivalQuiz>();
            if(q==null)throw new Exception("Open CyberCarnivalQuiz first.");
            if(q.displays!=null)return;
            if(q.gameObject.scene.isDirty)throw new Exception("Save the current scene before running two-display setup.");
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/CyberQuiz/Art/Inter.ttf");
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach(var file in new[]{"Registration place holder","Group 8","image 1"})
            {
                var importer=AssetImporter.GetAtPath("Assets/CyberQuiz/Art/"+file+".png") as TextureImporter;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=4096;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var d=q.gameObject.AddComponent<CarnivalDisplays>();d.quiz=q;q.displays=d;
            var oldCamera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.gameObject.scene==q.gameObject.scene);
            oldCamera.name="Camera 2 — TOP · Display 2";oldCamera.tag="Untagged";d.camera2=oldCamera;
            var lowerCamera=new GameObject("Camera 1 — BOTTOM · Display 1",typeof(Camera)).GetComponent<Camera>();lowerCamera.tag="MainCamera";d.camera1=lowerCamera;
            SetCamera(lowerCamera,0,30);SetCamera(oldCamera,1,31);
            d.display2=q.GetComponent<Canvas>();q.name="Display 2 — TOP · Leaderboard and Quiz";
            ConfigureCanvas(d.display2,oldCamera,1,31);
            d.display1=new GameObject("Display 1 — BOTTOM · Touchscreen",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            ConfigureCanvas(d.display1,lowerCamera,0,30);

            // Keep the authored question objects and NFC mappings intact.
            foreach(var text in q.GetComponentsInChildren<Text>(true))text.font=font;
            var logo=q.transform.Find("Department logo — original Figma asset");
            if(logo!=null)Place((RectTransform)logo,707,32,506,121);
            var mainTitle=Pic("Faint Carnival watermark",q.transform,0,0,1920,1080,"CarnivalTitle");mainTitle.color=new Color(1,1,1,.065f);mainTitle.transform.SetSiblingIndex(2);

            // The small-screen idle artwork remains a separate authored screen.
            d.idle=q.splash;d.idle.transform.SetParent(d.display1.transform,false);
            foreach(var button in d.idle.GetComponentsInChildren<Button>(true))UnityEngine.Object.DestroyImmediate(button.gameObject);
            var idlePanel=d.idle.transform.Find("Idle panel");if(idlePanel!=null)UnityEngine.Object.DestroyImmediate(idlePanel.gameObject);
            foreach(var text in d.idle.GetComponentsInChildren<Text>(true))UnityEngine.Object.DestroyImmediate(text.gameObject);
            var idleLogo=d.idle.transform.Find("Department Logo");if(idleLogo!=null)Place((RectTransform)idleLogo,535,52,850,212);
            var idleHit=Pic("Touch idle message to REGISTER",d.idle.transform,240,825,1440,190,"FigmaQuestionPanel");
            idleHit.raycastTarget=true;var register=idleHit.gameObject.AddComponent<Button>();register.targetGraphic=idleHit;
            UnityEventTools.AddPersistentListener(register.onClick,d.OpenRegistration);
            Label("DATA CLASSIFICATION · LEADERBOARD\nAwaiting the first agent\nTouch REGISTER on the smaller screen to start.",idleHit.transform,53,27,1334,130,31);

            d.registration=Screen("R02 — Registration · Display 1 ONLY",d.display1.transform);
            Backdrop(d.registration.transform,true);
            d.nameField=Field("Name",d.registration.transform,456,360,1008,160,48);
            d.employeeIdField=Field("Email ID",d.registration.transform,456,558,1008,160,64);
            var submit=ArtButton("Submit",d.registration.transform,740,870,440,132,"Group 8",d.SubmitRegistration,38);
            d.validation=Label("",d.registration.transform,456,750,1008,64,27);d.validation.color=new Color(1,.72f,.52f);
            var nav=d.nameField.navigation;nav.mode=Navigation.Mode.Explicit;nav.selectOnDown=d.employeeIdField;d.nameField.navigation=nav;
            nav=d.employeeIdField.navigation;nav.mode=Navigation.Mode.Explicit;nav.selectOnUp=d.nameField;nav.selectOnDown=submit;d.employeeIdField.navigation=nav;
            nav=submit.navigation;nav.mode=Navigation.Mode.Explicit;nav.selectOnUp=d.employeeIdField;submit.navigation=nav;
            d.thankYou=Screen("R03 — Registration Thank You",d.display1.transform);Backdrop(d.thankYou.transform,true);
            Pic("Thank you panel",d.thankYou.transform,310,365,1300,340,"FigmaQuestionPanel");
            d.thankYouText=Label("Thank you!\nYour registration is complete.",d.thankYou.transform,380,420,1160,190,42);

            // Name belongs to registration now, never to the public instruction panel.
            q.playerName.gameObject.SetActive(false);
            var instructionTitle=q.instructions.GetComponentsInChildren<Text>(true).First(t=>t.text=="HOW TO PLAY");instructionTitle.text="Instructions";
            var instructionBody=q.instructions.GetComponentsInChildren<Text>(true).First(t=>t.text.StartsWith("1."));
            Place(instructionBody.rectTransform,310,435,1300,170);
            instructionBody.text="1. Read each question on the screen.\n2. Tap the OPEN, SECRET, CONFIDENTIAL or SENSITIVE cube.\n3. Answer 10 random questions. The four choices move each question.";
            var start=q.instructions.GetComponentInChildren<Button>(true);Place((RectTransform)start.transform,740,670,440,90);
            start.GetComponent<Image>().sprite=Sprite("Group 8");start.GetComponent<Image>().color=Color.white;

            var heading=q.leaderboard.GetComponentsInChildren<Text>(true).First(t=>t.text.StartsWith("LEADERBOARD"));heading.text="Leaderboard";heading.fontSize=42;
            var headers=q.leaderboard.GetComponentsInChildren<Text>(true).First(t=>t.text=="CORRECT / 5");headers.text="Correct";
            foreach(var t in q.leaderboard.GetComponentsInChildren<Text>(true)){if(t.text=="RANK")t.text="RANK";if(t.text=="PLAYER")t.text="PLAYER";}
            d.leaderboardReplay=q.leaderboard.GetComponentInChildren<Button>(true);
            Place((RectTransform)d.leaderboardReplay.transform,735,930,450,56);
            var footer=Rect("Registration prompt",q.leaderboard.transform,725,877,470,50).gameObject.AddComponent<Image>();footer.color=new Color(0,.36f,.55f,.85f);
            Label("Register on the screen below to start",footer.transform,0,0,470,50,23);

            d.gameBackdrop=Rect("Game artwork — Display 1",d.display1.transform,0,0,1920,1080).gameObject;
            Pic("Background",d.gameBackdrop.transform,0,0,1920,1080,"FigmaBackground");
            var wash=Rect("Navy atmosphere",d.gameBackdrop.transform,0,0,1920,1080).gameObject.AddComponent<Image>();wash.color=new Color(0,.015f,.065f,.36f);wash.raycastTarget=false;
            Pic("Department Logo",d.gameBackdrop.transform,592.75f,64.5f,734.5f,176,"FigmaDepartmentLogo");
            var panels=new[]{q.instructions}.Concat(q.questions.Select(v=>v.panel)).Concat(new[]{q.feedback,q.results,q.leaderboard}).ToArray();
            d.mirrors=new CarnivalDisplays.Mirror[panels.Length];
            for(int i=0;i<panels.Length;i++)
            {
                var source=panels[i];var view=UnityEngine.Object.Instantiate(source.gameObject,d.display1.transform,false).GetComponent<CanvasGroup>();view.name="Touchscreen — "+source.name;
                d.mirrors[i]=new CarnivalDisplays.Mirror{source=source,view=view,sourceTexts=source.GetComponentsInChildren<Text>(true),viewTexts=view.GetComponentsInChildren<Text>(true)};
                if(source==q.leaderboard){var replay=view.GetComponentInChildren<Button>(true);replay.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(replay.onClick,q.ReturnHome);}
                view.gameObject.SetActive(false);
            }
            d.mirrorHud=UnityEngine.Object.Instantiate(q.hud,d.display1.transform,false);d.mirrorHud.name="Touchscreen — Player HUD";
            var hudTexts=d.mirrorHud.GetComponentsInChildren<Text>(true);d.mirrorName=hudTexts[0];d.mirrorTime=hudTexts[1];d.mirrorCorrect=hudTexts[2];
            // Mirror the existing reader error with the other live texts.
            var reader=UnityEngine.Object.Instantiate(q.readerStatus.gameObject,d.display1.transform,false).GetComponent<Text>();
            var errorGroup=reader.gameObject.AddComponent<CanvasGroup>();var sourceGroup=q.readerStatus.gameObject.AddComponent<CanvasGroup>();
            d.mirrors=d.mirrors.Concat(new[]{new CarnivalDisplays.Mirror{source=sourceGroup,view=errorGroup,sourceTexts=new[]{q.readerStatus},viewTexts=new[]{reader}}}).ToArray();
            SetLayer(d.display1.gameObject,30);SetLayer(d.display2.gameObject,31);
            foreach(var p in panels)p.gameObject.SetActive(p==q.leaderboard);
            q.leaderboard.alpha=1;q.leaderboard.interactable=q.leaderboard.blocksRaycasts=true;
            d.leaderboardReplay.gameObject.SetActive(false);d.idle.gameObject.SetActive(true);d.idle.alpha=1;
            d.registration.gameObject.SetActive(false);d.thankYou.gameObject.SetActive(false);d.gameBackdrop.SetActive(false);d.mirrorHud.SetActive(false);q.hud.SetActive(false);
            EditorUtility.SetDirty(q);EditorUtility.SetDirty(d);
            EditorSceneManager.MarkSceneDirty(q.gameObject.scene);EditorSceneManager.SaveScene(q.gameObject.scene);
            Debug.Log("Two displays ready: Camera 1 / Display 1 = bottom touchscreen; Camera 2 / Display 2 = top quiz.");
        }
        static void SetCamera(Camera camera,int display,int layer)
        {
            camera.targetDisplay=display;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.005f,.02f,.08f);camera.cullingMask=1<<layer;
            camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.nearClipPlane=.1f;camera.farClipPlane=1000;
        }
        static void ConfigureCanvas(Canvas canvas,Camera camera,int display,int layer)
        {
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.targetDisplay=display;canvas.planeDistance=100;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            SetLayer(canvas.gameObject,layer);
        }
        static void SetLayer(GameObject go,int layer){go.layer=layer;foreach(Transform child in go.transform)SetLayer(child.gameObject,layer);}
        static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CyberQuiz/Art/"+name+".png");
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x+w/2,-y-h/2);r.localScale=Vector3.one;}
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;Place(r,x,y,w,h);return r;}
        static CanvasGroup Screen(string name,Transform parent)=>Rect(name,parent,0,0,1920,1080).gameObject.AddComponent<CanvasGroup>();
        static Image Pic(string name,Transform parent,float x,float y,float w,float h,string asset){var im=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();im.sprite=Sprite(asset);im.raycastTarget=false;return im;}
        static Text Label(string value,Transform parent,float x,float y,float w,float h,int size){var t=Rect(string.IsNullOrEmpty(value)?"Validation":value.Replace('\n',' '),parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.supportRichText=false;t.lineSpacing=1.15f;return t;}
        static void Backdrop(Transform parent,bool watermark)
        {
            Pic("Figma Background",parent,0,0,1920,1080,"FigmaBackground");
            if(watermark){var mark=Pic("Faint Carnival wordmark",parent,0,0,1920,1080,"CarnivalTitle");mark.color=new Color(1,1,1,.09f);}
            var wash=Rect("Navy atmosphere",parent,0,0,1920,1080).gameObject.AddComponent<Image>();wash.color=new Color(0,.015f,.065f,.26f);wash.raycastTarget=false;
            Pic("Department Logo — original registration asset",parent,634,113,652,157,"image 1");
        }
        static InputField Field(string name,Transform parent,float x,float y,float w,float h,int limit)
        {
            var im=Pic(name+" — editable input",parent,x,y,w,h,"Registration place holder");im.raycastTarget=true;
            var f=im.gameObject.AddComponent<InputField>();f.targetGraphic=im;f.characterLimit=limit;f.lineType=InputField.LineType.SingleLine;
            f.textComponent=Label("",im.transform,34,20,w-68,h-40,40);f.textComponent.alignment=TextAnchor.MiddleLeft;
            var placeholder=Label(name,im.transform,34,20,w-68,h-40,40);placeholder.alignment=TextAnchor.MiddleLeft;f.placeholder=placeholder;
            f.caretColor=Color.white;f.customCaretColor=true;f.selectionColor=new Color(0,.7f,1,.35f);
            return f;
        }
        static Button ArtButton(string value,Transform parent,float x,float y,float w,float h,string asset,UnityEngine.Events.UnityAction action,int size)
        {
            var im=Pic(value,parent,x,y,w,h,asset);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;
            var colors=b.colors;colors.highlightedColor=new Color(.8f,1,1);colors.pressedColor=new Color(.5f,.8f,1);b.colors=colors;
            UnityEventTools.AddPersistentListener(b.onClick,action);Label(value,im.transform,10,10,w-20,h-20,size);return b;
        }
    }
}
