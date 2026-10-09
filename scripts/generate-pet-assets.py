#!/usr/bin/env python3
"""Original layered SVG artwork, animation rigs and an offline review page.

No raster tracing, third-party art, fonts, CDN, or image downloads are required.
Artwork uses a fixed 320×360 rig; slot anchors follow the head/body as they move.
"""
from pathlib import Path
import argparse
import html
import json
import math
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'src/MathPet.UI/wwwroot/pet-assets'
INK = '#63473f'
PETS = {
    'cat': dict(name='团团猫', coat='#ffe7c5', shade='#edc992', inner='#f3b8b4', chest='#fff7e7', eye='#43323d'),
    'rabbit': dict(name='糯糯兔', coat='#ffe1df', shade='#efb1b1', inner='#f3b4b1', chest='#fff4ef', eye='#43323d'),
    'fox': dict(name='星星狐', coat='#f8bd81', shade='#e69757', inner='#f4baaa', chest='#fff4df', eye='#43323d')
}
STAGES = {'baby':dict(name='幼年',levels=[1,5],headY=205,bodyY=280,bodyRx=43,bodyRy=44,headRx=65,headRy=58),
          'young':dict(name='成长',levels=[6,12],headY=181,bodyY=266,bodyRx=48,bodyRy=58,headRx=64,headRy=57),
          'mature':dict(name='成熟',levels=[13,20],headY=158,bodyY=252,bodyRx=52,bodyRy=72,headRx=63,headRy=56)}


def ellipse(cx,cy,rx,ry,fill,stroke=True,extra=''):
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="{fill}"'+(f' stroke="{INK}" stroke-width="3"' if stroke else '')+f' {extra}/>'


def path(d,fill='none',stroke=INK,width=3,extra=''):
    return f'<path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="{width}" stroke-linecap="round" stroke-linejoin="round" {extra}/>'


def star(cx,cy,r,color='#f2c265',stroke=True):
    points=[]
    for i in range(10):
        a=math.pi*i/5-math.pi/2;length=r if i%2==0 else r*.46
        points.append(f'{cx+math.cos(a)*length:.2f},{cy+math.sin(a)*length:.2f}')
    return f'<polygon points="{" ".join(points)}" fill="{color}"'+(f' stroke="{INK}" stroke-width="2"' if stroke else '')+'/>'


def wrap(markup,w=320,h=360,title='',style=''):
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" role="img" aria-label="{html.escape(title)}"><title>{html.escape(title)}</title>'+(f'<style>{style}</style>' if style else '')+markup+'</svg>\n'


