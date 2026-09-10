"""Reports are facts from execution and sensor candidates, never inferred success."""
def report(record):
    findings=record.get("detections",[]);events=record.get("events",[])
    damage=[d for d in findings if d["category"]=="damage"]
    final=record.get("final_telemetry") or {}
    parts=[f"Mission {record['status']}. Final flight state: {final.get('state','unavailable')}."]
    if events:
        parts.extend(f"Observed track {e['track_id'][:8]} in {e['zone']} for {e['dwell_seconds']:.1f} simulation seconds; conditional policy triggered investigation." for e in events if e["type"]=="RestrictedZoneDwell")
    if damage: parts.append(f"Camera imagery contains {len(damage)} visible damage candidates across captured frames; these are simulation-calibrated visual findings, not calibrated real-world cracks.")
    markers=[d for d in findings if d["category"]=="inspection_marker"]
    vehicles=[d for d in findings if d["category"]=="vehicle"]
    if markers: parts.append("Numbered-sign visual candidates are present in the captured inspection view; exact marker text was not decoded.")
    if vehicles: parts.append(f"Camera imagery contains {len(vehicles)} vehicle candidates across frames; review evidence for confirmation.")
    if not events and not damage: parts.append("No qualifying dwell event or damage candidate was established in this mission.")
    if record.get("failure"): parts.append("Execution reason: "+str(record["failure"]))
    parts.append(f"Evidence frames stored: {len(record.get('evidence',[]))}.")
    return " ".join(parts)

def explain(record, events=None):
    safety=[e for e in (events or []) if e.get("reason") in {"LowBattery","CriticalBattery","ConnectionLoss"} and e.get("status")=="Accepted"]
    if safety:
        last=safety[-1]
        return f"Unity safety initiated an override at tick {last['tick']} because of {last['reason']}. The AI cannot override that policy."
    if not record:return "No recorded mission is available."
    return " ".join(d["reason"] for d in record.get("decisions",[])[-6:]) or record.get("report","No decision recorded.")
