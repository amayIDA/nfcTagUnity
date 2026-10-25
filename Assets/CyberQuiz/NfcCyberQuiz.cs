using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace CyberQuiz
{
    public class NfcCyberQuiz : MonoBehaviour
    {
        public int pointsPerCorrectAnswer = 20;
        [Tooltip("Allow readers which do not send Enter to finish after this idle interval.")]
        public float scanIdleSeconds = 0.35f;
        ClassificationBank.Item[] questions;
        string[] order;
        Font font;
        Transform root;
        Text status;
        string buffer = "";
        float lastCharacter;
        int current, score;
        bool answering, running;
        Keyboard subscribedKeyboard;
        readonly Color background = new Color32(10, 18, 34, 255);
        readonly Color card = new Color32(23, 38, 59, 255);
        readonly Color accent = new Color32(33, 211, 177, 255);

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Quiz Camera", typeof(Camera));
                cameraObject.transform.SetParent(transform, false);
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = background;
                camera.cullingMask = 0;
            }
            var canvas = new GameObject("Quiz Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 900);
            scaler.matchWidthOrHeight = 0.5f;
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("Quiz Event System", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(transform);
            root = canvas.transform;
            Splash();
        }
        void OnEnable() { HookKeyboard(); }
        void OnDisable() { if (subscribedKeyboard != null) subscribedKeyboard.onTextInput -= OnCharacter; subscribedKeyboard=null; }
        void HookKeyboard()
        {
            if (subscribedKeyboard == Keyboard.current) return;
            if (subscribedKeyboard != null) subscribedKeyboard.onTextInput -= OnCharacter;
            subscribedKeyboard=Keyboard.current;
            if (subscribedKeyboard != null) subscribedKeyboard.onTextInput += OnCharacter;
        }
        void Update()
        {
            HookKeyboard();
            if (buffer.Length > 0 && Time.unscaledTime-lastCharacter > scanIdleSeconds) FlushScan();
        }
        void OnCharacter(char c)
        {
            if (!running || !answering) { buffer=""; return; }
            if (c=='\r' || c=='\n') { FlushScan(); return; }
            if (c>='0' && c<='9')
            {
                buffer+=c; lastCharacter=Time.unscaledTime;
                if (buffer.Length>64) { buffer=""; status.text="Scan too long. Please tap again."; }
            }
        }
        void FlushScan() { string value=buffer; buffer=""; if(value.Length>0) SubmitTag(value); }
        // Reader integrations may call this on Unity's main thread with a complete tag value.
        public void SubmitTag(string value)
        {
            if (!running || !answering) return;
            string label=ClassificationBank.LabelForTag(value);
            int option=label==null?-1:Array.IndexOf(order, label);
            if(option<0) { status.text="Unknown tag. Tap OPEN, SECRET, CONFIDENTIAL or SENSITIVE."; return; }
            Choose(option);
        }
        void Clear()
        {
            buffer="";
            foreach(Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            Box("Background", root, 0,0,1,1,background);
        }
        void Splash()
        {
            running=false; answering=false; Clear();
            Label("NFC  /  SECURITY TRAINING",root,.1f,.8f,.9f,.87f,24,accent);
            Label("CYBER SHIELD",root,.1f,.61f,.9f,.78f,64,Color.white);
            Label("Ten questions. Four cubes. Think before you tap.",root,.1f,.51f,.9f,.61f,28,Color.white);
            Label("Tap the OPEN, SECRET, CONFIDENTIAL or SENSITIVE cube. The four choices move each question.\nCorrect answers earn "+pointsPerCorrectAnswer+" points. You can also click an option to test.",root,.1f,.33f,.9f,.48f,23,new Color32(169,188,210,255));
            Button("START QUIZ",root,.32f,.17f,.68f,.28f,accent,()=> { current=0; score=0; running=true; questions=ClassificationBank.DrawRound(); ShowQuestion(); });
        }
        void ShowQuestion()
        {
            Clear(); answering=true;
            var q=questions[current];
            Label("CYBER SHIELD     /     QUESTION "+(current+1)+" OF "+questions.Length,root,.06f,.9f,.78f,.96f,22,accent);
            Label("SCORE  "+score,root,.78f,.9f,.95f,.96f,22,Color.white);
            Label(q.prompt,root,.06f,.72f,.94f,.88f,28,Color.white);
            order=ClassificationBank.ShuffleClassifications();
            for(int i=0;i<ClassificationBank.Labels.Length;i++)
            {
                int option=i; int col=i%2, row=i/2;
                float x=.06f+col*.45f, top=.68f-row*.22f;
                Button(order[i],root,x,top-.18f,x+.43f,top,card,()=>Choose(option));
            }
            status=Label("Ready — tap an NFC tag or click an option.",root,.06f,.115f,.94f,.19f,21,accent);
        }
        void Choose(int option)
        {
            if(!answering) return;
            answering=false; buffer="";
            foreach(var button in root.GetComponentsInChildren<Button>()) button.interactable=false;
            var q=questions[current]; bool correct=order[option]==q.answer;
            if(correct) score+=pointsPerCorrectAnswer;
            status.text=(correct ? "CORRECT  +"+pointsPerCorrectAnswer : "INCORRECT  +0")+"   |   You chose "+order[option]+". Answer: "+q.answer+"\n"+q.explanation;
            status.fontSize=19; status.color=correct ? accent : new Color32(255,191,116,255);
            Button(current==questions.Length-1 ? "SEE RESULTS" : "NEXT QUESTION",root,.64f,.025f,.94f,.095f,accent,()=> { current++; if(current<questions.Length) ShowQuestion(); else Results(); });
        }
        void Results()
        {
            running=false; answering=false; Clear();
            Label("TRAINING COMPLETE",root,.1f,.77f,.9f,.87f,28,accent);
            Label(score+" / "+(questions.Length*pointsPerCorrectAnswer),root,.1f,.55f,.9f,.74f,72,Color.white);
            Label("You answered "+(pointsPerCorrectAnswer>0 ? score/pointsPerCorrectAnswer : 0)+" of "+questions.Length+" questions correctly.\nKeep verifying requests, protecting accounts, and reporting threats.",root,.1f,.34f,.9f,.52f,26,Color.white);
            Button("PLAY AGAIN",root,.32f,.16f,.68f,.27f,accent,Splash);
        }
        GameObject Box(string name,Transform parent,float x0,float y0,float x1,float y1,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x0,y0,x1,y1); go.GetComponent<Image>().color=color; return go;
        }
        void Place(RectTransform r,float x0,float y0,float x1,float y1) { r.anchorMin=new Vector2(x0,y0); r.anchorMax=new Vector2(x1,y1); r.offsetMin=Vector2.zero; r.offsetMax=Vector2.zero; }
        Text Label(string value,Transform parent,float x0,float y0,float x1,float y1,int size,Color color)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false); Place(go.GetComponent<RectTransform>(),x0,y0,x1,y1);
            var text=go.GetComponent<Text>(); text.font=font; text.text=value; text.fontSize=size; text.color=color; text.alignment=TextAnchor.MiddleLeft; text.raycastTarget=false; return text;
        }
        void Button(string value,Transform parent,float x0,float y0,float x1,float y1,Color color,UnityEngine.Events.UnityAction action)
        {
            var go=Box(value,parent,x0,y0,x1,y1,color); var button=go.AddComponent<Button>(); button.targetGraphic=go.GetComponent<Image>(); button.onClick.AddListener(action);
            var text=Label(value,go.transform,.045f,.07f,.955f,.93f,23,color==accent ? background : Color.white); text.resizeTextForBestFit=true; text.resizeTextMinSize=16; text.resizeTextMaxSize=23;
        }
    }
}


