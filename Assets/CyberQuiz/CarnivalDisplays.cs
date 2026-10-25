using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CyberQuiz
{
    // Display numbers in the Inspector are one-based; Unity's targetDisplay is zero-based.
    public class CarnivalDisplays : MonoBehaviour
    {
        [Serializable] public class Mirror
        {
            public CanvasGroup source, view;
            public Text[] sourceTexts, viewTexts;
            public Image[] sourceImages, viewImages;
            public RectTransform[] sourceRects, viewRects;
            public CanvasGroup[] sourceGroups, viewGroups;
        }
        public CarnivalQuiz quiz;
        public Camera camera1, camera2;
        public Canvas display1, display2;
        public CanvasGroup idle, registration, thankYou;
        public InputField nameField, employeeIdField;
        public CarnivalKeyboard keyboard;
        public Text validation, thankYouText;
        public GameObject gameBackdrop, mirrorHud;
        public Text mirrorName, mirrorTime, mirrorCorrect;
        public Button leaderboardReplay;
        [Tooltip("Bottom touchscreen only. Edit this object in the scene; the top display does not copy over it.")]
        public Button clearLeaderboard;
        public GameObject registrationPrompt;
        public Mirror[] mirrors;
        public bool Registered { get; private set; }
        public string EmailId { get; private set; }
        public string EmployeeId => EmailId;
        bool transitioning, thanking;
        CanvasGroup kioskVisible;

        void Awake()
        {
            if (Display.displays.Length > 1) Display.displays[1].Activate();
            foreach (var mirror in mirrors)
                foreach (var option in mirror.view.GetComponentsInChildren<CarnivalOption>(true))
                {
                    option.quiz=quiz;
                    option.button.onClick.AddListener(option.Select);
                }
        }
        void Start() { ResetRegistration(); }
        public void OpenRegistration()
        {
            if (Registered || transitioning || kioskVisible!=idle) return;
            if(validation!=null)validation.text="";
            if(keyboard!=null)keyboard.Hide();
            CarnivalAudio.Whoosh();
            StartCoroutine(ChangeKiosk(registration));
        }
        public void SubmitRegistration()
        {
            if (Registered || transitioning || quiz.IsBusy) return;
            var email=(employeeIdField.text??"").Trim();
            if(nameField==null||employeeIdField==null)return;
            if (string.IsNullOrWhiteSpace(nameField.text) || email.Length==0 || email.IndexOf('@')<1)
            {
                CarnivalAudio.Invalid();
                validation.text="Please enter your name and email ID.";
                (string.IsNullOrWhiteSpace(nameField.text)?nameField:employeeIdField).Select();
                if(keyboard!=null)keyboard.Focus(string.IsNullOrWhiteSpace(nameField.text)?nameField:employeeIdField);
                return;
            }
            CarnivalAudio.Register();
            Registered=true; EmailId=email;
            quiz.playerName.text=nameField.text.Trim();
            validation.text="";
            if(keyboard!=null)keyboard.Hide();
            if (EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null);
            thankYouText.text="Thank you, "+nameField.text.Trim()+"!\nYour registration is complete.";
            quiz.OpenInstructions();
            StartCoroutine(CompleteRegistration());
        }
        IEnumerator CompleteRegistration()
        {
            thanking=true;
            yield return ChangeKiosk(thankYou);
            yield return new WaitForSecondsRealtime(1.4f);
            Hide(thankYou); kioskVisible=null; thanking=false;
        }
        public void ResetRegistration()
        {
            StopAllCoroutines(); Registered=false; EmailId=""; transitioning=thanking=false;
            if(nameField!=null)nameField.text="";
            if(employeeIdField!=null)employeeIdField.text="";
            if(validation!=null)validation.text="";
            if(keyboard!=null)keyboard.Hide();
            if(registration!=null)Hide(registration);
            if(thankYou!=null)Hide(thankYou);
            if(mirrors!=null)foreach(var mirror in mirrors)if(mirror!=null&&mirror.view!=null)Hide(mirror.view);
            if(gameBackdrop!=null)gameBackdrop.SetActive(false);
            if(mirrorHud!=null)mirrorHud.SetActive(false);
            if(idle!=null){Set(idle,1,true);kioskVisible=idle;}
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        }
        void LateUpdate()
        {
            bool showGame=Registered&&!thanking;
            gameBackdrop.SetActive(showGame);
            mirrorHud.SetActive(showGame&&quiz.hud.activeSelf);
            mirrorName.text=quiz.hudName.text;mirrorTime.text=quiz.hudTime.text;mirrorCorrect.text=quiz.hudCorrect.text;
            mirrorCorrect.transform.localScale=quiz.hudCorrect.transform.localScale;
            leaderboardReplay.gameObject.SetActive(Registered);
            if(registrationPrompt!=null)registrationPrompt.SetActive(!Registered);
            foreach(var mirror in mirrors)
            {
                bool show=showGame&&mirror.source.gameObject.activeSelf;
                if(!show){if(mirror.view.gameObject.activeSelf)mirror.view.gameObject.SetActive(false);continue;}
                Set(mirror.view,show?mirror.source.alpha:0,show&&mirror.source.interactable);
                mirror.view.transform.localScale=mirror.source.transform.localScale;
                ((RectTransform)mirror.view.transform).anchoredPosition=((RectTransform)mirror.source.transform).anchoredPosition;
                for(int i=0;i<mirror.sourceTexts.Length;i++)
                {
                    mirror.viewTexts[i].text=mirror.sourceTexts[i].text;
                    mirror.viewTexts[i].color=mirror.sourceTexts[i].color;
                    mirror.viewTexts[i].gameObject.SetActive(mirror.sourceTexts[i].gameObject.activeSelf);
                }
                if(mirror.sourceImages!=null&&mirror.viewImages!=null)
                    for(int i=0;i<Mathf.Min(mirror.sourceImages.Length,mirror.viewImages.Length);i++)
                    {
                        mirror.viewImages[i].color=mirror.sourceImages[i].color;
                        mirror.viewImages[i].gameObject.SetActive(mirror.sourceImages[i].gameObject.activeSelf);
                    }
                if(mirror.sourceRects!=null&&mirror.viewRects!=null)
                    for(int i=0;i<Mathf.Min(mirror.sourceRects.Length,mirror.viewRects.Length);i++)
                    {
                        mirror.viewRects[i].anchoredPosition=mirror.sourceRects[i].anchoredPosition;
                        mirror.viewRects[i].localScale=mirror.sourceRects[i].localScale;
                        mirror.viewRects[i].localRotation=mirror.sourceRects[i].localRotation;
                    }
                if(mirror.sourceGroups!=null&&mirror.viewGroups!=null)
                    for(int i=0;i<Mathf.Min(mirror.sourceGroups.Length,mirror.viewGroups.Length);i++)mirror.viewGroups[i].alpha=mirror.sourceGroups[i].alpha;
            }
        }
        IEnumerator ChangeKiosk(CanvasGroup next)
        {
            transitioning=true;
            var old=kioskVisible;
            if(old!=null){old.interactable=false;old.blocksRaycasts=false;}
            Set(next,0,false);
            float duration=quiz.reducedMotion?.01f:.32f;
            for(float t=0;t<duration;t+=Time.unscaledDeltaTime)
            {
                float u=Mathf.SmoothStep(0,1,t/duration);
                next.alpha=u;
                next.transform.localScale=Vector3.one*Mathf.Lerp(.975f,1,1-Mathf.Pow(1-u,3));
                if(old!=null)old.alpha=1-u;
                yield return null;
            }
            if(old!=null)Hide(old);
            Set(next,1,true);next.transform.localScale=Vector3.one;kioskVisible=next;transitioning=false;
        }
        static void Hide(CanvasGroup panel){panel.alpha=0;panel.interactable=panel.blocksRaycasts=false;panel.gameObject.SetActive(false);}
        static void Set(CanvasGroup panel,float alpha,bool interactive){panel.gameObject.SetActive(true);panel.alpha=alpha;panel.interactable=panel.blocksRaycasts=interactive;}
    }
}
