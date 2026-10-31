const test=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');

const root=path.resolve(__dirname,'..');
const resources=path.join(root,'Assets/KidoColoring/Resources');
const catalog=JSON.parse(fs.readFileSync(path.join(resources,'Kido/catalog.json'))).canvases;

test('Play Store launch configuration is child-safe and uses the permanent identity',()=>{
 const settings=JSON.parse(fs.readFileSync(path.join(resources,'Kido/settings.json')));
 assert.equal(settings.previewAllCanvases,false);assert.equal(settings.adsEnabled,false);
 assert.equal(settings.freeCanvasIds.length,9);assert.equal(new Set(settings.freeCanvasIds).size,9);
 for(let category=0;category<3;category++){
  const ids=new Set(catalog.filter(c=>c.category===category).map(c=>c.id));
  assert.equal(settings.freeCanvasIds.filter(id=>ids.has(id)).length,3);
 }
 assert.equal(settings.googleProductId,'kidoworld_full_access');
 assert.equal(settings.billingProvider,'none');
 const project=fs.readFileSync(path.join(root,'ProjectSettings/ProjectSettings.asset'),'utf8');
 assert.match(project,/Android: com\.kidoworld\.coloring/);assert.match(project,/AndroidTargetSdkVersion: 36/);
 assert.match(project,/scriptingBackend:\s*\n\s*Android: 1/);assert.match(project,/AndroidTargetArchitectures: 2/);
});