CSS = '''
.rig,.body,.head,.tail,.ear,.paw,.mouth-chew,.eyes {transform-box:fill-box}
.rig {transform-origin:50% 100%;animation:pet-breathe 3.6s ease-in-out infinite}
.tail {transform-origin:10% 85%;animation:pet-tail 7s ease-in-out infinite}
.ear.left {transform-origin:80% 20%}.ear.right {transform-origin:20% 20%}
.eyes {transform-origin:50% 50%;animation:pet-blink 6.8s linear infinite}
.eyes-happy,.mouth-chew,.effect {opacity:0}.head {transform-origin:50% 85%}
.paw.left {transform-origin:70% 10%}.paw.right {transform-origin:30% 10%}
svg[data-action="pet"] .head {animation:pet-nuzzle 1.2s ease-in-out 1}
svg[data-action="pet"] .eyes,svg[data-action="pet"] .mouth-normal {opacity:0;animation:none}
svg[data-action="pet"] .eyes-happy,svg[data-action="pet"] .hearts {opacity:1}
svg[data-action="feed"] .head {animation:pet-nod 1.9s ease-in-out 1}
svg[data-action="feed"] .mouth-normal {opacity:0}
svg[data-action="feed"] .mouth-chew {opacity:1;animation:pet-chew .35s ease-in-out 5}
svg[data-action="feed"] .food-effect {opacity:1;animation:pet-food 2s ease-out 1 forwards}
svg[data-action="play"] .rig {animation:pet-hop .7s ease-in-out 3}
svg[data-action="play"] .paw.left {animation:pet-wave .6s ease-in-out 3}
svg[data-action="play"] .toy-effect {opacity:1;animation:pet-ball 1.9s ease-in-out 1}
svg[data-action="correct"] .rig {animation:pet-hop .65s ease-in-out 2}
svg[data-action="correct"] .sparkles {opacity:1;animation:pet-sparkle 1.4s ease-out 1}
svg[data-action="encourage"] .head {animation:pet-nuzzle 1.2s ease-in-out 1}
svg[data-action="encourage"] .ear {animation:pet-ear .8s ease-in-out 2}
svg[data-action="upgrade"] .rig {animation:pet-celebrate 2s ease-in-out 1}
svg[data-action="upgrade"] .sparkles,svg[data-action="evolve"] .sparkles {opacity:1;animation:pet-sparkle 2.8s ease-in-out 1}
svg[data-action="evolve"] .rig {animation:pet-evolve 2.8s ease-in-out 1}
@keyframes pet-breathe {0%,100%{transform:scale(1,1)}50%{transform:scale(1.012,1.018)}}
@keyframes pet-tail {0%,70%,100%{transform:rotate(0)}76%{transform:rotate(-7deg)}84%{transform:rotate(5deg)}92%{transform:rotate(-3deg)}}
@keyframes pet-blink {0%,42%,46%,100%{transform:scaleY(1)}44%{transform:scaleY(.06)}}
@keyframes pet-nuzzle {0%,100%{transform:rotate(0)}35%{transform:rotate(-7deg)}70%{transform:rotate(4deg)}}
@keyframes pet-nod {0%,100%{transform:rotate(0) translateY(0)}25%,65%{transform:rotate(3deg) translateY(4px)}45%,85%{transform:rotate(-2deg) translateY(1px)}}
@keyframes pet-chew {50%{transform:scale(1.2,.7)}}
@keyframes pet-hop {0%,100%{transform:translateY(0)}45%{transform:translateY(-12px) rotate(-2deg)}}
@keyframes pet-wave {0%,100%{transform:rotate(0)}50%{transform:rotate(-24deg)}}
@keyframes pet-ear {50%{transform:rotate(5deg)}}
@keyframes pet-celebrate {0%,100%{transform:translateY(0) scale(1)}25%,65%{transform:translateY(-14px) scale(1.035)}45%{transform:translateY(0)}}
@keyframes pet-evolve {0%,100%{opacity:1;transform:scale(1)}40%{opacity:.25;transform:scale(.95)}55%{opacity:.1;transform:scale(1.08)}80%{opacity:1;transform:scale(1.025)}}
@keyframes pet-sparkle {0%{opacity:0}25%,65%{opacity:1}100%{opacity:0}}
@keyframes pet-food {0%{opacity:0}25%,60%{opacity:1}100%{opacity:0}}
@keyframes pet-ball {0%,100%{transform:translateX(0)}35%{transform:translateX(-25px) rotate(-20deg)}70%{transform:translateX(15px) rotate(20deg)}}
svg[data-motion="reduced"] * {animation:none!important}
svg[data-motion="reduced"] .rig {transform:none!important}
@media(prefers-reduced-motion:reduce){.rig,.tail,.ear,.paw,.head,.eyes,.effect,.mouth-chew {animation:none!important}}
'''


