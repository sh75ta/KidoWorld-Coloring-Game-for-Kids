from pathlib import Path
import json,base64,struct
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
RES=ROOT/'Assets/KidoColoring/Resources'
catalog=json.loads((RES/'Kido/catalog.json').read_text())['canvases']
entries=[]
for entry in catalog:
    png=(RES/(entry['artPath']+'.png')).read_bytes()
    region=(RES/(entry['regionPath']+'.bytes')).read_bytes()
    w,h,n=struct.unpack('<iii',region[8:20])
    labels=np.flipud(np.frombuffer(region[20:],dtype='<u2').reshape(h,w)).ravel()
    starts=np.r_[0,np.flatnonzero(labels[1:]!=labels[:-1])+1]
    lengths=np.diff(np.r_[starts,len(labels)])
    rle=[int(v) for pair in zip(labels[starts],lengths) for v in pair]
    entries.append({'id':entry['id'],'title':entry['title'],'category':entry['category'],'png':'data:image/png;base64,'+base64.b64encode(png).decode(),'rle':rle})
template=(ROOT/'Preview/preview.template.html').read_text()
(ROOT/'Preview/Kido-Coloring-Preview.html').write_text(template.replace('/*ENGINE*/',(ROOT/'Preview/PaintEngine.js').read_text()).replace('/*ASSETS*/',json.dumps(entries,separators=(',',':'),ensure_ascii=False)))
print('Built self-contained browser preview with 30 canvases.')
