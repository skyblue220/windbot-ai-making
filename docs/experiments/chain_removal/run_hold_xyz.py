# Offline dumps behind docs/CHAIN_REMOVAL.md 4 (2026-11-11): Plasma Hole (2) quick Xyz summon on the opponent's turn, and the
# hold modes of a detach-paid removal (Detonator (1)): reserve / phase / hand / future.
# Run: python run_hold_xyz.py   (env PROJECTIGNIS = ProjectIgnis folder; default ../../../../ProjectIgnis)
# Needs the deployed WindBot.exe in ProjectIgnis/WindBot. Writes out/xyz_*.md, out/hold_modes.md and hold_xyz.json next to this file.
import json, base64, os, subprocess, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out"); os.makedirs(OUT, exist_ok=True)
PI = os.environ.get("PROJECTIGNIS", os.path.normpath(os.path.join(HERE, "..", "..", "..", "..", "ProjectIgnis")))
paths = [os.path.join(PI, 'expansions', 'cards.cdb'), os.path.join(PI, 'repositories', 'delta-bagooska', 'cards.delta.cdb')]
B = base64.b64encode(json.dumps(paths).encode()).decode()
if hasattr(sys.stdout, "reconfigure"): sys.stdout.reconfigure(encoding="utf-8")

ICE, SWORD, DUO, DET, EXT, F0 = 8633261, 35844557, 7511613, 34909328, 34022970, 41522092
def L(*x): return ",".join(str(i) for i in x)

def dump(name, extra):
    out = os.path.join(OUT, name + ".md")
    args = [os.path.join(PI, "WindBot", "WindBot.exe"), "AssetPath=./WindBot", "DbPaths=" + B,
            "AgentDbDump=AI_Ryzeal", "AgentOpponentDeck=AI_Ryzeal", "AgentDbInterrupts=false", "AgentDbRemoval=true",
            "AgentDbOut=" + out] + ["%s=%s" % kv for kv in extra.items()]
    subprocess.run(args, cwd=PI, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=300)
    txt = open(out, encoding="utf-8").read()
    txt = txt[txt.find("# 체인 중 제거 시험"):]
    open(out, "w", encoding="utf-8", newline="\n").write(txt)
    return txt

results = {"xyz": {}, "hold": {}}

# 1. Plasma Hole (2): our board on their turn
xyz_cases = {
    "xyz_two_det": L("%d:2" % DET, "%d:1" % DET),       # two Rank 4 Xyz: F0
    "xyz_det_duo": L("%d:3" % DET, "%d:0" % DUO),       # Detonator 3 materials + spent Duo Drive
    "xyz_one_det": "%d:2" % DET,                        # one Xyz, nothing to pair with (Ray Lancer needs a hand Spell/Trap)
    "xyz_det_ice": L("%d:2" % DET, ICE, SWORD),         # Detonator + two Level 4 Ryzeal left on the field
    "xyz_two_det_spent": L("%d:0" % DET, "%d:0" % DET), # both Detonators spent their materials this turn: F0
    "xyz_det1_duo0": L("%d:1" % DET, "%d:0" % DUO),     # one material left on Detonator, spent Duo Drive: F0
}
for k, field in xyz_cases.items():
    txt = dump(k, {"AgentDbOurField": field, "AgentDbOppField": "%d:2" % DET})
    sec = txt[txt.find("## 상대 턴 엑시즈 소환"):]
    sec = sec[:sec.find("\n## ", 5)]
    lines = [l for l in sec.splitlines()[2:] if l.strip()]
    results["xyz"][k] = lines
    print("==", k, field)
    for l in lines: print("  ", l[:300])

# 2. Hold modes for Detonator (1): Ext Ryzeal (2) on the chain (Ext body 25) beside an untouched Detonator of theirs
modes = ["reserve", "phase", "hand", "future"]
hold_cases = [
    # (name, uses left, turn left, opponent hand)
    ("uses3_early_hand5", 3, 1.0, 5),
    ("uses2_early_hand5", 2, 1.0, 5),
    ("uses1_early_hand5", 1, 1.0, 5),
    ("uses1_early_hand1", 1, 1.0, 1),
    ("uses1_main2_hand1", 1, 0.3, 1),
    ("uses2_main2_hand0", 2, 0.3, 0),
]
fields = {"ext": EXT, "ext_det": L(EXT, "%d:2" % DET)}
rows = []
for fname, ofield in fields.items():
  for name0, uses, left, hand in hold_cases:
    name = fname + "_" + name0
    for m in modes:
        txt = dump("hold_" + name + "_" + m, {"AgentDbOppField": ofield, "AgentDbOppChain": "%d:2" % EXT, "AgentDbOurXyz": "1",
                                            "AgentDbOurDetachUses": str(uses), "AgentDbTurnLeft": str(left), "AgentDbOppHand": str(hand),
                                            "AgentRemovalHold": m})
        line = next((l for l in txt.splitlines() if l.startswith("- Ryzeal Detonator 효과 1")), "")
        mm = re.search(r"\]: (발동|보류): 얻는 값 ([\d.]+) vs 아껴 둘 값 ([\d.]+)(?: \(([^)]*)\))?", line)
        fut = next((l for l in txt.splitlines() if l.startswith("이번 턴 나올 대상")), "")
        row = {"case": name, "mode": m, "fire": mm.group(1) if mm else "?", "gain": mm.group(2) if mm else "", "hold": mm.group(3) if mm else "",
               "note": mm.group(4) if mm and mm.group(4) else "", "future": fut}
        rows.append(row)
        print(name, m, row["fire"], row["gain"], row["hold"], row["note"])
        os.remove(os.path.join(OUT, "hold_" + name + "_" + m + ".md"))
results["hold"] = rows

md = ["# Detonator ① 아껴 둘 값 방식 비교 (상대 Ext ② 서치 체인)", ""]
titles = {"ext": "상대 필드: Ext만 (얻는 값 = Ext 몸체 25)", "ext_det": "상대 필드: Ext + 미사용 Detonator 2소재 (얻는 값 = Detonator 73)"}
for fname in fields:
    md += ["## " + titles[fname], "", "| 남은 사용 / 남은 턴 / 상대 패 | " + " | ".join(modes) + " |", "|---|" + "---|" * len(modes)]
    for name0, uses, left, hand in hold_cases:
        cells = []
        for m in modes:
            r = next(x for x in rows if x["case"] == fname + "_" + name0 and x["mode"] == m)
            cells.append("%s (%s vs %s)" % (r["fire"], r["gain"], r["hold"]))
        md.append("| %d회 / %s / %d장 | %s |" % (uses, left, hand, " | ".join(cells)))
    fut = next((x["future"] for x in rows if x["mode"] == "future" and x["case"] == fname + "_uses1_early_hand5"), "")
    md += ["", "future 방식의 나올 대상(남은 턴 1; 판단에서는 지금 고른 대상을 뺌): " + re.sub(r"^이번 턴 나올 대상 \([^)]*\): ", "", fut), ""]
open(os.path.join(OUT, "hold_modes.md"), "w", encoding="utf-8", newline="\n").write("\n".join(md) + "\n")
json.dump(results, open(os.path.join(HERE, "hold_xyz.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
