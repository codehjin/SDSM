# -*- coding: utf-8 -*-
"""
계산 회귀 대조 (05_REFACTORING §2-C · R2)

    dotnet run --project tools\\CalcParity -- --out before.csv   # 바꾸기 전
    ... 계산 코드 수정 ...
    dotnet run --project tools\\CalcParity -- --out after.csv    # 바꾼 뒤
    python tools\\calc_parity_diff.py before.csv after.csv

`GB` 의 계산 결과가 한 건이라도 달라졌는지 본다.
계산식을 손댈 때마다 돌린다 — 과거 조사자료의 해석이 달라지는 것을 막는 마지막 방어선이다.

종료 코드 0 = 전건 일치, 1 = 불일치.
"""
import csv
import io
import sys
from collections import Counter

sys.stdout.reconfigure(encoding="utf-8")

MAX_REPORT = 30


def load(path):
    with io.open(path, encoding="utf-8", newline="") as f:
        rows = list(csv.reader(f))
    if not rows:
        return []
    return rows[1:]   # 머리글 제거


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 2

    a, b = load(sys.argv[1]), load(sys.argv[2])
    print(f"이전 : {sys.argv[1]}  ({len(a):,}건)")
    print(f"이후 : {sys.argv[2]}  ({len(b):,}건)\n")

    if len(a) != len(b):
        print("!! 케이스 수가 다릅니다. 하네스의 케이스를 바꾸면 이전 덤프와 비교할 수 없습니다.")
        print("   케이스를 늘릴 때는 기존 것을 두고 추가하세요.")
        return 1

    diffs = [(x, y) for x, y in zip(a, b) if x != y]

    by_kind = Counter(x[0] for x, _ in diffs)
    for x, y in diffs[:MAX_REPORT]:
        print(f"  [{x[0]}] {x[1]}")
        print(f"      이전 = {x[2]}")
        print(f"      이후 = {y[2]}")
    if len(diffs) > MAX_REPORT:
        print(f"  … 외 {len(diffs) - MAX_REPORT}건")

    print()
    if not diffs:
        print(f"===== 전건 일치 ({len(a):,}건) =====")
        return 0

    print(f"===== 불일치 {len(diffs):,}건 / {len(a):,}건 =====")
    for kind, cnt in by_kind.most_common():
        print(f"   {kind:<12} {cnt}건")
    return 1


if __name__ == "__main__":
    sys.exit(main())
