# Offline dumps behind docs/DRAW_HANDTRAPS.md (2026-11-11): our turn's plan after the opponent resolved Maxx "C" / Mulcharmy.
# Run: python run_dumps.py   (env PROJECTIGNIS = ProjectIgnis folder; default ../../../../ProjectIgnis)
# Needs the deployed WindBot.exe in ProjectIgnis/WindBot. Writes out/<case>.md and results.json next to this file.
import json, base64, os, subprocess, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out"); os.makedirs(OUT, exist_ok=True)
PI = os.environ.get("PROJECTIGNIS", os.path.normpath(os.path.join(HERE, "..", "..", "..", "..", "ProjectIgnis")))
paths = [os.path.join(PI, 'expansions', 'cards.cdb'), os.path.join(PI, 'repositories', 'delta-bagooska', 'cards.delta.cdb')]
B = base64.b64encode(json.dumps(paths).encode()).decode()
if hasattr(sys.stdout, "reconfigure"): sys.stdout.reconfigure(encoding="utf-8")

ICE, EXT, SAKI, CRASH, TTT = 8633261, 34022970, 67972302, 89753095, 25311006
MAXX, FUWA, PURU = "23434538:1", "42141493:1", "84192580:1"
def L(*x): return ",".join(str(i) for i in x)
HAND = L(ICE, EXT, SAKI, CRASH, TTT)   # opening hand of log 192542_528 without its dead cards

cases = {
    "none":       {"AgentDbHand": HAND},
    "maxx":       {"AgentDbHand": HAND, "AgentDbOppWatch": MAXX, "AgentDbOppHand": "5"},
    "fuwalos":    {"AgentDbHand": HAND, "AgentDbOppWatch": FUWA, "AgentDbOppHand": "5"},
    "purulia":    {"AgentDbHand": HAND, "AgentDbOppWatch": PURU, "AgentDbOppHand": "5"},
    "maxx_fuwa":  {"AgentDbHand": HAND, "AgentDbOppWatch": L(MAXX, FUWA), "AgentDbOppHand": "4"},
    # their hand already large: the Mulcharmy End Phase shuffle leaves little room for its draws
    "fuwa_hand9": {"AgentDbHand": HAND, "AgentDbOppWatch": FUWA, "AgentDbOppHand": "9"},
}

results = {}
for k, extra in cases.items():   # one at a time: the planner has a time budget per plan
    out = os.path.join(OUT, k + ".md")
    args = [os.path.join(PI, "WindBot", "WindBot.exe"), "AssetPath=./WindBot", "DbPaths=" + B,
            "AgentDbDump=AI_Ryzeal", "AgentOpponentDeck=AI_Ryzeal", "AgentDbInterrupts=false",
            "AgentDbOut=" + out] + ["%s=%s" % kv for kv in extra.items()]
    subprocess.run(args, cwd=PI, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=300)
    txt = open(out, encoding="utf-8").read()
    # keep only the part after the card DB listing: the watches, the hand values and the plan
    starts = [x for x in (txt.find("- 상대 드로우 감시"), txt.find("### 패 카드 가치")) if x >= 0]
    txt = txt[min(starts):] if starts else txt
    open(out, "w", encoding="utf-8", newline="\n").write(txt)
    r = {}
    m = re.search(r"- 최선 계획: ([\d.]+) \(([^)]*)\)(?: 행동: (.*?))?(?: \| 방해: (.*))?$", txt, re.M)
    if m: r["plan"], r["terms"], r["moves"], r["board"] = float(m.group(1)), m.group(2), m.group(3) or "", m.group(4) or ""
    m = re.search(r"- 아무것도 안 하고 턴 종료: ([\d.]+)", txt)
    if m: r["stop"] = float(m.group(1))
    r["summons"] = len(re.findall(r"소환", r.get("moves", "")))
    results[k] = r
    print("==", k, r.get("plan"), "|", r.get("terms"), "| 소환 언급", r["summons"])
    print("   ", r.get("moves", "")[:400])
    print("    방해:", r.get("board", "")[:300])

json.dump(results, open(os.path.join(HERE, "results.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
