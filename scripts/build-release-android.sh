#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${ANDROID_HOME:?}" "${JAVA_HOME:?}" "${MATHPET_RELEASE_KEYSTORE:?}" "${MATHPET_RELEASE_PASSWORD_FILE:?}"
: "${MATHPET_VERSION:?}" "${MATHPET_VERSION_CODE:?}"
[[ "$MATHPET_VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]
[[ "$MATHPET_VERSION_CODE" =~ ^[1-9][0-9]*$ ]]
[[ -f "$MATHPET_RELEASE_KEYSTORE" && -f "$MATHPET_RELEASE_PASSWORD_FILE" ]]
# apksigner consumes successive passwords when the same file is reused.
# Separate private files let keystore/key passwords share a value safely.
umask 077
mathpet_key_password_file="$(mktemp)"
trap 'rm -f "$mathpet_key_password_file"' EXIT
cp "$MATHPET_RELEASE_PASSWORD_FILE" "$mathpet_key_password_file"
dotnet restore src/MathPet.Android/MathPet.Android.csproj --locked-mode -p:Configuration=Release
dotnet build src/MathPet.Android/MathPet.Android.csproj --no-restore -c Release \
  -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory="$JAVA_HOME" \
  -p:ApplicationDisplayVersion="$MATHPET_VERSION" -p:ApplicationVersion="$MATHPET_VERSION_CODE" \
  -p:EmbedAssembliesIntoApk=true -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore="$MATHPET_RELEASE_KEYSTORE" \
  -p:AndroidSigningKeyAlias=mathpet \
  -p:AndroidSigningStorePass="file:$MATHPET_RELEASE_PASSWORD_FILE" \
  -p:AndroidSigningKeyPass="file:$mathpet_key_password_file"
mkdir -p artifacts
mathpet_apk="mathpet-$MATHPET_VERSION.apk"
cp src/MathPet.Android/bin/Release/net10.0-android/com.hypnus.mathpet-Signed.apk "artifacts/$mathpet_apk"
"$ANDROID_HOME/build-tools/36.0.0/apksigner" verify --verbose "artifacts/$mathpet_apk"
python3 scripts/package-release.py
