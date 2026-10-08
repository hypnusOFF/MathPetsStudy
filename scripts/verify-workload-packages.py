"""Verify installed workload archives; surface failures in Actions annotations."""
import os
from pathlib import Path
import subprocess
import sys

root=Path(os.environ['DOTNET_ROOT'])
packages=sorted({p for directory in ['packs','template-packs','library-packs']
                 for p in (root/directory).rglob('*.nupkg')})
if not packages:
    print('::error::No installed workload package archives were found.')
    sys.exit(1)
failed=[]
for package in packages:
    result=subprocess.run(['dotnet','nuget','verify','--all',str(package)],capture_output=True,text=True)
    if result.returncode:
        failed.append(package.name)
        detail=(result.stdout+result.stderr)[-7000:]
        detail=detail.replace('%','%25').replace('\r','%0D').replace('\n','%0A')
        print(f'::error title=Signature verification failed for {package.name}::{detail}',flush=True)
    else:
        print(f'Verified {package}',flush=True)
if failed:
    print(f'Signature verification failed for {len(failed)} of {len(packages)} packages.')
    sys.exit(1)
print(f'All {len(packages)} package signatures verified.')
