#!/usr/bin/env python
"""
Lê a ajuda oficial do TIA Portal (o que o F1 abre) como texto, sem navegador.

Quatro modos, do mais barato ao mais caro:
  --study  o que ler antes de escrever código sobre um tema (mapa curado + os dois índices)
  --sdk    assinatura de API no IntelliSense XML das assemblies — local, casa no corpo
  --search grep no título dos tópicos do F1
  --deep   baixa e grepa o CORPO dos tópicos mais plausíveis (cache em disco)

Protocolo (descoberto em runtime, não documentado):
  - O viewer é um SPA em https://localhost:<porta>/ ; a API fica em /<api>/HelpViewer/...
    onde <api> é o valor do parâmetro ?api= da URL do F1 (ex.: PortalV21).
  - Servidor só fala **HTTP/2 sobre TLS**. Isso exclui `curl.exe` do Windows e
    `Invoke-WebRequest` (schannel: SEC_E_ILLEGAL_MESSAGE / conexão fechada sem resposta).
    Daí httpx[http2] (OpenSSL). Certificado é self-signed -> verify=False.
  - /HelpViewer/Search responde 404 (assinatura não confere); o índice sai do /Toc.
  - /HelpViewer/Html quer helpReferenceString = objeto HelpReference completo; com campo
    faltando devolve `null` com status 200, não erro.

O servidor é o serviço do Windows "Siemens TIA Help Viewer Service" (StartMode Auto) — vive
sozinho, **não** depende do F1 estar aberto. `--ensure` sobe se estiver parado.

Uso:
  python scripts/tia-help.py --study "braço de 5 eixos"   # COMECE AQUI numa tarefa de engenharia
  python scripts/tia-help.py --search "clock memory"      # título; sobe o serviço e indexa se preciso
  python scripts/tia-help.py --deep "MC_Transformation"   # corpo dos tópicos plausíveis
  python scripts/tia-help.py --sdk "insert main telegram" # assinatura da API Openness
  python scripts/tia-help.py --topic ProgKOP2MenUS/10867183243/10383119371.htm
  python scripts/tia-help.py --ensure                     # só o preflight (serviço + índice)
"""
# ====================== BEGIN NAV INDEX ======================
# NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
#   L74    DEFAULT_BASE
#   L75    DEFAULT_API
#   L76    CULTURE
#   L79    api
#   L85    _params
#   L90    topic
#   L117   index
#   L141   índice do SDK (IntelliSense XML das assemblies Openness) ----------
#   L150   _sdk_dirs
#   L161   sdk_index
#   L191   sdk_search
#   L201   SERVICE
#   L204   ensure
#   L228   _listening
#   L241   search
#   L252   busca no corpo, sob demanda ----------
#   L260   _cache_path
#   L264   body
#   L276   deep
#   L313   study: o que ler antes de escrever código ----------
#   L318   _repo_root
#   L322   _fold
#   L329   study
#   L386   selftest
#   L412   main
# ======================= END NAV INDEX =======================

import argparse
import html
import json
import os
import re
import socket
import subprocess
import sys
import time
import urllib.parse

import httpx

DEFAULT_BASE = "https://localhost:5112"
DEFAULT_API = "PortalV21"
CULTURE = "en-us"


def api(base, name):
    """Client apontado pra raiz da API do viewer."""
    return httpx.Client(http2=True, verify=False, timeout=300,
                        base_url="{}/{}".format(base.rstrip("/"), name))


def _params(base, name):
    return json.dumps({"Culture": CULTURE,
                       "RequestUrl": "{}/{}".format(base.rstrip("/"), name)})


def topic(base, name, item_id):
    """item_id = "PKG/TOC/ID.htm" (é o ItemId do TOC). Devolve o texto do tópico."""
    pkg, _, internal = item_id.partition("/")
    ref = {"IconCategory": "", "Title": "", "ItemId": item_id,
           "IsBrandingHelpReference": False, "IsExternalReference": False,
           "IsContextSensitiveReference": False, "IsPackagedFileReference": True,
           "IsTextFileReference": True, "IsHtmlFileReference": True,
           "PackageName": pkg, "InternalFileName": internal,
           "AbsoluteHelpUrl": "http://localhost:123/tiahelp/14.0/" + item_id,
           "RequestUrl": None}
    with api(base, name) as c:
        r = c.get("/HelpViewer/Html?webApiParamsString="
                  + urllib.parse.quote(_params(base, name))
                  + "&&helpReferenceString=" + urllib.parse.quote(json.dumps(ref)))
    r.raise_for_status()
    if r.text.strip() == "null":
        raise SystemExit("HelpReference recusado (ItemId errado?): " + item_id)
    raw = r.text
    if raw.lstrip().startswith('"'):
        raw = json.loads(raw)          # a API devolve o HTML como string JSON, não como corpo
    body = re.sub(r"(?is)<(script|style).*?</\1>", "", raw)
    body = re.sub(r"(?i)</(p|div|tr|li|h\d|table)>", "\n", body)
    text = html.unescape(re.sub(r"(?s)<[^>]+>", " ", body))
    text = re.sub(r"[ \t　]+", " ", text)
    return "\n".join(l.strip() for l in text.splitlines() if l.strip())


