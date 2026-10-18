"""Author original editable SVG art; render with sharp; derive fill-region data.
Requires Python + numpy/scipy/Pillow and Node + sharp. Run from any directory.
Region maps are technical label data, not visible artwork. Output Y=0 is bottom.
"""
import json, math, os, struct, subprocess
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/KidoColoring/Resources/Kido'
S = []
def shape(tag, **attrs):
    S.append('<'+tag+' '+ ' '.join(f'{k.replace("_","-")}="{v}"' for k,v in attrs.items())+'/>')
def e(x,y,rx,ry=None,**kw): shape('ellipse',cx=x,cy=y,rx=rx,ry=rx if ry is None else ry,**kw)
def r(x,y,w,h,rad=12,**kw): shape('rect',x=x,y=y,width=w,height=h,rx=rad,**kw)
def p(d,**kw): shape('path',d=d,**kw)
def line(d): p(d,fill='none')
def eye(x,y,sz=9):
    e(x,y,sz,sz*1.25,fill='#202638',stroke='none'); e(x-2,y-3,2.5,fill='white',stroke='none')
def face(x,y,gap=26):
    eye(x-gap,y);eye(x+gap,y);line(f'M{x-14} {y+24} Q{x} {y+38} {x+14} {y+24}')
def star(x,y,rad=26):
    pts=[]
    for i in range(10):
        a=-math.pi/2+i*math.pi/5; rr=rad if i%2==0 else rad*.46
        pts.append(f'{x+rr*math.cos(a):.2f},{y+rr*math.sin(a):.2f}')
    shape('polygon',points=' '.join(pts))
def gift(x,y,w=64,h=65):
    r(x,y,w,h,6);r(x-3,y,w+6,17,4);r(x+w*.43,y,12,h,2)
    p(f'M{x+w/2} {y} C{x} {y-40} {x} {y-4} {x+w/2} {y} Z')
    p(f'M{x+w/2} {y} C{x+w} {y-40} {x+w} {y-4} {x+w/2} {y} Z')
def wheels(xs,y,rad=35):
    for x in xs:e(x,y,rad);e(x,y,rad*.43)
def cloud(x,y):p(f'M{x-38} {y+14} C{x-70} {y+12} {x-54} {y-24} {x-26} {y-12} C{x-24} {y-52} {x+34} {y-48} {x+35} {y-14} C{x+77} {y-29} {x+80} {y+18} {x+48} {y+18} Z')
def leaf(x,y): p(f'M{x} {y} Q{x-55} {y-45} {x-6} {y-70} Q{x+35} {y-48} {x} {y} Z')
def ground():line('M120 434 Q256 448 392 434')
def animal_body():e(256,337,83,90);e(202,410,34,23);e(310,410,34,23)
def santa():
    e(358,310,79,112);p('M330 204 L375 204 L365 179 L338 178 Z');gift(340,360)
    e(218,334,105,99);r(126,346,185,24,8);e(218,355,17,14)
    e(132,209,19,26);e(305,209,19,26);e(218,208,88,83)
    p('M138 222 Q116 312 181 315 Q218 362 254 315 Q319 309 297 222 Q274 247 249 238 Q218 275 185 238 Q157 252 138 222 Z')
    p('M136 155 Q161 63 257 75 L310 141 Q265 114 268 153 Z');e(311,140,19);r(128,147,178,28,14)
    eye(185,201);eye(248,201);e(218,222,15,12);line('M198 271 Q218 288 239 271')
def reindeer():
    line('M195 153 L158 77 M175 112 L142 110 M164 90 L176 62 M315 153 L354 77 M337 112 L370 110 M347 90 L335 62')
    animal_body();e(162,183,42,24);e(350,183,42,24);e(256,236,95,100)
    r(181,306,150,30);p('M280 335 L312 335 L315 389 L279 380 Z');e(256,278,42,30);eye(218,222);eye(295,222);e(256,260,24,20)
