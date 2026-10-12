# Data Shards display regression tests

```sh
dotnet build
dotnet build tests/DataShards
godot-mono --headless --path tests/DataShards
```

Tests cover every requested example, six live boundary sequences, deterministic
rounding and suffix carry, trimming, four cultures, tiny floating-point noise,
and preservation of Token precision/suffixes. The real Shop header is exercised
with Orbitron and the readability pass: label, panel, and neighboring icon
rectangles must stay identical. Repeated frame refreshes must preserve the same
formatted text and underlying balance; unchanged display buckets skip writes.
Ads are stubbed and never initialized. Cosmetic textures are supplied in memory.
The runner also verifies removal of both competing currency rewrite paths.

Visual rendering and mobile touch/device checks remain manual.
