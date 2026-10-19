const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const sharp=require(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES?process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES+'/sharp':'sharp');
const root=path.resolve(__dirname,'..'),resources=path.join(root,'Assets/KidoColoring/Resources');
const catalog=JSON.parse(fs.readFileSync(path.join(resources,'Kido/catalog.json'))).canvases;
test('30 stable unique canvases, ten per category, vector masters preserved',()=>{
 assert.equal(catalog.length,30);assert.equal(new Set(catalog.map(c=>c.id)).size,30);
 for(let i=0;i<3;i++)assert.equal(catalog.filter(c=>c.category===i).length,10);
 for(const c of catalog){const svg=fs.readFileSync(path.join(root,'ApprovedArt/Canvases',c.id+'.svg'),'utf8');assert.ok(svg.includes('<path'));assert.ok(!/<image/.test(svg));assert.equal(c.artRevision,2);}
});
test('every outline has real transparency and independent 384px gallery thumbnail',async()=>{
 for(const c of catalog){
  const art=sharp(path.join(resources,c.artPath+'.png')),meta=await art.metadata();assert.equal(meta.width,2048);assert.equal(meta.height,2048);assert.ok(meta.hasAlpha);
  const {data}=await art.ensureAlpha().raw().toBuffer({resolveWithObject:true});let clear=0,ink=0;
  for(let i=3;i<data.length;i+=4){if(data[i]===0)clear++;if(data[i]===255)ink++;}
  assert.ok(clear>data.length/8);assert.ok(ink>100);
  const thumb=await sharp(path.join(resources,c.thumbPath+'.png')).metadata();assert.equal(thumb.width,384);assert.equal(thumb.height,384);
 }
});
test('Fairy Dust shader avoids Metal reserved identifiers and keeps visible sparkle settings',()=>{
 const shader=fs.readFileSync(path.join(root,'Assets/KidoColoring/Resources/Kido/Shaders/FairyDust.shader'),'utf8');
 assert.ok(!/\bfloat2 point\b/.test(shader));assert.ok(!/\bfixed4[^;]*\bline\b/.test(shader));
 assert.match(shader,/step\(\.76,seed\)/);assert.match(shader,/sparklePoint/);
});
test('region maps have correct orientation, dimensions, labels and exterior corners',()=>{
 for(const c of catalog){
  const b=fs.readFileSync(path.join(resources,c.regionPath+'.bytes'));assert.equal(b.toString('ascii',0,8),'KIDORGN1');
  const w=b.readInt32LE(8),h=b.readInt32LE(12),count=b.readInt32LE(16);assert.equal(w,c.paintWidth);assert.equal(h,c.paintHeight);assert.equal(b.length,20+w*h*2);
  const hits=new Uint32Array(count+1);for(let i=20;i<b.length;i+=2){const v=b.readUInt16LE(i);assert.ok(v<=count);hits[v]++;}
  for(let i=1;i<=count;i++)assert.ok(hits[i]>0);
  for(const i of [0,w-1,(h-1)*w,w*h-1])assert.equal(b.readUInt16LE(20+i*2),0);
  assert.ok(hits[0]>w*h*.08);assert.ok(hits[0]<w*h*.9);
 }
});
test('15 approved pattern resources, including three fixed Christmas palettes',async()=>{
 const patterns=JSON.parse(fs.readFileSync(path.join(resources,'Kido/patterns.json'))).patterns;
 assert.equal(patterns.length,15);
 for(let i=0;i<patterns.length;i++){const p=patterns[i];assert.ok(p.approved);assert.equal(p.tint,i<12);const m=await sharp(path.join(resources,p.path+'.png')).metadata();assert.equal(m.width,256);assert.equal(m.height,256);}
});
test('all shipped Unity assets have unique stable metadata',()=>{
 const ids=new Set();function walk(dir){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){if(entry.name.startsWith('.'))continue;const p=path.join(dir,entry.name);if(p.endsWith('.meta'))continue;assert.ok(fs.existsSync(p+'.meta'),'Missing metadata: '+p);const m=fs.readFileSync(p+'.meta','utf8').match(/guid: ([a-f0-9]{32})/);assert.ok(m);assert.ok(!ids.has(m[1]),'Duplicate GUID');ids.add(m[1]);if(entry.isDirectory())walk(p);}}walk(path.join(root,'Assets'));
});
