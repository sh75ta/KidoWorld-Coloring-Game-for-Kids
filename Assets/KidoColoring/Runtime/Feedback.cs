using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kido.Coloring
{
    public sealed class Feedback:MonoBehaviour
    {
        AudioSource source;AudioClip tap,finish;
        public bool Sound {get;private set;}
        public bool Motion {get;private set;}
        void Awake()
        {
            Sound=PlayerPrefs.GetInt("kido.sound",1)==1;Motion=PlayerPrefs.GetInt("kido.motion",1)==1;
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.volume=.18f;
            tap=Tone("soft tap",520,.075f);finish=Tone("finished",780,.22f);
        }
        AudioClip Tone(string name,float hz,float duration)
        {
            const int rate=22050;var data=new float[(int)(rate*duration)];
            for(int i=0;i<data.Length;i++){float t=(float)i/rate;data[i]=Mathf.Sin(2*Mathf.PI*hz*t)*Mathf.Exp(-t*22)*Mathf.Min(1,t*180)*.35f;}
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        public void Tap(){if(Sound)source.PlayOneShot(tap);}
        public void SetSound(bool enabled){Sound=enabled;PlayerPrefs.SetInt("kido.sound",enabled?1:0);PlayerPrefs.Save();}
        public void SetMotion(bool enabled){Motion=enabled;PlayerPrefs.SetInt("kido.motion",enabled?1:0);PlayerPrefs.Save();}
        public void Celebrate(RectTransform parent)
        {
            if(Sound)source.PlayOneShot(finish);
            if(Motion)StartCoroutine(Confetti(parent));
        }
        IEnumerator Confetti(RectTransform parent)
        {
            const int count=18;var pieces=new RectTransform[count];var starts=new Vector2[count];
            for(int i=0;i<count;i++)
            {
                pieces[i]=KidoUI.Panel("Confetti",parent,KidoUI.Hex(KidoUI.HexColors[i%11]));pieces[i].anchorMin=pieces[i].anchorMax=new Vector2(.5f,.65f);pieces[i].sizeDelta=new Vector2(12,20);starts[i]=new Vector2(Random.Range(-320,320),Random.Range(80,230));
            }
            float t=0;
            while(t<1.1f && parent)
            {
                t+=Time.unscaledDeltaTime;for(int i=0;i<count;i++)if(pieces[i]){pieces[i].anchoredPosition=new Vector2(starts[i].x*t,starts[i].y*t-320*t*t);pieces[i].localRotation=Quaternion.Euler(0,0,i*23+t*160);pieces[i].GetComponent<Image>().color=new Color(KidoUI.Hex(KidoUI.HexColors[i%11]).r,KidoUI.Hex(KidoUI.HexColors[i%11]).g,KidoUI.Hex(KidoUI.HexColors[i%11]).b,1-t/1.1f);}yield return null;
            }
            foreach(var piece in pieces)if(piece)Destroy(piece.gameObject);
        }
        void OnDestroy(){if(tap)Destroy(tap);if(finish)Destroy(finish);}
    }
}
