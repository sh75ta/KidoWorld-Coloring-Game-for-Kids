using UnityEngine;

namespace Kido.Coloring
{
    public sealed class Feedback:MonoBehaviour
    {
        public static Feedback Current {get;private set;}
        AudioSource source,voiceSource,musicSource;AudioClip tap,finish,music,tick,paintChime,voice;
        float lastTap=-10,lastTick=-10,lastPaint=-10,musicVolume;
        bool musicStarted;
        const float MusicLevel=.14f;
        public bool Sound {get;private set;}
        public bool Motion {get;private set;}
        public float IntroVoiceLength=>Sound&&voice?voice.length:0;
        void Awake()
        {
            Current=this;
            Sound=PlayerPrefs.GetInt("kido.sound",1)==1;Motion=PlayerPrefs.GetInt("kido.motion",1)==1;
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.volume=.48f;
            tap=Tone("soft tap",620,.11f);finish=Fanfare();
            tick=Tone("palette detent",1050,.035f);paintChime=Tone("paint sparkle",930,.3f);
            voice=Resources.Load<AudioClip>("Kido/Audio/KidoWorldChorus");
            music=Resources.Load<AudioClip>("Kido/Audio/ChristmasFunShort");
            if(voice){voiceSource=gameObject.AddComponent<AudioSource>();voiceSource.playOnAwake=false;voiceSource.clip=voice;voiceSource.volume=.9f;}
            if(music)
            {
                musicSource=gameObject.AddComponent<AudioSource>();musicSource.playOnAwake=false;musicSource.loop=true;
                musicSource.volume=0;musicSource.clip=music;musicSource.ignoreListenerPause=false;
            }
        }
        // The children's chorus owns the intro; the background track fades in once the intro ends
        // so the two never play over each other.
        public void PlayIntroVoice(){if(voiceSource&&Sound){voiceSource.Stop();voiceSource.Play();}}
        public void StartMusic(){musicStarted=true;if(musicSource&&Sound&&!musicSource.isPlaying)musicSource.Play();}
        void Update()
        {
            if(!musicSource||!musicStarted)return;
            musicVolume=Mathf.MoveTowards(musicVolume,Sound?MusicLevel:0,Time.unscaledDeltaTime*MusicLevel/1.5f);
            musicSource.volume=musicVolume;
        }
        // Rising sparkle arpeggio for Done / completion.
        AudioClip Fanfare()
        {
            const int rate=22050;float[] notes={523.25f,659.25f,783.99f,1046.5f,1318.5f};float length=1.25f;
            var data=new float[(int)(rate*length)];
            for(int n=0;n<notes.Length;n++)
            {
                int start=(int)(n*.11f*rate);
                for(int i=start;i<data.Length;i++)
                {
                    float t=(float)(i-start)/rate,env=Mathf.Exp(-t*4.5f)*Mathf.Min(1,t*200);
                    data[i]+=(Mathf.Sin(2*Mathf.PI*notes[n]*t)+.35f*Mathf.Sin(4*Mathf.PI*notes[n]*t))*env*.16f;
                }
            }
            var clip=AudioClip.Create("done fanfare",data.Length,1,rate,false);clip.SetData(data,0);return clip;
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
            if(!enabled){source.Stop();if(voiceSource)voiceSource.Stop();}
            if(!musicSource||!musicStarted)return;
            if(enabled){if(!musicSource.isPlaying)musicSource.UnPause();if(!musicSource.isPlaying)musicSource.Play();}
            else{musicVolume=0;musicSource.volume=0;musicSource.Pause();}
        }
        public void SetMotion(bool enabled)
        {
            Motion=enabled;PlayerPrefs.SetInt("kido.motion",enabled?1:0);PlayerPrefs.Save();
            if(!enabled)foreach(var effect in FindObjectsByType<KidsEffects>(FindObjectsSortMode.None))effect.Clear();
        }
        public void Celebrate(RectTransform parent)
        {
            if(Sound)source.PlayOneShot(finish);
            if(Motion&&parent)KidsEffects.Get(parent).Shower();
        }
        void OnDestroy(){if(Current==this)Current=null;if(tap)Destroy(tap);if(finish)Destroy(finish);if(tick)Destroy(tick);if(paintChime)Destroy(paintChime);}
    }
}
