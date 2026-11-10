# Offline dumps behind docs/GRAVE_PAYOFF.md (2026-11-11): material ids, GY payoffs now / kept, turn-negate residual.
# Run: python run_dumps.py   (env PROJECTIGNIS = ProjectIgnis folder; default ../../../../ProjectIgnis)
# Needs the deployed WindBot.exe in ProjectIgnis/WindBot. Writes out/<case>.md and results.json next to this file.
import json, base64, os, subprocess, re

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out"); os.makedirs(OUT, exist_ok=True)
PI = os.environ.get("PROJECTIGNIS", os.path.normpath(os.path.join(HERE, "..", "..", "..", "..", "ProjectIgnis")))
paths = [os.path.join(PI, 'expansions', 'cards.cdb'), os.path.join(PI, 'repositories', 'delta-bagooska', 'cards.delta.cdb')]
B = base64.b64encode(json.dumps(paths).encode()).decode()

ICE, STAR, NODE, DUO, DET, RAF, AGG, TWINS = 8633261, 84433129, 72238166, 7511613, 34909328, 6511113, 9940036, 45852939
def L(*x): return ",".join(str(i) for i in x)

# case -> extra dump arguments. Turn plans (sections 3, 7, 8) and the turn-negate residual table (section 6).
cases = {
 # our turn plans
 "ice":           {"AgentDbHand": L(ICE)},
 "ice_fu0":       {"AgentDbHand": L(ICE), "AgentDbOppFaceUp": "0"},
 "ice_duo":       {"AgentDbHand": L(ICE, DUO)},
 "ice_vs_det":    {"AgentDbHand": L(ICE), "AgentDbOppMonsters": "%d:a" % DET, "AgentDbOppFaceUp": "1"},
 "ice_vs_duo":    {"AgentDbHand": L(ICE), "AgentDbOppMonsters": "%d:a" % DUO, "AgentDbOppFaceUp": "1"},
 # detach now vs keep: our Detonator holds Aggregator + Node, Star's rule summon detaches one
 "agg_target":    {"AgentDbHand": L(STAR), "AgentDbMonsters": "%d:%d/%d" % (DET, AGG, NODE), "AgentDbOppMonsters": "%d:a" % DET, "AgentDbOppFaceUp": "1"},
 "agg_no_target": {"AgentDbHand": L(STAR), "AgentDbMonsters": "%d:%d/%d" % (DET, AGG, NODE), "AgentDbOppFaceUp": "0"},
 # residual after the opponent's Ice ②: their field Ice, Duo Drive (2), Detonator (2), Rafflesia (1)
 "resid_fresh":   {"AgentDbOppActivate": "%d:2" % ICE, "AgentDbOppField": L(ICE, "%d:2" % DUO, "%d:2" % DET, "%d:1" % RAF)},
 "resid_used":    {"AgentDbOppActivate": "%d:2" % ICE, "AgentDbOppField": L(ICE, "%d:2" % DUO, "%d:2" % DET, "%d:1" % RAF),
                   "AgentDbOppUsed": L("%d:3" % DUO, "%d:1" % DET, "%d:5" % RAF)},
}

import sys
if hasattr(sys.stdout, "reconfigure"): sys.stdout.reconfigure(encoding="utf-8")

# One dump at a time: the planner has a time budget per plan, so parallel runs search less and give lower values.
results = {}
for k, extra in cases.items():
    args = [os.path.join(PI, "WindBot", "WindBot.exe"), "AssetPath=./WindBot", "DbPaths=" + B,
            "AgentDbDump=AI_Ryzeal", "AgentOpponentDeck=AI_Ryzeal", "AgentDbInterrupts=false",
            "AgentDbOut=" + os.path.join(OUT, k + ".md")] + ["%s=%s" % kv for kv in extra.items()]
    subprocess.run(args, cwd=PI, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=300)
    txt = open(os.path.join(OUT, k + ".md"), encoding="utf-8").read()
    r = {}
    m = re.search(r"- 최선 계획: ([\d.]+) \(([^)]*)\) 행동: (.*)", txt)
    if m: r["plan"], r["terms"], r["moves"] = float(m.group(1)), m.group(2), m.group(3)
    m = re.search(r"- 패: Ice Ryzeal → 최선 ([\d.]+) = (.*)", txt)
    if m: r["ice_chain"], r["ice_chain_path"] = float(m.group(1)), m.group(2)
    i = txt.find("## 엑스트라 덱 코스트 순서")
    if i >= 0: r["extra_cost_order"] = [l[2:].split(":")[0] for l in txt[i:].split("\n\n")[0].splitlines()[1:]]
    i = txt.find("## 묘지 이득")
    if i >= 0: r["gy_payoffs"] = txt[i:].split("\n\n")[0].splitlines()[1:]
    i = txt.find("## 앞면 카드 무효")
    if i >= 0: r["turn_negate_residual"] = [l for l in txt[i:].split("\n\n")[1].splitlines()[2:]]
    results[k] = r
    print("==", k, r.get("plan", ""), r.get("ice_chain", ""))
    for l in r.get("turn_negate_residual", []): print("  ", l)

json.dump(results, open(os.path.join(HERE, "results.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
