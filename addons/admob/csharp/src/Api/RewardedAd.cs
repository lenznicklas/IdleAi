// MIT License
//
// Copyright (c) 2023-present Poing Studios
//
// Idle AI robustness adjustments:
// - reward signal is connected BEFORE native show
// - show exceptions become normal failure callbacks
// - show watchdog prevents permanent "VIDEO PLAYING..." state
// - Destroy disconnects every signal to avoid stale RewardedAd instances

using System;
using Godot;
using Godot.Collections;
using PoingStudios.AdMob.Api.Core;
using PoingStudios.AdMob.Api.Listeners;
using PoingStudios.AdMob.Core;

namespace PoingStudios.AdMob.Api
{
    public class RewardedAd : MobileSingletonPlugin
    {
        private const double ShowTimeoutSeconds =
            180.0;

        private static readonly GodotObject _plugin =
            GetPlugin(
                "PoingGodotAdMobRewardedAd"
            );

        public FullScreenContentCallback FullScreenContentCallback
        {
            get;
            set;
        } =
            new FullScreenContentCallback();

        public Action<AdValue> OnAdPaid
        {
            get;
            set;
        }

        private readonly int _uid;

        private OnUserEarnedRewardListener _rewardListener;

        private readonly Callable _onClickedCallable;

        private readonly Callable _onDismissedCallable;

        private readonly Callable _onFailedToShowCallable;

        private readonly Callable _onImpressionCallable;

        private readonly Callable _onShowedCallable;

        private readonly Callable _onRewardCallable;

        private readonly Callable _onPaidCallable;

        private bool _destroyed;

        private bool _showRequested;

        private bool _showFinished;

        private int _showGeneration;


        internal RewardedAd(
            int uid)
        {
            _uid =
                uid;

            _onClickedCallable =
                Callable.From<int>(
                    OnClicked
                );

            _onDismissedCallable =
                Callable.From<int>(
                    OnDismissed
                );

            _onFailedToShowCallable =
                Callable.From<
                    int,
                    Dictionary
                >(
                    OnFailedToShow
                );

            _onImpressionCallable =
                Callable.From<int>(
                    OnImpression
                );

            _onShowedCallable =
                Callable.From<int>(
                    OnShowed
                );

            _onRewardCallable =
                Callable.From<
                    int,
                    Dictionary
                >(
                    OnReward
                );

            _onPaidCallable =
                Callable.From<
                    int,
                    Dictionary
                >(
                    OnPaid
                );

            RegisterCallbacks();
        }


        public void Show(
            OnUserEarnedRewardListener rewardListener = null)
        {
            if (_destroyed)
            {
                ReportShowFailure(
                    CreateLocalError(
                        "Rewarded Ad was already destroyed."
                    )
                );

                return;
            }

            if (_showRequested)
            {
                ReportShowFailure(
                    CreateLocalError(
                        "Rewarded Ad can only be shown once."
                    )
                );

                return;
            }

            if (
                _plugin == null
                || !GodotObject.IsInstanceValid(
                    _plugin
                )
            )
            {
                ReportShowFailure(
                    CreateLocalError(
                        "Native rewarded-ad plugin is not available."
                    )
                );

                return;
            }

            _showRequested =
                true;

            _showFinished =
                false;

            int generation =
                ++_showGeneration;

            _rewardListener =
                rewardListener
                ?? new OnUserEarnedRewardListener();

            /*
             * Connect BEFORE show().
             *
             * The old wrapper called the native show first and connected the
             * reward signal afterwards. Connecting first removes a race where
             * a very fast reward callback could be missed.
             */
            SafeConnect(
                _plugin,
                "on_rewarded_ad_user_earned_reward",
                _onRewardCallable
            );

            try
            {
                _plugin.Call(
                    "show",
                    _uid
                );

                StartShowWatchdog(
                    generation
                );
            }
            catch (Exception exception)
            {
                ReportShowFailure(
                    CreateLocalError(
                        "Rewarded Ad native show threw: "
                        + exception.Message
                    )
                );
            }
        }


        private void StartShowWatchdog(
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
                    ShowTimeoutSeconds,
                    processAlways: true
                );

            timer.Timeout +=
                () =>
                {
                    if (
                        _destroyed
                        || _showFinished
                        || generation
                            != _showGeneration
                    )
                    {
                        return;
                    }

                    ReportShowFailure(
                        CreateLocalError(
                            "Rewarded Ad did not return a close/failure callback "
                            + "within "
                            + ShowTimeoutSeconds
                            + " seconds."
                        )
                    );
                };
        }


        public void Destroy()
        {
            if (_destroyed)
                return;

            _destroyed =
                true;

            _showFinished =
                true;

            _showGeneration++;

            DisconnectCallbacks();

            if (
                _plugin == null
                || !GodotObject.IsInstanceValid(
                    _plugin
                )
            )
            {
                return;
            }

            try
            {
                _plugin.Call(
                    "destroy",
                    _uid
                );
            }
            catch (Exception exception)
            {
                GD.PushWarning(
                    "[AdMob] RewardedAd.Destroy failed: "
                    + exception.Message
                );
            }
        }


        public ResponseInfo GetResponseInfo()
        {
            if (
                _destroyed
                || _plugin == null
                || !GodotObject.IsInstanceValid(
                    _plugin
                )
            )
            {
                return null;
            }

            try
            {
                var responseInfoDictionary =
                    (Dictionary)_plugin.Call(
                        "get_response_info",
                        _uid
                    );

                return ResponseInfo.Create(
                    responseInfoDictionary
                );
            }
            catch (Exception exception)
            {
                GD.PushWarning(
                    "[AdMob] Could not read Rewarded Ad response info: "
                    + exception.Message
                );

                return null;
            }
        }


