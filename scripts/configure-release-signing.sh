#!/usr/bin/env bash
# Run on the owner's trusted computer, not in CI. No secret values are printed.
set -euo pipefail
umask 077
mathpet_private_dir="${MATHPET_SIGNING_DIR:?Set MATHPET_SIGNING_DIR to a private backup directory outside the repository}"
mkdir -p "$mathpet_private_dir"
mathpet_repo_dir="$(cd "$(dirname "$0")/.." && pwd -P)"
mathpet_private_dir="$(cd "$mathpet_private_dir" && pwd -P)"
case "$mathpet_private_dir/" in "$mathpet_repo_dir/"*) echo 'Signing files must stay outside the repository.' >&2; exit 1;; esac
mathpet_key="$mathpet_private_dir/mathpet-release.keystore"
mathpet_pass="$mathpet_private_dir/mathpet-password.txt"
if [[ ! -e "$mathpet_key" && ! -e "$mathpet_pass" ]]; then
  openssl rand -base64 36 > "$mathpet_pass"
  keytool -genkeypair -keystore "$mathpet_key" -storepass:file "$mathpet_pass" -keypass:file "$mathpet_pass" \
    -alias mathpet -keyalg RSA -keysize 3072 -validity 10000 -dname 'CN=MathPet Release'
fi
[[ -f "$mathpet_key" && -f "$mathpet_pass" ]]
keytool -list -keystore "$mathpet_key" -storepass:file "$mathpet_pass" -alias mathpet >/dev/null
base64 < "$mathpet_key" | tr -d '\n' | gh secret set ANDROID_RELEASE_KEYSTORE_BASE64 --repo hypnusOFF/MathPetsStudy
gh secret set ANDROID_RELEASE_PASSWORD --repo hypnusOFF/MathPetsStudy < "$mathpet_pass"
echo 'Signing secrets configured. Keep both private files backed up; never commit them.'
