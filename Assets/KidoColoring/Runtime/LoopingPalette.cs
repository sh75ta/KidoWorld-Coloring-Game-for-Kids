using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kido.Coloring
{
    // Repositions the existing cells around a continuous circular index. No
    // duplicated textures, cloned cells or allocations during drag/inertia.
    public sealed class LoopingPalette:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,IScrollHandler
    {
        RectTransform viewport;RectTransform[] cells;
        Action<int> changed;
        float cellHeight=100,position,velocity,target;
        int current=-1,pointer=int.MinValue;
        bool dragging,seeking,inputEnabled=true;
        public int SelectedIndex=>current;
        public void SetInputEnabled(bool value)
        {if(!value)Settle();inputEnabled=value;}
        public void Settle()
        {
            dragging=false;pointer=int.MinValue;velocity=0;seeking=false;
            position=target=Mathf.Round(position);Arrange(false);
        }
        public void Configure(RectTransform content,Action<int> onChanged)
        {
            viewport=(RectTransform)transform;changed=onChanged;
            cells=new RectTransform[content.childCount];
            for(int i=0;i<cells.Length;i++)cells[i]=(RectTransform)content.GetChild(i);
            Resize(viewport.rect.height/8);Select(0,false);
        }
        public void Resize(float size){cellHeight=Mathf.Max(1,size);Arrange();}
        public void Select(int index,bool animate)
        {
            if(cells==null||cells.Length==0)return;
            target=position+WrappedDelta(index-position,cells.Length);velocity=0;
            seeking=animate&&!dragging;
            if(!seeking){position=target;Arrange();}
        }
        public static float WrappedDelta(float delta,int count)=>Mathf.Repeat(delta+count*.5f,count)-count*.5f;
        public static int WrappedIndex(int value,int count)=>((value%count)+count)%count;
        public void OnBeginDrag(PointerEventData e)
        {if(!inputEnabled||pointer!=int.MinValue)return;pointer=e.pointerId;dragging=true;seeking=false;velocity=0;}
        public void OnDrag(PointerEventData e)
        {
            if(!inputEnabled||!dragging||e.pointerId!=pointer)return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,e.position,e.pressEventCamera,out var now);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,e.position-e.delta,e.pressEventCamera,out var before);
            float delta=(now.y-before.y)/cellHeight;position+=delta;
            velocity=Mathf.Clamp(delta/Mathf.Max(.008f,Time.unscaledDeltaTime),-35,35);Arrange();
        }
        public void OnEndDrag(PointerEventData e){if(e.pointerId!=pointer)return;pointer=int.MinValue;dragging=false;}
        public void OnScroll(PointerEventData e)
        {if(!inputEnabled||cells==null)return;target=Mathf.Round(position-e.scrollDelta.y);seeking=true;velocity=0;}
        void Update()
        {
            if(cells==null||dragging||!inputEnabled)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(seeking)
            {
                position=Mathf.Lerp(position,target,1-Mathf.Exp(-dt*18));
                if(Mathf.Abs(position-target)<.002f){position=target;seeking=false;}
                Arrange();
            }
            else if(Mathf.Abs(velocity)>.12f)
            {position+=velocity*dt;velocity*=Mathf.Exp(-dt*6);Arrange();}
            else if(Mathf.Abs(position-Mathf.Round(position))>.002f)
            {velocity=0;target=Mathf.Round(position);seeking=true;}
        }
        void Arrange(bool sound=true)
        {
            if(cells==null||cells.Length==0)return;
            // Rebase both targets together to avoid floating-point drift after
            // prolonged play without introducing a visible wrap or jump.
            if(Mathf.Abs(position)>cells.Length*4){float shift=Mathf.Floor(position/cells.Length)*cells.Length;position-=shift;target-=shift;}
            for(int i=0;i<cells.Length;i++)
            {
                var cell=cells[i];cell.anchorMin=new Vector2(0,.5f);cell.anchorMax=new Vector2(1,.5f);cell.pivot=new Vector2(.5f,.5f);
                cell.sizeDelta=new Vector2(0,cellHeight);cell.anchoredPosition=new Vector2(0,-WrappedDelta(i-position,cells.Length)*cellHeight);
            }
            int index=WrappedIndex(Mathf.RoundToInt(position),cells.Length);
            if(index!=current){bool tick=current>=0;current=index;changed?.Invoke(index);if(tick&&sound)Feedback.Current?.PaletteTick();}
        }
        void OnDisable(){Settle();}
        void OnEnable(){if(cells!=null)Arrange(false);}
    }
}
