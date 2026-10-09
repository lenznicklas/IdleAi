// MIT License
//
// Copyright (c) 2023-present Poing Studios
//
// Idle AI robustness adjustments:
// - connect initialization signal BEFORE invoking native initialize
// - initialization is requested only once
// - additional callers can wait for the same initialization
// - missing native plugin / lost callback can no longer leave callers waiting forever

using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PoingStudios.AdMob.Api.Core;
using PoingStudios.AdMob.Api.Listeners;
using PoingStudios.AdMob.Core;

namespace PoingStudios.AdMob.Api
{
    public class MobileAds : MobileSingletonPlugin
    {
        private const double InitializationTimeoutSeconds = 15.0;

        private static readonly GodotObject _plugin =
            GetPlugin("PoingGodotAdMob");

        private static readonly List<OnInitializationCompleteListener>
            _initializationListeners =
                new();

        private static readonly Callable _onInitCompleteCallable =
            Callable.From<Dictionary>(OnInitializationComplete);

        private static AdInspectorClosedListener _currentAdInspectorListener;

        private static readonly Callable _onAdInspectorClosedCallable =
            Callable.From<Dictionary>(OnAdInspectorClosed);

        private static bool _initializationStarted;

        private static bool _initializationCompleted;

        private static InitializationStatus _lastInitializationStatus;

        private static int _initializationGeneration;


        public static bool IsAvailable =>
            _plugin != null
            && GodotObject.IsInstanceValid(_plugin);


        public static bool IsInitialized =>
            _initializationCompleted;


        public static void Initialize(
            OnInitializationCompleteListener listener = null)
        {
            if (listener != null)
            {
                if (_initializationCompleted)
                {
                    InitializationStatus current =
                        _lastInitializationStatus
                        ?? GetInitializationStatus();

                    Callable
                        .From(
                            () =>
                                listener
                                    .OnInitializationComplete?
                                    .Invoke(current)
                        )
                        .CallDeferred();

                    return;
                }

                _initializationListeners.Add(
                    listener
                );
            }

            if (!IsAvailable)
            {
                FailInitialization(
                    "[AdMob] Native MobileAds plugin is not available."
                );

                return;
            }

            if (_initializationCompleted)
                return;

            if (_initializationStarted)
                return;

            _initializationStarted =
                true;

            int generation =
                ++_initializationGeneration;

            /*
             * IMPORTANT:
             * Connect BEFORE calling initialize().
             *
             * The original wrapper called initialize() first and connected
             * afterwards. A very fast native completion could therefore be
             * missed completely.
             */
            SafeConnect(
                _plugin,
                "on_initialization_complete",
                _onInitCompleteCallable,
                (uint)GodotObject.ConnectFlags.OneShot
            );

            /*
             * Defer the native call out of the UI button's pressed callback.
             * This keeps the current input event short and avoids doing native
             * SDK startup directly inside the Shop button handler.
             */
            Callable
                .From(
                    () =>
                    {
                        if (
                            generation
                                != _initializationGeneration
                            || _initializationCompleted
                            || !IsAvailable
                        )
                        {
                            return;
                        }

                        try
                        {
                            _plugin.Call(
                                "initialize"
                            );

                            StartInitializationWatchdog(
                                generation
                            );
                        }
                        catch (Exception exception)
                        {
                            FailInitialization(
                                "[AdMob] MobileAds initialization threw: "
                                + exception.Message
                            );
                        }
                    }
                )
                .CallDeferred();
        }


        private static void StartInitializationWatchdog(
            int generation)
        {
            if (
                Engine.GetMainLoop()
                    is not SceneTree tree
            )
            {
                return;
            }

            SceneTreeTimer timer =
                tree.CreateTimer(
                    InitializationTimeoutSeconds,
                    processAlways: true
                );

            timer.Timeout +=
                () =>
                {
                    if (
                        generation
                            != _initializationGeneration
                        || _initializationCompleted
                        || !_initializationStarted
                    )
                    {
                        return;
                    }

                    FailInitialization(
                        "[AdMob] MobileAds initialization timed out after "
                        + InitializationTimeoutSeconds
                        + " seconds."
                    );
                };
        }


        private static void OnInitializationComplete(
            Dictionary statusDict)
        {
            _initializationStarted =
                false;

            _initializationCompleted =
                true;

            InitializationStatus status =
                InitializationStatus.Create(
                    statusDict
                );

            _lastInitializationStatus =
                status;

            List<OnInitializationCompleteListener> listeners =
                new(
                    _initializationListeners
                );

            _initializationListeners.Clear();

            foreach (
                OnInitializationCompleteListener listener
                    in listeners
            )
            {
                Callable
                    .From(
                        () =>
                            listener
                                .OnInitializationComplete?
                                .Invoke(status)
                    )
                    .CallDeferred();
            }
        }


