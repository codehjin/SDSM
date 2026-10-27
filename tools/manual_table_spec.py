#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""
manual_table_spec.py — 관리자 사용설명서 부록 B 테이블 명세서 자동 생성(설명서 계획 §5 · D2 · D3)

근거 두 가지를 맞대어 만든다.
    SDSM_Models\*.cs   [Table] · [Key] · [Required] · [DisplayName](항목명) · 줄 끝 주석(소수점 n자리)
                       [NotMapped] · [ForeignKey] 탐색 속성 · ICollection · => 식 속성은 뺀다
    DB 사본            pragma table_info · foreign_key_list — 열 이름 · SQLite 형 · NOT NULL · PK · FK
    tools\manual_spec_notes.csv   비고 보충(표, 열, 항목명, 비고) — 손으로 관리

칸: No. · 항목명 · 열 이름 · 데이터 타입 · 필수 · KEY · 비고 ('길이' 칸은 없음 — SQLite 는 글자 길이를 제한하지 않는다)
표: 주 표 9 + SiteDivision + 종목록 2 = 12표(SelfUser · SavedFileInfo · 뷰 · 식생 4표는 뺀다)

사용법
    python tools/manual_table_spec.py --db <DB 사본> [--write <원고.md>] [--check <원고.md>]
      --write  원고의 '자동 생성 시작/끝' 표시 사이를 새로 만든 명세서로 바꾼다
      --check  모델 ↔ DB 열이 어긋나거나 원고의 명세서가 지금 만든 것과 다르면 종료코드 1
    DB 는 사본만 받는다 — mode=ro&immutable=1 로 열고, 실행 폴더 DB(SDSMDB.sqlite) · bin · 보낸파일 경로면 멈춘다.
    Python 표준 라이브러리만 쓴다.
"""
import argparse
import csv
import datetime
import io
import pathlib
import re
import sqlite3
import subprocess
import sys

if sys.stdout.encoding.lower() != "utf-8":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

ROOT = pathlib.Path(__file__).resolve().parent.parent
MODELS = ROOT / "SDSM_Models"
NOTES = ROOT / "tools" / "manual_spec_notes.csv"
BEGIN = "<!-- manual_table_spec: 자동 생성 시작 — 직접 고치지 말 것(tools/manual_table_spec.py 로 다시 만든다) -->"
END = "<!-- manual_table_spec: 자동 생성 끝 -->"
META = "<!-- manual_table_spec 근거:"

# (표, 제목, 한 줄 설명)
TABLES = [
    ("Fish", "어류", "어류 조사 한 번(연도차수 · 지점)마다 한 줄 — 조사개황 · 채집 · 하상 · 비정상종 · 군집정보 · FAI. 출현종은 B.2."),
    ("FishSpecies", "어류 출현종", "어류 조사 한 번의 출현종 — 종마다 한 줄. 지표값 · 길드는 종목록(B.11) 값."),
    ("Benthos", "저서동물", "저서동물 조사 한 번마다 한 줄 — 조사개황 · 채집 · 서식처 · 하상 · 군집지수 · BMI. 출현종은 B.4."),
    ("BenthosSpecies", "저서동물 출현종", "저서동물 조사 한 번의 출현종 — 종마다 한 줄. 오탁치 · 지표가중치는 종목록(B.12) 값."),
    ("Bird", "조류", "관찰 기록마다 한 줄."),
    ("Mammal", "포유류", "관찰 기록마다 한 줄 — 확인 · 흔적 12칸은 건수."),
    ("AmphibianReptile", "양서파충류", "관찰 기록마다 한 줄 — 관찰 6칸은 건수."),
    ("HabitatWaterEdge", "서식수변", "조사 한 번마다 한 줄 — 평가항목 10 · 평가점수 · 평가등급."),
    ("WaterQuality", "수질", "조사 한 번마다 한 줄 — 측정항목 · 추가항목 · 수질등급."),
    ("SiteDivision", "프로젝트 사업장 지점", "설정 [프로젝트 사업장 지점]의 지점 구분 — 조사 자료가 지점 구분 ID 로 가리킨다."),
    ("FishSpeciesList", "어류 종목록", "어류 종목록(지침 별표 3 정비) — 출현종 지표값 · 길드의 출처(부록 C)."),
    ("BenthosSpeciesList", "저서동물 종목록", "저서동물 종목록(지침 별표 2 정비) — 출현종 오탁치 · 지표가중치의 출처(부록 C)."),
]

ATTR = re.compile(r"\[(\w+)(?:\((.*?)\))?\]")
PROP = re.compile(r"\bpublic\s+(?:virtual\s+)?(?P<type>[\w<>?,\[\]\. ]+?)\s+(?P<name>\w+)\s*(?P<rest>\{|=>)")
DECIMALS = re.compile(r"소수점\s*(\d)\s*자리")


class SpecError(Exception):
    pass


def split_comment(line):
    """코드 · // 뒤 주석(글자 상수 안의 // 는 코드)."""
    in_str, i = False, 0
    while i < len(line):
        c = line[i]
        if c == '"' and (i == 0 or line[i - 1] != "\\"):
            in_str = not in_str
        elif not in_str and line.startswith("//", i):
            return line[:i], line[i + 2:].strip()
        i += 1
    return line, ""


