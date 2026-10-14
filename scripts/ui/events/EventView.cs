using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

/// <summary>The shared Event page retains all cards across ticks, upgrades, prestige and reopen.</summary>
internal sealed partial class EventView : Control
{
    private readonly EventManager _manager;
    private readonly EventTextureCache _textures;
    private readonly Label _title, _time, _currency, _description, _status, _level, _production, _cores, _next, _story, _ended, _rewardSummary;
    private readonly ColorRect _backgroundColor;
    private readonly TextureRect _background, _currencyIcon;
    private readonly PanelContainer _header;
    private readonly Dictionary<string, Control> _sections = [];
    private readonly Dictionary<string, Button> _tabs = [];
    private readonly List<(EventGeneratorDefinition Definition, EventGeneratorCard Card)> _generators = [];
    private readonly List<(EventMilestone Definition, EventItemCard Card)> _rewards = [];
    private readonly List<(EventBotDefinition Definition, EventItemCard Card)> _bots = [];
    private readonly List<(EventShopItem Definition, EventItemCard Card)> _shop = [];
    private readonly VBoxContainer _generatorRows, _rewardRows, _botRows, _shopRows, _archiveRows;
    private readonly Dictionary<string, Label> _archives = [];
    private readonly HashSet<string> _archiveWanted = [];
    private readonly List<string> _archiveRemoved = [];
    private readonly EventPrestigePanel _prestige;
    private readonly Action<string> _upgradeAction, _claimAction, _botAction, _buyAction;
    private string? _eventId;
    private string? _focusedEventId;
    internal EventDefinition? CurrentEvent => _focusedEventId == null ? _manager.DisplayEvent : _manager.GetDefinition(_focusedEventId);
    internal void OpenCurrent() { _focusedEventId = _manager.DisplayEvent?.Id; Refresh(true); }
    private string _selectedTab = "Progress";
    private long _lastCountdownUnix = -1;
    internal MarginContainer ContentMargin { get; }

