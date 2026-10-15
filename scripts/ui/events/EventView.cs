using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

/// <summary>The shared Event page retains all cards across ticks, upgrades, prestige and reopen.</summary>
internal sealed partial class EventView : Control
{
    private readonly EventManager _manager;
    private readonly EventTextureCache _textures;
    private readonly Label _title, _time, _currency, _description, _status, _level, _production, _cores, _next, _story, _ended, _rewardSummary;
    private (double Earned, int Claimed, bool Ended, bool ReadOnly, bool Active)? _lastRewardState;
    private readonly ProgressBar _rewardProgress;
    private readonly Button _claimAll;
    private readonly VBoxContainer _challengeRows;
    private readonly Label _challengeSummary;
    private readonly Button _claimAllChallenges;
    private readonly Dictionary<string, EventChallengeCard> _challengeCards = [];
    private (long Revision, bool Ended, bool ReadOnly, bool Active)? _lastChallengeState;
    private readonly Action<string> _claimChallengeAction;
    private readonly ColorRect _backgroundColor;
    private readonly TextureRect _background, _currencyIcon;
    private readonly PanelContainer _header;
    private readonly Dictionary<string, Control> _sections = [];
    private readonly Dictionary<string, Button> _tabs = [];
    private readonly List<(EventGeneratorDefinition Definition, EventGeneratorCard Card)> _generators = [];
    private readonly List<(EventMilestone Definition, EventRewardCard Card)> _rewards = [];
    private readonly List<(EventBotDefinition Definition, EventBotCard Card)> _bots = [];
    private readonly List<(EventShopItem Definition, EventShopCard Card)> _shop = [];
    private readonly VBoxContainer _generatorRows, _rewardRows, _botRows, _shopRows, _archiveRows;
    private readonly Dictionary<string, Label> _archives = [];
    private readonly HashSet<string> _archiveWanted = [];
    private readonly List<string> _archiveRemoved = [];
    private readonly EventPrestigePanel _prestige;
    private readonly EventBotDetailPanel _botDetail;
    private readonly EventImageView _coreIcon;
    private readonly HBoxContainer _endIcons;
    private readonly Action<string> _upgradeAction, _claimAction, _botAction;
    private readonly Action<string, int> _buyAction;
    private readonly Label _shopSummary;
    private string? _eventId;
    private string? _focusedEventId;
    private string? _observedDisplayEventId;
    private bool _viewingArchive;
    internal EventDefinition? CurrentEvent
    {
        get
        {
            var display = _manager.DisplayEvent;
            if (!_viewingArchive && _observedDisplayEventId != display?.Id)
            {
                // Only explicit Archive navigation may pin a historical definition.
                _focusedEventId = display?.Id;
                _observedDisplayEventId = display?.Id;
            }
            return _focusedEventId == null ? display : _manager.GetDefinition(_focusedEventId);
        }
    }
    internal void OpenCurrent()
    {
        _viewingArchive = false;
        _focusedEventId = _observedDisplayEventId = _manager.DisplayEvent?.Id;
#if DEBUG
        GD.Print("[Event] Active Event ID: " + (_manager.ActiveEvent?.Id ?? "none")
            + " | Rendered Event ID: " + (_focusedEventId ?? "none"));
#endif
        Refresh(true);
        RestoreTabScroll();
    }
    internal bool DismissConfirmationOnBack() => _botDetail.DismissOnBack() || _prestige.DismissOnBack();
    private string _selectedTab = "";
    private readonly ScrollContainer _scroll;
    private readonly EventContentFrame _contentFrame;
    private readonly MobileScrollController _mobileScroll;
    private readonly Dictionary<(string EventId, string Tab), int> _scrollPositions = [];
    private bool _scrollRestorePending;
    private int _scrollRestoreRevision;
    private long _lastCountdownUnix = -1;
    internal MarginContainer ContentMargin { get; }

