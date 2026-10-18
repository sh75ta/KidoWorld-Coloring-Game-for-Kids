// Optional integration check. Requires Playwright and an installed Chromium binary.
// Not executed successfully in the authoring workspace (browser executable absent).
const path=require('path'),{pathToFileURL}=require('url'),assert=require('node:assert/strict');
const {chromium}=require(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES?path.join(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES,'playwright'):'playwright');
(async()=>{
 const browser=await chromium.launch({headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1366,height:900}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.goto(process.env.KIDO_PREVIEW_URL||pathToFileURL(path.resolve(__dirname,'../Preview/Kido-Coloring-Preview.html')).href);
  await page.waitForSelector('#splash.hidden');assert.equal(await page.locator('.card').count(),30);
  await page.evaluate(()=>kidoQA.openPicture(kidoQA.assets[14]));
  const hit=await page.evaluate(()=>kidoQA.sampleRegion());assert(hit);
  const bounds=await page.locator('#canvas').boundingBox();assert(Math.abs(bounds.width-bounds.height)<1);
  await page.mouse.click(bounds.x+hit.x/512*bounds.width,bounds.y+hit.y/512*bounds.height);
  await page.waitForFunction(()=>kidoQA.getState().painted>0);
  const colored=await page.evaluate(()=>kidoQA.getPaint());
  await page.locator('#undo').click();assert.equal((await page.evaluate(()=>kidoQA.getState())).painted,0);
  await page.mouse.click(bounds.x+hit.x/512*bounds.width,bounds.y+hit.y/512*bounds.height);
  await page.locator('#reset').click();await page.getByRole('button',{name:'Keep coloring',exact:true}).click();
  assert.deepEqual(await page.evaluate(()=>kidoQA.getPaint()),colored);
  await page.locator('#reset').click();await page.getByRole('button',{name:'Clear picture',exact:true}).click();
  assert.equal((await page.evaluate(()=>kidoQA.getState())).painted,0);
  await page.locator('#undo').click();await page.locator('#done').click();await page.waitForSelector('#paint.hidden');
  await page.reload();await page.waitForSelector('#splash.hidden');await page.evaluate(()=>kidoQA.openPicture(kidoQA.assets[14]));
  assert.deepEqual(await page.evaluate(()=>kidoQA.getPaint()),colored);
  for(const viewport of [{width:844,height:390},{width:1024,height:768},{width:390,height:844}]){
   await page.setViewportSize(viewport);await page.waitForTimeout(100);
   const rect=await page.locator('#canvas').boundingBox();assert(rect&&rect.width>0&&Math.abs(rect.width-rect.height)<1);
   const done=await page.locator('#done').boundingBox();assert(done.y>=0&&done.y+done.height<=viewport.height);
  }
  assert.deepEqual(errors,[]);console.log('PASS: browser pointer fill, undo, reset cancel/confirm, save/reload, and canvas sizing.');
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
