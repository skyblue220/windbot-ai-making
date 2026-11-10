# Hit-loss check behind docs/INTERRUPTION_RISK.md 8.1 (step 1): what negating one opponent effect costs them,
# by their unknown hand size. Run: python hit_loss.py   (env PROJECTIGNIS = ProjectIgnis folder)
import json, base64, os, subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out"); os.makedirs(OUT, exist_ok=True)
PI = os.environ.get("PROJECTIGNIS", os.path.normpath(os.path.join(HERE, "..", "..", "..", "..", "ProjectIgnis")))
paths = [os.path.join(PI, 'expansions', 'cards.cdb'), os.path.join(PI, 'repositories', 'delta-bagooska', 'cards.delta.cdb')]
B = base64.b64encode(json.dumps(paths).encode()).decode()
ICE, SWORD, EXT, NODE, STAR = 8633261, 35844557, 34022970, 72238166, 84433129
BAN, DUO, CROSS, M104, ASH, IMPERM, OGRE = 29423048, 7511613, 6798031, 2061963, 14558127, 10045474, 59438930

def L(*x): return ",".join(str(i) for i in x)
cases = {
 # name: (activation "id:effect", their field (Xyz as id:materials), their grave, effects they already used, our answer)
 "ice2_ash":    ("%d:2" % ICE, L(ICE), "", "", ASH),
 "sword2_ash":  ("%d:2" % SWORD, L(ICE, SWORD), "", "%d:2" % ICE, ASH),
 "ext2_ash":    ("%d:2" % EXT, L(ICE, SWORD, EXT), L(M104), L("%d:2" % ICE, "%d:2" % SWORD), ASH),
 "ban1_ash":    ("%d:1" % BAN, L(EXT, "%d:2" % BAN), L(M104), L("%d:2" % ICE, "%d:2" % SWORD, "%d:2" % EXT), ASH),
 "duo3_ash":    ("%d:3" % DUO, L(NODE, ICE, "%d:3" % DUO, CROSS), L(M104, BAN), L("%d:2" % ICE, "%d:2" % SWORD, "%d:1" % BAN, "%d:2" % STAR, "%d:2" % EXT, "%d:2" % NODE, "%d:2" % DUO), ASH),
 "ice2_ogre":   ("%d:2" % ICE, L(ICE), "", "", OGRE),
}
procs = {}
for k, (act, field, grave, used, hitter) in cases.items():
    args = [os.path.join(PI, "WindBot", "WindBot.exe"), "AssetPath=./WindBot", "DbPaths=" + B,
            "AgentDbDump=AI_Ryzeal", "AgentOpponentDeck=AI_Ryzeal", "AgentDbInterrupts=false",
            "AgentDbOppActivate=" + act, "AgentDbHitter=%d" % hitter, "AgentDbOut=" + os.path.join(OUT, "h_%s.md" % k)]
    if field: args.append("AgentDbOppField=" + field)
    if grave: args.append("AgentDbOppGrave=" + grave)
    if used: args.append("AgentDbOppUsed=" + used)
    procs[k] = subprocess.Popen(args, cwd=PI, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
for k, p in procs.items():
    p.wait(timeout=300)
    txt = open(os.path.join(OUT, "h_%s.md" % k), encoding="utf-8").read()
    i = txt.find("# 상대 손실 시험")
    print("==", k)
    print(txt[i:].strip() if i >= 0 else "(no section)")