    internal EventView(EventManager manager, EventTextureCache textures, Action close, Action<string> upgrade,
        Action<string> claim, Action claimAll, Action<string> bot, Action<string, int> buy, Action<int, long> prestige, Action<string> claimChallenge, Action claimAllChallenges)
    {
        _manager = manager; _textures = textures;
        _upgradeAction = id => RunContentAction(() => upgrade(id));
        _claimAction = id => RunContentAction(() => claim(id));
        _botAction = id => RunContentAction(() => bot(id));
        _buyAction = (id, count) => RunContentAction(() => buy(id, count));
        _claimChallengeAction = id => RunContentAction(() => claimChallenge(id));
        Name = "EventPage"; ZIndex = 650; ClipContents = true; Visible = false;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _backgroundColor = new ColorRect { Color = new(.008f, .018f, .035f, .99f), MouseFilter = MouseFilterEnum.Ignore };
        _backgroundColor.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(_backgroundColor);
        _background = new TextureRect { Name = "EventBackground", ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, Modulate = new(1, 1, 1, .45f), MouseFilter = MouseFilterEnum.Ignore };
        _background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(_background);
        ContentMargin = new MarginContainer(); ContentMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ContentMargin.AddThemeConstantOverride("margin_left", 22); ContentMargin.AddThemeConstantOverride("margin_right", 22);
        AddChild(ContentMargin);
        var layout = new VBoxContainer { Name = "EventLayout" };
        layout.AddThemeConstantOverride("separation", 12); ContentMargin.AddChild(layout);
        var header = EventUiStyle.Panel(layout, "EventHeader");
        _header = (PanelContainer)header.GetParent().GetParent();
        _header.CustomMinimumSize = new(0, 156);
        var headerMargin = (MarginContainer)header.GetParent();
        foreach (string side in new[] { "left", "right", "top", "bottom" }) headerMargin.AddThemeConstantOverride("margin_" + side, 12);
        header.AddThemeConstantOverride("separation", 6);
        var top = new HBoxContainer(); top.AddThemeConstantOverride("separation", 10); header.AddChild(top);
        var identity = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        identity.AddThemeConstantOverride("separation", 2); top.AddChild(identity);
        _title = Summary(identity, "EventTitle", 20);
        _title.Text = "Event";
        _time = Summary(identity, "EventCountdown", 12);
        var closeButton = EventUiStyle.Button("EventCloseButton", "", close);
        closeButton.Icon = textures.Get("res://assets/ui/x.png"); closeButton.ExpandIcon = true;
        closeButton.CustomMinimumSize = new(56, 56); closeButton.TooltipText = "Back to map"; top.AddChild(closeButton);
        var metrics = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        metrics.AddThemeConstantOverride("h_separation", 12); header.AddChild(metrics);
        var currencyRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        metrics.AddChild(currencyRow);
        _currencyIcon = new TextureRect { CustomMinimumSize = new(24, 24), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        currencyRow.AddChild(_currencyIcon);
        _currency = Summary(currencyRow, "EventCurrency", 16);
        var coreRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; metrics.AddChild(coreRow);
        _coreIcon = new EventImageView(null, new(24, 24)) { Name = "EventCoreIcon", Visible = false, CustomMinimumSize = new(24, 24) }; coreRow.AddChild(_coreIcon);
        _cores = Summary(coreRow, "EventCores", 13);
        _production = Summary(header, "EventProduction", 12);
        _scroll = new ScrollContainer { Name = "EventScroll", SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
            ClipContents = true, FollowFocus = false };
        _contentFrame = new EventContentFrame(_scroll);
        layout.AddChild(_contentFrame);
        var body = new VBoxContainer { Name = "EventContent", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 12); _scroll.AddChild(body);
        _mobileScroll = new MobileScrollController { Name = "EventMobileScroll", SmoothMouseWheel = true };
        AddChild(_mobileScroll); _mobileScroll.Setup(_scroll, allowTopOverscroll: false, allowBottomOverscroll: false);
        _generatorRows = Section(body, "Progress", "EventProgressPanel");
        _description = EventUiStyle.Label("EventDescription", "", 12); _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _description.MaxLinesVisible = 3; _description.ClipText = true; _generatorRows.AddChild(_description);
        _status = EventUiStyle.Label("EventStatus", "", 13); _generatorRows.AddChild(_status);
        _level = Summary(_generatorRows, "EventLevel");
        _next = Summary(_generatorRows, "EventNextUnlock");
        _story = EventUiStyle.Label("EventStory", "", 13); _story.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _story.ClipText = true; _generatorRows.AddChild(_story);
        _endIcons = new HBoxContainer { Name = "EventEndIcons", Visible = false }; _generatorRows.AddChild(_endIcons);
        _ended = EventUiStyle.Label("EventEndedStats", "", 13); _ended.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _ended.ClipText = true; _generatorRows.AddChild(_ended);
        _prestige = new EventPrestigePanel(manager, textures, this, prestige, () => CurrentEvent, () => !_mobileScroll.ShouldSuppressTap);
        _prestige.ConfirmationVisibilityChanged += SyncScrollInput; _sections["Prestige"] = _prestige.Root; body.AddChild(_prestige.Root);
        _rewardRows = Section(body, "Rewards", "EventRewardsPanel");
        _rewardSummary = EventUiStyle.Label("EventRewardSummary", "", 13);
        _rewardSummary.ClipText = true; _rewardSummary.CustomMinimumSize = new(0, 152);
        _rewardRows.AddChild(_rewardSummary);
        _rewardProgress = new ProgressBar { Name = "EventTrackProgress", ShowPercentage = false, CustomMinimumSize = new(0, 12) };
        _rewardProgress.AddThemeStyleboxOverride("background", MapController.CreateNodeStyle(new(.04f, .05f, .07f), EventUiStyle.Muted, 0, 0));
        _rewardProgress.AddThemeStyleboxOverride("fill", MapController.CreateNodeStyle(EventUiStyle.Accent, EventUiStyle.Accent, 0, 0));
        _rewardRows.AddChild(_rewardProgress);
        _claimAll = EventUiStyle.Button("EventClaimAll", "CLAIM ALL", () => RunContentAction(claimAll));
        EventUiStyle.StyleButton(_claimAll, EventUiStyle.Muted);
        _rewardRows.AddChild(_claimAll);
        _challengeRows = Section(body, "Challenges", "EventChallengesPanel");
        _challengeSummary = Message("EventChallengeSummary", "", 13); _challengeRows.AddChild(_challengeSummary);
        _claimAllChallenges = EventUiStyle.Button("EventChallengeClaimAll", "CLAIM ALL", () => RunContentAction(claimAllChallenges));
        EventUiStyle.StyleButton(_claimAllChallenges, EventUiStyle.Muted);
        _claimAllChallenges.Hide(); _challengeRows.AddChild(_claimAllChallenges);
        _botRows = Section(body, "Bots", "EventBotsPanel");
        _shopRows = Section(body, "Shop", "EventShopPanel");
        _shopSummary = Message("EventShopSummary", "", 13); _shopRows.AddChild(_shopSummary);
        _archiveRows = Section(body, "Archive", "EventArchivePanel");
        _archiveRows.AddChild(Message("ArchiveNote", "Completed Major Events retain their final stats, rewards and cosmetic completion.", 12));
        body.AddChild(new Control { Name = "EventContentBottomClearance", CustomMinimumSize = new(0, EventContentFrame.BottomClearance),
            MouseFilter = MouseFilterEnum.Ignore });
        var navigation = new VBoxContainer { Name = "EventTabs" };
        navigation.AddThemeConstantOverride("separation", 8); layout.AddChild(navigation);
        var tabs = new GridContainer { Name = "EventPrimaryTabs", Columns = 3 };
        tabs.AddThemeConstantOverride("h_separation", 8); tabs.AddThemeConstantOverride("v_separation", 8); navigation.AddChild(tabs);
        foreach (string tab in new[] { "Progress", "Prestige", "Bots", "Rewards", "Shop", "Archive", "Challenges" })
        {
            string selected = tab;
            var button = EventUiStyle.Button("Event" + tab + "Tab", tab, () => SelectTab(selected));
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            button.AddThemeFontSizeOverride("font_size", 14); // Readability pass brings navigation text to 16px.
            button.CustomMinimumSize = new(0, 56); button.ToggleMode = true; button.FocusMode = FocusModeEnum.All;
            if (tab == "Challenges") navigation.AddChild(button); else tabs.AddChild(button);
            _tabs[tab] = button;
        }
        _botDetail = new EventBotDetailPanel(this, manager, textures);
        _botDetail.VisibilityChanged += SyncScrollInput;
        SelectTab("Progress");
        VisibilityChanged += () =>
        {
            SyncScrollInput();
            if (Visible) return;
            RememberTabScroll();
            _scrollRestoreRevision++; _scrollRestorePending = false;
            _prestige.Dismiss(); _botDetail.Dismiss();
        };
    }
    private void RunContentAction(Action action)
    {
        if (!_mobileScroll.ShouldSuppressTap) action();
    }
    private void SyncScrollInput()
    {
        _mobileScroll.ResetMotion();
        _mobileScroll.SetProcessInput(Visible && !_prestige.ConfirmationVisible && !_botDetail.Visible);
    }
    private static Label Message(string name, string text, int size)
    {
        var label = EventUiStyle.Label(name, text, size);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart; label.ClipText = true;
        return label;
    }
    private static Label Summary(Control parent, string name, int size = 12)
    {
        var label = EventUiStyle.Label(name, "", size); label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.ClipText = true; label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.MaxLinesVisible = 1; label.AutowrapMode = TextServer.AutowrapMode.Off; parent.AddChild(label); return label;
    }
    private VBoxContainer Section(Control body, string tab, string name)
    {
        var box = new VBoxContainer { Name = name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 16); body.AddChild(box); _sections[tab] = box; return box;
    }
    private void SelectTab(string tab)
    {
        if (!_sections.ContainsKey(tab)) return;
        if (_selectedTab == tab) { _mobileScroll.ResetMotion(); _tabs[tab].SetPressedNoSignal(true); return; }
        RememberTabScroll();
        _mobileScroll.ResetMotion();
        _scrollRestorePending = true;
        _selectedTab = tab; _prestige.Dismiss(); _botDetail.Dismiss();
        foreach (var pair in _sections) pair.Value.Visible = pair.Key == tab;
        StyleTabs();
        Refresh();
        RestoreTabScroll();
    }
    private void StyleTabs()
    {
        Color accent = Color.FromHtml(CurrentEvent?.Theme.AccentHex ?? "#33b3f2");
        foreach (var pair in _tabs) EventUiStyle.StyleTabButton(pair.Value, accent, pair.Key == _selectedTab);
    }
    private void RememberTabScroll()
    {
        if (_selectedTab.Length > 0 && !_scrollRestorePending)
            _scrollPositions[(_eventId ?? "", _selectedTab)] = _scroll.ScrollVertical;
    }
    private async void RestoreTabScroll()
    {
        _scrollRestorePending = true;
        int revision = ++_scrollRestoreRevision;
        if (!IsInsideTree()) return;
        // The selected section must finish container layout before its scroll range is known.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || revision != _scrollRestoreRevision) return;
        _scrollPositions.TryGetValue((_eventId ?? "", _selectedTab), out int offset);
        _scroll.ScrollVertical = offset;
        _scrollRestorePending = false;
    }
    public override void _ExitTree() { _scrollRestoreRevision++; }
    private void BuildCards(EventDefinition? d)
    {
        _lastRewardState = null;
        _lastChallengeState = null;
        foreach (var card in _challengeCards.Values) { card.Root.Hide(); card.Root.QueueFree(); }
        _challengeCards.Clear();
        EventUiStyle.SetText(_challengeSummary, ""); _claimAllChallenges.Hide();
        EventUiStyle.SetText(_tabs["Challenges"], "Challenges");
        foreach (var item in _generators) { item.Card.Root.Hide(); item.Card.Root.QueueFree(); } _generators.Clear();
        foreach (var item in _rewards) { item.Card.Root.Hide(); item.Card.Root.QueueFree(); } _rewards.Clear();
        foreach (var item in _bots) { item.Card.Root.Hide(); item.Card.Root.QueueFree(); } _bots.Clear();
        foreach (var item in _shop) { item.Card.Root.Hide(); item.Card.Root.QueueFree(); } _shop.Clear();
        foreach (var container in new[] { _botRows, _shopRows })
            foreach (Node child in container.GetChildren()) if (child is Label && child != _shopSummary) { container.RemoveChild(child); child.QueueFree(); }
        _claimAll.Hide();
        EventUiStyle.SetText(_rewardSummary, "");
        _rewardProgress.Value = 0;
        if (d == null) return;
        var definitions = _manager.GetGenerators(d.Id);
        foreach (var g in definitions)
        {
            string target = g.Id;
            var card = new EventGeneratorCard(d, g, _textures, definitions, () => _upgradeAction(target), _generators.Count == 0);
            _generatorRows.AddChild(card.Root); _generators.Add((g, card));
        }
        Color accent = Color.FromHtml(d.Theme.AccentHex);
        _rewardProgress.AddThemeStyleboxOverride("fill", MapController.CreateNodeStyle(accent, accent, 0, 0));
        foreach (var m in d.Milestones.OrderBy(m => m.TargetAmount))
        {
            string target = m.Id;
            var card = new EventRewardCard(d, m, _textures, () => _claimAction(target));
            _rewardRows.AddChild(card.Root); _rewards.Add((m, card));
        }
        foreach (var b in d.Bots.Where(b => b.Enabled))
        {
            string target = b.Id;
            var card = new EventBotCard(d, b, _textures, () => RunContentAction(() => _botDetail.Open(d, b)), () => _botAction(target));
            _botRows.AddChild(card.Root); _bots.Add((b, card));
        }
        if (d.Bots.Count == 0) _botRows.AddChild(Message("EventBotsPlaceholder", "No event bots in this protocol.", 14));
        string previousCategory = "";
        var items = _manager.GetShopItems(d.Id);
        bool showCategories = items.Select(i => i.Category).Distinct().Count() > 1 || items.Any(i => i.Featured);
        foreach (var s in items)
        {
            string category = s.Featured ? "Featured" : s.Category;
            if (showCategories && previousCategory != category)
            {
                _shopRows.AddChild(Message("EventShopCategory_" + _shop.Count, category.ToUpperInvariant(), 16));
                previousCategory = category;
            }
            string target = s.Id;
            var card = new EventShopCard(d, s, _textures, count => _buyAction(target, count));
            _shopRows.AddChild(card.Root); _shop.Add((s, card));
        }
        if (items.Count == 0) _shopRows.AddChild(Message("EventShopPlaceholder", "No event items available yet.", 14));
        GetParent()?.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(this);
    }
    internal void Refresh(bool force = false)
    {
        if (!force && !IsVisibleInTree()) return;
        var d = CurrentEvent;
        if (_eventId != d?.Id)
        {
            RememberTabScroll();
            _eventId = d?.Id; BuildCards(d); _lastCountdownUnix = -1;
            RestoreTabScroll();
            _prestige.Dismiss(); _botDetail.Dismiss();
            _backgroundColor.Color = Color.FromHtml(d?.Theme.BackgroundHex ?? "#020509");
            _background.Texture = _textures.Get(d?.BackgroundPath ?? "");
            _currencyIcon.Texture = _textures.Get(d?.CurrencyIconPath ?? ""); _currencyIcon.Visible = _currencyIcon.Texture != null;
            _coreIcon.SetTexture(_textures.Get(d?.Prestige.IconPath ?? "")); _coreIcon.Visible = d?.Prestige.Enabled == true;
            foreach (Node child in _endIcons.GetChildren()) { _endIcons.RemoveChild(child); child.QueueFree(); }
            if (d != null) AddResultIcons(_endIcons, d);
            Color accent = Color.FromHtml(d?.Theme.AccentHex ?? "#33b3f2");
            _contentFrame.SetTheme(_backgroundColor.Color, accent);
            _header.AddThemeStyleboxOverride("panel", MapController.CreateNodeStyle(new(.025f, .018f, .04f, .97f), accent, 2, 8));
            _title.Modulate = accent.Lightened(.35f);
            StyleTabs();
        }
        EventUiStyle.SetText(_title, d?.Theme.UppercaseTitle == true ? d.Name.ToUpperInvariant() : d?.Name ?? "Event");
        EventUiStyle.SetText(_description, d?.Description ?? "No active event. New protocols will appear here when they become available.");
        bool active = d != null && _manager.IsActive(d);
        bool ended = d != null && (_manager.HasEnded(d.Id) || _manager.RemainingSeconds(d) == 0);
        EventUiStyle.SetText(_status, _manager.ReadOnly ? "SAVE UNAVAILABLE" : ended ? "EVENT ENDED" : active ? "ACTIVE" : "No active event");
        if (_lastCountdownUnix != _manager.NowUnix)
        {
            _lastCountdownUnix = _manager.NowUnix;
            EventUiStyle.SetText(_time, d == null ? "No active event" : ended ? "EVENT ENDED" : EventUiStyle.Countdown(_manager.RemainingSeconds(d)));
        }
        var p = d == null ? default : _manager.GetProgress(d.Id);
        EventUiStyle.SetText(_currency, d == null ? "" : NumberFormatter.Format(p.Currency) + " " + d.CurrencyName);
        EventUiStyle.SetText(_level, d == null ? "Awaiting a protocol" : "Event level: " + p.Level);
        EventUiStyle.SetText(_production, d == null ? "" : ended ? "Production: 0/s (ended)" : "Production: +" + NumberFormatter.Format(_manager.GetTotalProduction(d.Id)) + "/s");
        EventUiStyle.SetText(_cores, d?.Prestige.Enabled == true ? NumberFormatter.Format(p.PrestigeResource) + " " + d.Prestige.ResourceName : "");
        EventUiStyle.SetText(_next, d == null ? "" : "Next: " + (_manager.GetNextUnlock(d.Id) ?? "All unlocked"));
        foreach (var label in new[] { _title, _currency, _cores, _production, _next }) label.TooltipText = label.Text;
        string story = "";
        if (d != null) foreach (var message in d.Story) if (p.TotalEarned >= message.TargetEarned) story = message.Text;
        EventUiStyle.SetText(_story, story);
        _story.Visible = story.Length > 0;
        _ended.Visible = ended; _endIcons.Visible = ended;
        EventUiStyle.SetText(_ended, !ended || d == null ? "" : "Final earnings: " + NumberFormatter.Format(p.TotalEarned) + " " + d.CurrencyName
            + "\nHighest production: " + NumberFormatter.Format(p.HighestProduction) + "/s • Prestiges: " + p.PrestigeCount
            + "\n" + d.Prestige.ResourceName + " earned: " + _manager.GetTotalPrestigeResourceEarned(d.Id)
            + " • Rewards claimed: " + _manager.ClaimedRewardCount(d.Id) + "/" + d.Milestones.Count);
        if (d != null)
        {
            if (_selectedTab == "Progress") foreach (var item in _generators) item.Card.Refresh(_manager.GetGenerator(d.Id, item.Definition.Id), ended, d.CurrencyName);
            var rewardState = (p.TotalEarned, _manager.ClaimedRewardCount(d.Id), ended, _manager.ReadOnly, active);
            if (_selectedTab == "Rewards" && _lastRewardState != rewardState)
            {
                _lastRewardState = rewardState;
                EventMilestone? nextReward = null;
                int ready = 0;
                double finalTarget = 0;
                foreach (var row in _rewards)
                {
                    bool unlocked = _manager.IsMilestoneUnlocked(d.Id, row.Definition.Id);
                    bool claimed = _manager.IsRewardClaimed(d.Id, row.Definition.Id);
                    bool canClaim = _manager.CanClaimReward(d.Id, row.Definition.Id);
                    if (canClaim) ready++;
                    if (!unlocked && nextReward == null) nextReward = row.Definition;
                    finalTarget = Math.Max(finalTarget, row.Definition.TargetAmount);
                    row.Card.Refresh(p.TotalEarned, unlocked, claimed, canClaim, ended);
                }
                EventUiStyle.SetText(_rewardSummary, "Event Progress (lifetime)\n" + NumberFormatter.Format(p.TotalEarned) + " / " + NumberFormatter.Format(finalTarget)
                    + "\nNext Reward: " + (nextReward == null ? "All requirements reached" : NumberFormatter.Format(nextReward.TargetAmount) + " " + d.CurrencyName)
                    + (nextReward == null ? "" : "\n" + NumberFormatter.Format(Math.Max(0, nextReward.TargetAmount - p.TotalEarned)) + " more needed")
                    + "\nClaimed: " + _manager.ClaimedRewardCount(d.Id) + "/" + d.Milestones.Count
                    + (ended ? "\nEvent ended • earned permanent rewards remain claimable" : ""));
                _rewardProgress.Value = finalTarget <= 0 ? 0 : Math.Clamp(p.TotalEarned / finalTarget * 100, 0, 100);
                _claimAll.Visible = ready >= 2;
                EventUiStyle.SetDisabled(_claimAll, ready < 2);
            }
            if (_selectedTab == "Bots") foreach (var row in _bots)
            {
                row.Card.Refresh(_manager.GetBot(d.Id, row.Definition.Id), active);
            }
            if (_selectedTab == "Shop")
            {
                EventUiStyle.SetText(_shopSummary, ended ? "EVENT ENDED\nPurchases are no longer available. Purchase history is preserved." :
                    "Event Shop\n" + string.Join("\n", d.Shop.Where(i => i.Enabled).Select(i => i.CostCurrency).Distinct().Select(c =>
                        NumberFormatter.Format(_manager.GetShopBalance(d.Id, c)) + " " + EventShopPresentation.CurrencyName(d, c))));
                foreach (var row in _shop) row.Card.Refresh(_manager.GetShopItem(d.Id, row.Definition.Id));
            }
        }
        RefreshChallengeCards(d, ended, active);
        _prestige.Refresh(d);
        _botDetail.Refresh();
        if (_selectedTab == "Archive") RefreshArchives();
    }
    public override void _Process(double delta)
    {
        if (!IsVisibleInTree() || _selectedTab != "Progress" || CurrentEvent is not { } definition) return;
        double now = Time.GetUnixTimeFromSystem();
        double fraction = now - Math.Floor(now);
        foreach (var item in _generators)
            item.Card.RefreshCycle(_manager.GetGeneratorCycleProgress(definition.Id, item.Definition.Id, fraction));
    }

    private void RefreshChallengeCards(EventDefinition? d, bool ended, bool active)
    {
        if (d == null) return;
        var key = (_manager.GetChallengeRevision(d.Id), ended, _manager.ReadOnly, active);
        if (_lastChallengeState == key) return;
        _lastChallengeState = key;
        var snapshots = _manager.GetChallenges(d.Id);
        var ids = snapshots.Select(s => s.Definition.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var id in _challengeCards.Keys.Where(id => !ids.Contains(id)).ToArray())
        {
            var card = _challengeCards[id]; _challengeRows.RemoveChild(card.Root); card.Root.QueueFree(); _challengeCards.Remove(id);
        }
        bool created = false;
        int index = 2, ready = 0;
        foreach (var snapshot in snapshots.OrderBy(s => s.Definition.Category))
        {
            string id = snapshot.Definition.Id;
            if (!_challengeCards.TryGetValue(id, out var card))
            {
                card = new EventChallengeCard(d, snapshot.Definition, _textures, () => _claimChallengeAction(id));
                _challengeRows.AddChild(card.Root); _challengeCards[id] = card; created = true;
            }
            if (card.Root.GetIndex() != index) _challengeRows.MoveChild(card.Root, index);
            index++;
            card.Refresh(snapshot, ended);
            if (snapshot.CanClaim) ready++;
        }
        EventUiStyle.SetText(_tabs["Challenges"], ready == 0 ? "Challenges" : "Challenges • " + ready);
        _tabs["Challenges"].TooltipText = ready + " challenges ready to claim";
        _claimAllChallenges.Visible = ready >= 2;
        EventUiStyle.SetDisabled(_claimAllChallenges, ready < 2);
        EventUiStyle.SetText(_challengeSummary, snapshots.Count == 0 ? "No challenges available for this Event." :
            ended ? "EVENT ENDED\nEarned permanent challenge rewards remain claimable." :
            "Daily tasks reset at 00:00 UTC.\nEvent and Major goals persist through Prestige.");
        if (created) GetParent()?.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(_challengeRows);
    }
    private void AddResultIcons(Control row, EventDefinition d)
    {
        foreach (string path in new[] { d.IconPath, d.CurrencyIconPath, d.Prestige.Enabled ? d.Prestige.IconPath : "" })
        {
            if (path.Length == 0) continue;
            row.AddChild(new EventImageView(_textures.Get(path), new(32, 32)) { CustomMinimumSize = new(32, 32) });
        }
    }
    private void RefreshArchives()
    {
        _archiveWanted.Clear();
        foreach (var result in _manager.GetArchives())
        {
            _archiveWanted.Add(result.EventId);
            if (!_archives.TryGetValue(result.EventId, out var label))
            {
                label = EventUiStyle.Label("Archive_" + result.EventId, "", 14); label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                var icons = new HBoxContainer { Name = "ArchiveIcons_" + result.EventId };
                if (_manager.GetDefinition(result.EventId) is { } archivedDefinition) AddResultIcons(icons, archivedDefinition);
                icons.Visible = icons.GetChildCount() > 0;
                _archiveRows.AddChild(icons);
                _archiveRows.AddChild(label); _archives.Add(result.EventId, label);
                string target = result.EventId;
                _archiveRows.AddChild(EventUiStyle.Button("ViewArchive_" + target, "View results", () => RunContentAction(() =>
                {
                    _focusedEventId = target;
                    _viewingArchive = true;
                    SelectTab("Progress");
                })));
                GetParent()?.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(label);
            }
            EventUiStyle.SetText(label, result.Name + " • " + result.Year + "\n" + NumberFormatter.Format(result.TotalEarned) + " " + result.CurrencyName
                + " • Highest level " + result.HighestLevel + "\nPrestiges: " + result.PrestigeCount + " • Highest: " + NumberFormatter.Format(result.HighestProduction)
                + "/s\n" + (_manager.GetDefinition(result.EventId)?.Prestige.ResourceName ?? "Prestige currency") + " earned: " + NumberFormatter.Format(result.TotalPrestigeResourceEarned)
                + "\nRewards: " + result.RewardsClaimed + "/" + result.RewardsTotal + " • Cosmetics: " + result.CosmeticsOwned
                + "\nBots unlocked: " + result.BotsUnlocked + (result.LegendaryBotOwned ? " • Legendary owned" : ""));
        }
        _archiveRemoved.Clear();
        foreach (var pair in _archives) if (!_archiveWanted.Contains(pair.Key)) _archiveRemoved.Add(pair.Key);
        foreach (string id in _archiveRemoved)
        {
            var label = _archives[id];
            _archiveRows.RemoveChild(label); label.QueueFree(); _archives.Remove(id);
            var icons = _archiveRows.GetNodeOrNull<Control>("ArchiveIcons_" + id);
            if (icons != null) { _archiveRows.RemoveChild(icons); icons.QueueFree(); }
            var button = _archiveRows.GetNodeOrNull<Button>("ViewArchive_" + id);
            if (button != null) { _archiveRows.RemoveChild(button); button.QueueFree(); }
        }
    }
}
