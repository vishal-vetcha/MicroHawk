import json
import sqlite3
from pathlib import Path

class MissionMemory:
    def __init__(self, root):
        self.root=Path(root);self.root.mkdir(parents=True,exist_ok=True)
        self.db=sqlite3.connect(self.root/"missions.sqlite")
        self.db.execute("CREATE TABLE IF NOT EXISTS missions (id TEXT PRIMARY KEY, record TEXT NOT NULL)")
        self.db.commit()
    def save(self, record):
        raw=json.dumps(record,allow_nan=False)
        self.db.execute("INSERT OR REPLACE INTO missions VALUES (?,?)",(record["id"],raw));self.db.commit()
        path=self.root/record["id"];path.mkdir(exist_ok=True)
        (path/"mission.json").write_text(json.dumps(record,indent=2),encoding="utf-8")
    def last(self):
        row=self.db.execute("SELECT record FROM missions ORDER BY rowid DESC LIMIT 1").fetchone()
        return json.loads(row[0]) if row else None
