#!/usr/bin/env python
"""
Lê a ajuda oficial do TIA Portal (o que o F1 abre) como texto, sem navegador.

NAV INDEX
  1-30    contrato/protocolo (por que não dá pra usar curl nem Invoke-WebRequest)
  32-52   client() / api()      — HTTP/2 + TLS sem verificação
  54-76   topic()               — ItemId "PKG/TOC/ID.htm" -> texto limpo
  78-104  index()               — TOC inteiro -> arquivo "ItemId|Título" por linha (grep depois)
 106-130  main()

Protocolo (descoberto em runtime, não documentado):
  - O viewer é um SPA em https://localhost:<porta>/ ; a API fica em /<api>/HelpViewer/...
    onde <api> é o valor do parâmetro ?api= da URL do F1 (ex.: PortalV21).
  - Servidor só fala **HTTP/2 sobre TLS**. Isso exclui `curl.exe` do Windows e
    `Invoke-WebRequest` (schannel: SEC_E_ILLEGAL_MESSAGE / conexão fechada sem resposta).
    Daí httpx[http2] (OpenSSL). Certificado é self-signed -> verify=False.
  - /HelpViewer/Search responde 404 (assinatura não confere); o índice sai do /Toc.
  - /HelpViewer/Html quer helpReferenceString = objeto HelpReference completo; com campo
    faltando devolve `null` com status 200, não erro.

Uso:
  python scripts/tia-help.py --index                      # 1x: cria workspace/help-index.txt
  grep -i "clock memory" workspace/help-index.txt
  python scripts/tia-help.py --topic ProgKOP2MenUS/10867183243/10383119371.htm
"""
import argparse
import html
import json
import os
import re
import sys
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


def main():
    p = argparse.ArgumentParser(description="Ajuda do TIA Portal (F1) como texto.")
    p.add_argument("--base", default=DEFAULT_BASE, help="default " + DEFAULT_BASE)
    p.add_argument("--api", default=DEFAULT_API, help="valor do ?api= (default PortalV21)")
    p.add_argument("--topic", help='ItemId "PKG/TOC/ID.htm"')
    p.add_argument("--index", action="store_true", help="gera o índice pesquisável")
    p.add_argument("--out", default=os.path.join("workspace", "help-index.txt"))
    a = p.parse_args()
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")  # ajuda tem CJK; console é cp1252
    if a.index:
        n, raw = index(a.base, a.api, a.out)
        print(json.dumps({"file": a.out, "topics": n, "streamed": raw}, ensure_ascii=False))
    elif a.topic:
        print(topic(a.base, a.api, a.topic))
    else:
        p.error("passe --index ou --topic")


if __name__ == "__main__":
    sys.exit(main())
