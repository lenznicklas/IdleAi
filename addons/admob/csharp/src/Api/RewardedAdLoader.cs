// MIT License
//
// Copyright (c) 2023-present Poing Studios
//
// Idle AI robustness adjustments:
// - wait until MobileAds initialization actually completed
// - return a normal load error when native plugin is missing
// - watchdog timeout prevents permanent "LOADING VIDEO..." state
// - callback/signals are cleaned up exactly once

using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PoingStudios.AdMob.Api.Core;
using PoingStudios.AdMob.Api.Listeners;
using PoingStudios.AdMob.Core;

namespace PoingStudios.AdMob.Api
{
    public class RewardedAdLoader : MobileSingletonPlugin
    {
        private const double LoadTimeoutSeconds =
            20.0;

        private static readonly GodotObject _plugin =
            GetPlugin(
                "PoingGodotAdMobRewardedAd"
            );

        // Prevent GC during initialization + asynchronous native load.
        private static readonly HashSet<RewardedAdLoader>
            _activeLoaders =
                new();

        private RewardedAdLoadCallback _callback;

        private readonly int _uid =
            -1;

        private readonly Callable _onLoadedCallable;

        private readonly Callable _onFailedCallable;

        private bool _completed;

        private bool _nativeLoadStarted;

        private int _loadGeneration;


        public RewardedAdLoader()
        {
            _onLoadedCallable =
                Callable.From<int>(
                    OnLoaded
                );

            _onFailedCallable =
                Callable.From<
                    int,
                    Godot.Collections.Dictionary
                >(
                    OnFailed
                );

            if (
                _plugin != null
                && GodotObject.IsInstanceValid(
                    _plugin
                )
            )
            {
                try
                {
                    _uid =
                        (int)_plugin.Call(
                            "create"
                        );
                }
                catch (Exception exception)
                {
                    GD.PushWarning(
                        "[AdMob] Could not create RewardedAd loader: "
                        + exception.Message
                    );
                }
            }
        }


        public void Load(
            string adUnitId,
            AdRequest adRequest,
            RewardedAdLoadCallback callback = null)
        {
            _callback =
                callback
                ?? new RewardedAdLoadCallback();

            _completed =
                false;

            _nativeLoadStarted =
                false;

            int generation =
                ++_loadGeneration;

            _activeLoaders.Add(
                this
            );

            StartLoadWatchdog(
                generation
            );

            if (
                _plugin == null
                || !GodotObject.IsInstanceValid(
                    _plugin
                )
                || _uid < 0
            )
            {
                FailOnce(
                    CreateLocalError(
                        "Native rewarded-ad plugin is not available. "
                        + "Check the AdMob Android binaries, "
                        + "admob/general/android/enabled and Gradle export."
                    )
                );

                return;
            }

            if (
                string.IsNullOrWhiteSpace(
                    adUnitId
                )
            )
            {
                FailOnce(
                    CreateLocalError(
                        "Rewarded Ad Unit ID is empty."
                    )
                );

                return;
            }

            /*
             * ShopController currently calls MobileAds.Initialize() immediately
             * before RewardedAdLoader.Load().
             *
             * This second Initialize(listener) does NOT initialize twice.
             * The fixed MobileAds wrapper queues this listener onto the same
             * initialization and calls it once the native SDK is really ready.
             */
            MobileAds.Initialize(
                new OnInitializationCompleteListener
                {
                    OnInitializationComplete =
                        status =>
                        {
                            if (
                                _completed
                                || generation
                                    != _loadGeneration
                            )
                            {
                                return;
                            }

                            if (status == null)
                            {
                                FailOnce(
                                    CreateLocalError(
                                        "Mobile Ads SDK initialization failed or timed out."
                                    )
                                );

                                return;
                            }

                            StartNativeLoad(
                                adUnitId,
                                adRequest
                                    ?? new AdRequest()
                            );
                        }
                }
            );
        }


        private void StartNativeLoad(
            string adUnitId,
            AdRequest adRequest)
        {
            if (
                _completed
                || _nativeLoadStarted
            )
            {
                return;
            }

            if (
                _plugin == null
                || !GodotObject.IsInstanceValid(
                    _plugin
                )
            )
            {
                FailOnce(
                    CreateLocalError(
                        "Rewarded Ad native plugin disappeared before loading."
                    )
                );

                return;
            }

            _nativeLoadStarted =
                true;

            SafeConnect(
                _plugin,
                "on_rewarded_ad_loaded",
                _onLoadedCallable,
                (uint)GodotObject.ConnectFlags.Deferred
            );

            SafeConnect(
                _plugin,
                "on_rewarded_ad_failed_to_load",
                _onFailedCallable,
                (uint)GodotObject.ConnectFlags.Deferred
            );

            try
            {
                _plugin.Call(
                    "load",
                    adUnitId,
                    adRequest.ConvertToDictionary(),
                    new Array<string>(
                        adRequest.Keywords
                    ),
                    _uid
                );
            }
            catch (Exception exception)
            {
                FailOnce(
                    CreateLocalError(
                        "Rewarded Ad native load threw: "
                        + exception.Message
                    )
                );
            }
        }


        private void StartLoadWatchdog(
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
                    LoadTimeoutSeconds,
                    processAlways: true
                );

            timer.Timeout +=
                () =>
                {
                    if (
                        _completed
                        || generation
                            != _loadGeneration
                    )
                    {
                        return;
                    }

                    FailOnce(
                        CreateLocalError(
                            "Rewarded Ad load timed out after "
                            + LoadTimeoutSeconds
                            + " seconds."
                        )
                    );
                };
        }


        private void OnLoaded(
            int uid)
        {
            if (
                uid != _uid
                || _completed
            )
            {
                return;
            }

            _completed =
                true;

            Cleanup();

            RewardedAd ad =
                new(
                    uid
                );

            Callable
                .From(
                    () =>
                        _callback?
                            .OnAdLoaded?
                            .Invoke(ad)
                )
                .CallDeferred();
        }


        private void OnFailed(
            int uid,
            Godot.Collections.Dictionary errorDict)
        {
            if (
                uid != _uid
                || _completed
            )
            {
                return;
            }

            LoadAdError error =
                LoadAdError.Create(
                    errorDict
                )
                ?? CreateLocalError(
                    "Rewarded Ad failed to load."
                );

            FailOnce(
                error
            );
        }


        private void FailOnce(
            LoadAdError error)
        {
            if (_completed)
                return;

            _completed =
                true;

            Cleanup();

            GD.PushWarning(
                "[AdMob] "
                + (
                    error?.Message
                    ?? "Rewarded Ad load failed."
                )
            );

            Callable
                .From(
                    () =>
                        _callback?
                            .OnAdFailedToLoad?
                            .Invoke(
                                error
                                ?? CreateLocalError(
                                    "Rewarded Ad load failed."
                                )
                            )
                )
                .CallDeferred();
        }


        private void Cleanup()
        {
            if (
                _plugin != null
                && GodotObject.IsInstanceValid(
                    _plugin
                )
            )
            {
                SafeDisconnect(
                    _plugin,
                    "on_rewarded_ad_loaded",
                    _onLoadedCallable
                );

                SafeDisconnect(
                    _plugin,
                    "on_rewarded_ad_failed_to_load",
                    _onFailedCallable
                );
            }

            _activeLoaders.Remove(
                this
            );
        }


        private static LoadAdError CreateLocalError(
            string message)
        {
            return new LoadAdError(
                null,
                -1,
                "PoingGodotAdMob.RewardedAdLoader",
                message,
                null
            );
        }
    }
}