def rig(kind,stage):
    p=PETS[kind];s=STAGES[stage];hy=s['headY'];by=s['bodyY'];rx=s['bodyRx'];ry=s['bodyRy'];hr=s['headRy'];coat=p['coat'];shade=p['shade'];chest=p['chest'];inner=p['inner']
    shadow=ellipse(161,337,65 if stage=='baby' else 76,9,'#bbabc7',False,extra='opacity=".23"')
    if kind=='cat':tail=path(f'M{160+rx-6} {by+18} Q284 {by-57} 279 {by-4} Q278 {by+48} {160+rx-4} {by+36}',coat)+path(f'M260 {by-29} Q267 {by-14} 273 {by-11}',stroke=shade,width=9)
    elif kind=='rabbit':tail=ellipse(160+rx+10,by+21,19,20,chest)+ellipse(160+rx+14,by+16,6,6,'#fffaf6',False)
    else:tail=path(f'M{160+rx-10} {by+30} Q252 {by+20} 291 {by-93} Q302 {by-34} 279 {by+10} Q252 {by+51} {160+rx-10} {by+39}',shade)+path(f'M277 {by-47} L291 {by-93} Q300 {by-57} 290 {by-25} L278 {by-32} L271 {by-11} L262 {by-18}',chest,stroke='none')
    torso=ellipse(160,by,rx,ry,coat)+ellipse(160,by+3,rx*.64,ry*.7,chest,False)
    if stage=='mature':
        if kind=='cat':torso+=star(160,by-14,15,'#f2c66e',False)
        elif kind=='rabbit':
            for i in range(5):torso+=ellipse(160+math.cos(i*math.tau/5)*10,by-14+math.sin(i*math.tau/5)*10,8,8,'#f5b7b2',False)
            torso+=ellipse(160,by-14,5,5,'#fff7e7',False)
        else:
            torso+=path(f'M147 {by-19} L163 {by-5} L153 {by+12}',stroke='#e8b96c',width=2)
            for x,y in [(147,by-19),(163,by-5),(153,by+12)]:torso+=star(x,y,6,'#efbf64',False)
    feet=ellipse(136,324,21,10,coat)+ellipse(185,324,21,10,coat)
    if kind=='fox':feet=ellipse(136,324,21,10,'#9d7057')+ellipse(185,324,21,10,'#9d7057')
    if kind=='rabbit':
        ears=[]
        for side in [-1,1]:
            x=160+side*36;ear_y=hy-80
            transform=f'transform="rotate({side*12} {x} {ear_y})"'
            ears.append(ellipse(x,ear_y,17,54,coat,extra=transform)+ellipse(x,ear_y-2,9,42,inner,False,extra=transform))
    else:
        ears=[]
        for side in [-1,1]:
            x=160+side*49;tip=hy-hr-39-(10 if kind=='fox' else 0)
            ears.append(path(f'M{x-side*25} {hy-34} Q{x-side*7} {tip+24} {x+side*10} {tip} Q{x+side*24} {tip+12} {x+side*15} {hy-12}',coat)+path(f'M{x-side*3} {hy-45} L{x+side*9} {tip+17} L{x+side*12} {hy-28}',inner,stroke='none'))
    if kind=='fox':
        face=path(f'M104 {hy-30} Q108 {hy-62} 154 {hy-58} L164 {hy-64} L171 {hy-56} Q206 {hy-55} 218 {hy-30} L231 {hy-13} L222 {hy-7} L239 {hy+9} L226 {hy+15} Q215 {hy+55} 160 {hy+57} Q107 {hy+55} 96 {hy+17} L82 {hy+9} L98 {hy-7} L90 {hy-13} Z',coat)
        face+=path(f'M99 {hy+8} Q130 {hy+26} 160 {hy+7} Q191 {hy+26} 225 {hy+8} Q215 {hy+55} 160 {hy+57} Q108 {hy+54} 99 {hy+8}',chest,stroke='none')
    else:face=ellipse(160,hy,s['headRx'],hr,coat)
    if kind=='cat':
        for x in (145,160,175):face+=path(f'M{x} {hy-hr+5} L{x+2} {hy-hr+19}',stroke=shade,width=5)
        face+=path(f'M105 {hy+14} L87 {hy+10} M105 {hy+25} L88 {hy+28} M215 {hy+14} L234 {hy+10} M215 {hy+25} L232 {hy+28}',stroke=INK,width=2)
    eyes=ellipse(136,hy,6,8,p['eye'],False)+ellipse(184,hy,6,8,p['eye'],False)+ellipse(134,hy-3,1.8,2,'#fff',False)+ellipse(182,hy-3,1.8,2,'#fff',False)
    happy=path(f'M129 {hy+1} Q136 {hy-9} 143 {hy+1} M177 {hy+1} Q184 {hy-9} 191 {hy+1}',width=3)
    blush=ellipse(117,hy+20,10,6,'#f4b4a7',False)+ellipse(204,hy+20,10,6,'#f4b4a7',False)
    mouth=path(f'M154 {hy+14} Q160 {hy+20} 166 {hy+14}',inner,width=2)+path(f'M160 {hy+18} L160 {hy+24} M160 {hy+24} Q151 {hy+35} 146 {hy+25} M160 {hy+24} Q169 {hy+35} 175 {hy+25}',width=2.5)
    chew=ellipse(160,hy+27,6,7,'#805b5a',False)
    paw_y=by+8
    paws=[]
    for x in (130,190):paws.append(ellipse(x,paw_y,13,22,coat)+path(f'M{x-4} {paw_y+13} L{x-4} {paw_y+18} M{x+3} {paw_y+13} L{x+3} {paw_y+18}',width=1.5))
    hearts=''.join(path(f'M{x} {y+4} C{x-15} {y-8} {x-17} {y+13} {x} {y+25} C{x+17} {y+13} {x+15} {y-8} {x} {y+4}', '#eea7b9',stroke='none') for x,y in [(65,145),(253,130)])
    sparkles=''.join(star(x,y,r,'#f2c96e',False) for x,y,r in [(66,hy-42,13),(253,hy-69,14),(62,by+7,8),(257,by+28,10)])
    food=path(f'M232 {hy+46} Q212 {hy+22} 230 {hy+19} Q241 {hy+12} 244 {hy+23} Q263 {hy+17} 261 {hy+35} Q252 {hy+54} 232 {hy+46}', '#efa28e')+path(f'M244 {hy+24} L245 {hy+12}',stroke=INK,width=2)
    ball=ellipse(254,316,19,19,'#ad98df')+path('M240 305 Q266 309 257 334 M245 303 Q243 321 268 322',stroke='#e9e1ff',width=3)
    markup=(f'<g class="shadow" id="shadow">{shadow}</g><g class="rig" id="rig">'
            f'<g class="tail" id="tail">{tail}</g><g id="slot-back"></g>'
            f'<g class="body" id="body">{torso}</g><g id="feet">{feet}</g><g id="slot-body"></g>'
            f'<g class="head" id="head"><g class="ear left" id="ear-left">{ears[0]}</g><g class="ear right" id="ear-right">{ears[1]}</g>'
            f'<g id="face-base">{face}{blush}</g><g class="eyes" id="eyes-open">{eyes}</g><g class="eyes-happy" id="eyes-happy">{happy}</g>'
            f'<g class="mouth-normal" id="mouth-normal">{mouth}</g><g class="mouth-chew" id="mouth-chew">{chew}</g><g id="slot-head"></g></g>'
            f'<g class="paw left" id="paw-left">{paws[0]}</g><g class="paw right" id="paw-right">{paws[1]}</g><g id="slot-accessory"></g></g>'
            f'<g class="effect hearts" id="effect-hearts">{hearts}</g><g class="effect sparkles" id="effect-sparkles">{sparkles}</g>'
            f'<g class="effect food-effect" id="effect-food">{food}</g><g class="effect toy-effect" id="effect-toy">{ball}</g>')
    markup=re.sub(r' id="([^"]+)"',r' data-layer="\1"',markup)
    svg=wrap(markup,title=f'{p["name"]} · {s["name"]}',style=CSS)
    # Coordinate systems for accessories are local 180×180, centered at 90,90.
    anchors={'head':[160,hy-hr+9], 'body':[160,by+3], 'accessory':[160,by-30], 'back':[160,by]}
    scales={'head':[.77,.77], 'body':[rx/70,ry/65], 'accessory':[.62,.62], 'back':[rx/70,ry/65]}
    return svg,anchors,scales


