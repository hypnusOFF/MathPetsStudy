"""Install SDK archives from Google's HTTPS repository and verify published checksums."""
import hashlib
import os
from pathlib import Path
import shutil
import sys
import tempfile
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

try:
    sdk=Path(os.environ['ANDROID_HOME']).resolve()
    sdk.mkdir(parents=True,exist_ok=True)
    with urllib.request.urlopen('https://dl.google.com/android/repository/repository2-3.xml',timeout=120) as response:
        repository=ET.fromstring(response.read())
    packages=[('platforms;android-36','platforms/android-36'),
              ('build-tools;36.0.0','build-tools/36.0.0'),
              ('platform-tools','platform-tools'),
              ('cmdline-tools;latest','cmdline-tools/latest')]
    with tempfile.TemporaryDirectory(prefix='mathpet-sdk-') as temporary:
        for identity,relative in packages:
            package=next(p for p in repository.findall('remotePackage') if p.get('path')==identity)
            entry=next(a for a in package.findall('./archives/archive') if a.findtext('host-os') in [None,'linux'])
            url='https://dl.google.com/android/repository/'+entry.findtext('./complete/url')
            archive=Path(temporary)/(identity.replace(';','-')+'.zip')
            urllib.request.urlretrieve(url,archive)
            expected=entry.find('./complete/checksum')
            with archive.open('rb') as stream:
                actual=hashlib.file_digest(stream,expected.get('type')).hexdigest()
            if actual.lower()!=expected.text.strip().lower():
                raise RuntimeError('SDK checksum mismatch: '+identity)
            destination=(sdk/relative).resolve()
            with zipfile.ZipFile(archive) as z:
                for member in z.infolist():
                    # Each official archive has a single top-level package directory.
                    name='/'.join(member.filename.split('/')[1:])
                    if not name:
                        continue
                    target=(destination/name).resolve()
                    if destination not in target.parents:
                        raise RuntimeError('Unsafe SDK archive path')
                    if member.is_dir():
                        target.mkdir(parents=True,exist_ok=True)
                        continue
                    target.parent.mkdir(parents=True,exist_ok=True)
                    with z.open(member) as source,target.open('wb') as output:
                        shutil.copyfileobj(source,output)
                    mode=member.external_attr>>16
                    if mode:
                        target.chmod(mode&0o777)
            print('Verified and installed '+identity,flush=True)
    print('Android SDK ready: '+str(sdk))
except Exception as error:
    message=str(error).replace('%','%25').replace('\r','%0D').replace('\n','%0A')
    print('::error title=Android SDK preparation failed::'+message,flush=True)
    sys.exit(1)
