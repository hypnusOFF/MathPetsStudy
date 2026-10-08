#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${ANDROID_HOME:?Set ANDROID_HOME to the Android SDK directory}"
: "${JAVA_HOME:?Set JAVA_HOME to a JDK 21 directory}"
# Dedicated development key outside the checkout. Retain it for in-place APK updates.
mathpet_key_dir="${MATHPET_KEY_DIR:-/workspace/.cloud-setup/keys}"
mkdir -p "$mathpet_key_dir"
mathpet_keystore="$mathpet_key_dir/android-debug.keystore"
if [[ ! -f "$mathpet_keystore" ]]; then
  "$JAVA_HOME/bin/keytool" -genkeypair -keystore "$mathpet_keystore" -storepass android -keypass android \
    -alias androiddebugkey -keyalg RSA -keysize 2048 -validity 10000 -dname 'CN=Android Debug,O=Android,C=US'
fi
dotnet restore src/MathPet.Android/MathPet.Android.csproj --locked-mode
dotnet build src/MathPet.Android/MathPet.Android.csproj --no-restore -c Debug \
  -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory="$JAVA_HOME" \
  -p:EmbedAssembliesIntoApk=true -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore="$mathpet_keystore" -p:AndroidSigningStorePass=android \
  -p:AndroidSigningKeyAlias=androiddebugkey -p:AndroidSigningKeyPass=android
mkdir -p artifacts
cp src/MathPet.Android/bin/Debug/net10.0-android/com.hypnus.mathpet-Signed.apk artifacts/mathpet-0.1.0.apk
"$ANDROID_HOME/build-tools/36.0.0/apksigner" verify --verbose artifacts/mathpet-0.1.0.apk
sha256sum artifacts/mathpet-0.1.0.apk > artifacts/mathpet-0.1.0.apk.sha256