def index(base, name, out):
    """
    TOC inteiro (~350 MB no fio) reduzido a "ItemId|Título" por linha (~alguns MB).
    Streaming + regex por chunk: o JSON nunca cabe na memória de uma vez.
    """
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    seen, total = set(), 0
    pattern = re.compile(r'"((?:[A-Za-z0-9]+enUS|[A-Za-z0-9]+US)/\d+/\d+\.htm)\|([^"]{1,200})"')
    tail = ""
    with api(base, name) as c, open(out, "w", encoding="utf-8") as fh:
        with c.stream("GET", "/HelpViewer/Toc?webApiParamsString="
                      + urllib.parse.quote(_params(base, name))) as r:
            for chunk in r.iter_text():
                total += len(chunk)
                buf = tail + chunk
                for m in pattern.finditer(buf):
                    line = m.group(1) + "|" + m.group(2)
                    if line not in seen:
                        seen.add(line)
                        fh.write(line + "\n")
                tail = buf[-300:]           # entrada partida entre chunks
    return len(seen), total


# ---------- índice do SDK (IntelliSense XML das assemblies Openness) ----------
#
# Fonte diferente da ajuda do F1 e complementar a ela: `PublicAPI\V21\net48\*.xml` traz ~31 mil
# membros documentados (assinatura + summary). Duas coisas que o índice do TOC não dá:
#   - assinatura exata de método/propriedade, que a ajuda HTML quase nunca mostra;
#   - busca **no corpo** — o índice do TOC só tem título, então termo que não está no título
#     dá 0 hits lá e acha aqui.
# É tudo local (arquivo em disco), não precisa do serviço nem de rede.

def _sdk_dirs():
    import glob
    pf = os.environ.get("ProgramFiles", r"C:\Program Files")
    for pattern in (r"Siemens\Automation\Portal V*\PublicAPI\V*\net48",
                    r"Siemens\Automation\Portal V*\PublicAPI\V*"):
        hits = sorted(glob.glob(os.path.join(pf, pattern)), reverse=True)
        if any(glob.glob(os.path.join(h, "*.xml")) for h in hits):
            return hits
    return []


def sdk_index(out):
    """Cada <member> vira "Assembly|Nome|summary" numa linha. Grep depois, como o índice do TOC."""
    import glob
    import xml.etree.ElementTree as ET
    dirs = _sdk_dirs()
    if not dirs:
        raise SystemExit("nenhum PublicAPI\\V*\\*.xml encontrado — TIA Portal com Openness instalado?")
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    seen, files = set(), 0
    with open(out, "w", encoding="utf-8") as fh:
        for d in dirs:
            for path in sorted(glob.glob(os.path.join(d, "*.xml"))):
                asm = os.path.splitext(os.path.basename(path))[0]
                try:
                    root = ET.parse(path).getroot()
                except ET.ParseError:
                    continue
                files += 1
                for member in root.iter("member"):
                    name = member.get("name")
                    if not name:
                        continue
                    body = " ".join("".join(member.itertext()).split())[:500]
                    line = "{}|{}|{}".format(asm, name, body)
                    if line not in seen:
                        seen.add(line)
                        fh.write(line + "\n")
    return len(seen), files


def sdk_search(out, term, limit):
    """AND de palavras sobre nome **e** summary — a diferença para o --search do TOC."""
    if not os.path.exists(out) or os.path.getsize(out) == 0:
        sdk_index(out)
    words = [re.compile(re.escape(w), re.I) for w in term.split()]
    with open(out, encoding="utf-8") as fh:
        hits = [l.rstrip('\\n') for l in fh if all(w.search(l) for w in words)]
    return hits[:limit], len(hits)


SERVICE = "Siemens TIA Help Viewer Service"


