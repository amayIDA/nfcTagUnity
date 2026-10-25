using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace CyberQuiz
{
    public class CarnivalQuiz : MonoBehaviour
    {
        [Serializable] public class QuestionView
        {
            public CanvasGroup panel;
            public Text questionText;
            public CarnivalOption[] options;
            [Range(0,3)] public int correctOption;
            [TextArea(2,5)] public string explanation;
        }
        [Serializable] public class LeaderboardRow { public Text rank, player, correct, time; }
        [Serializable] public class Entry { public string player; public string email; public int correct; public double seconds; public long completed; }
        [Serializable] public class Scores { public List<Entry> entries = new List<Entry>(); }
        public CanvasGroup splash, instructions, feedback, results, leaderboard;
        public QuestionView[] questions;
        public InputField playerName;
        public Text hudName, hudTime, hudCorrect, feedbackTitle, feedbackBody, resultBody, readerStatus;
        public Image feedbackTint;
        public RectTransform[] celebrationPieces;
        public LeaderboardRow[] rows;
        public GameObject hud;
        [Tooltip("Idle seconds ending a keyboard-reader scan without Enter.")] public float scanIdleSeconds=.35f;
        [Tooltip("Minimum visible feedback duration. Timer pauses during feedback.")] public float feedbackSeconds=2.8f;
        [Min(1),Tooltip("Seconds the completed-game leaderboard stays visible before both screens return to idle.")] public float leaderboardSeconds=10f;
        [Min(1),Tooltip("After the quiz starts, reload the scene when nobody taps, scans, or clicks for this many seconds.")] public float inactivitySeconds=120f;
        public bool reducedMotion;
        public bool RecordScores=true;
        public CarnivalDisplays displays;
        public CarnivalMotion motion;
        public static int CompareEntries(Entry a, Entry b)
        {
            int c=b.correct.CompareTo(a.correct); if(c!=0)return c;
            c=a.seconds.CompareTo(b.seconds);return c!=0?c:a.completed.CompareTo(b.completed);
        }
        Scores scores=new Scores();
        ClassificationBank.Item[] round;
        CanvasGroup visible;
        Keyboard keyboard;
        string scan="", participant;
        float lastChar;
        double elapsed;
        int current,correct;
        bool accepting, busy, saved, quizRunning, restarting;
        float autoIdleAt=-1, lastActivity;
        public float LeaderboardSecondsRemaining=>autoIdleAt<0?0:Mathf.Max(0,autoIdleAt-Time.unscaledTime);
        public int CorrectCount=>correct;
        public int RoundCount=>round==null?0:round.Length;
        public QuestionView ActiveQuestion=>questions[current%questions.Length];
        public double AnswerSeconds=>elapsed;
        [NonSerialized] public string testScorePath;
        [NonSerialized] public string testCsvPath;
        public string ScorePath=>string.IsNullOrEmpty(testScorePath)?Path.Combine(Application.persistentDataPath,"cyber-carnival-leaderboard.json"):testScorePath;
        public string CsvPath=>string.IsNullOrEmpty(testCsvPath)?ClassificationBank.ResultsCsvPath:testCsvPath;
        void Awake()
        {
            LoadScores();
            foreach(var q in questions) { Hide(q.panel); foreach(var o in q.options) { o.quiz=this; o.button.onClick.AddListener(o.Select); } }
            foreach(var p in new[]{splash,instructions,feedback,results,leaderboard}) Hide(p);
            foreach(var p in celebrationPieces)p.gameObject.SetActive(false);
            hud.SetActive(false); ShowImmediate(displays != null ? leaderboard : splash); FillLeaderboard();
        }
        void OnEnable(){HookKeyboard();}
        void OnDisable(){if(keyboard!=null)keyboard.onTextInput-=OnCharacter;keyboard=null;}
        void HookKeyboard(){if(keyboard==Keyboard.current)return;if(keyboard!=null)keyboard.onTextInput-=OnCharacter;keyboard=Keyboard.current;if(keyboard!=null)keyboard.onTextInput+=OnCharacter;}
        void Update()
        {
            HookKeyboard();
            if(accepting) { elapsed+=Time.unscaledDeltaTime;hudTime.text="Time : "+elapsed.ToString("0.0")+" sec"; }
            if(scan.Length>0 && Time.unscaledTime-lastChar>scanIdleSeconds) Flush();
            if(autoIdleAt>=0&&Time.unscaledTime>=autoIdleAt&&!busy){autoIdleAt=-1;ReturnHome();}
            if(quizRunning&&!restarting)
            {
                if(AnyPlayerInput())lastActivity=Time.unscaledTime;
                if(Time.unscaledTime-lastActivity>=inactivitySeconds){restarting=true;quizRunning=false;SceneManager.LoadScene(gameObject.scene.name);}
            }
        }
        static bool AnyPlayerInput()
        {
            var keys=Keyboard.current; if(keys!=null&&keys.anyKey.wasPressedThisFrame)return true;
            var mouse=Mouse.current; if(mouse!=null&&(mouse.leftButton.wasPressedThisFrame||mouse.rightButton.wasPressedThisFrame||mouse.middleButton.wasPressedThisFrame))return true;
            var touch=Touchscreen.current; return touch!=null&&touch.primaryTouch.press.wasPressedThisFrame;
        }
        void OnCharacter(char c)
        {
            if(!accepting){scan="";return;}
            if(c=='\r'||c=='\n'){Flush();return;}
            if(c>='0'&&c<='9'){scan+=c;lastChar=Time.unscaledTime;if(scan.Length>64)scan="";}
        }
        void Flush(){var value=scan;scan="";if(value.Length>0)SubmitTag(value);}
        public void SubmitTag(string value)
        {
            if(!accepting||busy)return;
            var options=ActiveQuestion.options;
            for(int i=0;i<options.Length;i++)if(options[i].nfcValue==(value??"").Trim()){CarnivalAudio.Nfc();SelectOption(i);return;}
            CarnivalAudio.Invalid();
            readerStatus.text="Tag not recognised. Tap the OPEN, SECRET, CONFIDENTIAL or SENSITIVE cube.";
        }
        public void OpenInstructions(){if(!busy){autoIdleAt=-1;accepting=false;hud.SetActive(false);StartCoroutine(Switch(instructions));}}
        public void StartQuiz()
        {
            if(busy)return;
            if(displays!=null && !displays.Registered)return;
            autoIdleAt=-1;
            if(motion!=null)motion.Clear();
            participant=playerName.text.Trim();if(participant.Length==0)participant="Agent "+(scores.entries.Count+1).ToString("000");
            quizRunning=true;lastActivity=Time.unscaledTime;
            current=correct=0;elapsed=0;saved=false;hud.SetActive(true);hudName.text=participant;
            hudTime.text="Time : 0.0 sec";hudCorrect.text="Correct Answers : 0";
            round=ClassificationBank.DrawRound();
            PrepareQuestion(); StartCoroutine(EnterQuestion());
        }
        void PrepareQuestion()
        {
            scan="";readerStatus.text="";
            var view=ActiveQuestion; var item=round[current];
            view.questionText.text=item.prompt;
            view.explanation=item.explanation;
            var order=ClassificationBank.ShuffleClassifications();
            view.correctOption=0;
            foreach(var t in view.panel.GetComponentsInChildren<Text>(true))
                if(t.text.StartsWith("QUESTION")) t.text="QUESTION "+(current+1)+" OF "+round.Length;
            for(int i=0;i<view.options.Length;i++)
            {
                var o=view.options[i];
                o.optionIndex=i;
                o.nfcValue=ClassificationBank.TagFor(order[i]);
                if(o.label!=null)o.label.text=order[i];
                if(order[i]==item.answer)view.correctOption=i;
                o.ResetVisual();
            }
        }
        IEnumerator EnterQuestion(){yield return Switch(ActiveQuestion.panel); accepting=true;}
        public void SelectOption(int option)
        {
            var view=ActiveQuestion;
            if(!accepting||busy||option<0||option>=view.options.Length)return;
            accepting=false;scan="";
            bool right=option==view.correctOption;
            if(right)correct++;
            hudCorrect.text="Correct Answers : "+correct;
            view.options[option].Mark(right);
            if(motion!=null)motion.LockAnswer((RectTransform)view.options[option].transform,right);
            StartCoroutine(Answer(right));
        }
        IEnumerator Answer(bool right)
        {
            busy=true;
            if(!reducedMotion)yield return new WaitForSecondsRealtime(.28f);
            feedbackTitle.text=right?"CORRECT!":"INCORRECT";
            feedbackBody.text=ActiveQuestion.explanation;
            feedbackTint.color=right?new Color(0,.9f,.43f,.27f):new Color(1,0,0,.62f);
            if(motion!=null)motion.Feedback(right);
            if(right)CarnivalAudio.Correct();else CarnivalAudio.Wrong();
            yield return Switch(feedback);
            if(motion==null&&right&&!reducedMotion)StartCoroutine(Celebrate());
            busy=true;yield return new WaitForSecondsRealtime(feedbackSeconds);
            current++;
            if(current<round.Length){PrepareQuestion();yield return Switch(ActiveQuestion.panel);accepting=true;}
            else
            {
                SaveResult();CarnivalAudio.Complete();resultBody.text="Thank you!\nYou got "+correct+" out of "+round.Length+" correct.\nTime : "+elapsed.ToString("0.0")+" sec";
                hud.SetActive(false);if(motion!=null)motion.Clear();FillLeaderboard();
                yield return Switch(leaderboard);
                autoIdleAt=Time.unscaledTime+leaderboardSeconds;
            }
            busy=false;
        }
        public void OpenLeaderboard(){if(busy)return;autoIdleAt=-1;accepting=false;hud.SetActive(false);FillLeaderboard();StartCoroutine(EnterLeaderboard());}
        IEnumerator EnterLeaderboard(){yield return Switch(leaderboard);if(displays==null||displays.Registered)autoIdleAt=Time.unscaledTime+leaderboardSeconds;}
        public void ReturnHome(){if(busy)return;quizRunning=false;autoIdleAt=-1;accepting=false;scan="";hud.SetActive(false);if(motion!=null)motion.Clear();if(displays!=null){displays.ResetRegistration();FillLeaderboard();}var home=displays!=null?leaderboard:splash;if(visible==home)ShowImmediate(home);else StartCoroutine(Switch(home));}
        public bool IsBusy=>busy;
        public CanvasGroup VisiblePanel=>visible;
        static void Hide(CanvasGroup group){group.alpha=0;group.gameObject.SetActive(false);group.interactable=false;group.blocksRaycasts=false;}
        void ShowImmediate(CanvasGroup group){visible=group;group.gameObject.SetActive(true);group.alpha=1;group.interactable=true;group.blocksRaycasts=true;}
        IEnumerator Switch(CanvasGroup next)
        {
            busy=true;scan="";
            CanvasGroup old=visible;if(old!=null){old.interactable=false;old.blocksRaycasts=false;}
            next.gameObject.SetActive(true);next.alpha=0;next.interactable=false;next.blocksRaycasts=false;
            CarnivalAudio.Whoosh();
            if(reducedMotion){if(old!=null&&old!=next)Hide(old);ShowImmediate(next);next.transform.localScale=Vector3.one;busy=false;yield break;}
            var nextRect=(RectTransform)next.transform;var nextHome=nextRect.anchoredPosition;
            var oldRect=old!=null?(RectTransform)old.transform:null;var oldHome=oldRect!=null?oldRect.anchoredPosition:Vector2.zero;
            float duration=reducedMotion?.01f:.52f;
            for(float t=0;t<duration;t+=Time.unscaledDeltaTime)
            {
                float u=Mathf.Clamp01(t/duration),e=1-Mathf.Pow(1-u,3);
                float spring=1+2.15f*Mathf.Pow(u-1,3)+1.15f*Mathf.Pow(u-1,2);
                next.alpha=Mathf.Clamp01(u*3);next.transform.localScale=Vector3.one*Mathf.LerpUnclamped(.95f,1,spring);
                nextRect.anchoredPosition=nextHome+new Vector2(60*(1-e),0);
                if(old!=null&&old!=next){old.alpha=1-Mathf.Clamp01(u*2);oldRect.anchoredPosition=oldHome+new Vector2(-35*e,0);}
                yield return null;
            }
            if(old!=null&&old!=next){oldRect.anchoredPosition=oldHome;Hide(old);}
            ShowImmediate(next);next.transform.localScale=Vector3.one;nextRect.anchoredPosition=nextHome;busy=false;
        }
        IEnumerator Celebrate()
        {
            for(int i=0;i<celebrationPieces.Length;i++) celebrationPieces[i].gameObject.SetActive(true);
            for(float t=0;t<1.3f;t+=Time.unscaledDeltaTime)
            {
                for(int i=0;i<celebrationPieces.Length;i++)
                {
                    var p=celebrationPieces[i];float angle=i*2.39996f;float speed=220+(i%7)*54;
                    p.anchoredPosition=new Vector2(Mathf.Cos(angle)*speed*t,Mathf.Sin(angle)*speed*t-170*t*t);
                    p.localRotation=Quaternion.Euler(0,0,i*31+t*(i%2==0?240:-240));p.localScale=Vector3.one*Mathf.Clamp01((1.3f-t)*3);
                }
                yield return null;
            }
            foreach(var p in celebrationPieces)p.gameObject.SetActive(false);
        }
        void LoadScores()
        {
            try{if(File.Exists(ScorePath))scores=JsonUtility.FromJson<Scores>(File.ReadAllText(ScorePath))??new Scores();}
            catch(Exception e){Debug.LogWarning("Leaderboard could not be read: "+e.Message);scores=new Scores();}
            if(scores.entries==null)scores.entries=new List<Entry>();scores.entries.RemoveAll(e=>e==null||e.seconds<0||double.IsNaN(e.seconds)||double.IsInfinity(e.seconds));scores.entries.Sort(CompareEntries);
        }
        void SaveResult()
        {
            if(saved||!RecordScores)return;saved=true;
            var email=displays!=null?displays.EmailId:"";
            scores.entries.Add(new Entry{player=participant,email=email,correct=correct,seconds=elapsed,completed=DateTime.UtcNow.Ticks});scores.entries.Sort(CompareEntries);
            if(scores.entries.Count>50)scores.entries.RemoveRange(50,scores.entries.Count-50);
            try{Directory.CreateDirectory(Application.persistentDataPath);var temp=ScorePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(scores,true));if(File.Exists(ScorePath))File.Replace(temp,ScorePath,ScorePath+".bak");else File.Move(temp,ScorePath);}
            catch(Exception e){Debug.LogError("Leaderboard could not be saved: "+e.Message);}
            AppendCsv(participant,email,correct,elapsed);
        }
        void AppendCsv(string player,string email,int score,double seconds)
        {
            try
            {
                var path=CsvPath;
                bool fresh=!File.Exists(path);
                using(var writer=new StreamWriter(path,true))
                {
                    if(fresh)writer.WriteLine("Name,Email,Correct,TimeSeconds,CompletedUtc");
                    writer.WriteLine(Csv(player)+","+Csv(email)+","+score+","+seconds.ToString("0.0")+","+Csv(DateTime.UtcNow.ToString("o")));
                }
            }
            catch(Exception e){Debug.LogError("Results CSV could not be saved: "+e.Message);}
        }
        static string Csv(string value)
        {
            value=value??"";
            if(value.IndexOfAny(new[]{',','"','\n','\r'})<0)return value;
            return "\""+value.Replace("\"","\"\"")+"\"";
        }
        public void ClearLeaderboard()
        {
            scores=new Scores();
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                var temp=ScorePath+".tmp";
                File.WriteAllText(temp,JsonUtility.ToJson(scores,true));
                if(File.Exists(ScorePath))File.Replace(temp,ScorePath,ScorePath+".bak");
                else File.Move(temp,ScorePath);
            }
            catch(Exception e){Debug.LogError("Leaderboard could not be cleared: "+e.Message);}
            FillLeaderboard();
        }
        void FillLeaderboard()
        {
            for(int i=0;i<rows.Length;i++)
            {
                bool exists=i<scores.entries.Count;var row=rows[i];
                foreach(var cell in new[]{row.rank,row.player,row.correct,row.time})cell.gameObject.SetActive(exists||i==0);row.rank.text=exists?(i+1).ToString():"—";
                row.player.text=exists?scores.entries[i].player:(i==0?"Awaiting first agent":"—");
                row.correct.text=exists?scores.entries[i].correct+" / "+ClassificationBank.RoundSize:"—";row.time.text=exists?scores.entries[i].seconds.ToString("0.0")+" sec":"—";
            }
        }
    }
}


