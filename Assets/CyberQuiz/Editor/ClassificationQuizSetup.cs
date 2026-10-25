using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CyberQuiz.Editor
{
    [InitializeOnLoad] static class ClassificationQuizSetup
    {
        static ClassificationQuizSetup(){EditorApplication.delayCall+=Apply;}
        static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            var q=Object.FindAnyObjectByType<CarnivalQuiz>();if(q==null||q.questions==null||q.questions.Length==0)return;
            bool already=q.questions[0].options!=null&&q.questions[0].options.Length==4&&q.questions[0].options[0]!=null&&q.questions[0].options[0].label!=null&&q.questions[0].options[0].label.text.StartsWith("A");
            if(already)return;
            foreach(var option in Object.FindObjectsByType<CarnivalOption>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(option.optionIndex>=4){option.gameObject.SetActive(false);continue;}
                option.nfcValue=ClassificationBank.Tags[option.optionIndex];
                option.label=option.GetComponentInChildren<Text>(true);
                if(option.label!=null)option.label.text=ClassificationBank.Labels[option.optionIndex];
                option.gameObject.name="Option "+ClassificationBank.Labels[option.optionIndex]+" — NFC "+ClassificationBank.Tags[option.optionIndex];
                Place((RectTransform)option.transform,option.optionIndex%2==0?280:1040,510+option.optionIndex/2*110,600,96);
                EditorUtility.SetDirty(option);
            }
            foreach(var view in q.questions)
            {
                var live=view.options.Where(o=>o!=null&&o.optionIndex<4).OrderBy(o=>o.optionIndex).ToArray();
                view.options=live;
                view.questionText.resizeTextForBestFit=true;view.questionText.resizeTextMinSize=22;view.questionText.resizeTextMaxSize=30;
                Place(view.questionText.rectTransform,250,390,1420,110);
                foreach(var t in view.panel.GetComponentsInChildren<Text>(true))
                    if(t.text.StartsWith("QUESTION")) t.text="QUESTION 1 OF 10";
            }
            foreach(var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(t.text.Contains("5 questions")||t.text.Contains("NFC card to answer"))
                    t.text="Read each question. Tap the OPEN, SECRET, CONFIDENTIAL or SENSITIVE cube.\nThe four choices move each question. Ten random questions. Correct answers rank first; time breaks ties.";
            }
            if(q.displays!=null)
            {
                foreach(var mirror in q.displays.mirrors)
                {
                    mirror.sourceTexts=mirror.source.GetComponentsInChildren<Text>(true);
                    mirror.viewTexts=mirror.view.GetComponentsInChildren<Text>(true);
                    mirror.sourceImages=mirror.source.GetComponentsInChildren<Image>(true);
                    mirror.viewImages=mirror.view.GetComponentsInChildren<Image>(true);
                    EditorUtility.SetDirty(q.displays);
                }
            }
            EditorUtility.SetDirty(q);
            EditorSceneManager.MarkSceneDirty(q.gameObject.scene);
            EditorSceneManager.SaveScene(q.gameObject.scene);
        }
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x+w/2,-y-h/2);r.localScale=Vector3.one;}
    }
}