def ensure(base, name, out):
    """
    Preflight: serviço no ar + índice em disco. Idempotente; devolve o que fez.
    O serviço é Auto, então o normal é já estar rodando — subir é o caso raro (e pede elevação).
    """
    did = []
    if not _listening(base):
        did.append("start-service")
        subprocess.run(["powershell", "-NoProfile", "-Command",
                        "Start-Service -Name '{}'".format(SERVICE)],
                       capture_output=True, timeout=120)
        for _ in range(20):
            if _listening(base):
                break
            time.sleep(1)
        if not _listening(base):
            raise SystemExit("serviço '{}' não subiu — `Start-Service` precisa de elevação."
                             .format(SERVICE))
    if not os.path.exists(out) or os.path.getsize(out) == 0:
        did.append("index")
        index(base, name, out)
    return did


def _listening(base):
    host, _, port = urllib.parse.urlsplit(base).netloc.partition(":")
    s = socket.socket()
    s.settimeout(2)
    try:
        s.connect((host or "localhost", int(port or 443)))
        return True
    except OSError:
        return False
    finally:
        s.close()


def search(base, name, out, term, limit):
    """Grep no índice. O endpoint /Search do viewer responde 404, então a busca é local."""
    # ponytail: casa por AND de palavras no *título* — o índice não tem corpo. Termo que só existe
    # no texto do tópico (ex.: "DisableENO") dá 0 hits; achar o tópico plausível e ler com --topic.
    ensure(base, name, out)
    words = [re.compile(re.escape(w), re.I) for w in term.split()]
    with open(out, encoding="utf-8") as fh:
        hits = [l.rstrip('\\n') for l in fh if all(w.search(l) for w in words)]
    return hits[:limit], len(hits)


# ---------- busca no corpo, sob demanda ----------
#
# O índice do TOC só tem título, então pergunta em linguagem natural dá 0 hits. Aqui: candidatos por
# OR de palavras no título (ranqueados por quantas casaram), corpo baixado só desses e guardado em
# disco. Custa N requisições na 1ª vez e nada nas seguintes.
# ponytail: crawl completo dos 45 mil tópicos daria busca instantânea, mas são horas de download e
# um cache que envelhece a cada versão do Portal. Sob demanda cobre o uso real.

def _cache_path(cache_dir, item_id):
    return os.path.join(cache_dir, re.sub(r"[^A-Za-z0-9]+", "_", item_id) + ".txt")


def body(base, name, item_id, cache_dir):
    """Texto do tópico, do cache se já tiver sido lido."""
    path = _cache_path(cache_dir, item_id)
    if os.path.exists(path):
        return open(path, encoding="utf-8").read()
    text = topic(base, name, item_id)
    os.makedirs(cache_dir, exist_ok=True)
    with open(path, "w", encoding="utf-8") as fh:
        fh.write(text)
    return text


def deep(base, name, out, term, limit, scan, cache_dir):
    """Busca no corpo dos `scan` tópicos mais plausíveis. Devolve (hits, candidatos lidos)."""
    ensure(base, name, out)
    words = [w for w in term.split() if len(w) > 2]
    if not words:
        return [], 0
    res = [re.compile(re.escape(w), re.I) for w in words]
    # candidato sai do título, e título não tem "DisableENO" inteiro: quebrar camelCase/underscore
    # dá "Disable" + "ENO", que aparecem. Sem isso, termo colado devolve zero candidatos.
    parts = [p for w in words for p in re.split(r"[_\W]+|(?<=[a-z])(?=[A-Z])", w) if len(p) > 2]
    cand = [re.compile(re.escape(p), re.I) for p in parts] or res
    ranked = []
    with open(out, encoding="utf-8") as fh:
        for line in fh:
            score = sum(1 for r in cand if r.search(line))
            if score:
                ranked.append((score, line.rstrip('\\n')))
    ranked.sort(key=lambda t: -t[0])
    hits, read = [], 0
    for _, line in ranked[:scan]:
        item_id, _, title = line.partition("|")
        try:
            text = body(base, name, item_id, cache_dir)
        except Exception as exc:                      # tópico que a API recusa não derruba a busca
            hits.append({"topic": item_id, "title": title, "error": str(exc)[:120]})
            continue
        read += 1
        if not all(r.search(text) for r in res):
            continue
        where = res[0].search(text)
        hits.append({"topic": item_id, "title": title,
                     "excerpt": " ".join(text[max(0, where.start() - 200):where.start() + 300].split())})
        if len(hits) >= limit:
            break
    return hits, read


