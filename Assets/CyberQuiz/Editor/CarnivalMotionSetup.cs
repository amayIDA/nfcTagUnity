using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CyberQuiz.Editor
{
    [InitializeOnLoad] public static class CarnivalMotionSetup
    {
        static CarnivalMotionSetup(){EditorApplication.update+=Check;}
        static void Check()
        {
            if(!File.Exists("Temp/CarnivalMotion.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Temp/CarnivalMotion.request");
            try{Apply();File.WriteAllText("Temp/CarnivalMotionSetup.txt","PASS: Figma action artwork; transparent text choices; motion authored on both displays");}
            catch(Exception e){File.WriteAllText("Temp/CarnivalMotionSetup.txt","FAIL: "+e);Debug.LogException(e);}
        }
        [MenuItem("Cyber Quiz/Apply Figma Buttons and Game Motion")]
        static void Apply()
        {
            var q=UnityEngine.Object.FindAnyObjectByType<CarnivalQuiz>();if(q==null||q.displays==null)throw new Exception("Open the dual-display Carnival scene.");
            var d=q.displays;
            if(q.motion!=null){RepairChoices(q);CacheMirrors(d);EditorSceneManager.MarkSceneDirty(q.gameObject.scene);EditorSceneManager.SaveScene(q.gameObject.scene);return;}
            var path="Assets/CyberQuiz/Art/FigmaAtmosphere.png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var atmosphere=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var action=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CyberQuiz/Art/Group 8.png");
            foreach(var root in new[]{d.display1.transform,d.display2.transform})
            {
                foreach(var image in root.GetComponentsInChildren<Image>(true))
                {
                    if(image.sprite!=null&&image.sprite.name.StartsWith("FigmaBackground"))image.sprite=atmosphere;
                    if(image.name=="Navy atmosphere"||image.name=="Figma navy atmosphere")image.color=Color.clear;
                }
                foreach(var option in root.GetComponentsInChildren<CarnivalOption>(true))
                {
                    option.surface.sprite=null;option.surface.color=Color.clear;option.button.transition=Selectable.Transition.None;
                    option.label=option.GetComponentInChildren<Text>(true);option.label.color=Color.white;
                    option.revealGroup=option.GetComponent<CanvasGroup>();if(option.revealGroup==null)option.revealGroup=option.gameObject.AddComponent<CanvasGroup>();
                    option.mirrored=root==d.display1.transform;
                    foreach(var outline in option.GetComponents<Outline>())UnityEngine.Object.DestroyImmediate(outline);
                }
                foreach(var button in root.GetComponentsInChildren<Button>(true))
                {
                    if(button.GetComponent<CarnivalOption>()!=null||button.transform.IsChildOf(d.idle.transform))continue;
                    var image=button.GetComponent<Image>();if(image==null)continue;
                    image.sprite=action;image.color=Color.white;image.preserveAspect=true;button.transition=Selectable.Transition.None;
                    foreach(var outline in button.GetComponents<Outline>())UnityEngine.Object.DestroyImmediate(outline);
                    var label=button.GetComponentInChildren<Text>(true);label.resizeTextForBestFit=false;label.fontSize=32;label.alignment=TextAnchor.MiddleCenter;
                    var motion=button.GetComponent<CarnivalButtonMotion>()??button.gameObject.AddComponent<CarnivalButtonMotion>();motion.quiz=q;motion.artwork=image;
                    if(label.text=="START"){label.text="Start Game";Place((RectTransform)button.transform,700,830,520,156);}
                    else if(label.text=="LEADERBOARD"){label.text="Leaderboard";Place((RectTransform)button.transform,740,770,440,132);}
                    else if(label.text=="PLAY AGAIN"){label.text="Play Again";Place((RectTransform)button.transform,740,845,440,132);}
                    else if(label.text=="Submit")label.fontSize=38;
                    var lr=label.rectTransform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(42,15);lr.offsetMax=new Vector2(-42,-15);
                }
            }
            d.registrationPrompt=q.leaderboard.transform.Find("Registration prompt").gameObject;
            foreach(var panel in new[]{q.instructions}.Concat(d.mirrors.Where(m=>m.source==q.instructions).Select(m=>m.view)))
            {
                var art=panel.GetComponentsInChildren<Image>(true).First(i=>i.sprite!=null&&i.sprite.name.StartsWith("FigmaQuestionPanel"));Place(art.rectTransform,285,205,1350,800);
                var title=panel.GetComponentsInChildren<Text>(true).First(t=>t.text=="Instructions");title.text="HOW TO PLAY · DATA CLASSIFICATION";title.fontSize=39;Place(title.rectTransform,350,380,1220,68);
                var body=panel.GetComponentsInChildren<Text>(true).First(t=>t.text.StartsWith("1.")||t.text.StartsWith("Read each"));body.text="Read each question. Tap the OPEN, SECRET, CONFIDENTIAL or SENSITIVE cube.\nThe four choices move each question. Ten random questions. Correct answers rank first; time breaks ties.";body.fontSize=39;body.lineSpacing=1.2f;Place(body.rectTransform,400,445,1120,210);
            }
            var game=q.gameObject.AddComponent<CarnivalMotion>();game.quiz=q;q.motion=game;game.feedbackTitle=q.feedbackTitle.rectTransform;game.feedbackBody=q.feedbackBody.rectTransform;
            var sparks=new System.Collections.Generic.List<RectTransform>();var rings=new System.Collections.Generic.List<CarnivalRing>();var labels=new System.Collections.Generic.List<Text>();var roots=new System.Collections.Generic.List<CanvasGroup>();
            foreach(var canvas in new[]{d.display2,d.display1})
            {
                var fx=Rect("Game response effects — authored reusable pool",canvas.transform,0,0,1920,1080);var group=fx.gameObject.AddComponent<CanvasGroup>();group.interactable=group.blocksRaycasts=false;group.alpha=0;roots.Add(group);
                for(int i=0;i<3;i++)
                {
                    var r=Center("NFC signal ring "+(i+1),fx,900,900);var ring=r.gameObject.AddComponent<CarnivalRing>();ring.thickness=3+i;ring.raycastTarget=false;rings.Add(ring);r.gameObject.SetActive(false);
                }
                for(int i=0;i<48;i++)
                {
                    var r=Center("Celebration spark "+(i+1),fx,i%3==0?7:12,i%3==0?20:7);var im=r.gameObject.AddComponent<Image>();im.raycastTarget=false;im.color=i%3==0?new Color(1,.78f,.23f):i%3==1?new Color(.2f,1,.75f):new Color(.65f,.9f,1);sparks.Add(r);r.gameObject.SetActive(false);
                }
                var text=Center("Answer reward",fx,1100,70).gameObject.AddComponent<Text>();text.font=q.feedbackTitle.font;text.fontSize=32;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;text.gameObject.SetActive(false);labels.Add(text);
            }
            game.sparks=sparks.ToArray();game.rings=rings.ToArray();game.rewardLabels=labels.ToArray();game.effectRoots=roots.ToArray();
            // Cache matching authored transforms so both screens share the same animation clock.
            RepairChoices(q);CacheMirrors(d);
            foreach(var canvas in new[]{d.display1,d.display2})foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=canvas.gameObject.layer;
            EditorUtility.SetDirty(q);EditorUtility.SetDirty(d);EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(q.gameObject.scene);EditorSceneManager.SaveScene(q.gameObject.scene);
        }
        static void RepairChoices(CarnivalQuiz q)
        {
            foreach(var canvas in new[]{q.displays.display1,q.displays.display2})foreach(var option in canvas.GetComponentsInChildren<CarnivalOption>(true))
            {
                option.revealGroup=option.GetComponent<CanvasGroup>();if(option.revealGroup==null)option.revealGroup=option.gameObject.AddComponent<CanvasGroup>();
                option.label=option.GetComponentInChildren<Text>(true);option.surface.color=Color.clear;option.surface.sprite=null;
                EditorUtility.SetDirty(option);EditorUtility.SetDirty(option.revealGroup);EditorUtility.SetDirty(option.surface);
            }
        }
        static void CacheMirrors(CarnivalDisplays d)
        {
            foreach(var mirror in d.mirrors)
            {
                mirror.sourceImages=mirror.source.GetComponentsInChildren<Image>(true);mirror.viewImages=mirror.view.GetComponentsInChildren<Image>(true);
                mirror.sourceRects=mirror.source.GetComponentsInChildren<RectTransform>(true).Where(r=>r.GetComponent<CarnivalButtonMotion>()==null).ToArray();
                mirror.viewRects=mirror.view.GetComponentsInChildren<RectTransform>(true).Where(r=>r.GetComponent<CarnivalButtonMotion>()==null).ToArray();
                mirror.sourceGroups=mirror.source.GetComponentsInChildren<CanvasGroup>(true);mirror.viewGroups=mirror.view.GetComponentsInChildren<CanvasGroup>(true);
                if(mirror.sourceRects.Length!=mirror.viewRects.Length||mirror.sourceGroups.Length!=mirror.viewGroups.Length||mirror.sourceImages.Length!=mirror.viewImages.Length)throw new Exception("Mirror hierarchy mismatch: "+mirror.source.name);
            }
            EditorUtility.SetDirty(d);
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();Place(r,x,y,w,h);return r;}
        static RectTransform Center(string name,Transform parent,float w,float h){var r=Rect(name,parent,0,0,w,h);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;return r;}
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x+w/2,-y-h/2);r.localScale=Vector3.one;}
    }
}
