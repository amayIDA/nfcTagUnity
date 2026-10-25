using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CyberQuiz
{
    public class CarnivalOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Button slot. The classification label and its NFC tag are assigned when the question appears.")][Range(0,3)] public int optionIndex;
        [Tooltip("Exact decimal NFC value for this option; keep it as text.")]public string nfcValue;
        public Button button;
        public Image surface;
        public Text label;
        public CanvasGroup revealGroup;
        public bool mirrored;
        [HideInInspector]public CarnivalQuiz quiz;
        bool hover,down,marked;
        Vector3 target=Vector3.one;
        float age;
        Vector2 home;
        bool placed;
        void Awake(){home=((RectTransform)transform).anchoredPosition;placed=true;}
        void OnEnable(){age=0;}
        void Update()
        {
            if(mirrored)return;
            if(quiz!=null&&quiz.reducedMotion){transform.localScale=Vector3.one;if(revealGroup!=null)revealGroup.alpha=1;return;}
            age+=Time.unscaledDeltaTime;
            float u=Mathf.Clamp01((age-optionIndex*.03f)/.3f),e=1-Mathf.Pow(1-u,3);
            float entrance=1+1.35f*Mathf.Pow(u-1,3)+Mathf.Pow(u-1,2)*.35f;
            transform.localScale=Vector3.Lerp(transform.localScale,target*Mathf.Lerp(.9f,1,entrance),1-Mathf.Exp(-24*Time.unscaledDeltaTime));
            if(placed)((RectTransform)transform).anchoredPosition=home+new Vector2(0,-24*(1-e));
            if(revealGroup!=null)revealGroup.alpha=e;
        }
        public void Select(){quiz.SelectOption(optionIndex);}
        public void ResetVisual(){marked=hover=down=false;button.interactable=true;surface.color=Color.clear;if(label!=null)label.color=Color.white;target=Vector3.one;transform.localScale=Vector3.one;age=0;}
        public void Mark(bool right){marked=true;surface.color=Color.clear;if(label!=null)label.color=right?new Color(.25f,1,.7f):new Color(1,0,0);target=Vector3.one*1.08f;}
        public void OnPointerEnter(PointerEventData e){hover=true;Refresh();}
        public void OnPointerExit(PointerEventData e){hover=down=false;Refresh();}
        public void OnPointerDown(PointerEventData e){down=true;Refresh();}
        public void OnPointerUp(PointerEventData e){down=false;Refresh();}
        void Refresh(){if(marked)return;target=Vector3.one*(down?.97f:hover?1.035f:1);surface.color=Color.clear;if(label!=null)label.color=hover?new Color(.55f,.95f,1):Color.white;}
    }
}