def tree():
    r(230,363,53,72,5);p('M256 95 L354 216 L318 216 L392 302 L349 302 L426 392 L86 392 L163 302 L120 302 L194 216 L158 216 Z');star(256,89,40)
    for x,y,rr in [(235,180,18),(285,231,19),(206,279,22),(317,321,22),(246,357,22),(151,355,17)]:e(x,y,rr)
    gift(92,390,76,61);gift(331,391,76,60)
def snowman():
    e(256,349,104,91);e(256,241,80,73);e(256,148,63,61)
    line('M183 255 L99 217 M122 228 L116 201 M329 255 L409 211 M388 222 L392 195')
    r(192,193,127,28);r(281,220,26,72,4);r(195,89,125,20,4);r(213,41,88,52,8)
    eye(235,141,7);eye(274,141,7);p('M249 155 L294 166 L249 175 Z')
    for yy in [255,304,355]:e(255,yy,10)
    line('M236 184 Q254 197 273 184')
def sleigh():
    gift(191,193,87,91);gift(280,166,89,117);gift(125,228,72,66)
    p('M78 282 Q110 312 153 289 L357 289 Q382 220 423 252 Q451 293 394 320 L362 375 L164 375 Q118 371 114 327 L81 327 Z')
    line('M166 376 L177 407 M351 375 L339 407');p('M115 407 L374 407 Q423 405 422 373',fill='none');star(257,329,26);e(115,121,28)
def gingerbread():
    r(130,206,254,217,12);p('M99 215 L256 79 L414 215 Z');p('M116 208 L256 98 L398 208 Q370 247 340 213 Q308 247 279 213 Q246 247 216 213 Q184 246 155 213 Q133 236 116 208 Z')
    r(220,309,72,114,34);r(155,262,52,53);r(309,262,52,53);e(273,367,5,fill='#202638');e(256,170,24)
    for x in [114,169,343,398]:e(x,422,22,29)
def penguin():
    p('M165 290 Q87 318 133 357 L190 330 Z');p('M344 290 Q421 315 376 357 L320 330 Z')
    e(256,302,102,121);e(256,327,66,82);e(256,210,83,80);e(230,229,34,43);e(282,229,34,43);eye(232,213);eye(281,213);p('M240 244 L271 244 L256 262 Z')
    p('M174 188 Q169 111 256 102 Q338 117 338 188 Z');r(170,178,172,27);e(256,91,24)
    r(185,279,145,23,8);r(286,301,26,57,5);e(207,419,37,16);e(305,419,37,16);line('M134 443 L378 443 Q410 440 407 419')
def teddy():
    e(189,163,33);e(325,163,33);e(256,223,95,87);e(256,250,42,30);eye(218,210);eye(293,210);e(256,239,12,9);line('M244 263 Q256 275 270 263')
    gift(143,317,228,115);e(153,325,34,29);e(359,325,34,29)
def stocking():
    e(211,128,47);e(188,105,15);e(234,105,15);face(211,129,17)
    r(272,105,74,85);r(282,83,55,30);e(294,136,7);e(324,136,7);line('M292 162 L323 162')
    p('M188 193 L345 193 L339 339 Q335 424 228 439 L131 439 Q83 430 106 385 L188 332 Z');r(175,176,183,46);star(267,287,35)
def elf():
    e(194,235,62,72);p('M128 161 L111 147 L132 194 Z');p('M259 161 L280 147 L259 194 Z');e(194,169,63,63)
    p('M134 128 L170 49 Q225 43 256 128 Z');r(131,121,128,23);e(171,48,16);face(194,171,23)
    r(97,331,320,26);r(113,357,24,76);r(376,357,24,76);r(257,270,115,49,9);r(286,234,60,48);wheels([283,348],321,18);star(379,202,25)
def lion():
    animal_body();p('M256 98 L288 115 L327 108 L340 143 L373 164 L362 201 L375 236 L344 262 L331 299 L292 296 L256 318 L221 296 L182 299 L169 261 L136 236 L149 200 L138 163 L172 142 L185 108 L224 115 Z')
    e(188,171,22);e(324,171,22);e(256,208,78,83);face(256,199,29);e(256,232,13,9)
