# PNG asset optimization results

- PNGs scanned: **213** (entire repository except Git internals).
- PNGs modified: **95**.
- All scanned PNGs: **367,088,123 → 214,726,297 bytes** (367.09 → 214.73 MB).
- Modified PNGs alone: **180,305,862 → 27,944,036 bytes** (180.31 → 27.94 MB).
- Saved: **152.36 MB** (145.30 MiB).

ImageMagick resized machine/bot sprites and skins within 512×512, token/level icons within 256×256, using aspect-preserving downscaling only. Alpha channels retained; no lossy PNG encoding used. oxipng is unavailable, so its optional lossless pass was skipped.

## Verification

- All 213 original PNG paths still exist.
- All 118 excluded PNGs have identical SHA-256 hashes.
- All 503 existing `.import` files have identical SHA-256 hashes.
- Every modified image decodes, matches its recommended dimensions, stays within its maximum, and retains its alpha channel where present.
- Checked 142 literal PNG resource references, including 142 references resolved against the original PNG inventory: no newly broken paths.
- 0 pre-existing unresolved literal references were found; none were introduced by resizing.
- No files renamed/deleted; no commits or pushes performed.

Godot will regenerate imported textures when the project is next opened/imported. These numbers measure source PNG savings, not a rebuilt Android AAB; packaged savings depend on import compression and the next export.

## Modified files by category

### machine sprites (16)

- `assets/machines/room_1/machine1.png` — 1448×1086 → 512×384; 1,205,255 → 193,487 bytes
- `assets/machines/room_1/machine2.png` — 1448×1086 → 512×384; 1,464,833 → 244,304 bytes
- `assets/machines/room_1/machine3.png` — 1448×1086 → 512×384; 1,575,731 → 263,018 bytes
- `assets/machines/room_1/machine4.png` — 1448×1086 → 512×384; 1,263,357 → 207,654 bytes
- `assets/machines/room_2/machine1.png` — 1448×1086 → 512×384; 1,250,527 → 206,042 bytes
- `assets/machines/room_2/machine2.png` — 1448×1086 → 512×384; 1,427,649 → 226,993 bytes
- `assets/machines/room_2/machine3.png` — 1448×1086 → 512×384; 1,692,671 → 272,758 bytes
- `assets/machines/room_2/machine4.png` — 1448×1086 → 512×384; 1,916,885 → 314,639 bytes
- `assets/machines/room_3/machine1.png` — 1448×1086 → 512×384; 1,435,913 → 235,435 bytes
- `assets/machines/room_3/machine2.png` — 1448×1086 → 512×384; 1,792,116 → 286,848 bytes
- `assets/machines/room_3/machine3.png` — 1448×1086 → 512×384; 2,078,636 → 338,705 bytes
- `assets/machines/room_3/machine4.png` — 1448×1086 → 512×384; 2,329,695 → 371,184 bytes
- `assets/machines/room_4/machine1.png` — 1448×1086 → 512×384; 1,636,066 → 259,061 bytes
- `assets/machines/room_4/machine2.png` — 1448×1086 → 512×384; 1,843,518 → 301,987 bytes
- `assets/machines/room_4/machine3.png` — 1448×1086 → 512×384; 2,139,242 → 342,346 bytes
- `assets/machines/room_4/machine4.png` — 1448×1086 → 512×384; 2,391,417 → 374,556 bytes

### machine skins (48)

