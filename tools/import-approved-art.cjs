// Offline content compiler. No Unity packages or project settings are changed.
const fs=require('fs'),path=require('path');
const sharp=require(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES?process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES+'/sharp':'sharp');
const project=path.resolve(__dirname,'..'),workspace=path.dirname(project);
const source=path.join(workspace,'Kido-Coloring-Transparent-Vector-Canvases');
const resources=path.join(project,'Assets/KidoColoring/Resources/Kido');
const size=1024;
function regions(rgba){
 const n=size*size,wall=new Uint8Array(n),labels=new Uint16Array(n),visited=new Uint8Array(n),queue=new Int32Array(n);
 // A one-pixel mask-only dilation closes subpixel gaps and keeps strokes beneath ink.
 for(let y=0;y<size;y++)for(let x=0;x<size;x++)if(rgba[(y*size+x)*4+3]>80)
  for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){const xx=x+dx,yy=y+dy;if(xx>=0&&xx<size&&yy>=0&&yy<size)wall[yy*size+xx]=1;}
 // Exterior is connected to the four empty page corners. Edge-clipped branches
 // and seaweed remain valid bounded regions without redrawing approved outlines.
 let count=0;const areas=[];
 for(let i=0;i<n;i++)if(!wall[i]&&!visited[i]){
  let head=0,tail=1,touches=false;queue[0]=i;visited[i]=1;
  while(head<tail){const k=queue[head++],x=k%size,y=(k/size)|0;if((x<2||x>size-3)&&(y<2||y>size-3))touches=true;
   const push=j=>{if(!wall[j]&&!visited[j]){visited[j]=1;queue[tail++]=j;}};
   if(x>0)push(k-1);if(x<size-1)push(k+1);if(y>0)push(k-size);if(y<size-1)push(k+size);
  }
  if(!touches&&tail>=24){count++;areas.push(tail);for(let j=0;j<tail;j++)labels[queue[j]]=count;}
 }
 // Extend paint under antialiased ink without connecting neighboring regions.
 for(let pass=0;pass<3;pass++){
  const before=labels.slice();
  for(let y=1;y<size-1;y++)for(let x=1;x<size-1;x++){const i=y*size+x;if(before[i]||!wall[i])continue;labels[i]=before[i-1]||before[i+1]||before[i-size]||before[i+size];}
 }
 const binary=Buffer.alloc(20+n*2);binary.write('KIDORGN1');binary.writeInt32LE(size,8);binary.writeInt32LE(size,12);binary.writeInt32LE(count,16);
 let paintable=0;
 for(let y=0;y<size;y++)for(let x=0;x<size;x++){const value=labels[y*size+x];binary.writeUInt16LE(value,20+((size-1-y)*size+x)*2);if(value)paintable++;}
 return {binary,count,areas,paintable,labels};
}
(async()=>{
 const external=fs.existsSync(path.join(source,'canvas-catalog.json'));
 const catalog=JSON.parse(fs.readFileSync(external?path.join(source,'canvas-catalog.json'):path.join(project,'ApprovedArt/canvas-catalog.json'))),entries=[],report=[];
 const masters=path.join(project,'ApprovedArt/Canvases');fs.mkdirSync(masters,{recursive:true});
 const checks=path.join(project,'docs/ArtQA');fs.mkdirSync(checks,{recursive:true});
 const thumbs=[];
 for(const c of catalog){
  const svg=fs.readFileSync(external?path.join(source,c.svg):path.join(masters,c.id+'.svg'));fs.writeFileSync(path.join(masters,c.id+'.svg'),svg);
  await sharp(svg).resize(2048,2048).png().toFile(path.join(resources,'Art',c.id+'.png'));
  await sharp(svg).resize(192,192).flatten({background:'#ffffff'}).png().toFile(path.join(resources,'Thumbs',c.id+'.png'));
  const rgba=await sharp(svg).resize(size,size).ensureAlpha().raw().toBuffer(),map=regions(rgba);
  if(map.count<3)throw new Error('Too few regions: '+c.id);
  fs.writeFileSync(path.join(resources,'Regions',c.id+'.bytes'),map.binary);
  entries.push({id:c.id,title:c.title,category:Math.floor((Number(c.number)-1)/10),artPath:'Kido/Art/'+c.id,thumbPath:'Kido/Thumbs/'+c.id,regionPath:'Kido/Regions/'+c.id,artRevision:2,paintWidth:size,paintHeight:size});
  report.push({id:c.id,regions:map.count,paintablePixels:map.paintable,smallestRegion:Math.min(...map.areas),review:c.review_note});
  const colors=['#FF303B','#FF8A00','#FFCE26','#22B85D','#38B5FF','#8B42EA','#FF78BE'].map(hex=>[1,3,5].map(k=>parseInt(hex.slice(k,k+2),16)));
  const preview=Buffer.alloc(size*size*4,255);
  for(let i=0;i<map.labels.length;i++){const col=map.labels[i]?colors[(map.labels[i]-1)%colors.length]:[255,255,255];for(let j=0;j<3;j++)preview[i*4+j]=Math.round(col[j]*(1-rgba[i*4+3]/255));}
  const input=await sharp(preview,{raw:{width:size,height:size,channels:4}}).resize(220,220).png().toBuffer();
  thumbs.push({input,left:((Number(c.number)-1)%5)*220,top:Math.floor((Number(c.number)-1)/5)*220});
  console.log(c.number,c.id,map.count+' regions');
 }
 fs.writeFileSync(path.join(resources,'catalog.json'),JSON.stringify({canvases:entries},null,2));
 fs.writeFileSync(path.join(checks,'region-report.json'),JSON.stringify(report,null,2));
 await sharp({create:{width:1100,height:1320,channels:4,background:'#ffffff'}}).composite(thumbs).png().toFile(path.join(checks,'regions-contact-sheet.png'));
 const patterns=path.join(workspace,'patterns-v2');
 const tilesDir=fs.existsSync(path.join(patterns,'tiles'))?path.join(patterns,'tiles'):path.join(project,'ApprovedArt/Patterns');
 const tiles=fs.readdirSync(tilesDir).filter(x=>x.endsWith('.svg')).sort();
 fs.mkdirSync(path.join(resources,'Patterns'),{recursive:true});fs.mkdirSync(path.join(project,'ApprovedArt/Patterns'),{recursive:true});
 const pc=JSON.parse(fs.readFileSync(fs.existsSync(path.join(patterns,'catalog.json'))?path.join(patterns,'catalog.json'):path.join(project,'ApprovedArt/pattern-catalog.json')));
 for(let i=0;i<tiles.length;i++){
  const svg=fs.readFileSync(path.join(tilesDir,tiles[i]));fs.writeFileSync(path.join(project,'ApprovedArt/Patterns',tiles[i]),svg);
  await sharp(svg).resize(256,256).png().toFile(path.join(resources,'Patterns',String(i+1).padStart(2,'0')+'.png'));
 }
 fs.writeFileSync(path.join(resources,'patterns.json'),JSON.stringify({patterns:pc.map((p,i)=>({id:i,name:p.name,path:'Kido/Patterns/'+String(i+1).padStart(2,'0'),tint:i<12,approved:true}))},null,2));
 console.log('Imported 30 vector masters, 30 outlines, 30 maps, 30 thumbnails, 15 patterns.');
})().catch(e=>{console.error(e);process.exit(1)});
