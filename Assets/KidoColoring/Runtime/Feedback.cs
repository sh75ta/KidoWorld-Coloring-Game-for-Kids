using UnityEngine;

namespace Kido.Coloring
{
    public sealed class Feedback:MonoBehaviour
    {
        public static Feedback Current {get;private set;}
        AudioSource source,musicSource;AudioClip tap,finish,music,tick,paintChime,voice;
        float lastTap=-10,lastTick=-10,lastPaint=-10;
        public bool Sound {get;private set;}
        public bool Motion {get;private set;}
        void Awake()
        {
            Current=this;
            Sound=PlayerPrefs.GetInt("kido.sound",1)==1;Motion=PlayerPrefs.GetInt("kido.motion",1)==1;
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.volume=.48f;
            tap=Tone("soft tap",620,.11f);finish=Tone("finished",880,.65f);
            tick=Tone("palette detent",1050,.035f);paintChime=Tone("paint sparkle",930,.3f);
            voice=Resources.Load<AudioClip>("Kido/Audio/KidoWorldChorus");
            if(voice&&Sound)source.PlayOneShot(voice);
            music=Resources.Load<AudioClip>("Kido/Audio/JingleBellsKids30s");
            if(music)
            {
                musicSource=gameObject.AddComponent<AudioSource>();musicSource.playOnAwake=false;musicSource.loop=true;
                musicSource.volume=.14f;musicSource.clip=music;musicSource.ignoreListenerPause=false;
                if(Sound)musicSource.Play();
            }
        }
        AudioClip Tone(string name,float hz,float duration)
        {
            const int rate=22050;var data=new float[(int)(rate*duration)];
            for(int i=0;i<data.Length;i++){float t=(float)i/rate;data[i]=Mathf.Sin(2*Mathf.PI*hz*t)*Mathf.Exp(-t*22)*Mathf.Min(1,t*180)*.35f;}
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        public void Tap(){if(Sound&&Time.unscaledTime-lastTap>.065f){lastTap=Time.unscaledTime;source.PlayOneShot(tap);}}
        public void PaletteTick(){if(Sound&&Time.unscaledTime-lastTick>.075f){lastTick=Time.unscaledTime;source.PlayOneShot(tick,.5f);}}
        public void PaintBurst(RectTransform parent,Vector2 position,Color color)
        {
            if(Sound&&Time.unscaledTime-lastPaint>.09f){lastPaint=Time.unscaledTime;source.PlayOneShot(paintChime,.7f);}
            if(Motion&&parent)KidsEffects.Get(parent).Burst(position,color);
        }
        public void SetSound(bool enabled)
        {
            Sound=enabled;PlayerPrefs.SetInt("kido.sound",enabled?1:0);PlayerPrefs.Save();
            if(!musicSource)return;
            if(enabled){if(!musicSource.isPlaying)musicSource.UnPause();if(!musicSource.isPlaying)musicSource.Play();}
            else musicSource.Pause();
        }
        public void SetMotion(bool enabled){Motion=enabled;PlayerPrefs.SetInt("kido.motion",enabled?1:0);PlayerPrefs.Save();}
        public void Celebrate(RectTransform parent)
        {
            if(Sound)source.PlayOneShot(finish);
            if(Motion&&parent)KidsEffects.Get(parent).Shower();
        }
        void OnDestroy(){if(Current==this)Current=null;if(tap)Destroy(tap);if(finish)Destroy(finish);if(tick)Destroy(tick);if(paintChime)Destroy(paintChime);}
    }
}