- `assets/machines/room_1/skin_halloween/machine_1.png` — 1448×1086 → 512×384; 2,019,196 → 291,685 bytes
- `assets/machines/room_1/skin_halloween/machine_2.png` — 1448×1086 → 512×384; 2,274,978 → 348,126 bytes
- `assets/machines/room_1/skin_halloween/machine_3.png` — 1448×1086 → 512×384; 2,369,387 → 358,119 bytes
- `assets/machines/room_1/skin_halloween/machine_4.png` — 1448×1086 → 512×384; 1,596,686 → 250,945 bytes
- `assets/machines/room_1/skin_lab/machine_1.png` — 1448×1086 → 512×384; 1,709,151 → 257,633 bytes
- `assets/machines/room_1/skin_lab/machine_2.png` — 1448×1086 → 512×384; 2,099,490 → 324,444 bytes
- `assets/machines/room_1/skin_lab/machine_3.png` — 1448×1086 → 512×384; 2,013,101 → 317,761 bytes
- `assets/machines/room_1/skin_lab/machine_4.png` — 1448×1086 → 512×384; 1,643,240 → 260,324 bytes
- `assets/machines/room_1/skin_retro/machine1.png` — 1448×1086 → 512×384; 1,742,201 → 240,110 bytes
- `assets/machines/room_1/skin_retro/machine2.png` — 1448×1086 → 512×384; 1,946,552 → 266,664 bytes
- `assets/machines/room_1/skin_retro/machine3.png` — 1536×1024 → 512×341; 1,830,107 → 224,108 bytes
- `assets/machines/room_1/skin_retro/machine4.png` — 1214×1295 → 480×512; 2,069,218 → 366,388 bytes
- `assets/machines/room_2/skin_halloween/machine_1.png` — 1448×1086 → 512×384; 1,676,738 → 254,850 bytes
- `assets/machines/room_2/skin_halloween/machine_2.png` — 1448×1086 → 512×384; 1,873,324 → 285,634 bytes
- `assets/machines/room_2/skin_halloween/machine_3.png` — 1448×1086 → 512×384; 2,211,399 → 329,488 bytes
- `assets/machines/room_2/skin_halloween/machine_4.png` — 1448×1086 → 512×384; 2,544,510 → 388,351 bytes
- `assets/machines/room_2/skin_lab/machine_1.png` — 1448×1086 → 512×384; 1,564,651 → 247,417 bytes
- `assets/machines/room_2/skin_lab/machine_2.png` — 1448×1086 → 512×384; 1,793,420 → 277,726 bytes
- `assets/machines/room_2/skin_lab/machine_3.png` — 1448×1086 → 512×384; 2,113,765 → 323,792 bytes
- `assets/machines/room_2/skin_lab/machine_4.png` — 1448×1086 → 512×384; 2,425,376 → 368,512 bytes
- `assets/machines/room_2/skin_retro/machine1.png` — 1122×1402 → 410×512; 2,154,167 → 326,714 bytes
- `assets/machines/room_2/skin_retro/machine2.png` — 1448×1086 → 512×384; 1,695,620 → 242,006 bytes
- `assets/machines/room_2/skin_retro/machine3.png` — 1448×1086 → 512×384; 2,147,799 → 313,308 bytes
- `assets/machines/room_2/skin_retro/machine4.png` — 1448×1086 → 512×384; 1,997,012 → 316,249 bytes
- `assets/machines/room_3/skin_halloween/machine_1.png` — 1448×1086 → 512×384; 1,986,470 → 298,582 bytes
- `assets/machines/room_3/skin_halloween/machine_2.png` — 1448×1086 → 512×384; 2,405,421 → 353,077 bytes
- `assets/machines/room_3/skin_halloween/machine_3.png` — 1448×1086 → 512×384; 2,562,426 → 395,390 bytes
- `assets/machines/room_3/skin_halloween/machine_4.png` — 1448×1086 → 512×384; 2,812,934 → 416,455 bytes
- `assets/machines/room_3/skin_lab/machine_1.png` — 1448×1086 → 512×384; 1,912,014 → 292,906 bytes
- `assets/machines/room_3/skin_lab/machine_2.png` — 1448×1086 → 512×384; 2,359,622 → 355,068 bytes
- `assets/machines/room_3/skin_lab/machine_3.png` — 1448×1086 → 512×384; 2,620,039 → 392,057 bytes
- `assets/machines/room_3/skin_lab/machine_4.png` — 1448×1086 → 512×384; 2,908,427 → 414,554 bytes
- `assets/machines/room_3/skin_retro/machine1.png` — 1448×1086 → 512×384; 1,851,872 → 276,584 bytes
- `assets/machines/room_3/skin_retro/machine2.png` — 1448×1086 → 512×384; 2,257,099 → 344,714 bytes
- `assets/machines/room_3/skin_retro/machine3.png` — 1448×1086 → 512×384; 2,429,729 → 375,520 bytes
- `assets/machines/room_3/skin_retro/machine4.png` — 1448×1086 → 512×384; 2,580,433 → 403,740 bytes
- `assets/machines/room_4/skin_halloween/machine_1.png` — 1448×1086 → 512×384; 2,103,843 → 309,310 bytes
- `assets/machines/room_4/skin_halloween/machine_2.png` — 1448×1086 → 512×384; 2,173,179 → 333,099 bytes
- `assets/machines/room_4/skin_halloween/machine_3.png` — 1448×1086 → 512×384; 2,698,117 → 393,753 bytes
- `assets/machines/room_4/skin_halloween/machine_4.png` — 1448×1086 → 512×384; 2,789,242 → 406,087 bytes
- `assets/machines/room_4/skin_lab/machine_1.png` — 1448×1086 → 512×384; 1,983,717 → 294,352 bytes
- `assets/machines/room_4/skin_lab/machine_2.png` — 1448×1086 → 512×384; 2,225,906 → 341,732 bytes
- `assets/machines/room_4/skin_lab/machine_3.png` — 1448×1086 → 512×384; 2,499,283 → 383,646 bytes
- `assets/machines/room_4/skin_lab/machine_4.png` — 1448×1086 → 512×384; 2,700,334 → 402,755 bytes
- `assets/machines/room_4/skin_retro/machine1.png` — 1447×1087 → 512×385; 1,748,776 → 266,583 bytes
- `assets/machines/room_4/skin_retro/machine2.png` — 1447×1087 → 512×385; 2,054,861 → 327,611 bytes
- `assets/machines/room_4/skin_retro/machine3.png` — 1447×1087 → 512×385; 2,438,553 → 382,342 bytes
- `assets/machines/room_4/skin_retro/machine4.png` — 1447×1087 → 512×385; 2,500,588 → 399,783 bytes

