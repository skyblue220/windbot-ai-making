# Offline dumps behind docs/INTERRUPTION_RISK.md (2026-11-10).
# Run from anywhere: python run_dumps.py   (env PROJECTIGNIS = ProjectIgnis folder; default ../../../../ProjectIgnis)
# Needs the deployed WindBot.exe in ProjectIgnis/WindBot. Results: out/r_<board>.md and results.json next to this file.
import json, base64, os, subprocess, re

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out"); os.makedirs(OUT, exist_ok=True)
PI = os.environ.get("PROJECTIGNIS", os.path.normpath(os.path.join(HERE, "..", "..", "..", "..", "ProjectIgnis")))
paths = [os.path.join(PI, 'expansions', 'cards.cdb'), os.path.join(PI, 'repositories', 'delta-bagooska', 'cards.delta.cdb')]
B = base64.b64encode(json.dumps(paths).encode()).decode()
ICE, SWORD, EXT, NODE, STAR, PLUGIN = 8633261, 35844557, 34022970, 72238166, 84433129, 60394026
BAN, DET, DUO, CROSS, M104 = 29423048, 34909328, 7511613, 6798031, 2061963

def L(*x): return ",".join(str(i) for i in x)
def X(card, mats): return "%d:%d" % (card, mats)

# name -> (hand, monsters "id:mats", grave, face-up spells). Hit boards: the hit step's result is missing, its effect is spent.
boards = {
 "start":     (L(ICE), "", "", ""),
 # Line A (planner's line from an Ice one-card hand)
 "A_ice2":    ("", L(ICE), "", ""),
 "A_sword2":  ("", L(ICE, SWORD), "", ""),
 "A_ext2":    ("", L(ICE, SWORD, EXT), L(M104), ""),
 "A_ban1":    (L(SWORD), L(EXT, X(BAN, 1)), L(M104), ""),
 "A_nib":     (L(SWORD), "", L(M104, ICE, SWORD, EXT, BAN, STAR), L(CROSS)),
 "A_star2":   (L(SWORD), L(EXT, X(BAN, 0), STAR), L(M104, ICE), ""),
 "A_det2":    (L(SWORD), L(X(BAN, 0), X(DET, 2)), L(M104, ICE), L(CROSS)),
 "A_end":     (L(SWORD), L(X(BAN, 0), X(DET, 3)), L(M104), L(CROSS)),
 "A_end_d":   (L(SWORD, STAR), L(X(BAN, 0), X(DET, 1)), L(M104), L(CROSS)),  # after Cross 3 (guessed: 2 materials off, drew Star)
 # Line B (user's safe line; details guessed)
 "B_ban1":    (L(EXT), L(X(BAN, 1)), L(SWORD), ""),
 "B_star2":   (L(EXT), L(X(BAN, 0), STAR), L(SWORD, ICE), ""),
 "B_ext2":    ("", L(X(BAN, 0), STAR, EXT), L(SWORD, ICE, M104), L(CROSS)),
 "B_nib":     (L(NODE), "", L(SWORD, ICE, M104, BAN, STAR, EXT), L(CROSS)),
 "B_node2":   ("", L(STAR, EXT, NODE), L(SWORD, ICE, M104, BAN), L(CROSS)),
 "B_duo2":    ("", L(NODE, ICE, X(DUO, 2)), L(SWORD, M104, BAN), L(CROSS)),
 "B_duo3":    ("", L(NODE, ICE, X(DUO, 1)), L(M104, BAN), L(CROSS)),
 "B_ready":   ("", L(NODE, ICE, X(DUO, 3)), L(M104, BAN), L(CROSS)),
 "B_end_PlS": (L(PLUGIN, SWORD), L(X(DUO, 1), X(DET, 3)), L(M104), L(CROSS)),
 "B_end_PlE": (L(PLUGIN, EXT), L(X(DUO, 1), X(DET, 3)), L(M104), L(CROSS)),
 "B_end_ES":  (L(EXT, SWORD), L(X(DUO, 1), X(DET, 3)), L(M104), L(CROSS)),
 "B_end_hit": ("", L(X(DUO, 0), X(DET, 3)), L(M104), L(CROSS)),
}

# One dump at a time: the planner has a time budget per plan, so parallel runs search less and give lower values
# (found 2026-11-11; results.json from 2026-11-10 was made with parallel runs).
num = r"(-?[\d.]+)"
res = {}
for k, (h, m, g, sp) in boards.items():
    args = [os.path.join(PI, "WindBot", "WindBot.exe"), "AssetPath=./WindBot", "DbPaths=" + B,
            "AgentDbDump=AI_Ryzeal", "AgentOpponentDeck=AI_Ryzeal", "AgentDbHand=" + (h or "0"),
            "AgentDbInterrupts=false", "AgentDbOut=" + os.path.join(OUT, "r_%s.md" % k)]
    if m: args.append("AgentDbMonsters=" + m)
    if g: args.append("AgentDbGrave=" + g)
    if sp: args.append("AgentDbSpells=" + sp)
    subprocess.run(args, cwd=PI, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=300)
    txt = open(os.path.join(OUT, "r_%s.md" % k), encoding="utf-8").read()
    best = re.search(r"최선 계획: " + num + r" \(흐름 " + num + r" \+ 턴 종료 방해 " + num + r" \+ 후속 " + num + r"(?: − 효율 " + num + r"장)?\)(.*)", txt)
    stop = re.search(r"아무것도 안 하고 턴 종료: " + num + r" \(흐름 " + num + r" \+ 턴 종료 방해 " + num + r" \+ 후속 " + num, txt)
    res[k] = dict(stop=round(float(stop.group(3)) + float(stop.group(4)), 1) if stop else None,
                  replan=round(float(best.group(3)) + float(best.group(4)), 1) if best else None,
                  total=float(best.group(1)) if best else None,
                  plan=best.group(6).strip() if best else "")
    print("%-10s stop=%6s replan=%6s total=%6s" % (k, res[k]["stop"], res[k]["replan"], res[k]["total"]))
json.dump(res, open(os.path.join(HERE, "results.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