    internal EventView(EventManager manager, EventTextureCache textures, Action close, Action<string> upgrade,
        Action<string> claim, Action<string> bot, Action<string> buy, Action<int, long> prestige)
    {
        _manager = manager; _textures = textures;
        _upgradeAction = upgrade; _claimAction = claim; _botAction = bot; _buyAction = buy;
        Name = "EventPage"; ZIndex = 650; ClipContents = true; Visible = false;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _backgroundColor = new ColorRect { Color = new(.008f, .018f, .035f, .99f), MouseFilter = MouseFilterEnum.Ignore };
        _backgroundColor.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(_backgroundColor);
        _background = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, Modulate = new(1, 1, 1, .45f), MouseFilter = MouseFilterEnum.Ignore };
        _background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(_background);
        ContentMargin = new MarginContainer(); ContentMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ContentMargin.AddThemeConstantOverride("margin_left", 22); ContentMargin.AddThemeConstantOverride("margin_right", 22);
        AddChild(ContentMargin);
        var layout = new VBoxContainer(); layout.AddThemeConstantOverride("separation", 12); ContentMargin.AddChild(layout);
        var header = EventUiStyle.Panel(layout, "EventHeader");
        _header = (PanelContainer)header.GetParent().GetParent();
        header.AddThemeConstantOverride("separation", 8);
        var top = new HBoxContainer(); top.AddThemeConstantOverride("separation", 10); header.AddChild(top);
        _title = EventUiStyle.Label("EventTitle", "Event", 22); _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.AutowrapMode = TextServer.AutowrapMode.WordSmart; _title.MaxLinesVisible = 2; top.AddChild(_title);
        var closeButton = EventUiStyle.Button("EventCloseButton", "", close);
        closeButton.Icon = textures.Get("res://assets/ui/x.png"); closeButton.ExpandIcon = true;
        closeButton.CustomMinimumSize = new(56, 56); closeButton.TooltipText = "Back to map"; top.AddChild(closeButton);
        _time = EventUiStyle.Label("EventCountdown", "No active event", 13); header.AddChild(_time);
        var currencyRow = new HBoxContainer(); header.AddChild(currencyRow);
        _currencyIcon = new TextureRect { CustomMinimumSize = new(28, 28), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        currencyRow.AddChild(_currencyIcon); _currency = EventUiStyle.Label("EventCurrency", "", 18); currencyRow.AddChild(_currency);
        var summary = new GridContainer { Columns = 2 }; summary.AddThemeConstantOverride("h_separation", 16); header.AddChild(summary);
        _level = Summary(summary, "EventLevel"); _production = Summary(summary, "EventProduction");
        _cores = Summary(summary, "EventCores"); _next = Summary(summary, "EventNextUnlock");
        var scroll = new ScrollContainer { Name = "EventScroll", SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever };
        layout.AddChild(scroll);
        var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; body.AddThemeConstantOverride("separation", 12); scroll.AddChild(body);
        _description = EventUiStyle.Label("EventDescription", "", 12); _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _description.MaxLinesVisible = 3; body.AddChild(_description);
        _status = EventUiStyle.Label("EventStatus", "", 13); body.AddChild(_status);
        _story = EventUiStyle.Label("EventStory", "", 13); _story.AutowrapMode = TextServer.AutowrapMode.WordSmart; body.AddChild(_story);
        _ended = EventUiStyle.Label("EventEndedStats", "", 13); _ended.AutowrapMode = TextServer.AutowrapMode.WordSmart; body.AddChild(_ended);
        _generatorRows = Section(body, "Progress", "EventProgressPanel");
        _prestige = new EventPrestigePanel(manager, this, prestige, () => CurrentEvent); _sections["Prestige"] = _prestige.Root; body.AddChild(_prestige.Root);
        _rewardRows = Section(body, "Rewards", "EventRewardsPanel");
        _rewardSummary = EventUiStyle.Label("EventRewardSummary", "", 13);
        _rewardSummary.ClipText = true; _rewardRows.AddChild(_rewardSummary);
        _botRows = Section(body, "Bots", "EventBotsPanel");
        _shopRows = Section(body, "Shop", "EventShopPanel");
        _archiveRows = Section(body, "Archive", "EventArchivePanel");
        _archiveRows.AddChild(EventUiStyle.Label("ArchiveNote", "Completed Major Events retain their final stats, rewards and cosmetic completion.", 12));
        var tabs = new GridContainer { Name = "EventTabs", Columns = 3 };
        tabs.AddThemeConstantOverride("h_separation", 8); tabs.AddThemeConstantOverride("v_separation", 8); layout.AddChild(tabs);
        foreach (string tab in new[] { "Progress", "Prestige", "Bots", "Rewards", "Shop", "Archive" })
        {
            string selected = tab;
            var button = EventUiStyle.Button("Event" + tab + "Tab", tab, () => SelectTab(selected));
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill; tabs.AddChild(button); _tabs[tab] = button;
        }
        SelectTab("Progress");
        VisibilityChanged += () => { if (!Visible) _prestige.Dismiss(); };
    }
    private static Label Summary(Control parent, string name)
    {
        var label = EventUiStyle.Label(name, "", 12); label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.ClipText = true; parent.AddChild(label); return label;
    }
    private VBoxContainer Section(Control body, string tab, string name)
    {
        var box = new VBoxContainer { Name = name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 16); body.AddChild(box); _sections[tab] = box; return box;
    }
    private void SelectTab(string tab)
    {
        _selectedTab = tab; _prestige.Dismiss();
        foreach (var pair in _sections) pair.Value.Visible = pair.Key == tab;
        Color accent = Color.FromHtml(CurrentEvent?.Theme.AccentHex ?? "#33b3f2");
        foreach (var pair in _tabs) EventUiStyle.StyleButton(pair.Value, pair.Key == tab ? accent : EventUiStyle.Muted);
        Refresh();
    }
    private void BuildCards(EventDefinition? d)
    {
        foreach (var item in _generators) { item.Card.Root.Hide(); item.Card.Root.QueueFree(); } _generators.Clear();
        foreach (var item in _rewards) { item.Card.Root.Hide(); item.Card.Root.QueueFree(); } _rewards.Clear();
        foreach (var item in _bots) { item.Card.Root.Hide(); item.Card.Root.QueueFree(); } _bots.Clear();
        foreach (var item in _shop) { item.Card.Root.Hide(); item.Card.Root.QueueFree(); } _shop.Clear();
        foreach (var container in new[] { _generatorRows, _botRows, _shopRows })
            foreach (Node child in container.GetChildren()) if (child is Label) { container.RemoveChild(child); child.QueueFree(); }
        if (d == null) return;
        var definitions = _manager.GetGenerators(d.Id);
        foreach (var g in definitions)
        {
            string target = g.Id;
            var card = new EventGeneratorCard(d, g, _textures, definitions, () => _upgradeAction(target), _generators.Count == 0);
            _generatorRows.AddChild(card.Root); _generators.Add((g, card));
        }
        Color accent = Color.FromHtml(d.Theme.AccentHex);
        foreach (var m in d.Milestones)
        {
            string target = m.Id;
            var card = new EventItemCard("EventReward_" + target, EventPresentation.RewardName(m.Reward, d),
                string.IsNullOrEmpty(m.Reward.IconPath) ? d.IconPath : m.Reward.IconPath, _textures, accent, () => _claimAction(target));
            _rewardRows.AddChild(card.Root); _rewards.Add((m, card));
        }
        foreach (var b in d.Bots)
        {
            string target = b.Id;
            var card = new EventItemCard("EventBot_" + target, b.Name, b.IconPath, _textures, accent, () => _botAction(target));
            _botRows.AddChild(card.Root); _bots.Add((b, card));
        }
        if (d.Bots.Count == 0) _botRows.AddChild(EventUiStyle.Label("EventBotsPlaceholder", "No event bots in this protocol.", 14));
        foreach (var s in d.Shop)
        {
            string target = s.Id;
            var card = new EventItemCard("EventShop_" + target, s.Name, s.IconPath, _textures, accent, () => _buyAction(target));
            _shopRows.AddChild(card.Root); _shop.Add((s, card));
        }
        if (d.Shop.Count == 0) _shopRows.AddChild(EventUiStyle.Label("EventShopPlaceholder", "No event items available yet.", 14));
        GetParent()?.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(this);
    }
    internal void Refresh(bool force = false)
    {
        if (!force && !IsVisibleInTree()) return;
        var d = CurrentEvent;
        if (_eventId != d?.Id)
        {
            _eventId = d?.Id; BuildCards(d); _lastCountdownUnix = -1;
            _prestige.Dismiss();
            _backgroundColor.Color = Color.FromHtml(d?.Theme.BackgroundHex ?? "#020509");
            _background.Texture = _textures.Get(d?.BackgroundPath ?? "");
            _currencyIcon.Texture = _textures.Get(d?.CurrencyIconPath ?? ""); _currencyIcon.Visible = _currencyIcon.Texture != null;
            Color accent = Color.FromHtml(d?.Theme.AccentHex ?? "#33b3f2");
            _header.AddThemeStyleboxOverride("panel", MapController.CreateNodeStyle(new(.025f, .018f, .04f, .97f), accent, 2, 8));
            _title.Modulate = accent.Lightened(.35f);
            foreach (var pair in _tabs) EventUiStyle.StyleButton(pair.Value, pair.Key == _selectedTab ? accent : EventUiStyle.Muted);
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
        EventUiStyle.SetText(_cores, d?.Prestige.Enabled == true ? NumberFormatter.Format(p.PrestigeResource) + " " + d.Prestige.ResourceName : "Prestige disabled");
        EventUiStyle.SetText(_next, d == null ? "" : "Next: " + (_manager.GetNextUnlock(d.Id) ?? "All unlocked"));
        _next.TooltipText = _next.Text;
        string story = "";
        if (d != null) foreach (var message in d.Story) if (p.TotalEarned >= message.TargetEarned) story = message.Text;
        EventUiStyle.SetText(_story, story);
        _ended.Visible = ended;
        EventUiStyle.SetText(_ended, !ended || d == null ? "" : "Final earnings: " + NumberFormatter.Format(p.TotalEarned) + " " + d.CurrencyName
            + "\nHighest production: " + NumberFormatter.Format(p.HighestProduction) + "/s • Prestiges: " + p.PrestigeCount
            + "\n" + d.Prestige.ResourceName + " earned: " + _manager.GetTotalPrestigeResourceEarned(d.Id)
            + " • Rewards claimed: " + _manager.ClaimedRewardCount(d.Id) + "/" + d.Milestones.Count);
        EventUiStyle.SetText(_rewardSummary, d == null ? "" : "Claimed: " + _manager.ClaimedRewardCount(d.Id) + "/" + d.Milestones.Count
            + (p.DeveloperCredits > 0 ? " • Developer credits: " + NumberFormatter.Format(p.DeveloperCredits) : ""));
        if (d != null)
        {
            if (_selectedTab == "Progress") foreach (var item in _generators) item.Card.Refresh(_manager.GetGenerator(d.Id, item.Definition.Id), ended, d.CurrencyName);
            if (_selectedTab == "Rewards") foreach (var row in _rewards)
            {
                bool unlocked = _manager.IsMilestoneUnlocked(d.Id, row.Definition.Id), claimed = _manager.IsRewardClaimed(d.Id, row.Definition.Id);
                row.Card.Refresh("Lifetime: " + NumberFormatter.Format(row.Definition.TargetAmount) + " " + d.CurrencyName,
                    claimed ? "CLAIMED" : !unlocked ? "LOCKED" : ended && !_manager.CanClaimReward(d.Id, row.Definition.Id) ? "EXPIRED" : "CLAIM",
                    _manager.CanClaimReward(d.Id, row.Definition.Id));
            }
            if (_selectedTab == "Bots") foreach (var row in _bots)
            {
                bool owned = _manager.IsBotOwned(d.Id, row.Definition.Id), equipped = _manager.IsBotActive(d.Id, row.Definition.Id);
                row.Card.Refresh(row.Definition.Rarity + " • +" + ((row.Definition.ProductionMultiplier - 1) * 100).ToString("0.#")
                    + "% production\nClaim milestone " + _manager.GetMilestoneTargetText(d.Id, row.Definition.UnlockMilestoneId),
                    !owned ? "LOCKED" : equipped ? "ACTIVE • UNEQUIP" : "OWNED • EQUIP", owned && active);
            }
            if (_selectedTab == "Shop") foreach (var row in _shop)
            {
                int count = _manager.PurchasedCount(d.Id, row.Definition.Id);
                row.Card.Refresh(row.Definition.Description + "\n" + NumberFormatter.Format(row.Definition.Cost) + " "
                    + (row.Definition.CostCurrency == EventShopCurrency.EventCurrency ? d.CurrencyName : d.Prestige.ResourceName)
                    + " • " + count + "/" + row.Definition.PurchaseLimit,
                    ended ? "EVENT ENDED" : count >= row.Definition.PurchaseLimit ? "SOLD OUT" : "BUY • " + NumberFormatter.Format(row.Definition.Cost),
                    _manager.CanBuy(d.Id, row.Definition.Id));
            }
        }
        _prestige.Refresh(d);
        if (_selectedTab == "Archive") RefreshArchives();
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
                _archiveRows.AddChild(label); _archives.Add(result.EventId, label);
                string target = result.EventId;
                _archiveRows.AddChild(EventUiStyle.Button("ViewArchive_" + target, "View results", () =>
                {
                    _focusedEventId = target;
                    SelectTab("Progress");
                }));
                GetParent()?.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(label);
            }
            EventUiStyle.SetText(label, result.Name + " • " + result.Year + "\n" + NumberFormatter.Format(result.TotalEarned) + " " + result.CurrencyName
                + " • Highest level " + result.HighestLevel + "\nPrestiges: " + result.PrestigeCount + " • Highest: " + NumberFormatter.Format(result.HighestProduction)
                + "/s\nRewards: " + result.RewardsClaimed + "/" + result.RewardsTotal + " • Cosmetics: " + result.CosmeticsOwned);
        }
        _archiveRemoved.Clear();
        foreach (var pair in _archives) if (!_archiveWanted.Contains(pair.Key)) _archiveRemoved.Add(pair.Key);
        foreach (string id in _archiveRemoved)
        {
            var label = _archives[id];
            _archiveRows.RemoveChild(label); label.QueueFree(); _archives.Remove(id);
            var button = _archiveRows.GetNodeOrNull<Button>("ViewArchive_" + id);
            if (button != null) { _archiveRows.RemoveChild(button); button.QueueFree(); }
        }
    }
}
