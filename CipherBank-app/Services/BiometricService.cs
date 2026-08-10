// <copyright file="BiometricService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Platform biometric gate. Android uses BiometricPrompt when available;
/// other hosts report unavailable so PIN remains the path.
/// Logical gate only — custody AES key is the device secret in SecureStorage.
/// </summary>
public sealed class BiometricService : IBiometricService
{
    public Task<bool> IsAvailableAsync()
    {
#if ANDROID
        try
        {
            return MainThread.InvokeOnMainThreadAsync(() =>
            {
                var activity = Platform.CurrentActivity;
                if (activity is null)
                {
                    return false;
                }

                var manager = AndroidX.Biometric.BiometricManager.From(activity);
                int result = manager.CanAuthenticate(
                    AndroidX.Biometric.BiometricManager.Authenticators.BiometricStrong
                    | AndroidX.Biometric.BiometricManager.Authenticators.DeviceCredential);
                return result == AndroidX.Biometric.BiometricManager.BiometricSuccess;
            });
        }
        catch
        {
            return Task.FromResult(false);
        }
#else
        return Task.FromResult(false);
#endif
    }

    public Task<bool> AuthenticateAsync(string reason)
    {
#if ANDROID
        return AuthenticateAndroidAsync(reason);
#else
        return Task.FromResult(false);
#endif
    }

#if ANDROID
    private static Task<bool> AuthenticateAndroidAsync(string reason)
    {
        TaskCompletionSource tcs = new TaskCompletionSource<bool>();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var activity = Platform.CurrentActivity as AndroidX.Fragment.App.FragmentActivity;
                if (activity is null)
                {
                    tcs.TrySetResult(false);
                    return;
                }

                var executor = AndroidX.Core.Content.ContextCompat.GetMainExecutor(activity);
                if (executor is null)
                {
                    tcs.TrySetResult(false);
                    return;
                }

                BioCallback callback = new BioCallback(tcs);
                AndroidX.Biometric.BiometricPrompt prompt = new AndroidX.Biometric.BiometricPrompt(activity, executor, callback);
                AndroidX.Biometric.BiometricPrompt.PromptInfo.Builder info = new AndroidX.Biometric.BiometricPrompt.PromptInfo.Builder()
                    .SetTitle("CipherBank")
                    .SetSubtitle(reason)
                    .SetAllowedAuthenticators(
                        AndroidX.Biometric.BiometricManager.Authenticators.BiometricStrong
                        | AndroidX.Biometric.BiometricManager.Authenticators.DeviceCredential)
                    .Build();
                prompt.Authenticate(info);
            }
            catch
            {
                tcs.TrySetResult(false);
            }
        });
        return tcs.Task;
    }

    private sealed class BioCallback : AndroidX.Biometric.BiometricPrompt.AuthenticationCallback
    {
        private readonly TaskCompletionSource<bool> _tcs;

        public BioCallback(TaskCompletionSource<bool> tcs) => _tcs = tcs;

        public override void OnAuthenticationSucceeded(AndroidX.Biometric.BiometricPrompt.AuthenticationResult result)
            => _tcs.TrySetResult(true);

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString)
            => _tcs.TrySetResult(false);

        public override void OnAuthenticationFailed()
        {
            // Keep waiting for success or terminal error.
        }
    }
#endif
}