        private static void FailInitialization(
            string message)
        {
            GD.PushWarning(
                message
            );

            _initializationStarted =
                false;

            _initializationCompleted =
                false;

            _lastInitializationStatus =
                null;

            _initializationGeneration++;

            if (
                IsAvailable
                && _plugin.IsConnected(
                    "on_initialization_complete",
                    _onInitCompleteCallable
                )
            )
            {
                _plugin.Disconnect(
                    "on_initialization_complete",
                    _onInitCompleteCallable
                );
            }

            List<OnInitializationCompleteListener> listeners =
                new(
                    _initializationListeners
                );

            _initializationListeners.Clear();

            foreach (
                OnInitializationCompleteListener listener
                    in listeners
            )
            {
                /*
                 * null explicitly means initialization failed/timed out.
                 * RewardedAdLoader handles this and reports a normal load error
                 * back to ShopController, which clears its loading state.
                 */
                Callable
                    .From(
                        () =>
                            listener
                                .OnInitializationComplete?
                                .Invoke(null)
                    )
                    .CallDeferred();
            }
        }


        public static void SetRequestConfiguration(
            RequestConfiguration config)
        {
            if (!IsAvailable)
                return;

            _plugin.Call(
                "set_request_configuration",
                config.ConvertToDictionary(),
                new Array<string>(
                    config.TestDeviceIds
                )
            );
        }


        public static InitializationStatus
            GetInitializationStatus()
        {
            if (!IsAvailable)
                return null;

            try
            {
                var dict =
                    (Dictionary)_plugin.Call(
                        "get_initialization_status"
                    );

                return InitializationStatus.Create(
                    dict
                );
            }
            catch (Exception exception)
            {
                GD.PushWarning(
                    "[AdMob] Could not read initialization status: "
                    + exception.Message
                );

                return null;
            }
        }


        public static void SetIosAppPauseOnBackground(
            bool pause)
        {
            if (
                IsAvailable
                && OS.GetName() == "iOS"
            )
            {
                _plugin.Call(
                    "set_ios_app_pause_on_background",
                    pause
                );
            }
        }


        public static void SetAppVolume(
            float volume)
        {
            if (!IsAvailable)
                return;

            _plugin.Call(
                "set_app_volume",
                Mathf.Clamp(
                    volume,
                    0.0f,
                    1.0f
                )
            );
        }


        public static void SetAppMuted(
            bool muted)
        {
            if (IsAvailable)
            {
                _plugin.Call(
                    "set_app_muted",
                    muted
                );
            }
        }


        public static void SetPublisherFirstPartyIDEnabled(
            bool enabled)
        {
            if (IsAvailable)
            {
                _plugin.Call(
                    "set_publisher_first_party_id_enabled",
                    enabled
                );
            }
        }


        public static void SetGadHasConsentForCookies(
            bool enabled)
        {
            if (IsAvailable)
            {
                _plugin.Call(
                    "set_gad_has_consent_for_cookies",
                    enabled
                );
            }
        }


        public static bool GetGadHasConsentForCookies()
        {
            if (!IsAvailable)
                return true;

            return (bool)_plugin.Call(
                "get_gad_has_consent_for_cookies"
            );
        }


        public static void DisableSdkCrashReporting()
        {
            if (
                IsAvailable
                && OS.GetName() == "iOS"
            )
            {
                _plugin.Call(
                    "disable_sdk_crash_reporting"
                );
            }
        }


        public static void OpenAdInspector(
            AdInspectorClosedListener listener = null)
        {
            if (!IsAvailable)
                return;

            if (listener != null)
            {
                _currentAdInspectorListener =
                    listener;

                SafeConnect(
                    _plugin,
                    "on_ad_inspector_closed",
                    _onAdInspectorClosedCallable,
                    (uint)GodotObject.ConnectFlags.OneShot
                );
            }

            _plugin.Call(
                "open_ad_inspector"
            );
        }


        public static string GetVersion()
        {
            return PoingStudios.AdMob.Core
                .PluginVersion.Current;
        }


        public static string GetPlatformVersion()
        {
            if (!IsAvailable)
                return "";

            return (string)_plugin.Call(
                "get_platform_version"
            );
        }


        private static void OnAdInspectorClosed(
            Dictionary errorDict)
        {
            Callable
                .From(
                    () =>
                        _currentAdInspectorListener?
                            .OnAdInspectorClosed?
                            .Invoke(errorDict)
                )
                .CallDeferred();
        }
    }
}
