"""Keep the command's exit status and expose failure details in Actions annotations."""
import subprocess
import sys

result=subprocess.run(sys.argv[1:],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
print(result.stdout,flush=True)
if result.returncode:
    tail=result.stdout[-7000:]
    escaped=tail.replace('%','%25').replace('\r','%0D').replace('\n','%0A')
    print('::error title=Build command failed::'+escaped,flush=True)
sys.exit(result.returncode)