### bot sprites (4)

- `assets/bots/common.png` — 1448×1086 → 512×384; 954,094 → 163,587 bytes
- `assets/bots/epic.png` — 1287×1222 → 512×486; 1,290,439 → 268,594 bytes
- `assets/bots/legendary.png` — 1448×1086 → 512×384; 1,436,273 → 244,328 bytes
- `assets/bots/rare.png` — 1448×1086 → 512×384; 1,011,182 → 174,091 bytes

### bot skins (24)

- `assets/bots/apocalypse_skin/common.png` — 1254×1254 → 512×512; 1,901,442 → 371,278 bytes
- `assets/bots/apocalypse_skin/epic.png` — 1294×1216 → 512×481; 1,811,670 → 327,362 bytes
- `assets/bots/apocalypse_skin/legendary.png` — 1402×1122 → 512×410; 2,209,639 → 350,106 bytes
- `assets/bots/apocalypse_skin/rare.png` — 1448×1086 → 512×384; 1,473,700 → 217,744 bytes
- `assets/bots/halloween_skin/common.png` — 1448×1086 → 512×384; 1,302,187 → 222,091 bytes
- `assets/bots/halloween_skin/epic.png` — 1448×1086 → 512×384; 1,493,618 → 248,578 bytes
- `assets/bots/halloween_skin/legendary.png` — 1448×1086 → 512×384; 1,618,674 → 270,496 bytes
- `assets/bots/halloween_skin/rare.png` — 1448×1086 → 512×384; 1,415,933 → 238,722 bytes
- `assets/bots/korpo_skin/common.png` — 1448×1086 → 512×384; 1,135,011 → 192,642 bytes
- `assets/bots/korpo_skin/epic.png` — 1448×1086 → 512×384; 1,288,504 → 210,760 bytes
- `assets/bots/korpo_skin/legendary.png` — 1254×1254 → 512×512; 1,733,540 → 362,215 bytes
- `assets/bots/korpo_skin/rare.png` — 1448×1086 → 512×384; 1,192,626 → 196,696 bytes
- `assets/bots/military_skin/common.png` — 1312×1199 → 512×468; 1,483,089 → 285,905 bytes
- `assets/bots/military_skin/epic.png` — 1287×1222 → 512×486; 1,518,956 → 294,648 bytes
- `assets/bots/military_skin/legendary.png` — 1447×1087 → 512×385; 1,921,832 → 302,562 bytes
- `assets/bots/military_skin/rare.png` — 1447×1087 → 512×385; 1,284,331 → 209,299 bytes
- `assets/bots/nature_overgrown_skin/common.png` — 1447×1087 → 512×385; 1,614,385 → 262,561 bytes
- `assets/bots/nature_overgrown_skin/epic.png` — 1287×1222 → 512×486; 1,923,098 → 358,206 bytes
- `assets/bots/nature_overgrown_skin/legendary.png` — 1447×1087 → 512×385; 2,228,682 → 326,403 bytes
- `assets/bots/nature_overgrown_skin/rare.png` — 1319×1193 → 512×463; 1,784,523 → 343,079 bytes
- `assets/bots/steampunk_skin/common.png` — 1448×1086 → 512×384; 1,352,958 → 210,492 bytes
- `assets/bots/steampunk_skin/epic.png` — 1122×1402 → 410×512; 1,735,161 → 287,694 bytes
- `assets/bots/steampunk_skin/legendary.png` — 1448×1086 → 512×384; 1,804,674 → 279,545 bytes
- `assets/bots/steampunk_skin/rare.png` — 1448×1086 → 512×384; 1,384,074 → 212,375 bytes

### token icons (1)

- `assets/ui/token.png` — 1254×1254 → 256×256; 1,547,802 → 101,948 bytes

### level icons (2)

- `assets/ui/core_level.png` — 1254×1254 → 256×256; 2,292,124 → 125,291 bytes
- `assets/ui/level.png` — 1254×1254 → 256×256; 1,604,157 → 105,667 bytes