def elephant():
    animal_body();e(155,214,61,77);e(357,214,61,77);e(155,218,34,49);e(357,218,34,49);e(256,211,89,90)
    p('M231 259 L232 329 Q239 380 284 365 L307 341 Q321 323 304 312 Q294 306 282 321 L275 332 Q262 339 262 317 L280 259 Z');eye(217,210);eye(295,210)
    e(366,104,17,24);e(405,141,13,19);e(328,78,12,18)
def puppy():
    animal_body();p('M181 146 Q104 120 127 241 Q146 271 178 222 Z');p('M331 146 Q408 120 385 241 Q367 271 334 222 Z');e(256,211,91,89)
    e(293,192,29,34);eye(218,205);eye(295,205);e(256,245,29,22);e(256,232,12,8);line('M246 262 Q256 273 269 262');e(376,395,40);line('M350 364 Q400 382 388 433')
def kitten():
    p('M179 163 L170 87 L231 132 L284 132 L345 87 L334 169 Z');e(256,213,88,81);face(256,207,28);p('M247 230 L266 230 L256 241 Z')
    line('M185 233 L133 220 M184 247 L133 252 M329 233 L380 220 M329 247 L380 252')
    p('M133 300 L379 300 L352 421 L160 421 Z');r(126,289,260,28);line('M176 322 L184 402 M219 322 L223 402 M264 322 L263 402 M308 322 L302 402 M350 322 L341 402');e(194,298,31,22);e(319,298,31,22)
def dino():
    p('M178 319 Q74 295 95 214 Q138 262 206 260 Z');e(274,223,99,115);p('M205 145 L189 115 L229 117 Z');p('M261 115 L282 80 L301 122 Z');e(291,175,98,83);e(344,213,64,42)
    eye(264,169,12);eye(330,163,11);e(365,203,5,fill='#202638');line('M299 234 Q326 257 365 235')
    p('M136 312 L168 277 L204 311 L237 280 L269 311 L303 282 L336 312 L372 281 Q408 420 270 441 Q121 441 136 312 Z');e(197,360,24,32);e(285,397,34,25);e(344,350,21,27);e(221,299,27,23);e(320,298,26,22)
def stego():
    p('M323 301 L428 257 L386 330 Z')
    for x,y in [(161,242),(209,207),(262,207),(314,236)]:p(f'M{x-25} {y+30} L{x} {y-33} L{x+28} {y+30} Z')
    e(254,315,132,82);r(167,346,43,77);r(289,346,43,77);e(133,302,65,56);eye(113,288);line('M105 326 Q132 343 155 325')
    for x,y in [(215,289),(274,316),(322,282)]:e(x,y,17)
    leaf(396,430)
def longneck():
    p('M325 323 Q411 323 435 279 Q440 343 354 362 Z');e(273,342,109,65);r(207,360,43,70);r(309,360,43,70)
    p('M174 339 L161 199 Q118 198 117 160 Q114 121 163 114 Q224 110 227 165 L242 332 Z');eye(155,151);line('M127 177 Q148 188 166 177')
    for x,y in [(195,234),(211,282),(265,337),(321,325)]:e(x,y,15)
    r(387,173,18,172);e(391,154,58,55)
def fish():
    p('M350 225 L437 174 L437 341 L350 290 Z');p('M206 202 L258 135 L288 204 Z');p('M211 307 L262 372 L289 307 Z');e(242,258,139,89)
    p('M199 174 Q171 259 200 342 L224 346 Q193 258 225 170 Z');p('M296 177 Q273 259 297 340 L324 329 Q300 258 324 188 Z');eye(146,247,13);line('M115 276 Q132 297 155 279');e(92,132,23);e(139,76,14);leaf(379,441)
