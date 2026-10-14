#!/usr/bin/env python3
"""Build the companion from cached Android/Godot dependencies (no secrets/network).
Run after an ordinary Android Gradle export has populated its dependency cache.
ANDROID_HOME and GRADLE_USER_HOME may select non-default SDK/cache locations.
Only the companion AAR is written; original plugin binaries are never modified.
"""
from pathlib import Path
import io, os, subprocess, tempfile, zipfile
root = Path(__file__).resolve().parent
cache = Path(os.environ.get('GRADLE_USER_HOME', str(Path.home() / '.gradle'))) / 'caches'
sdk = Path(os.environ.get('ANDROID_HOME', str(Path.home() / 'Android/Sdk')))
android = sorted((sdk / 'platforms').glob('*/android.jar'))[-1]
# Include extracted AAR jars used by the project's existing export, never package them again.
jars = list(cache.glob('*/transforms/*/transformed/*/jars/classes.jar'))
jars += list(cache.glob('modules-2/files-2.1/**/*.jar'))
if not any('godot-lib' in str(p) for p in jars): raise SystemExit('Missing cached Godot Android library; export Android first.')
with tempfile.TemporaryDirectory(prefix='idle-ai-firebase-bridge-') as tmp:
    classes = Path(tmp) / 'classes'; classes.mkdir()
    subprocess.run(['javac', '-proc:none', '-Xlint:-options', '-source', '8', '-target', '8', '-classpath', os.pathsep.join(map(str, [android] + jars)),
                    '-d', str(classes)] + [str(p) for p in root.glob('src/**/*.java')], check=True)
    binary = io.BytesIO()
    def entry(archive, name, data):
        info = zipfile.ZipInfo(name, (1980, 1, 1, 0, 0, 0)); info.compress_type = zipfile.ZIP_DEFLATED
        archive.writestr(info, data)
    with zipfile.ZipFile(binary, 'w') as jar:
        for file in sorted(classes.rglob('*.class')): entry(jar, file.relative_to(classes).as_posix(), file.read_bytes())
    out = root.parent / 'bin/IdleAiFirebaseAuth.aar'
    with zipfile.ZipFile(out, 'w') as aar:
        entry(aar, 'AndroidManifest.xml', (root / 'AndroidManifest.xml').read_bytes())
        entry(aar, 'classes.jar', binary.getvalue())
        entry(aar, 'R.txt', b'')
        entry(aar, 'proguard.txt', b'-keep class com.idleai.firebase.IdleAiFirebaseAuth { *; }\n')
    print('Built', out)
