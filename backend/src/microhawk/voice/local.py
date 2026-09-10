import asyncio
import json
import subprocess
from microhawk.config.settings import ROOT

async def listen():
    def capture():
        result=subprocess.run(["powershell.exe","-NoProfile","-ExecutionPolicy","Bypass","-File",str(ROOT/"tools/voice.ps1"),"-Mode","listen"],capture_output=True,text=True,timeout=25,creationflags=subprocess.CREATE_NO_WINDOW)
        if result.returncode: raise RuntimeError(result.stderr.strip()[-1200:])
        return json.loads(result.stdout)
    return await asyncio.to_thread(capture)
