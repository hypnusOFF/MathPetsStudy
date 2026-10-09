#!/usr/bin/env python3
"""Compute CSS display alignment from recorded bounds; never edit PNG pixels."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1]
assets = root / 'src/MathPet.UI/wwwroot/pet-animation'
manifest = json.loads((assets / 'manifest.json').read_text())
rules = []
for sprite in manifest['sprites']:
    w, h = sprite['size']
    x0, y0, x1, y1 = sprite['alphaBounds']
    scale = min(.82 / (y1-y0), .90 / (x1-x0))
    width, height = w*scale*100, h*scale*100
    left, top = 50-(x0+x1)/2*scale*100, 92-y1*scale*100
    rules.append(f'.pet-motion .sprite-{sprite["id"]}{{position:absolute!important;width:{width:.4f}%!important;height:{height:.4f}%!important;left:{left:.4f}%;top:{top:.4f}%;object-fit:fill;transform-origin:{(x0+x1)/2/w*100:.4f}% {y1/h*100:.4f}%}}')
(assets / 'sprite-layout.css').write_text('\n'.join(rules)+'\n')
print('Aligned nine sprites with CSS; source PNG pixels unchanged.')