        public void SetServerSideVerificationOptions(
            ServerSideVerificationOptions options)
        {
            if (
                _destroyed
                || _plugin == null
                || !GodotObject.IsInstanceValid(
                    _plugin
                )
                || options == null
            )
            {
                return;
            }

            _plugin.Call(
                "set_server_side_verification_options",
                _uid,
                options.ConvertToDictionary()
            );
        }


        private void RegisterCallbacks()
        {
            if (
                _plugin == null
                || !GodotObject.IsInstanceValid(
                    _plugin
                )
            )
            {
                return;
            }

            SafeConnect(
                _plugin,
                "on_rewarded_ad_clicked",
                _onClickedCallable
            );

            SafeConnect(
                _plugin,
                "on_rewarded_ad_dismissed_full_screen_content",
                _onDismissedCallable
            );

            SafeConnect(
                _plugin,
                "on_rewarded_ad_failed_to_show_full_screen_content",
                _onFailedToShowCallable
            );

            SafeConnect(
                _plugin,
                "on_rewarded_ad_impression",
                _onImpressionCallable
            );

            SafeConnect(
                _plugin,
                "on_rewarded_ad_showed_full_screen_content",
                _onShowedCallable
            );

            SafeConnect(
                _plugin,
                "on_rewarded_ad_paid",
                _onPaidCallable
            );
        }


        private void DisconnectCallbacks()
        {
            if (
                _plugin == null
                || !GodotObject.IsInstanceValid(
                    _plugin
                )
            )
            {
                return;
            }

            SafeDisconnect(
                _plugin,
                "on_rewarded_ad_clicked",
                _onClickedCallable
            );

            SafeDisconnect(
                _plugin,
                "on_rewarded_ad_dismissed_full_screen_content",
                _onDismissedCallable
            );

            SafeDisconnect(
                _plugin,
                "on_rewarded_ad_failed_to_show_full_screen_content",
                _onFailedToShowCallable
            );

            SafeDisconnect(
                _plugin,
                "on_rewarded_ad_impression",
                _onImpressionCallable
            );

            SafeDisconnect(
                _plugin,
                "on_rewarded_ad_showed_full_screen_content",
                _onShowedCallable
            );

            SafeDisconnect(
                _plugin,
                "on_rewarded_ad_user_earned_reward",
                _onRewardCallable
            );

            SafeDisconnect(
                _plugin,
                "on_rewarded_ad_paid",
                _onPaidCallable
            );
        }


        private void OnReward(
            int uid,
            Dictionary rewardDict)
        {
            if (
                uid != _uid
                || _destroyed
            )
            {
                return;
            }

            RewardedItem item =
                RewardedItem.Create(
                    rewardDict
                );

            Callable
                .From(
                    () =>
                        _rewardListener?
                            .OnUserEarnedReward?
                            .Invoke(item)
                )
                .CallDeferred();
        }


        private void OnClicked(
            int uid)
        {
            if (
                uid != _uid
                || _destroyed
            )
            {
                return;
            }

            Callable
                .From(
                    () =>
                        FullScreenContentCallback
                            .OnAdClicked?
                            .Invoke()
                )
                .CallDeferred();
        }


        private void OnDismissed(
            int uid)
        {
            if (
                uid != _uid
                || _destroyed
                || _showFinished
            )
            {
                return;
            }

            _showFinished =
                true;

            Callable
                .From(
                    () =>
                        FullScreenContentCallback
                            .OnAdDismissedFullScreenContent?
                            .Invoke()
                )
                .CallDeferred();
        }


        private void OnFailedToShow(
            int uid,
            Dictionary errorDict)
        {
            if (
                uid != _uid
                || _destroyed
                || _showFinished
            )
            {
                return;
            }

            AdError error =
                AdError.Create(
                    errorDict
                )
                ?? CreateLocalError(
                    "Rewarded Ad failed to show."
                );

            ReportShowFailure(
                error
            );
        }


        private void OnImpression(
            int uid)
        {
            if (
                uid != _uid
                || _destroyed
            )
            {
                return;
            }

            Callable
                .From(
                    () =>
                        FullScreenContentCallback
                            .OnAdImpression?
                            .Invoke()
                )
                .CallDeferred();
        }


        private void OnShowed(
            int uid)
        {
            if (
                uid != _uid
                || _destroyed
            )
            {
                return;
            }

            Callable
                .From(
                    () =>
                        FullScreenContentCallback
                            .OnAdShowedFullScreenContent?
                            .Invoke()
                )
                .CallDeferred();
        }


        private void OnPaid(
            int uid,
            Dictionary adValueDictionary)
        {
            if (
                uid != _uid
                || _destroyed
            )
            {
                return;
            }

            AdValue adValue =
                AdValue.Create(
                    adValueDictionary
                );

            Callable
                .From(
                    () =>
                        OnAdPaid?
                            .Invoke(adValue)
                )
                .CallDeferred();
        }


        private void ReportShowFailure(
            AdError error)
        {
            if (_showFinished)
                return;

            _showFinished =
                true;

            GD.PushWarning(
                "[AdMob] "
                + (
                    error?.Message
                    ?? "Rewarded Ad failed to show."
                )
            );

            Callable
                .From(
                    () =>
                        FullScreenContentCallback
                            .OnAdFailedToShowFullScreenContent?
                            .Invoke(
                                error
                                ?? CreateLocalError(
                                    "Rewarded Ad failed to show."
                                )
                            )
                )
                .CallDeferred();
        }


        private static AdError CreateLocalError(
            string message)
        {
            return new AdError(
                -1,
                "PoingGodotAdMob.RewardedAd",
                message,
                null
            );
        }
    }
}
