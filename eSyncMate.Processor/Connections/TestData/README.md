# Committed sample documents (W3-23)

One saved document per type, sitting beside the renderers that produce them. The tracker's rule is
that no map is `Done` without its sample committed alongside it, so these land with the code rather
than after it.

## What these are

Every `.edi` here is **the artifact eSyncMate actually put on the partner's transfer**, byte for
byte, read back out of `EDILedgerArtifact` — not a document re-rendered to make a sample. That
matters: a re-rendered sample proves only that the renderer agrees with itself today, while a
delivered artifact is evidence of what a partner received, carries the hash it was stored under,
and cannot quietly drift from what shipped.

Each was taken from the newest delivered ledger row of its type on the EU instance, 2026-09-09.

| File | Ledger row | Control number | Bytes | SHA-256 |
|---|---|---|---|---|
| `846.x12.edi` | 323 | 323 | 29,063 | `291c6907ff1a33df27b0b37bc5dc6fe5ff2492299936ed71275cf1a2fcca7135` |
| `855.x12.edi` | 318 | 318 | 500 | `829c44d269ffe03f1d005a509834f61265ec59f94fed62392b83297c2ef1d908` |
| `856.x12.edi` | 305 | 305 | 1,085 | `abc8bf771ef550a8c9b242c3e2dc65ed7d9d1857271bcdc0134789ea1231319c` |
| `865.x12.edi` | 178 | 178 | 294 | `60cd1e65d8c3fea723fa5c7eefaec74ca2fe59178fea720b04e51de577c17d1e` |
| `997.901.edi` | 324 (inbound) | 901 | 244 | — |
| `997.902.edi` | 325 (inbound) | 902 | 269 | — |

The control number is the ledger row id in every case, which is EQ-01 visible in a file you can
open: `ISA13` and `GS06` both carry it, and it is what an inbound 997 quotes back in `AK102`.

## The two 997s

These are **not** partner documents. No partner is returning acknowledgments yet, so both were
written by hand to exercise the correlation path (W2-10) and are kept because they are the only
examples of the shape that exist:

- `997.901.edi` — a clean accept (`AK9*A`) of the 855 in `855.x12.edi`.
- `997.902.edi` — a rejection (`AK5*R*5`, `AK9*R`) of the 856 in `856.x12.edi`.

That second one is why ledger row 305 carries *"The partner acknowledged this as Rejected"*. The
ASN itself is a perfectly good sample; the rejection is a fact about the test, not about the
document.

**Replace both the moment a real partner acknowledgment arrives.** A hand-written 997 proves the
reader parses what we think a 997 looks like, which is not the same as proving it parses what a
partner sends.

## What varies between runs

These are reference samples, not golden files to assert against byte-for-byte. Re-rendering the
same canonical payload tomorrow changes:

- `ISA09` / `ISA10` and `GS04` / `GS05` — the date and time of the interchange
- `ISA13` / `GS06` / the file name — the control number, which is the ledger row id and therefore
  new on every send

Everything below the envelope is deterministic for a given canonical payload. Assert on that.

## Missing

- **850** — the inbound partner order. Its sample belongs with W3-01, whose remaining work is a real
  saved partner file rather than a generated one, so it is deliberately not faked here.
- **The canonical payloads** that produced each rendering. eSyncMate keeps the as-sent artifact, not
  the canonical input BizMate staged, so they are not ours to commit; BizMate holds them against the
  `rawFileRef` on each ledger row.
