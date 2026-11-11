# Offline dumps behind docs/CHAIN_REMOVAL.md (2026-11-11): what removing each opponent card takes now, and fire / keep
# for Ghost Ogre, Detonator (1), Tornado Dragon, Plasma Hole (1). Exciton Knight is left out (too slow to count as an answer).
# Run: python run_dumps.py   (env PROJECTIGNIS = ProjectIgnis folder; default ../../../../ProjectIgnis)
# Needs the deployed WindBot.exe in ProjectIgnis/WindBot. Writes out/<case>.md and results.json next to this file.
import json, base64, os, subprocess, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out"); os.makedirs(OUT, exist_ok=True)
PI = os.environ.get("PROJECTIGNIS", os.path.normpath(os.path.join(HERE, "..", "..", "..", "..", "ProjectIgnis")))
paths = [os.path.join(PI, 'expansions', 'cards.cdb'), os.path.join(PI, 'repositories', 'delta-bagooska', 'cards.delta.cdb')]
B = base64.b64encode(json.dumps(paths).encode()).decode()

ICE, SWORD, DUO, DET, IMPERM, CROSS = 8633261, 35844557, 7511613, 34909328, 10045474, 6798031
def L(*x): return ",".join(str(i) for i in x)

cases = {
 # our turn: they answer with Impermanence (a normal trap: gone after resolving) while Detonator (2) and Duo Drive (2) stand
 "imperm_on_chain": {"AgentDbOppTurn": "false", "AgentDbOppField": L("%d:2" % DET, "%d:2" % DUO), "AgentDbOppChain": "%d:1" % IMPERM, "AgentDbOurXyz": "2"},
 # their turn: Duo Drive (2) attaches from the GY, which needs Duo Drive on the field
 "duo_attach":      {"AgentDbOppField": L("%d:2" % DET, "%d:2" % DUO), "AgentDbOppChain": "%d:2" % DUO, "AgentDbOppGrave": L(ICE, SWORD), "AgentDbOurXyz": "2"},
 # their Standby: an untouched Detonator (2) and one face-down card, no chain
 "standby_det":     {"AgentDbOppField": L("%d:2" % DET, 0), "AgentDbOurXyz": "1"},
 # their turn: Detonator (1) already on the chain, its last material paid as the cost
 "det_spent":       {"AgentDbOppField": "%d:0" % DET, "AgentDbOppChain": "%d:1" % DET, "AgentDbOurXyz": "1"},
 # their turn: Duo Drive (3) search on the chain, (2) already used
 "duo_search":      {"AgentDbOppField": "%d:1" % DUO, "AgentDbOppChain": "%d:3" % DUO, "AgentDbOppUsed": "%d:2" % DUO, "AgentDbOurXyz": "1"},
 # our turn: they activate Ryzeal Cross (a field spell: stays after resolving) beside Detonator (2)
 "cross_on_chain":  {"AgentDbOppTurn": "false", "AgentDbOppField": "%d:2" % DET, "AgentDbOppChain": "%d:1" % CROSS, "AgentDbOurXyz": "2"},
 # their turn: Ryzeal Cross face-up, (3) unused, no chain
 "cross_standby":   {"AgentDbOppField": L(CROSS, "%d:2" % DET), "AgentDbOurXyz": "1"},
}

if hasattr(sys.stdout, "reconfigure"): sys.stdout.reconfigure(encoding="utf-8")
results = {}
for k, extra in cases.items():
    args = [os.path.join(PI, "WindBot", "WindBot.exe"), "AssetPath=./WindBot", "DbPaths=" + B,
            "AgentDbDump=AI_Ryzeal", "AgentOpponentDeck=AI_Ryzeal", "AgentDbInterrupts=false", "AgentDbRemoval=true",
            "AgentDbOut=" + os.path.join(OUT, k + ".md")] + ["%s=%s" % kv for kv in extra.items()]
    subprocess.run(args, cwd=PI, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=300)
    txt = open(os.path.join(OUT, k + ".md"), encoding="utf-8").read()
    txt = txt[txt.find("# 체인 중 제거 시험"):]
    open(os.path.join(OUT, k + ".md"), "w", encoding="utf-8", newline="\n").write(txt)
    r = {"worth": {}, "choice": {}}
    sec = txt.split("## ")
    for l in sec[1].splitlines()[1:]:
        m = re.match(r"- (.+?)(?: \(체인[^)]*\))?: ([\d.]+) ", l)
        if m: r["worth"][m.group(1)] = float(m.group(2))
    for l in sec[2].splitlines()[1:]:
        m = re.match(r"- (.+?) 효과 (\d) \[[^\]]*\]: (.*)", l)
        if m: r["choice"][m.group(1) + " " + m.group(2)] = m.group(3)
    results[k] = r
    print("==", k)
    for n, v in r["worth"].items(): print("   worth", n, v)
    for n, v in r["choice"].items(): print("  ", n, ":", v)

json.dump(results, open(os.path.join(HERE, "results.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
