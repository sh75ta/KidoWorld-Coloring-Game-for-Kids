/* Shared, DOM-free painting logic for the browser review build. */
(function(root){
  class PaintEngine {
    constructor(labels,width=512,height=512,saved=null){
      if(labels.length!==width*height)throw new Error('Region dimensions mismatch');
      if(saved&&saved.length!==labels.length)throw new Error('Save dimensions mismatch');
      this.labels=labels;this.width=width;this.height=height;this.paint=saved?saved.slice():new Uint32Array(labels.length);this.history=[];this.action=null;this.revision=0;this.total=0;this.painted=0;
      for(let i=0;i<labels.length;i++){if(!labels[i])this.paint[i]=0;else{this.total++;if(this.paint[i])this.painted++}}
      if(!this.total)throw new Error('No coloring regions');
    }
    begin(){this.end();this.action=new Map()}
    set(i,color){color>>>=0;if(!this.labels[i]||this.paint[i]===color)return;if(!this.action)this.begin();if(!this.action.has(i))this.action.set(i,this.paint[i]);if(!this.paint[i]&&color)this.painted++;if(this.paint[i]&&!color)this.painted--;this.paint[i]=color;this.revision++}
    end(){const changed=Boolean(this.action?.size);if(changed){this.history.push(this.action);while(this.history.length>1&&(this.history.length>24||this.history.reduce((n,a)=>n+a.size,0)>2000000))this.history.shift()}this.action=null;return changed}
    fill(region,color){if(!region)return;for(let i=0;i<this.paint.length;i++)if(this.labels[i]===region)this.set(i,color)}
    stroke(from,to,region,color,tool='brush',phase=0,radius=14){
      if(!region)return;const steps=Math.max(1,Math.ceil(Math.hypot(to.x-from.x,to.y-from.y)/Math.max(1,radius*.3))),rgb=[color&255,(color>>>8)&255,(color>>>16)&255],pack=(r,g,b)=>(0xff000000|(b<<16)|(g<<8)|r)>>>0;
      for(let s=0;s<=steps;s++){const x=from.x+(to.x-from.x)*s/steps,y=from.y+(to.y-from.y)*s/steps;
        for(let py=Math.max(0,Math.floor(y-radius));py<=Math.min(this.height-1,Math.ceil(y+radius));py++)for(let px=Math.max(0,Math.floor(x-radius));px<=Math.min(this.width-1,Math.ceil(x+radius));px++){
          const i=py*this.width+px;if(this.labels[i]!==region||(px-x)**2+(py-y)**2>radius**2)continue;let c=color;
          if(tool==='eraser')c=0;
          else if(tool==='pattern'&&Math.floor((px+py)/12)%2===0)c=pack(...rgb.map(n=>Math.round(n+(255-n)*.65)));
          else if(tool==='glitter'&&((Math.imul(px,73856093)^Math.imul(py,19349663))>>>0)%17<3)c=pack(...rgb.map(n=>Math.round(n+(255-n)*.78)));
          else if(tool==='rainbow'){const h=((px+py)*.002+phase)%1,k=n=>(n+h*6)%6,f=n=>Math.round(255*(1-.65*Math.max(0,Math.min(k(n),4-k(n),1))));c=pack(f(5),f(3),f(1))}
          this.set(i,c);
        }
      }
    }
    undo(){this.end();const item=this.history.pop();if(!item)return false;for(const [i,c] of item){if(this.paint[i]&&!c)this.painted--;if(!this.paint[i]&&c)this.painted++;this.paint[i]=c}this.revision++;return true}
    clear(){this.begin();for(let i=0;i<this.paint.length;i++)if(this.paint[i])this.set(i,0);return this.end()}
    get coverage(){return this.painted/this.total}
  }
  root.KidoPaint=PaintEngine;if(typeof module!=='undefined')module.exports=PaintEngine;
})(typeof globalThis!=='undefined'?globalThis:this);
