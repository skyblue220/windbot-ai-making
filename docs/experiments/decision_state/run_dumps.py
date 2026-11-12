# Offline dumps behind docs/DECISION_STATE.md (2026-11-13): the one full decision state, before / after.
# Run: python run_dumps.py [old WindBot.exe]   (env PROJECTIGNIS = ProjectIgnis folder; default ../../../../ProjectIgnis)
# Needs the deployed WindBot.exe in ProjectIgnis/WindBot. With an old exe (a build of the commit before), each case also
# runs on it for comparison. Writes out/<case>.<new|old>.md and results.json next to this file.
import json, base64, os, subprocess, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out"); os.makedirs(OUT, exist_ok=True)
PI = os.environ.get("PROJECTIGNIS", os.path.normpath(os.path.join(HERE, "..", "..", "..", "..", "ProjectIgnis")))
paths = [os.path.join(PI, 'expansions', 'cards.cdb'), os.path.join(PI, 'repositories', 'delta-bagooska', 'cards.delta.cdb')]
B = base64.b64encode(json.dumps(paths).encode()).decode()
if hasattr(sys.stdout, "reconfigure"): sys.stdout.reconfigure(encoding="utf-8")

ICE, STAR, NODE, DUO, DET, F0, AGG = 8633261, 84433129, 72238166, 7511613, 34909328, 41522092, 9940036
def L(*x): return ",".join(str(i) for i in x)

cases = {
 # Two Detonators attached to F0, the third in the GY: none left in the Extra Deck (the old count missed attached ones).
 "f0_det_mats":  {"AgentDbHand": L(ICE), "AgentDbMonsters": "%d:%d/%d" % (F0, DET, DET), "AgentDbGrave": L(DET)},
 # Ice in hand and two attached to Duo Drive: no Ice left in the Deck.
 "duo_ice_mats": {"AgentDbHand": L(ICE, STAR), "AgentDbMonsters": "%d:%d/%d" % (DUO, ICE, ICE)},
 # The opponent's whole side: Detonator with materials, a set monster, two set spells / traps, GY, Deck.
 "opp_full":     {"AgentDbHand": L(ICE), "AgentDbOppMonsters": "%d:a:3000:2500:%d/%d" % (DET, NODE, ICE), "AgentDbOppSetMonsters": "1",
                  "AgentDbOppSpells": "0,0", "AgentDbOppGrave": L(AGG, DUO), "AgentDbOppHand": "3", "AgentDbOppDeck": "30"},
}

def run(exe, k, extra, tag):
    out = os.path.join(OUT, "%s.%s.md" % (k, tag))
    args = [exe, "AssetPath=./WindBot", "DbPaths=" + B, "AgentDbDump=AI_Ryzeal", "AgentOpponentDeck=AI_Ryzeal", "AgentDbInterrupts=false",
            "AgentDbOut=" + out] + ["%s=%s" % kv for kv in extra.items()]
    subprocess.run(args, cwd=PI, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=300)
    txt = open(out, encoding="utf-8").read()
    starts = [x for x in (txt.find("## 판단 입력"), txt.find("### 패 카드 가치")) if x >= 0]
    txt = txt[min(starts):] if starts else txt   # drop the card DB listing
    open(out, "w", encoding="utf-8", newline="\n").write(txt)
    r = {}
    m = re.search(r"- 최선 계획: ([\d.]+) \(([^)]*)\)(?: 행동: (.*?))?(?: \| 방해: (.*))?$", txt, re.M)
    if m: r["plan"], r["terms"], r["moves"], r["board"] = float(m.group(1)), m.group(2), m.group(3) or "", m.group(4) or ""
    i = txt.find("## 덱에 대상이 없어 0점")
    if i >= 0: r["deck_blocked"] = [l[2:] for l in txt[i:].split("\n\n")[0].splitlines()[1:] if l != "- 없음"]
    i = txt.find("## 판단 입력")
    if i >= 0: r["state"] = txt[i:].split("\n\n")[0].splitlines()[1:]
    return r

old = sys.argv[1] if len(sys.argv) > 1 else None
results = {}
for k, extra in cases.items():   # one at a time: the planner has a time budget per plan
    results[k] = {"new": run(os.path.join(PI, "WindBot", "WindBot.exe"), k, extra, "new")}
    if old: results[k]["old"] = run(old, k, extra, "old")
    for tag, r in results[k].items():
        print("==", k, tag, r.get("plan"), "|", r.get("moves", "")[:300])
        print("    방해:", r.get("board", "")[:200])
        print("    덱 0점:", len(r.get("deck_blocked", [])), r.get("deck_blocked", [])[:6])
    for l in results[k]["new"].get("state", []): print("   ", l)

json.dump(results, open(os.path.join(HERE, "results.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