def cs_string(arg):
    m = re.match(r'\s*"((?:[^"\\]|\\.)*)"', arg or "")
    if not m:
        return None
    s = m.group(1).replace("\\n", "\n").replace('\\"', '"')
    s = re.sub(r"\s*\n\s*(?=\()", "", s)   # 줄바꿈 뒤 괄호는 붙여 쓴다 — 진흙이하(<0.063mm)
    return re.sub(r"\s*\n\s*", " ", s).strip()


def read_models():
    """표 이름 → (클래스, [속성 dict]) — 매핑되는 속성만."""
    out = {}
    for f in sorted(MODELS.glob("*.cs")):
        text = f.read_text(encoding="utf-8-sig")
        m = re.search(r'\[Table\("(\w+)"\)\]\s*public\s+class\s+(\w+)', text)
        if not m:
            continue
        table, cls = m.group(1), m.group(2)
        body = text[m.end():]
        props, pending = [], []
        for raw in body.splitlines():
            code, comment = split_comment(raw)
            attrs = [(a, cs_string(v)) for a, v in ATTR.findall(code.split("public")[0])]
            pm = PROP.search(code)
            if not pm:
                if attrs and not code.strip().startswith("//"):
                    pending.extend(attrs)
                continue
            attrs = pending + attrs
            pending = []
            names = {a for a, _ in attrs}
            typ = pm.group("type").strip()
            if pm.group("rest") == "=>" or "NotMapped" in names or "ForeignKey" in names \
                    or "virtual" in code or "ICollection" in typ or "class" in typ:
                continue
            props.append({
                "name": pm.group("name"), "cstype": typ,
                "display": next((v for a, v in attrs if a == "DisplayName"), None),
                "desc": next((v for a, v in attrs if a == "Description"), None),
                "required": "Required" in names, "key": "Key" in names,
                "comment": comment, "file": f.name,
            })
        out[table] = (cls, props)
    return out


def open_db(path):
    p = pathlib.Path(path).resolve()
    s = str(p).lower()
    if p.name.lower() == "sdsmdb.sqlite" or "\\bin\\" in s or "보낸파일" in s:
        raise SpecError(f"DB 사본만 받습니다 — 실행 폴더 DB · bin · 보낸파일 경로는 열지 않음: {p}")
    if not p.exists():
        raise SpecError(f"DB 사본이 없습니다: {p}")
    return sqlite3.connect(f"{p.as_uri()}?mode=ro&immutable=1", uri=True), p


def db_columns(con, table):
    cols = [{"name": r[1], "type": (r[2] or "").upper(), "notnull": bool(r[3]), "pk": bool(r[5])}
            for r in con.execute(f'pragma table_info("{table}")')]
    fks = {r[3]: (r[2], r[4]) for r in con.execute(f'pragma foreign_key_list("{table}")')}
    return cols, fks


def read_notes():
    notes = {}
    with NOTES.open(encoding="utf-8-sig", newline="") as f:
        for row in csv.DictReader(f):
            notes[(row["표"].strip(), row["열"].strip())] = (row["항목명"].strip(), row["비고"].strip())
    return notes


def cell(s):
    return (s or "").replace("|", "\\|").strip()