def wearables():
    data={}
    def add(key,name,slot,level,markup):data[key]=dict(name=name,slot=slot,unlockLevel=level,markup=markup)
    add('bow','紫色蝴蝶结','head',1,path('M86 83 Q54 48 49 70 L51 103 Q59 113 86 92 M94 83 Q126 48 131 70 L129 103 Q122 113 94 92','#b6a1e6')+path('M81 96 L66 125 L89 115 L93 98 M98 96 L113 125 L92 115','#cabbef')+ellipse(90,88,10,12,'#9479cc'))
    add('hat','薄荷渔夫帽','head',1,path('M59 84 L65 56 Q90 44 116 56 L123 85 Z','#a5d5c3')+path('M58 82 Q47 95 43 103 Q89 123 139 103 Q133 92 124 82 Z','#bde7d6')+path('M63 78 Q90 87 119 78',stroke='#6fae98',width=4)+star(91,65,8,'#fff1cb',False))
    add('star-clip','星星发夹','head',1,path('M62 103 L123 77',stroke='#ba8760',width=8)+star(93,83,27))
    add('scarf','珊瑚围巾','body',1,path('M30 43 Q89 73 150 43 L149 66 Q88 97 30 66 Z','#f3afa2')+path('M112 69 L127 123 L100 128 L91 78','#f3afa2')+path('M103 115 L106 124 M114 112 L117 124 M125 112 L127 122',stroke='#fff3e7',width=3))
    add('overalls','薄荷背带装','body',6,path('M47 55 L69 55 L70 86 L110 86 L111 55 L133 55 L127 145 L100 145 L100 125 L80 125 L80 145 L53 145 Z','#a5d5c3')+path('M67 87 L113 87 L110 115 L70 115 Z','#cceadb')+ellipse(60,69,5,5,'#f6d683')+ellipse(120,69,5,5,'#f6d683')+star(90,101,8,'#fff4d5',False))
    add('cape','学习小披风','body',6,path('M63 49 Q91 60 119 49 L151 135 Q92 158 31 135 Z','#bca8e5')+path('M65 56 L49 130 M116 55 L134 131',stroke='#e9dfff',width=4)+star(91,70,10,'#f6d683'))
    add('backpack','小书包','back',1,path('M42 59 Q32 48 35 124 M139 60 Q150 49 145 125',stroke='#d7a55e',width=9)+path('M47 43 Q46 29 65 27 L117 27 Q134 29 134 43 L136 139 L46 139 Z','#f4d58f')+path('M53 77 L129 77 L129 129 L53 129 Z','#fff0c7')+path('M57 44 L125 44',stroke='#fff3d5',width=6)+star(91,104,15,'#b8a5e4'))
    add('bell','铃铛项链','accessory',1,path('M33 52 Q91 109 148 52',stroke='#9b806b',width=5)+ellipse(90,104,16,17,'#f4d07a')+path('M78 109 L102 109 M90 112 L90 118',width=2)+ellipse(90,95,3,3,'#fff0ba',False))
    add('badge','星星徽章','accessory',13,path('M81 90 L66 131 L88 121 L96 133 L106 92','#b9a3e0')+star(90,85,25,'#f5d17a'))
    return data


