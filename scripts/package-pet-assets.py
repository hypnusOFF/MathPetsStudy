#!/usr/bin/env python3
"""Package the original assets and a standalone, offline HTML preview."""
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

root = Path(__file__).resolve().parents[1]
subprocess.run([sys.executable, str(root / 'scripts/generate-pet-assets.py'), '--check'], check=True)
assets = root / 'src/MathPet.UI/wwwroot/pet-assets'
out = root / 'artifacts'
out.mkdir(exist_ok=True)
shutil.copyfile(assets / 'gallery.html', out / 'mathpet-pet-preview.html')
with zipfile.ZipFile(out / 'mathpet-pet-assets.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(assets.rglob('*')):
        if path.is_file():
            archive.write(path, 'pet-assets/' + str(path.relative_to(assets)))
    archive.write(root / 'docs/pet-assets.md', 'README.md')
    concept = root / 'docs/pet-concept.png'
    if concept.exists():
        archive.write(concept, 'pet-concept.png')
print('Created artifacts/mathpet-pet-assets.zip and artifacts/mathpet-pet-preview.html')
