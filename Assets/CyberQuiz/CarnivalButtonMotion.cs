using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CyberQuiz
{
    public class CarnivalButtonMotion : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
    {
        public CarnivalQuiz quiz;
        public Image artwork;
        bool hover,down,selected;
        float scale=1,velocity,age;
        Vector3 baseScale=Vector3.one;
        void Awake(){baseScale=transform.localScale;}
        void OnEnable(){age=0;scale=.9f;velocity=0;hover=down=selected=false;}
        void OnDisable(){transform.localScale=baseScale;if(artwork!=null)artwork.color=Color.white;}
        void Update()
        {
            if(quiz!=null&&quiz.reducedMotion){transform.localScale=baseScale;if(artwork!=null)artwork.color=Color.white;return;}
            age+=Time.unscaledDeltaTime;
            float target=down?.945f:(hover||selected)?1.045f:1;
            scale=Mathf.SmoothDamp(scale,target,ref velocity,down?.045f:.09f,10,Mathf.Min(Time.unscaledDeltaTime,.05f));
            float settle=1+Mathf.Sin(age*22)*Mathf.Exp(-age*9)*.018f;
            transform.localScale=baseScale*scale*settle;
            if(artwork!=null)artwork.color=Color.Lerp(Color.white,new Color(.72f,.95f,1),down?.32f:(hover||selected)?.15f:0);
        }
        public void OnPointerEnter(PointerEventData e){hover=true;}
        public void OnPointerExit(PointerEventData e){hover=down=false;}
        public void OnPointerDown(PointerEventData e){down=true;}
        public void OnPointerUp(PointerEventData e){down=false;velocity=1.8f;}
        public void OnSelect(BaseEventData e){selected=true;}
        public void OnDeselect(BaseEventData e){selected=down=false;}
    }
}
