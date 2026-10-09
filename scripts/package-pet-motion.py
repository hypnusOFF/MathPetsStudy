#!/usr/bin/env python3
"""Verify unchanged generated sprites and package an offline motion review page."""
from pathlib import Path
import base64
import hashlib
import json
import zipfile

root = Path(__file__).resolve().parents[1]
assets = root / 'src/MathPet.UI/wwwroot/pet-animation'
manifest = json.loads((assets / 'manifest.json').read_text())
payload = {k: manifest[k] for k in ('species', 'stages', 'actions')}
payload['sprites'] = {}
for sprite in manifest['sprites']:
    data = (assets / sprite['path']).read_bytes()
    assert hashlib.sha256(data).hexdigest() == sprite['sha256'], f"Changed sprite: {sprite['id']}"
    payload['sprites'][sprite['id']] = 'data:image/png;base64,' + base64.b64encode(data).decode()
for key, file in [('food', 'apple.svg'), ('toy', 'ball.svg')]:
    data = (root / 'src/MathPet.UI/wwwroot/pet-assets/items' / file).read_bytes()
    payload[key] = 'data:image/svg+xml;base64,' + base64.b64encode(data).decode()
page = (root / 'scripts/pet-motion-preview.template.html').read_text()
page = page.replace('__MOTION_CSS__', (assets / 'motion.css').read_text().replace('@import url("sprite-layout.css");', (assets / 'sprite-layout.css').read_text()))
page = page.replace('__PAYLOAD__', json.dumps(payload, ensure_ascii=False))
out = root / 'artifacts'
out.mkdir(exist_ok=True)
(out / 'mathpet-motion-preview.html').write_text(page)
with zipfile.ZipFile(out / 'mathpet-motion-assets.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(assets.rglob('*')):
        if path.is_file():
            archive.write(path, 'pet-animation/' + str(path.relative_to(assets)))
    archive.write(out / 'mathpet-motion-preview.html', 'mathpet-motion-preview.html')
    archive.write(root / 'docs/pet-motion.md', 'README.md')
    for file in ['apple.svg', 'ball.svg']:
        archive.write(root / 'src/MathPet.UI/wwwroot/pet-assets/items' / file, 'effects/' + file)
print(f"Verified {len(payload['sprites'])} transparent sprites, packaged offline motion preview and assets.")