def parrot():
    r(106,390,302,19,8);p('M224 339 L215 419 L272 388 L291 328 Z');e(266,275,75,106);e(244,164,70,73);p('M188 156 Q112 157 149 214 L191 190 Z');e(274,279,42,69);eye(224,157,12);line('M257 253 L290 306');leaf(377,392)
def owl_at(x,y,scale):
    S.append(f'<g transform="translate({x} {y}) scale({scale})">')
    p('M-68 -55 L-79 -109 L-30 -79 Q0 -91 30 -79 L79 -109 L68 -55 Q104 25 61 80 Q0 118 -61 80 Q-104 25 -68 -55 Z');e(-33,-30,34);e(33,-30,34);eye(-33,-29,11);eye(33,-29,11);p('M-12 3 L12 3 L0 20 Z');e(0,53,35,31);S.append('</g>')
def owls():
    r(82,386,351,20);owl_at(201,281,1.0);owl_at(350,323,.60);e(365,114,36);star(137,117,26)
def car(monster=False):
    if monster:
        p('M100 239 L165 237 L191 173 L300 173 L349 237 L407 251 L407 326 L100 326 Z');r(205,188,78,49);line('M193 325 L319 363 M316 325 L190 363');wheels([159,351],364,65);star(260,282,29)
    else:
        p('M77 282 Q86 252 148 250 L199 178 L310 178 L365 249 Q427 251 435 282 L435 343 L77 343 Z');p('M207 197 L252 197 L252 248 L173 248 Z');p('M269 197 L302 197 L343 248 L269 248 Z');wheels([156,359],346,44);r(245,277,55,41,10);star(272,297,15)
def plane():
    p('M234 88 Q256 44 279 88 L290 206 L439 301 L439 331 L290 292 L284 370 L339 414 L339 437 L256 413 L175 437 L175 414 L228 370 L223 292 L74 331 L74 301 L223 206 Z');e(256,143,19,35);r(242,211,28,48);cloud(108,133);cloud(407,141)
def ship():
    p('M88 337 L430 337 L381 414 L141 414 Z');r(247,104,15,233,3);p('M237 124 L126 312 L237 312 Z');p('M273 151 L383 311 L273 311 Z');p('M263 102 L333 102 L314 121 L333 141 L263 141 Z');star(299,121,12)
    for x in [173,256,339]:e(x,370,14)
    line('M67 438 Q101 416 135 438 Q169 460 203 438 Q237 416 271 438 Q305 460 339 438 Q373 416 407 438 Q433 454 453 438')
def fire():
    r(82,254,232,111);p('M314 215 L386 215 L433 275 L433 365 L314 365 Z');p('M330 235 L376 235 L409 276 L330 276 Z');wheels([154,365],365,39);r(109,279,79,59);e(250,307,35);e(250,307,18);r(111,207,194,25,3)
    for x in [144,180,216,252,288]:line(f'M{x} 208 L{x} 231')
    r(344,192,33,23,7)
def icecream():
    p('M86 223 L332 223 L423 274 L423 366 L86 366 Z');r(113,248,151,83);p('M302 245 L327 245 L390 282 L302 282 Z');wheels([158,355],365,40)
    p('M200 132 L278 132 L239 211 Z');e(239,123,44);p('M103 247 L274 247 L274 270 Q257 289 240 270 Q223 289 206 270 Q189 289 172 270 Q155 289 138 270 Q121 289 103 270 Z')
def train():
    r(96,273,168,99);r(215,201,109,171);r(207,185,127,24,5);r(237,222,65,59);r(112,216,42,58);r(102,202,63,21);r(345,281,94,89);line('M324 349 L345 349');wheels([136,244,388],373,35);e(127,152,23);e(162,102,30);e(210,62,21)
def digger():
    r(109,340,258,80,40);r(136,360,205,40,20);r(151,282,159,60);r(174,207,111,79);r(193,224,74,45);p('M292 284 L364 181 L392 195 L322 306 Z');p('M364 181 L384 174 L435 298 L411 309 Z');p('M397 303 L448 285 L468 355 L390 355 Z')
