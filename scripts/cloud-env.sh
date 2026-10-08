#!/usr/bin/env bash
# Source this file in the prepared cloud environment. Local IDE users do not need it.
export DOTNET_ROOT=/workspace/.dotnet
export PATH="$DOTNET_ROOT:/workspace/.android-sdk/platform-tools:$PATH"
export DOTNET_CLI_HOME=/workspace/.cloud-setup/dotnet-home
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NUGET_SIGNATURE_VERIFICATION=true
export NUGET_PACKAGES=/workspace/.nuget/packages
export JAVA_HOME=/workspace/.jdk/jdk-21.0.12.1
export ANDROID_HOME=/workspace/.android-sdk
export npm_config_cache=/workspace/.npm-cache
