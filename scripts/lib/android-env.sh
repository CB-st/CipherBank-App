#!/usr/bin/env bash
# Exports Android + .NET paths for E2E harness.
# Use: High (every e2e script invocation). Scope: process-wide shell env.
export ANDROID_HOME="${ANDROID_HOME:-$HOME/Android/Sdk}"
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.local/dotnet}"
export JAVA_HOME="${JAVA_HOME:-$HOME/.local/jdk-17}"
# Some dev boxes ship a broken system npm/npx (apt nodejs/npm version mismatch). If a known-good
# standalone Node.js install is present at CB_NODE_HOME, prefer its bin dir so `npx --yes appium`
# (used by e2e-android.sh) resolves to a working npx instead of the broken system one.
export CB_NODE_HOME="${CB_NODE_HOME:-$HOME/.local/nodejs}"
CB_NODE_BIN=""
[[ -x "$CB_NODE_HOME/bin/npx" ]] && CB_NODE_BIN="$CB_NODE_HOME/bin:"
export PATH="$DOTNET_ROOT:$JAVA_HOME/bin:${CB_NODE_BIN}$ANDROID_HOME/emulator:$ANDROID_HOME/platform-tools:$ANDROID_HOME/cmdline-tools/latest/bin:$PATH"
export CB_MAUI_PACKAGE="${CB_MAUI_PACKAGE:-com.companyname.cipherbankapp}"
export CB_AVD="${CB_AVD:-CipherBank_API34}"
