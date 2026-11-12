# Offline dumps behind docs/VALUE_LEARNING_PLAN.md step 0 (2026-11-13): interruption values from Lua facts, no per-card numbers.
# Run: python run_dumps.py [old WindBot.exe]   (env PROJECTIGNIS = ProjectIgnis folder; default ../../../../ProjectIgnis)
# Needs the deployed WindBot.exe in ProjectIgnis/WindBot. With an old exe (a build of the commit before, which still has the
# per-card numbers in its code), each case also runs on it. Writes out/<case>.<new|old>.md and results.json next to this file.
import json, base64, os, subprocess, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out"); os.makedirs(OUT, exist_ok=True)
PI = os.environ.get("PROJECTIGNIS", os.path.normpath(os.path.join(HERE, "..", "..", "..", "..", "ProjectIgnis")))
paths = [os.path.join(PI, 'expansions', 'cards.cdb'), os.path.join(PI, 'repositories', 'delta-bagooska', 'cards.delta.cdb')]
B = base64.b64encode(json.dumps(paths).encode()).decode()
if hasattr(sys.stdout, "reconfigure"): sys.stdout.reconfigure(encoding="utf-8")

ICE, SWORD, EXT, NODE, STAR, SAKI, CRASH, TTT = 8633261, 35844557, 34022970, 72238166, 84433129, 67972302, 89753095, 25311006
DET, DUO, AGG, F0, M104, RAF = 34909328, 7511613, 9940036, 41522092, 2061963, 6511113
FUWA = "42141493:1"
def L(*x): return ",".join(str(i) for i in x)

cases = {
 # The interruption table of the whole deck (one value per effect).
 "table":        {"AgentDbInterrupts": "true"},
 # Turn plans: the end board's interruptions now come from Lua (N104 ① battle-only, Rafflesia ⑤ = the copied trap).
 "ice":          {"AgentDbHand": L(ICE)},
 "ice_sword":    {"AgentDbHand": L(ICE, SWORD)},
 "ice_ext_star": {"AgentDbHand": L(ICE, EXT, STAR)},
 "handtraps":    {"AgentDbHand": L(ICE, EXT, SAKI, CRASH, TTT)},
 "fuwalos":      {"AgentDbHand": L(ICE, EXT, SAKI, CRASH, TTT), "AgentDbOppWatch": FUWA, "AgentDbOppHand": "5"},
 # Ready end boards (no plan moves needed): what each board holds.
 "end_m104":     {"AgentDbHand": "0", "AgentDbMonsters": "%d:%d/%d/%d,%d:2" % (M104, ICE, SWORD, EXT, DET)},
 "end_raf":      {"AgentDbHand": "0", "AgentDbMonsters": "%d:%d/%d,%d:2" % (RAF, ICE, SWORD, DET)},
 "end_two_det":  {"AgentDbHand": "0", "AgentDbMonsters": "%d:2,%d:2" % (DET, DET)},
}

def run(exe, k, extra, tag):
    out = os.path.join(OUT, "%s.%s.md" % (k, tag))
    args = [exe, "AssetPath=./WindBot", "DbPaths=" + B, "AgentDbDump=AI_Ryzeal", "AgentOpponentDeck=AI_Ryzeal",
            "AgentDbOut=" + out] + ["%s=%s" % kv for kv in extra.items()]
    if "AgentDbInterrupts" not in extra: args.append("AgentDbInterrupts=false")
    subprocess.run(args, cwd=PI, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=300)
    txt = open(out, encoding="utf-8").read()
    r = {}
    if k == "table":
        i = txt.find("# 방해 가치 (이 덱)")
        txt = txt[i:txt.find("# 미끼 시험")] if i >= 0 else txt
        r["rows"] = [l for l in txt.splitlines() if l.startswith("| ") and not l.startswith("| 카드")]
    else:
        starts = [x for x in (txt.find("## 판단 입력"), txt.find("### 패 카드 가치")) if x >= 0]
        txt = txt[min(starts):] if starts else txt   # drop the card DB listing
        m = re.search(r"- 최선 계획: ([\d.]+) \(([^)]*)\)(?: 행동: (.*?))?(?: \| 방해: (.*))?$", txt, re.M)
        if m: r["plan"], r["terms"], r["moves"], r["board"] = float(m.group(1)), m.group(2), m.group(3) or "", m.group(4) or ""
    open(out, "w", encoding="utf-8", newline="\n").write(txt)
    return r

old = sys.argv[1] if len(sys.argv) > 1 else None
results = {}
for k, extra in cases.items():   # one at a time: the planner has a time budget per plan
    results[k] = {"new": run(os.path.join(PI, "WindBot", "WindBot.exe"), k, extra, "new")}
    if old: results[k]["old"] = run(old, k, extra, "old")
    for tag, r in results[k].items():
        if k == "table": continue
        print("==", k, tag, r.get("plan"), "|", r.get("terms", ""))
        print("    행동:", r.get("moves", "")[:300])
        print("    방해:", r.get("board", "")[:300])

json.dump(results, open(os.path.join(HERE, "results.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
