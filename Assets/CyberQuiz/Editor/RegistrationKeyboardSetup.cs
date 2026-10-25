using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CyberQuiz.Editor
{
    [InitializeOnLoad] public static class RegistrationKeyboardSetup
    {
        const string KeyArt="Assets/CyberQuiz/Art/KeyboardKeyBlank.png";
        static RegistrationKeyboardSetup(){EditorApplication.delayCall+=Apply;}
        [MenuItem("Cyber Quiz/Add Registration Keyboard")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            ConfigureSprite();
            var q=Object.FindAnyObjectByType<CarnivalQuiz>();if(q==null||q.displays==null)return;
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(KeyArt);if(sprite==null)return;
            var holder=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CyberQuiz/Art/Registration place holder.png");
            var panel=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CyberQuiz/Art/FigmaQuestionPanel.png");
            var action=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CyberQuiz/Art/Group 8.png");
            var d=q.displays;
            if(d.registration!=null&&d.keyboard!=null&&d.registration.transform.Find("Registration card")!=null)return;
            var font=q.hudName!=null?q.hudName.font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CoverIdle(d);
            if(d.nameField!=null)d.nameField.transform.SetParent(d.registration.transform,true);
            if(d.employeeIdField!=null)d.employeeIdField.transform.SetParent(d.registration.transform,true);
            var keepSubmit=d.registration.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>
            {
                if(b.GetComponent<CarnivalKey>()!=null)return false;
                var t=b.GetComponentInChildren<Text>(true);
                return t!=null&&(t.text=="Register"||t.text=="Submit");
            });
            if(keepSubmit!=null)keepSubmit.transform.SetParent(d.registration.transform,true);
            foreach(var extra in new[]{"Registration card","Virtual Keyboard","Keyboard dimmer"})
            {
                var found=d.registration.transform.Find(extra);
                if(found!=null)Object.DestroyImmediate(found.gameObject);
            }
            if(d.nameField==null)d.nameField=CreateField(d.registration.transform,font,holder!=null?holder:sprite,"Name — editable input");
            if(d.employeeIdField==null)d.employeeIdField=CreateField(d.registration.transform,font,holder!=null?holder:sprite,"Email ID — editable input");
            var email=d.employeeIdField;
            email.characterLimit=64;
            email.gameObject.name="Email ID — editable input";
            var card=Rect("Registration card",d.registration.transform,430,70,1060,430);
            var cardImage=card.gameObject.AddComponent<Image>();if(panel!=null){cardImage.sprite=panel;cardImage.color=Color.white;}else cardImage.color=new Color(0,.05f,.12f,.82f);cardImage.raycastTarget=false;
            d.nameField.transform.SetParent(card,false);
            email.transform.SetParent(card,false);
            StyleField(d.nameField,holder!=null?holder:sprite,font,"Name",70,42,920,118);
            StyleField(email,holder!=null?holder:sprite,font,"Email ID",70,176,920,118);
            var submit=d.registration.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.GetComponent<CarnivalKey>()==null&&b.GetComponentInChildren<Text>(true)!=null&&(b.GetComponentInChildren<Text>(true).text=="Register"||b.GetComponentInChildren<Text>(true).text=="Submit"));
            if(submit==null)submit=CreateRegister(card,font,action,d);
            submit.transform.SetParent(card,false);
            Place((RectTransform)submit.transform,260,300,540,112);
            var simg=submit.GetComponent<Image>();if(action!=null){simg.sprite=action;simg.preserveAspect=true;}simg.color=Color.white;simg.raycastTarget=true;
            submit.transition=Selectable.Transition.None;
            var sl=submit.GetComponentInChildren<Text>(true);if(sl==null){sl=Rect("Label",submit.transform,40,20,460,72).gameObject.AddComponent<Text>();sl.font=font;sl.alignment=TextAnchor.MiddleCenter;sl.raycastTarget=false;}
            sl.text="Register";sl.fontSize=32;sl.color=Color.white;
            submit.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(submit.onClick,d.SubmitRegistration);
            var motion=submit.GetComponent<CarnivalButtonMotion>()??submit.gameObject.AddComponent<CarnivalButtonMotion>();motion.quiz=q;motion.artwork=simg;
            Place(d.validation.rectTransform,430,510,1060,36);d.validation.fontSize=22;
            var root=Rect("Virtual Keyboard",d.registration.transform,160,555,1600,490);
            var tray=root.gameObject.AddComponent<Image>();if(panel!=null){tray.sprite=panel;tray.color=Color.white;}else tray.color=new Color(0,.04f,.1f,.88f);tray.raycastTarget=false;
            var group=root.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.interactable=false;group.blocksRaycasts=false;
            var board=root.gameObject.AddComponent<CarnivalKeyboard>();
            board.group=group;board.nameField=d.nameField;board.emailField=email;
            board.nameHighlight=d.nameField.GetComponent<Image>();board.emailHighlight=email.GetComponent<Image>();
            var hint=Label(root,"Name",48,28,240,32,font,22,new Color(.45f,.9f,1));
            board.targetLabel=hint;
            const float boardW=1600,keyW=108,keyH=64,gap=12,rowY=78,rowH=76;
            var letters=new List<CarnivalKey>();
            LayoutRow(root,rowY,boardW,gap,keyH,font,sprite,board,letters,
                Keys("1234567890",keyW).Concat(new[]{Key(180,"BACK","Backspace")}).ToArray());
            LayoutRow(root,rowY+rowH,boardW,gap,keyH,font,sprite,board,letters,Keys("QWERTYUIOP",keyW));
            LayoutRow(root,rowY+rowH*2,boardW,gap,keyH,font,sprite,board,letters,Keys("ASDFGHJKL",keyW));
            LayoutRow(root,rowY+rowH*3,boardW,gap,keyH,font,sprite,board,letters,
                new[]{Key(150,"CLEAR","Clear")}.Concat(Keys("ZXCVBNM",keyW)).Concat(new[]{Key(keyW,"-","-"),Key(150,"NEXT","Next")}).ToArray());
            LayoutRow(root,rowY+rowH*4,boardW,gap,keyH,font,sprite,board,letters,
                new[]{Key(keyW,"@","@"),Key(keyW,".","."),Key(720,"SPACE","Space"),Key(420,"HIDE","Hide keyboard")});
            board.letterKeys=letters.ToArray();
            d.keyboard=board;
            d.nameField.shouldHideMobileInput=true;email.shouldHideMobileInput=true;
            foreach(var t in d.registration.GetComponentsInChildren<Transform>(true))t.gameObject.layer=d.display1.gameObject.layer;
            EditorUtility.SetDirty(d);EditorUtility.SetDirty(board);EditorUtility.SetDirty(q);
            EditorSceneManager.MarkSceneDirty(q.gameObject.scene);EditorSceneManager.SaveScene(q.gameObject.scene);
        }
        static void CoverIdle(CarnivalDisplays d)
        {
            if(d.idle==null)return;
            var hit=d.idle.transform.Find("Tap anywhere");
            if(hit==null)
            {
                hit=Rect("Tap anywhere",d.idle.transform,0,0,1920,1080);
                var image=hit.gameObject.AddComponent<Image>();image.color=new Color(0,0,0,0.01f);image.raycastTarget=true;
                var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
                UnityEventTools.AddPersistentListener(button.onClick,d.OpenRegistration);
            }
            hit.SetAsFirstSibling();
            foreach(var image in d.idle.GetComponentsInChildren<Image>(true))
                if(image.gameObject!=hit.gameObject)image.raycastTarget=false;
        }
        static InputField CreateField(Transform parent,Font font,Sprite sprite,string name)
        {
            var r=Rect(name,parent,0,0,880,92);
            var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.type=Image.Type.Simple;image.preserveAspect=false;image.color=Color.white;
            var field=r.gameObject.AddComponent<InputField>();field.targetGraphic=image;field.lineType=InputField.LineType.SingleLine;
            var text=Rect("Text",r,52,28,808,56).gameObject.AddComponent<Text>();text.font=font;text.fontSize=30;text.color=new Color(.55f,.92f,1);text.alignment=TextAnchor.MiddleLeft;text.supportRichText=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;
            var ph=Rect("Placeholder",r,52,28,808,56).gameObject.AddComponent<Text>();ph.font=font;ph.fontSize=30;ph.color=new Color(.45f,.78f,.95f,.7f);ph.alignment=TextAnchor.MiddleLeft;ph.raycastTarget=false;ph.horizontalOverflow=HorizontalWrapMode.Overflow;ph.verticalOverflow=VerticalWrapMode.Truncate;
            field.textComponent=text;field.placeholder=ph;return field;
        }
        static void ConfigureSprite()
        {
            var importer=AssetImporter.GetAtPath(KeyArt) as TextureImporter;if(importer==null)return;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.spriteBorder=new Vector4(18,18,18,18);
            importer.SaveAndReimport();
        }
        static void StyleField(InputField field,Sprite sprite,Font font,string placeholder,float x,float y,float w,float h)
        {
            Place((RectTransform)field.transform,x,y,w,h);
            var image=field.GetComponent<Image>();image.sprite=sprite;image.color=Color.white;image.type=Image.Type.Simple;image.preserveAspect=false;
            field.lineType=InputField.LineType.SingleLine;
            float left=56,right=40,top=26,bottom=32;
            var text=field.textComponent;text.font=font;text.fontSize=30;text.alignment=TextAnchor.MiddleLeft;text.color=new Color(.55f,.92f,1);text.resizeTextForBestFit=false;
            text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;
            Place(text.rectTransform,left,top,w-left-right,h-top-bottom);
            if(field.placeholder is Text ph)
            {
                ph.text=placeholder;ph.font=font;ph.fontSize=30;ph.alignment=TextAnchor.MiddleLeft;ph.color=new Color(.45f,.78f,.95f,.7f);
                ph.horizontalOverflow=HorizontalWrapMode.Overflow;ph.verticalOverflow=VerticalWrapMode.Truncate;
                Place(ph.rectTransform,left,top,w-left-right,h-top-bottom);
            }
        }
        struct KeySpec{public float w;public string glyph;public string label;}
        static KeySpec Key(float w,string glyph,string label){return new KeySpec{w=w,glyph=glyph,label=label};}
        static KeySpec[] Keys(string glyphs,float w)
        {
            var list=new KeySpec[glyphs.Length];
            for(int i=0;i<glyphs.Length;i++)list[i]=Key(w,glyphs[i].ToString(),glyphs[i].ToString());
            return list;
        }
        static void LayoutRow(Transform parent,float y,float boardW,float gap,float keyH,Font font,Sprite sprite,CarnivalKeyboard board,List<CarnivalKey> letters,KeySpec[] keys)
        {
            float width=0;for(int i=0;i<keys.Length;i++)width+=keys[i].w+(i>0?gap:0);
            float x=(boardW-width)/2;
            foreach(var spec in keys)
            {
                var key=MakeKey(parent,x,y,spec.w,keyH,spec.glyph,spec.label,font,sprite,board);
                if(spec.glyph.Length==1&&char.IsLetter(spec.glyph[0]))letters.Add(key);
                x+=spec.w+gap;
            }
        }
        static Button CreateRegister(Transform parent,Font font,Sprite action,CarnivalDisplays d)
        {
            var r=Rect("Register",parent,260,300,540,112);
            var image=r.gameObject.AddComponent<Image>();image.sprite=action;image.color=Color.white;image.preserveAspect=true;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
            var text=Rect("Label",r,40,20,460,72).gameObject.AddComponent<Text>();
            text.font=font;text.text="Register";text.fontSize=32;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;
            return button;
        }
        static CarnivalKey MakeKey(Transform parent,float x,float y,float w,float h,string glyph,string label,Font font,Sprite sprite,CarnivalKeyboard board)
        {
            var r=Rect(glyph,parent,x,y,w,h);
            var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=Color.white;image.type=Image.Type.Sliced;image.preserveAspect=false;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.82f,.95f,1);colors.pressedColor=new Color(.7f,.88f,1);button.colors=colors;
            var text=Rect("Label",r,8,6,w-16,h-12).gameObject.AddComponent<Text>();
            text.font=font;text.fontSize=label.Length>8?20:label.Length>1?22:26;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;text.text=label;
            var key=r.gameObject.AddComponent<CarnivalKey>();key.keyboard=board;key.glyph=glyph;key.label=text;key.button=button;
            UnityEventTools.AddPersistentListener(button.onClick,key.Press);
            return key;
        }
        static Text Label(Transform parent,string value,float x,float y,float w,float h,Font font,int size,Color color)
        {
            var t=Rect("Target field",parent,x,y,w,h).gameObject.AddComponent<Text>();
            t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();Place(r,x,y,w,h);return r;
        }
        static void Place(RectTransform r,float x,float y,float w,float h)
        {
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x+w/2,-y-h/2);r.localScale=Vector3.one;
        }
    }
}