# ---------- study: o que ler antes de escrever código ----------
#
# Roteador sobre o que já existe (índice do F1 + índice do SDK + docs do repo). O conhecimento
# curado mora em docs/study-map.json — domínio novo é mais um objeto lá, nenhuma linha aqui muda.

def _repo_root():
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def _fold(s):
    """minúscula sem acento — a pergunta vem em português, as chaves do mapa são ASCII."""
    import unicodedata
    return "".join(c for c in unicodedata.normalize("NFD", s.lower())
                   if not unicodedata.combining(c))


def study(base, name, out, sdk_out, term, limit, map_file=None):
    path = map_file or os.path.join(_repo_root(), "docs", "study-map.json")
    with open(path, encoding="utf-8") as fh:
        smap = json.load(fh)
    low = _fold(term)
    # palavra inteira: com `in` cru, "ob" casa dentro de "robótico" e o domínio errado ganha.
    # E sem dobrar acento, "código de barras" não casa a chave "codigo de barras".
    def hits_kw(kw):
        return re.search(r"(?<![a-z0-9])" + re.escape(_fold(kw)) + r"(?![a-z0-9])", low) is not None
    matched = [d for d in smap["domains"] if any(hits_kw(k) for k in d["match"])]

    # termos de busca: os do domínio quando casou, senão o próprio termo do usuário.
    # Intercala entre domínios — concatenar deixa o 1º domínio comer todo o orçamento de busca.
    def interleave(key):
        rows = [d.get(key, []) for d in matched]
        return [r[i] for i in range(max((len(r) for r in rows), default=0)) for r in rows if i < len(r)]
    f1_terms = interleave("f1") or [term]
    sdk_terms = interleave("sdk") or [term]

    topics, seen = [], set()
    for t in f1_terms[:6]:
        try:
            hits, _ = search(base, name, out, t, 4)
        except SystemExit:                             # serviço fora do ar não zera o resto do study
            break
        for h in hits:
            item_id, _, title = h.partition("|")
            if item_id in seen:
                continue
            seen.add(item_id)
            topics.append({"topic": item_id, "title": title})
    api = []
    for t in sdk_terms[:4]:
        hits, _ = sdk_search(sdk_out, t, 3)
        api.extend(hits)

    return {
        "term": term,
        "domains": [d["name"] for d in matched] or ["(nenhum domínio casou — caminho genérico)"],
        "readFirst": smap["readFirst"],
        "workflow": smap["workflow"],
        "hardware": [h for d in matched for h in d.get("hardware", [])],
        "warnings": [w for d in matched for w in d.get("warn", [])],
        "rules": sorted({r for d in matched for r in d.get("rules", [])}),
        "libraries": sorted({l for d in matched for l in d.get("libs", [])}),
        "verbs": [v for d in matched for v in d.get("next", [])],
        "topics": topics[:limit],
        "api": api[:limit],
        "catalog": smap["catalog"],
        "note": "topics = ItemId para `--topic`; api = membros do Openness. Nada aqui é a resposta: "
                "é onde procurar antes de escrever código."
                + ("" if matched else " Nenhum domínio casou: o índice do F1 é em inglês e casa só "
                   "no título — repetir com o termo técnico em inglês, ou usar `--deep` (busca no "
                   "corpo). `catalog` diz o que existe na plataforma."),
    }


def selftest():
    """Offline: só o roteamento do --study, que é onde mora a lógica que quebra calada."""
    import json as _json
    smap = _json.load(open(os.path.join(_repo_root(), "docs", "study-map.json"), encoding="utf-8"))
    names = [d["name"] for d in smap["domains"]]
    assert len(names) == len(set(names)), "domínio duplicado no study-map"
    for d in smap["domains"]:
        assert d["match"] and d.get("f1") is not None, d["name"]
        assert all(k == _fold(k) for k in d["match"]), "chave com acento/maiúscula: " + d["name"]

    def domains(term):
        low = _fold(term)
        return [d["name"] for d in smap["domains"]
                if any(re.search(r"(?<![a-z0-9])" + re.escape(_fold(k)) + r"(?![a-z0-9])", low)
                       for k in d["match"])]

    # o bug que já aconteceu: "ob" casando dentro de "robótico"
    assert "estrutura de programa" not in domains("braço robótico de 5 eixos")
    assert "motion / eixos / cinemática" in domains("braço robótico de 5 eixos")
    # acento na pergunta, chave em ASCII
    assert "visão / câmera / leitura de código" in domains("ler código de barras")
    # camelCase quebrado para achar candidato no título
    assert "Transformation" in re.split(r"[_\W]+|(?<=[a-z])(?=[A-Z])", "MC_Transformation")
    print(_json.dumps({"selftest": "ok", "domains": len(names)}))


