const fs = require('fs');
const path = require('path');
const sharp = require(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES ? path.join(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES,'sharp') : 'sharp');
const root = path.resolve(__dirname,'..');
(async()=>{
  for(const file of fs.readdirSync(path.join(root,'ArtSource')).filter(f=>f.endsWith('.svg'))){
    const source=path.join(root,'ArtSource',file),name=file.replace('.svg','.png');
    await sharp(source).resize(512,512).png().toFile(path.join(root,'Assets/KidoColoring/Resources/Kido/Art',name));
    await sharp(source).resize(192,192).png().toFile(path.join(root,'Assets/KidoColoring/Resources/Kido/Thumbs',name));
  }
})().catch(e=>{console.error(e);process.exit(1)});
