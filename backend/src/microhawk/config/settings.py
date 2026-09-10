import os
import secrets
from pathlib import Path
ROOT=Path(os.environ.get("MICROHAWK_ROOT",Path(__file__).resolve().parents[4]))
DATA=ROOT/"runtime-data"
DATA.mkdir(exist_ok=True)
TOKEN_PATH=DATA/"operator.token"
if not TOKEN_PATH.exists(): TOKEN_PATH.write_text(secrets.token_hex(32))
TOKEN=TOKEN_PATH.read_text().strip()
