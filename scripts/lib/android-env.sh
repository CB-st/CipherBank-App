#!/usr/bin/env bash
# Exports Android + .NET paths for E2E harness.
# Use: High (every e2e script invocation). Scope: process-wide shell env.
export ANDROID_HOME="${ANDROID_HOME:-$HOME/Android/Sdk}"
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.local/dotnet}"
export PATH="$DOTNET_ROOT:$ANDROID_HOME/emulator:$ANDROID_HOME/platform-tools:$ANDROID_HOME/cmdline-tools/latest/bin:$PATH"
export CB_MAUI_PACKAGE="${CB_MAUI_PACKAGE:-com.companyname.cipherbankapp}"
export CB_AVD="${CB_AVD:-CipherBank_API34}"
