#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""
manual_annotate.py — 관리자 사용설명서 그림: 원본 PNG(docs/manual/_png · 커밋 제외) → 강조 표시 JPEG(docs/manual/img)

    강조 번호 = 원고 그림 아래 '번호 | 설명' 표 줄(docs/manual/figures.json 의 marks).
    자리는 사진 하네스가 찍을 때 화면에서 재서 남긴 _png/marks.json 을 쓴다 — 손으로 좌표를 적지 않아 늘 같게 그린다.
    모양은 조사자 설명서(S3)와 같다: 빨간 네모(220,30,30 · 3px · 3px 여유) + 왼쪽 위 빨간 원 번호(반지름 13 · 흰 테두리 · Pretendard Bold 16)
    · 폭 1200px 넘으면 줄임(LANCZOS) · JPEG 품질 75(지시서 §30-3) · 작은 창(폭 1200px 미만)은 원래 크기 · 품질 80
    · 그림별 예외는 figures.json "jpeg"(폭 · 품질 — 지도 화면 품질 70 등, 설명서 계획 §7).

사용법
    python tools/manual_annotate.py [그림 파일이름 …]      (없으면 figures.json 의 전부)
    강조 자리를 못 잰 번호가 있거나 원본이 없으면 그 그림은 '실패'로 적고 종료코드 1.
"""
import io
import json
import pathlib
import sys

from PIL import Image, ImageDraw, ImageFont

if sys.stdout.encoding.lower() != "utf-8":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

ROOT = pathlib.Path(__file__).resolve().parent.parent
MAN = ROOT / "docs" / "manual"
RAW, OUT = MAN / "_png", MAN / "img"
FONT = MAN / "fonts" / "Pretendard-Bold.ttf"
RED = (220, 30, 30)
MAXW, Q, Q_SMALL = 1200, 75, 80


def badge(d, n, x, y, font):
    r = 13
    d.ellipse((x - r, y - r, x + r, y + r), fill=RED, outline="white", width=2)
    t = str(n)
    bb = d.textbbox((0, 0), t, font=font)
    d.text((x - (bb[2] - bb[0]) / 2 - bb[0], y - (bb[3] - bb[1]) / 2 - bb[1]), t, fill="white", font=font)


def render(fig, measured, font):
    name = fig["file"]
    src = RAW / f"{name}.png"
    if not src.exists():
        return f"원본 없음 {src.name}"
    m = measured.get(name)
    if m is None:
        return "marks.json 에 없음"
    rects = {x["n"]: x.get("rect") for x in m["marks"]}
    missing = [mk["n"] for mk in fig["marks"] if not rects.get(mk["n"])]
    if missing:
        return f"강조 자리 못 잼 {missing}"
    im = Image.open(src).convert("RGB")
    W, H = im.size
    outside = [n for n, (x, y, w, h) in ((mk["n"], rects[mk["n"]]) for mk in fig["marks"])
               if x + w <= 0 or y + h <= 0 or x >= W or y >= H]
    if outside:
        return f"강조 자리가 그림 밖 {outside}"
    d = ImageDraw.Draw(im)
    boxes = []
    for mk in fig["marks"]:
        x, y, w, h = rects[mk["n"]]
        x, y, w, h = x - 3, y - 3, w + 6, h + 6
        x0, y0, x1, y1 = max(1, x), max(1, y), min(W - 2, x + w), min(H - 2, y + h)
        d.rectangle((x0, y0, x1, y1), outline=RED, width=3)
        boxes.append((mk["n"], x0, y0))
    for n, x0, y0 in boxes:
        badge(d, n, min(max(14, x0), W - 15), min(max(14, y0), H - 15), font)
    # 그림별 예외(figures.json "jpeg": {"w": 폭, "q": 품질}) — 지도처럼 큰 그림만 PDF 3.0 MB 안에 들게(설명서 계획 §7)
    opt = fig.get("jpeg", {})
    maxw = opt.get("w", MAXW)
    q = opt.get("q", Q if im.width >= MAXW else Q_SMALL)
    if im.width > maxw:
        im = im.resize((maxw, round(im.height * maxw / im.width)), Image.LANCZOS)
    OUT.mkdir(parents=True, exist_ok=True)
    im.save(OUT / f"{name}.jpg", "JPEG", quality=q, optimize=True, progressive=False)
    return None


def main():
    figs = json.loads((MAN / "figures.json").read_text(encoding="utf-8"))["figures"]
    measured = json.loads((RAW / "marks.json").read_text(encoding="utf-8")) if (RAW / "marks.json").exists() else {}
    want = set(sys.argv[1:])
    font = ImageFont.truetype(str(FONT), 16)
    bad, total = 0, 0
    for f in figs:
        if want and f["file"] not in want and f["id"] not in want:
            continue
        total += 1
        err = render(f, measured, font)
        if err:
            bad += 1
            print(f"실패  {f['id']} {f['file']}: {err}")
        else:
            j = OUT / f"{f['file']}.jpg"
            with Image.open(j) as im:
                print(f"ok    {f['id']} {f['file']} {im.width}×{im.height} · 강조 {len(f['marks'])} · {j.stat().st_size / 1024:.0f} KB")
    size = sum(p.stat().st_size for p in OUT.glob("*.jpg"))
    print(f"그림  {total - bad}/{total} · img 폴더 JPEG {len(list(OUT.glob('*.jpg')))}장 {size / 1024 / 1024:.2f} MB")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