def build(con, models, notes):
    lines, problems = [], []
    for i, (table, title, intro) in enumerate(TABLES, 1):
        if table not in models:
            problems.append(f"{table}: 모델에 [Table] 이 없음")
            continue
        cls, props = models[table]
        cols, fks = db_columns(con, table)
        if not cols:
            problems.append(f"{table}: DB 에 표가 없음")
            continue
        dbmap = {c["name"]: c for c in cols}
        mnames = [p["name"] for p in props]
        only_model = [n for n in mnames if n not in dbmap]
        only_db = [c["name"] for c in cols if c["name"] not in mnames]
        if only_model or only_db:
            problems.append(f"{table}: 모델에만 {only_model} · DB 에만 {only_db}")
        lines += [f"## B.{i}. {title}({table})", "", intro, "",
                  "| No. | 항목명 | 열 이름 | 데이터 타입 | 필수 | KEY | 비고 |",
                  "|---|---|---|---|---|---|---|"]
        for n, p in enumerate(props, 1):
            c = dbmap.get(p["name"], {"type": "?", "notnull": False, "pk": False})
            fk = fks.get(p["name"])
            label_note = notes.get((table, p["name"]), ("", ""))
            label = label_note[0] or p["display"] or ("ID" if c["pk"] else p["desc"] or p["name"])
            auto = []
            if c["pk"]:
                auto.append("자동 번호")
            if fk:
                auto.append(f"→ {fk[0]}.{fk[1]}")
            d = DECIMALS.search(p["comment"])
            if d:
                auto.append("소수 6자리로 저장" if p["name"] in ("Lat", "Lng") else f"화면 소수 {d.group(1)}자리")
            # 보충 비고: '=' 로 시작하면 자동 비고를 바꿈 · '자동계산' 으로 시작하면 앞에 · 그 밖은 뒤에 붙임
            note = label_note[1]
            parts = [note] + auto if note.startswith("자동계산") else auto + [note]
            remark = note[1:].strip() if note.startswith("=") else " · ".join(x for x in parts if x)
            req = "Y" if (p["required"] or c["notnull"] or c["pk"]) else ""
            key = "PK" if c["pk"] else ("FK" if fk else "")
            lines.append(f"| {n} | {cell(label)} | `{p['name']}` | {c['type']} | {req} | {key} | {cell(remark)} |")
        lines.append("")
    for (t, col) in notes:
        if t not in models or col not in [p["name"] for p in models[t][1]]:
            problems.append(f"보충 파일의 {t}.{col} 이 모델에 없음")
    return "\n".join(lines).rstrip() + "\n", problems


def model_commit():
    try:
        h = subprocess.run(["git", "-C", str(ROOT), "log", "-1", "--format=%h", "--", "SDSM_Models"],
                           capture_output=True, text=True).stdout.strip()
        dirty = subprocess.run(["git", "-C", str(ROOT), "status", "--porcelain", "--", "SDSM_Models"],
                               capture_output=True, text=True).stdout.strip()
        return h + (" + 작업 폴더 변경" if dirty else "")
    except OSError:
        return "-"


def block(body, dbpath):
    meta = f"{META} 모델 SDSM_Models @ {model_commit()} · DB {dbpath.name} · {datetime.date.today():%Y-%m-%d} -->"
    return f"{BEGIN}\n{meta}\n\n{body}{END}"


def current_block(text):
    i, j = text.find(BEGIN), text.find(END)
    if i < 0 or j < 0 or j < i:
        raise SpecError("원고에 '자동 생성 시작/끝' 표시가 없습니다")
    return i, j + len(END)


def strip_meta(s):
    return "\n".join(l for l in s.splitlines() if not l.startswith(META))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--db", required=True)
    ap.add_argument("--write")
    ap.add_argument("--check")
    a = ap.parse_args()
    try:
        con, dbpath = open_db(a.db)
        models = read_models()
        body, problems = build(con, models, read_notes())
        rows = sum(1 for l in body.splitlines() if re.match(r"\| \d+ \|", l))
        print(f"명세서  {len(TABLES)}표 · {rows}줄 · DB {dbpath.name} · 모델 {model_commit()}")
        new = block(body, dbpath)
        if a.write:
            p = pathlib.Path(a.write)
            text = p.read_text(encoding="utf-8")
            i, j = current_block(text)
            p.write_bytes((text[:i] + new + text[j:]).encode("utf-8"))
            print(f"썼음    {p}")
        if a.check:
            text = pathlib.Path(a.check).read_text(encoding="utf-8")
            i, j = current_block(text)
            if strip_meta(text[i:j]) != strip_meta(new):
                problems.append("원고의 명세서가 지금 모델 · DB 로 만든 것과 다름 — --write 로 다시 만들 것")
        for pr in problems:
            print("문제    " + pr)
        print("검사    " + ("통과" if not problems else f"실패 {len(problems)}"))
        return 0 if not problems else 1
    except SpecError as e:
        print("멈춤    " + str(e))
        return 1


if __name__ == "__main__":
    sys.exit(main())