def bus():
    r(72,223,366,151,25);r(360,248,52,110,7)
    for x in [94,158,222,286]:r(x,248,48,49,5)
    line('M93 320 L338 320');wheels([151,353],375,41);r(185,187,145,29,9);star(258,202,11)
def rocket():
    p('M195 286 L135 346 L135 414 L211 374 Z');p('M317 286 L377 346 L377 414 L301 374 Z');p('M228 371 Q201 425 256 465 Q310 425 284 371 Z');p('M256 64 Q338 133 324 275 L299 380 L213 380 L188 275 Q174 133 256 64 Z');p('M256 64 Q300 104 315 156 L198 156 Q214 104 256 64 Z');e(256,229,43);e(256,229,29);r(220,329,73,27);star(109,169,27);e(405,109,33);line('M364 129 L443 91')

DRAW=[santa,reindeer,tree,snowman,sleigh,gingerbread,penguin,teddy,stocking,elf,lion,elephant,puppy,kitten,dino,stego,longneck,fish,parrot,owls,lambda:car(False),lambda:car(True),plane,ship,fire,icecream,train,digger,bus,rocket]

def main():
    catalog=json.loads((ROOT/'content/canvases.json').read_text())
    for record,draw in zip(catalog['canvases'],DRAW):
        S.clear();draw()
        svg='<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 512 512"><rect width="512" height="512" fill="white"/><g stroke="#202638" stroke-width="6" stroke-linecap="round" stroke-linejoin="round" fill="white">'+''.join(S)+'</g></svg>'
        (ROOT/'ArtSource'/f'{record["id"]}.svg').write_text(svg)
    subprocess.run([os.environ.get('CODEX_PRIMARY_RUNTIME_NODE','node'),str(ROOT/'tools/render_art.cjs')],check=True)
    stats=[]; entries=[]
    for rec in catalog['canvases']:
        key=rec['id'];im=Image.open(OUT/'Art'/f'{key}.png').convert('RGB');rgb=np.array(im)
        white=np.min(rgb,axis=2)>180
        labels,n=ndimage.label(white)
        border=set(np.concatenate([labels[0],labels[-1],labels[:,0],labels[:,-1]]).tolist())
        sizes=np.bincount(labels.ravel());regions=np.zeros_like(labels,dtype=np.uint16);count=0
        for label in range(1,n+1):
            if label not in border and sizes[label]>=70:
                count+=1;regions[labels==label]=count
        if count==0:raise RuntimeError('No fillable regions: '+key)
        # Snap a one-pixel antialias fringe to its nearest region, but never cross black strokes.
        near=ndimage.distance_transform_edt(regions==0,return_distances=False,return_indices=True)
        add=(regions==0)&(np.min(rgb,axis=2)>90)&(np.min(rgb,axis=2)<=180)
        regions[add]=regions[tuple(near[:,add])]
        height,width=regions.shape
        with (OUT/'Regions'/f'{key}.bytes').open('wb') as f:
            f.write(b'KIDORGN1');f.write(struct.pack('<iii',width,height,count));f.write(np.flipud(regions).astype('<u2').tobytes())
        stats.append({'id':key,'width':width,'height':height,'regions':count,'paintable_pixels':int(np.count_nonzero(regions))})
        entries.append({'id':key,'title':rec['title'],'category':next(i for i,x in enumerate(catalog['categories']) if x['id']==rec['category_id']),'artPath':'Kido/Art/'+key,'thumbPath':'Kido/Thumbs/'+key,'regionPath':'Kido/Regions/'+key,'artRevision':1})
    (OUT/'catalog.json').write_text(json.dumps({'canvases':entries},ensure_ascii=False,indent=2)+'\n')
    (ROOT/'docs/ART_VALIDATION.json').write_text(json.dumps(stats,indent=2)+'\n')
    print(f'Built {len(entries)} original vector canvases; regions {min(x["regions"] for x in stats)}–{max(x["regions"] for x in stats)}.')
if __name__=='__main__': main()