def items():
    data={}
    def add(key,name,category,markup):data[key]=dict(name=name,category=category,markup=markup)
    add('apple','苹果点心','food',path('M86 53 Q53 39 42 69 Q31 104 60 138 Q74 151 89 142 Q110 155 132 119 Q153 82 131 62 Q111 44 95 56','#f09d8b')+path('M91 58 Q89 43 97 29',width=5)+path('M99 39 Q113 20 137 34 Q125 53 99 39','#b1cba0')+path('M60 72 Q48 92 62 108',stroke='#fff0de',width=6))
    add('carrot','胡萝卜点心','food',path('M65 57 Q88 37 118 61 Q126 93 75 144 Q57 145 58 126 Z','#f5be7f')+path('M87 54 Q64 25 80 23 Q92 22 96 45 M94 48 Q99 14 114 25 Q122 34 105 56',stroke='#93b898',width=10)+path('M64 82 L81 90 M72 107 L88 113',stroke='#d99055',width=4))
    add('biscuit','星星饼干','food',star(90,91,58,'#edcb90')+star(90,91,45,'#ffe2ac',False)+ellipse(74,84,4,4,'#b88d67',False)+ellipse(102,106,4,4,'#b88d67',False)+ellipse(101,69,3,3,'#b88d67',False))
    add('water','清水','care',path('M90 26 Q140 94 135 118 Q129 153 90 154 Q48 151 45 118 Q42 92 90 26','#b8dce6')+path('M64 109 Q61 129 80 134',stroke='#effaff',width=7))
    add('yarn','彩色毛线球','toy',ellipse(84,86,50,50,'#b4a0df')+path('M53 53 Q99 87 105 128 M38 76 Q89 65 122 102 M46 111 Q78 86 99 40 M89 135 Q93 158 136 144',stroke='#e9dffc',width=5))
    add('ball','彩虹皮球','toy',ellipse(90,91,55,55,'#b6d8c5')+path('M90 36 Q54 91 90 146 Q129 91 90 36','#efbbaf')+path('M37 91 L143 91',stroke='#fff1ce',width=8))
    add('wand','星星逗宠棒','toy',path('M58 146 L114 54',stroke='#a384bc',width=10)+path('M113 52 Q147 72 130 107',stroke='#9479aa',width=3)+star(128,111,22,'#f4d181')+star(115,49,25,'#d5c4ef'))
    add('brush','软毛梳','care',path('M72 89 L58 147 Q55 161 76 158 L93 105','#c1b0e7')+path('M68 44 Q94 30 119 46 L128 90 Q102 112 73 95 Z','#e2d6f7')+path('M80 51 L86 86 M96 48 L101 88 M112 51 L114 84',stroke='#a998c6',width=5))
    add('book','学习小书','learning',path('M27 44 Q58 35 90 52 Q123 35 153 44 L153 134 Q119 129 90 145 Q57 128 27 134 Z','#fff1cc')+path('M90 53 L90 144 M41 65 L73 72 M41 88 L73 94 M107 71 L137 64',stroke='#ad97d1',width=3)+star(121,108,15,'#ebc771',False))
    add('coin','学习积分','reward',ellipse(90,92,56,56,'#f2d187')+ellipse(90,92,43,43,'#ffe8b1')+star(90,92,27,'#d8a558',False))
    add('heart','心情','status',path('M90 146 C-5 95 38 20 90 63 C145 18 188 98 90 146','#efb4bd'))
    add('food-bowl','饱腹','status',path('M33 82 L147 82 L136 128 Q91 151 45 128 Z','#b7d8c5')+ellipse(90,82,57,16,'#e4ead1')+ellipse(90,79,41,9,'#d7b38a',False))
    add('experience','成长经验','status',path('M81 137 L82 87 M82 100 Q36 101 45 61 Q81 55 86 91 M85 77 Q115 31 143 56 Q143 91 85 88',fill='#b8d5a6',stroke='#7b9c70',width=4)+star(132,34,10,'#f3d187',False))
    add('main-pet','主力宠物','status',path('M40 60 L61 91 L89 45 L117 91 L142 60 L130 128 L51 128 Z','#f4d391')+ellipse(89,45,6,6,'#fff0c8')+ellipse(40,60,5,5,'#bda7df')+ellipse(142,60,5,5,'#bda7df'))
    return data