def main():
    p = argparse.ArgumentParser(description="Ajuda do TIA Portal (F1) como texto.")
    p.add_argument("--base", default=DEFAULT_BASE, help="default " + DEFAULT_BASE)
    p.add_argument("--api", default=DEFAULT_API, help="valor do ?api= (default PortalV21)")
    p.add_argument("--topic", help='ItemId "PKG/TOC/ID.htm"')
    p.add_argument("--search", help="grep no índice (sobe o serviço e indexa se preciso)")
    p.add_argument("--limit", type=int, default=15, help="hits do --search (default 15)")
    p.add_argument("--index", action="store_true", help="(re)gera o índice pesquisável")
    p.add_argument("--ensure", action="store_true", help="preflight: serviço no ar + índice")
    # Ancorado no REPO, nao no cwd: o SKILL.md manda chamar este script por caminho absoluto de
    # qualquer diretorio, e com default relativo cada cwd novo virava um workspace/ proprio — ou
    # seja, reindexar os ~350 MB do TOC e os 5,8 MB do SDK a cada lugar de onde se chamasse.
    ws = os.path.join(_repo_root(), "workspace")
    p.add_argument("--out", default=os.path.join(ws, "help-index.txt"))
    p.add_argument("--sdk", help="busca no IntelliSense XML das assemblies Openness "
                                 "(assinatura + summary, casa no corpo; local, sem serviço)")
    p.add_argument("--sdk-index", action="store_true", help="(re)gera o índice do SDK")
    p.add_argument("--sdk-out", default=os.path.join(ws, "sdk-index.txt"))
    p.add_argument("--deep", help="busca no CORPO dos tópicos mais plausíveis (custa download na "
                                  "1ª vez, cache depois) — é o que responde pergunta em prosa")
    p.add_argument("--scan", type=int, default=40, help="tópicos que o --deep abre (default 40)")
    p.add_argument("--cache", default=os.path.join(ws, "help-cache"))
    p.add_argument("--study", help="o que ler antes de escrever código sobre um tema: tópicos do F1, "
                                   "API do Openness, biblioteca oficial, restrição de hardware, "
                                   "regra da casa e o verbo seguinte")
    p.add_argument("--study-map", help="outro docs/study-map.json")
    p.add_argument("--selftest", action="store_true", help="checa o roteamento do --study (offline)")
    a = p.parse_args()
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")  # ajuda tem CJK; console é cp1252
    if a.selftest:
        selftest()
    elif a.study:
        print(json.dumps(study(a.base, a.api, a.out, a.sdk_out, a.study, a.limit, a.study_map),
                         ensure_ascii=False, indent=2))
    elif a.deep:
        hits, read = deep(a.base, a.api, a.out, a.deep, a.limit, a.scan, a.cache)
        print(json.dumps({"term": a.deep, "hits": len(hits), "topicsRead": read,
                          "results": hits}, ensure_ascii=False, indent=2))
    elif a.sdk:
        hits, total = sdk_search(a.sdk_out, a.sdk, a.limit)
        print(json.dumps({"term": a.sdk, "hits": total, "shown": len(hits)}, ensure_ascii=False))
        for h in hits:
            print(h)
    elif a.sdk_index:
        n, files = sdk_index(a.sdk_out)
        print(json.dumps({"file": a.sdk_out, "members": n, "assemblies": files}))
    elif a.search:
        hits, total = search(a.base, a.api, a.out, a.search, a.limit)
        print(json.dumps({"term": a.search, "hits": total, "shown": len(hits)}, ensure_ascii=False))
        for h in hits:
            print(h)
    elif a.ensure:
        print(json.dumps({"did": ensure(a.base, a.api, a.out) or ["nothing"], "index": a.out}))
    elif a.index:
        n, raw = index(a.base, a.api, a.out)
        print(json.dumps({"file": a.out, "topics": n, "streamed": raw}, ensure_ascii=False))
    elif a.topic:
        print(topic(a.base, a.api, a.topic))
    else:
        p.error("passe --search, --topic, --ensure ou --index")


if __name__ == "__main__":
    sys.exit(main())
