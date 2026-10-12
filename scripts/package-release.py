"""Package a signed APK and official version manifest, without credentials."""
import hashlib, json, os, pathlib, re, zipfile
version = os.environ['MATHPET_VERSION']
code = int(os.environ['MATHPET_VERSION_CODE'])
if not re.fullmatch(r'\d+\.\d+\.\d+', version) or not 0 < code <= 2100000000:
    raise ValueError('Invalid Android version')
root = pathlib.Path('artifacts')
apk = root / f'mathpet-{version}.apk'
digest = hashlib.sha256(apk.read_bytes()).hexdigest()
checksum = apk.with_suffix('.apk.sha256')
checksum.write_text(f'{digest}  {apk.name}\n')
manifest = root / 'update.json'
manifest.write_text(json.dumps(dict(versionCode=code, versionName=version,
    apkUrl=f'https://github.com/hypnusOFF/MathPetsStudy/releases/download/v{version}/{apk.name}',
    sha256=digest), indent=2) + '\n')
with zipfile.ZipFile(root / f'mathpet-{version}-android.zip', 'w', zipfile.ZIP_STORED) as archive:
    for path in (apk, checksum, manifest, pathlib.Path('LICENSE')):
        archive.write(path, path.name)