def room(theme):
    # 960×600, central stage kept free for a pet at x=480, floor y=490.
    palettes={'study':('#f6f0e7','#e4d3ba','#b9d5cf'),'spring':('#f4f1e7','#ddd5b6','#c1d9a9'),'stars':('#ece8f5','#c6bcdf','#afa0d3')}
    wall,floor,accent=palettes[theme]
    art=f'<rect width="960" height="600" fill="{wall}"/><rect y="440" width="960" height="160" fill="{floor}"/>'+path('M0 440 H960',stroke='#c4b49e',width=3)
    art+=path('M92 88 L287 88 L287 314 L92 314 Z','#fffcf4')+path('M104 100 H275 V301 H104 Z','#d6e6e0',width=2)+path('M189 100 V301 M104 202 H275',stroke='#fffcf4',width=10)
    art+=ellipse(210,149,24,24,'#fff0b0',False)+path('M104 267 Q157 218 191 264 Q232 222 275 263 L275 301 L104 301 Z',accent,stroke='none')
    art+=path('M96 79 H284',stroke=accent,width=15)+path('M83 88 Q68 185 85 309 L111 309 L111 88 M268 88 L268 309 L296 309 Q312 174 294 88',accent,width=2)
    art+=ellipse(485,507,219,45,'#fff4dd',False)+ellipse(485,507,192,34,'#eee2cc',False)
    art+=path('M704 145 H873 V430 H704 Z','#f8e5c7')+path('M704 238 H873 M704 335 H873',stroke='#c8a98b',width=7)
    for x,h,color in [(721,52,'#b5a2d8'),(743,67,'#b5d3bb'),(767,58,'#efb4a6'),(790,49,'#e7cc83')]:art+=path(f'M{x} 238 V{238-h} H{x+17} V238',color,width=2)
    art+=star(790,293,22,'#edd08a')+ellipse(842,312,14,14,'#beaadc')
    art+=path('M759 373 L820 373 L831 419 L748 419 Z','#b8d5b8')+path('M790 374 V347',stroke='#799c75',width=4)
    if theme=='spring':
        for x,y in [(70,411),(898,403),(342,112)]:
            for i in range(5):art+=ellipse(x+math.cos(i*math.tau/5)*15,y+math.sin(i*math.tau/5)*15,11,11,'#eebbb6',False)
            art+=ellipse(x,y,9,9,'#f4d390',False)
    elif theme=='stars':
        for x,y,r in [(352,103,20),(449,67,14),(568,119,22),(642,83,12)]:art+=path(f'M{x} 0 V{y-r}',stroke='#c2b0dc',width=2)+star(x,y,r,'#f3d796',False)
        art+=path('M791 286 Q817 252 830 287 Q802 296 791 286','#fff2c7',stroke='none')
    else:art+=path('M384 111 H584 V255 H384 Z','#e0d8ed')+path('M397 123 H571 V243 H397 Z','#fff9ef',width=1)+path('M419 161 H551 M419 191 H523 M419 220 H541',stroke='#b7cdb7',width=5)
    return wrap(art,960,600,{'study':'温暖学习角','spring':'春日小屋','stars':'星光小屋'}[theme])


def gallery(manifest):
    # Inline files give the user a single HTML file they can open without a server.
    rigs={p['id']: (OUT/p['path']).read_text() for p in manifest['pets']}
    clothes={w['id']: (OUT/w['path']).read_text() for w in manifest['wearables']}
    rooms={r['id']: (OUT/r['path']).read_text() for r in manifest['rooms']}
    icons={i['id']: (OUT/i['path']).read_text() for i in manifest['items']}
    payload=json.dumps(dict(manifest=manifest,rigs=rigs,clothes=clothes,rooms=rooms,icons=icons),ensure_ascii=False).replace('</',r'<\/')
    return r'''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>数学宠物岛 · 宠物资源预览</title>
<style>
*{box-sizing:border-box}body{margin:0;color:#4c3c4b;background:#faf8f2;font:15px/1.6 system-ui,-apple-system,sans-serif}main{max-width:1180px;margin:auto;padding:42px 26px 70px}header{display:flex;justify-content:space-between;gap:20px;align-items:center}h1{font-size:32px;letter-spacing:-1px;margin:5px 0}h2{font-size:23px;margin:32px 0 15px}p{color:#8e8292;margin:7px 0}.eyebrow{font-size:11px;letter-spacing:3px;color:#9b89b6}.pill{background:#eee7f9;color:#8b71b6;padding:7px 15px;border-radius:30px;font-size:12px}.lab{display:grid;grid-template-columns:1.45fr 1fr;gap:24px;margin-top:28px}.scene{position:relative;aspect-ratio:8/5;border-radius:28px;overflow:hidden;background:#f6efe6;border:1px solid #e9dfd0}.scene>svg{width:100%;height:100%;display:block}.actor{position:absolute;left:50%;bottom:14%;width:33%;transform:translateX(-50%)}.actor>svg{width:100%;display:block}.panel{background:#fff;border:1px solid #ede5ef;border-radius:24px;padding:23px}.controls{display:flex;gap:8px;flex-wrap:wrap;margin:10px 0 19px}button,select{font:inherit;color:#6c547e;background:#f3edf9;border:1px solid #e3d9ed;padding:8px 12px;border-radius:12px;cursor:pointer}button.active{background:#947ac5;color:white;border-color:#947ac5}button:disabled{opacity:.5;cursor:default}label{font-size:12px;font-weight:600;color:#8f7a9c}select{max-width:100%}.row{display:flex;gap:12px;flex-wrap:wrap}.row>div{flex:1;min-width:125px}.status{font-size:12px;background:#faf5e9;padding:11px;border-radius:12px}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}.card{background:#fff;border:1px solid #ede5ef;border-radius:22px;padding:15px;text-align:center}.card svg{height:220px;max-width:100%}.card strong{display:block}.card small{color:#9b8a9c}.stages{display:grid;grid-template-columns:repeat(3,1fr);margin:8px 0;color:#a592ad;text-align:center;font-size:12px}.inventory{display:grid;grid-template-columns:repeat(7,1fr);gap:12px}.item{background:#fff;border-radius:18px;border:1px solid #ede5ef;padding:10px;text-align:center;font-size:12px}.item svg{width:100%;height:90px}.rooms{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}.rooms svg{width:100%;display:block;border-radius:16px}.note{font-size:12px;margin-top:20px;max-width:850px}.scene,.card,.item{contain:layout}footer{margin-top:32px;color:#a393a7;font-size:12px}
@media(max-width:720px){main{padding:24px 16px 50px}h1{font-size:25px}header{display:block}.pill{display:inline-block;margin-top:10px}.lab{grid-template-columns:1fr}.grid{gap:8px}.card{padding:8px}.card svg{height:140px}.card strong{font-size:12px}.card small{font-size:10px}.inventory{grid-template-columns:repeat(3,1fr)}.rooms{grid-template-columns:1fr}.panel{padding:18px}}
</style><main><header><div><div class="eyebrow">MATH & LITTLE FRIENDS · ORIGINAL ASSET KIT</div><h1>小伙伴，陪你一点点长大。</h1><p>三只宠物 · 三个成长阶段 · 独立穿搭与轻量互动</p></div><span class="pill">透明 SVG · 离线可用</span></header>
<div class="lab"><div><div class="scene" id="scene"></div><p id="caption" aria-live="polite"></p></div><div class="panel"><label>选择伙伴</label><div class="controls" id="species"></div><label>成长阶段</label><div class="controls" id="stages"></div><div class="row"><div><label>头饰</label><br><select id="head"></select></div><div><label>衣服</label><br><select id="body"></select></div><div><label>随身饰品</label><br><select id="accessory"></select></div></div><label>家园</label><div class="controls" id="homes"></div><label>互动动作</label><div class="controls" id="actions"></div><label><input type="checkbox" id="reduced"> 减少动画</label><div class="status" id="status" role="status">呼吸与眨眼正在播放。选择一个动作试试看。</div></div></div>
<h2>九种成长形态</h2><div class="stages"><span>幼年 · Lv.1–5</span><span>成长 · Lv.6–12</span><span>成熟 · Lv.13–20</span></div><div class="grid" id="catalog"></div>
<h2>换装与背包</h2><p>衣服随身体缩放，头饰随头部动作，书包放在身体后方。</p><div class="inventory" id="wardrobe"></div>
<h2>食物、玩具与状态图标</h2><div class="inventory" id="items"></div><h2>三套家园</h2><div class="rooms" id="roomlist"></div>
<p class="note">这是资源与动画预览。等级解锁、积分购买和三只宠物独立存档由游戏系统控制，此处可以自由试穿。进化预览在动画中段替换身体形态，衣服跟随新的锚点重新适配。</p><footer>数学宠物岛 · 原创矢量资源第一版 / 未使用外部字体、CDN 或第三方角色</footer></main><script>
const D=PAYLOAD;const M=D.manifest;let kind='cat',stage='baby',room='study',currentAction='idle',timer=null,evolutionTimer=null,busy=false;const wearing={head:'bow',body:'',accessory:''};
const $=id=>document.getElementById(id);const strip=svg=>svg.replace(/^.*?<svg[^>]*>/s,'').replace(/<\/svg>\s*$/,'').replace(/<title>.*?<\/title>/s,'');
function button(text,click,parent,value,selected){const b=document.createElement('button');b.textContent=text;b.dataset.value=value;b.className=value===selected?'active':'';b.onclick=click;parent.append(b);return b}
function buildControls(){for(const[k,name]of Object.entries(M.species))button(name,()=>{stop();kind=k;draw();},$('species'),k,kind);for(const[k,s]of Object.entries(M.stages))button(s.name,()=>{stop();stage=k;draw();},$('stages'),k,stage);for(const r of M.rooms)button(r.name,()=>{room=r.id;draw();},$('homes'),r.id,room);for(const a of M.actions)button(a.name,()=>act(a.id),$('actions'),a.id,'idle');for(const slot of ['head','body','accessory']){$(slot).add(new Option('不穿戴',''));for(const w of M.wearables.filter(w=>w.slot===slot||slot==='accessory'&&w.slot==='back'))$(slot).add(new Option(w.name,w.id));$(slot).value=wearing[slot];$(slot).onchange=()=>{wearing[slot]=$(slot).value;draw()};}for(const p of M.pets){const div=document.createElement('div');div.className='card';div.innerHTML=D.rigs[p.id]+'<strong>'+M.species[p.species]+'</strong><small>'+M.stages[p.stage].name+' · Lv.'+p.levels.join('–')+'</small>';$('catalog').append(div);}for(const w of M.wearables){const div=document.createElement('div');div.className='item';div.innerHTML=D.clothes[w.id]+'<div>'+w.name+'</div>';$('wardrobe').append(div)}for(const i of M.items){const div=document.createElement('div');div.className='item';div.innerHTML=D.icons[i.id]+'<div>'+i.name+'</div>';$('items').append(div)}for(const r of M.rooms){const div=document.createElement('div');div.innerHTML=D.rooms[r.id]+'<small>'+r.name+'</small>';$('roomlist').append(div)}}
function draw(){const p=M.pets.find(p=>p.species===kind&&p.stage===stage);$('scene').innerHTML=D.rooms[room]+'<div class="actor">'+D.rigs[p.id]+'</div>';const svg=$('scene').querySelector('.actor svg');svg.dataset.action=currentAction;svg.dataset.motion=$('reduced').checked?'reduced':'full';for(const id of Object.values(wearing)){const w=M.wearables.find(w=>w.id===id);if(!w)continue;const anchor=p.anchors[w.slot],sc=p.scales[w.slot];const slot=svg.querySelector('[data-layer="slot-'+w.slot+'"]');slot.innerHTML='<g transform="translate('+anchor.join(' ')+') scale('+sc.join(' ')+') translate(-90 -90)">'+strip(D.clothes[id])+'</g>';}for(const[id,val]of [['species',kind],['stages',stage],['homes',room],['actions',currentAction]])for(const b of $(id).querySelectorAll('button'))b.classList.toggle('active',b.dataset.value===val);$('caption').textContent=M.species[kind]+' · '+M.stages[stage].name+' / '+M.stages[stage].levels.join('–')+'级';for(const b of $('actions').querySelectorAll('button'))b.disabled=busy;}
function stop(){clearTimeout(timer);clearTimeout(evolutionTimer);busy=false;currentAction='idle'}
function act(id){if(busy)return;if(id==='idle'){stop();draw();return}const a=M.actions.find(a=>a.id===id);busy=true;currentAction=id;draw();$('status').textContent=a.name+' · '+(a.durationMs/1000)+'秒';if(id==='evolve'){evolutionTimer=setTimeout(()=>{stage=stage==='baby'?'young':'mature';draw()},1400)}timer=setTimeout(()=>{stop();draw();$('status').textContent='动作完成，回到呼吸态。'},a.durationMs)}
$('reduced').onchange=()=>draw();buildControls();draw();
</script></html>'''.replace('PAYLOAD',payload)


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--check',action='store_true');args=parser.parse_args()
    files={};manifest=dict(version=1,viewBox=[0,0,320,360],species={k:v['name'] for k,v in PETS.items()},stages=STAGES,pets=[],wearables=[],items=[],rooms=[],actions=[])
    def output(relative,text):files[relative]=text
    for kind in PETS:
        for stage in STAGES:
            svg,anchors,scales=rig(kind,stage);asset_id=f'{kind}-{stage}';relative=f'pets/{asset_id}.svg';output(relative,svg)
            manifest['pets'].append(dict(id=asset_id,species=kind,stage=stage,path=relative,levels=STAGES[stage]['levels'],anchors=anchors,scales=scales,
                                         layers=['shadow','tail','slot-back','body','feet','slot-body','head','ear-left','ear-right','face-base','eyes-open','eyes-happy','mouth-normal','mouth-chew','slot-head','paw-left','paw-right','slot-accessory','effect-hearts','effect-sparkles','effect-food','effect-toy']))
    for key,w in wearables().items():
        relative=f'wearables/{key}.svg';output(relative,wrap(w['markup'],180,180,w['name']));manifest['wearables'].append(dict(id=key,path=relative,**{k:v for k,v in w.items() if k!='markup'}))
    for key,i in items().items():
        relative=f'items/{key}.svg';output(relative,wrap(i['markup'],180,180,i['name']));manifest['items'].append(dict(id=key,path=relative,**{k:v for k,v in i.items() if k!='markup'}))
    for key,name in [('study','温暖学习角'),('spring','春日小屋'),('stars','星光小屋')]:
        relative=f'rooms/{key}.svg';output(relative,room(key));manifest['rooms'].append(dict(id=key,name=name,path=relative,petAnchor=[480,490]))
    for key,name,duration in [('idle','呼吸待机',0),('pet','摸摸',1200),('feed','喂食',2000),('play','玩耍',2100),('correct','答对庆祝',1400),('encourage','温柔鼓励',1600),('upgrade','升级庆祝',2000),('evolve','进化',2800)]:manifest['actions'].append(dict(id=key,name=name,durationMs=duration))
    output('pet-animations.css',CSS.strip()+'\n');output('manifest.json',json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    for relative,text in files.items():
        if relative.endswith('.svg'):ET.fromstring(text)
        dest=OUT/relative
        if args.check:assert dest.read_text()==text,f'Outdated asset: {relative}'
        else:dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(text)
    # Single-file interactive gallery is built from the same assets, no fetch required.
    page=gallery(manifest)
    dest=OUT/'gallery.html'
    if args.check:assert dest.read_text()==page,'Outdated gallery'
    else:dest.write_text(page)
    print(f'Validated {len(manifest["pets"])} layered pets, {len(manifest["wearables"])} wearables, {len(manifest["items"])} item icons, {len(manifest["rooms"])} rooms, {len(manifest["actions"])} animation states.')


if __name__=='__main__':main()
