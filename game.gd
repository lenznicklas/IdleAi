extends Control


const MAIN_THEME: Theme = preload(
	"res://assets/themes/main_theme.tres"
)

const BACKGROUND_TEXTURE: Texture2D = preload(
	"res://assets/background/bg.png"
)


# --------------------------------------------------
# SAVE SETTINGS
# --------------------------------------------------

const SAVE_VERSION: int = 1

# Autosave every 10 seconds.
const AUTOSAVE_INTERVAL_SECONDS: float = 10.0

# Offline production = 25 % of normal production.
const OFFLINE_INCOME_FACTOR: float = 0.25


# --------------------------------------------------
# GAME DATA
# --------------------------------------------------

var tokens: float = 0.0

var machines: Array[MachineData] = []
var slots: Array[SlotData] = []

var stats := StatsData.new()

var save_manager: SaveManager


var last_save_unix: int = 0

var last_saved_income_per_second: float = 0.0


var slot_unlock_costs: Array[float] = [
	0.0,
	10.0,
	100.0,
	1_000.0,
	10_000.0,
	100_000.0
]


# --------------------------------------------------
# UI REFERENCES
# --------------------------------------------------

@onready var background: TextureRect = (
	$Background
)


@onready var token_card: TextureButton = (
	$MarginContainer/VBoxContainer/TopStats/TokenCard
)

@onready var stats_card: TextureButton = (
	$MarginContainer/VBoxContainer/TopStats/StatsCard
)

@onready var level_card: TextureButton = (
	$MarginContainer/VBoxContainer/TopStats/LevelCard
)


@onready var token_label: Label = (
	$MarginContainer/VBoxContainer/TopStats/TokenCard/TokenLabel
)

@onready var total_level_label: Label = (
	$MarginContainer/VBoxContainer/TopStats/LevelCard/TotalLevelLabel
)


@onready var slot_grid: GridContainer = (
	$MarginContainer/VBoxContainer/RoomPanel/RoomVBox/ScrollContainer/SlotGrid
)

@onready var message_label: Label = (
	$MarginContainer/VBoxContainer/MessageLabel
)


@onready var token_popup: PanelContainer = (
	$TokenPopup
)

@onready var level_popup: PanelContainer = (
	$LevelPopup
)


@onready var stats_overlay: Control = (
	$StatsOverlay
)

@onready var stats_income_label: Label = (
	$StatsOverlay/StatsPanel/Margin/VBox/IncomeLabel
)

@onready var stats_earned_label: Label = (
	$StatsOverlay/StatsPanel/Margin/VBox/EarnedLabel
)

@onready var stats_spent_label: Label = (
	$StatsOverlay/StatsPanel/Margin/VBox/SpentLabel
)

@onready var stats_slots_label: Label = (
	$StatsOverlay/StatsPanel/Margin/VBox/SlotsLabel
)

@onready var stats_level_label: Label = (
	$StatsOverlay/StatsPanel/Margin/VBox/LevelLabel
)

@onready var stats_unlock_spend_label: Label = (
	$StatsOverlay/StatsPanel/Margin/VBox/UnlockSpendLabel
)

@onready var stats_machine_spend_label: Label = (
	$StatsOverlay/StatsPanel/Margin/VBox/MachineSpendLabel
)

@onready var stats_close_button: Button = (
	$StatsOverlay/StatsPanel/Margin/VBox/CloseButton
)


# --------------------------------------------------
# READY
# --------------------------------------------------

func _ready() -> void:
	theme = MAIN_THEME

	setup_background()
	setup_topbar_actions()
	setup_stats_overlay()

	create_machine_data()
	create_slots()

	setup_save_system()

	var offline_earned: float = (
		load_game()
	)

	update_ui()

	if offline_earned > 0.0:
		message_label.text = (
			"Welcome back! +"
			+ format_number(
				offline_earned
			)
			+ " offline Tokens"
		)

	else:
		message_label.text = (
			"Upgrade your first machine."
		)

	save_game()


# --------------------------------------------------
# GAME LOOP
# --------------------------------------------------

func _process(
	delta: float
) -> void:
	var earned: float = (
		get_total_income()
		* delta
	)

	tokens += earned

	stats.add_earned(
		earned
	)

	update_top_bar()


# --------------------------------------------------
# APPLICATION EVENTS
# --------------------------------------------------

func _notification(
	what: int
) -> void:
	if save_manager == null:
		return

	if (
		what
		== NOTIFICATION_APPLICATION_PAUSED
	):
		save_game()

	elif (
		what
		== NOTIFICATION_APPLICATION_RESUMED
	):
		var earned: float = (
			apply_offline_income(
				last_save_unix,
				last_saved_income_per_second
			)
		)

		if earned > 0.0:
			message_label.text = (
				"Welcome back! +"
				+ format_number(
					earned
				)
				+ " offline Tokens"
			)

		update_ui()
		save_game()

	elif (
		what
		== NOTIFICATION_WM_CLOSE_REQUEST
	):
		save_game()

		get_tree().quit()


# --------------------------------------------------
# SAVE SYSTEM
# --------------------------------------------------

func setup_save_system() -> void:
	save_manager = SaveManager.new()

	add_child(
		save_manager
	)

	save_manager.autosave_requested.connect(
		save_game
	)

	save_manager.start_autosave(
		AUTOSAVE_INTERVAL_SECONDS
	)

	last_save_unix = (
		get_current_unix_time()
	)


func save_game() -> void:
	if save_manager == null:
		return

	if slots.is_empty():
		return


	var slot_data: Array = []

	for slot: SlotData in slots:
		slot_data.append(
			slot.to_dict()
		)


	var now: int = (
		get_current_unix_time()
	)

	var current_income: float = (
		get_total_income()
	)


	var save_data: Dictionary = {
		"save_version":
			SAVE_VERSION,

		"tokens":
			tokens,

		"last_save_unix":
			now,

		"income_per_second":
			current_income,

		"slots":
			slot_data,

		"stats":
			stats.to_dict()
	}


	var success: bool = (
		save_manager.save_data(
			save_data
		)
	)


	if success:
		last_save_unix = now

		last_saved_income_per_second = (
			current_income
		)


func load_game() -> float:
	if save_manager == null:
		return 0.0

	if not save_manager.has_save():
		last_save_unix = (
			get_current_unix_time()
		)

		last_saved_income_per_second = (
			get_total_income()
		)

		return 0.0


	var save_data: Dictionary = (
		save_manager.load_data()
	)

	if save_data.is_empty():
		return 0.0


	tokens = float(
		save_data.get(
			"tokens",
			0.0
		)
	)


	load_slots_from_save(
		save_data
	)


	var saved_stats: Variant = (
		save_data.get(
			"stats",
			{}
		)
	)

	if saved_stats is Dictionary:
		stats.load_from_dict(
			saved_stats
			as Dictionary
		)


	var saved_time: int = int(
		save_data.get(
			"last_save_unix",
			get_current_unix_time()
		)
	)


	var saved_income: float = float(
		save_data.get(
			"income_per_second",
			get_total_income()
		)
	)


	var offline_earned: float = (
		apply_offline_income(
			saved_time,
			saved_income
		)
	)


	last_save_unix = (
		get_current_unix_time()
	)

	last_saved_income_per_second = (
		get_total_income()
	)


	return offline_earned


func load_slots_from_save(
	save_data: Dictionary
) -> void:
	var saved_slots: Variant = (
		save_data.get(
			"slots",
			[]
		)
	)

	if not saved_slots is Array:
		return


	var saved_array: Array = (
		saved_slots as Array
	)


	var amount: int = mini(
		saved_array.size(),
		slots.size()
	)


	for i: int in range(
		amount
	):
		var slot_entry: Variant = (
			saved_array[i]
		)

		if not slot_entry is Dictionary:
			continue

		slots[i].load_from_dict(
			slot_entry as Dictionary
		)


func apply_offline_income(
	saved_time: int,
	income_per_second: float
) -> float:
	var now: int = (
		get_current_unix_time()
	)

	var seconds_offline: int = (
		now - saved_time
	)


	if seconds_offline <= 0:
		return 0.0


	if income_per_second <= 0.0:
		return 0.0


	var full_income: float = (
		income_per_second
		* float(
			seconds_offline
		)
	)


	var offline_income: float = (
		full_income
		* OFFLINE_INCOME_FACTOR
	)


	tokens += offline_income


	stats.add_offline_earned(
		offline_income
	)


	return offline_income


func get_current_unix_time() -> int:
	return int(
		Time.get_unix_time_from_system()
	)


# --------------------------------------------------
# SETUP
# --------------------------------------------------

func setup_background() -> void:
	background.texture = (
		BACKGROUND_TEXTURE
	)

	background.set_anchors_and_offsets_preset(
		Control.PRESET_FULL_RECT
	)

	background.expand_mode = (
		TextureRect.EXPAND_IGNORE_SIZE
	)

	background.stretch_mode = (
		TextureRect.STRETCH_KEEP_ASPECT_COVERED
	)

	background.mouse_filter = (
		Control.MOUSE_FILTER_IGNORE
	)


func setup_topbar_actions() -> void:
	token_card.pressed.connect(
		toggle_token_popup
	)

	level_card.pressed.connect(
		toggle_level_popup
	)

	stats_card.pressed.connect(
		open_stats
	)


func setup_stats_overlay() -> void:
	stats_close_button.pressed.connect(
		close_stats
	)

	setup_round_button(
		stats_close_button
	)


func setup_round_button(
	button: Button
) -> void:
	var normal := StyleBoxFlat.new()

	normal.bg_color = Color(
		0.025,
		0.04,
		0.065,
		0.96
	)

	normal.corner_radius_top_left = 12
	normal.corner_radius_top_right = 12
	normal.corner_radius_bottom_left = 12
	normal.corner_radius_bottom_right = 12


	var hover := (
		normal.duplicate()
		as StyleBoxFlat
	)

	hover.bg_color = Color(
		0.03,
		0.12,
		0.2,
		1.0
	)


	var pressed := (
		normal.duplicate()
		as StyleBoxFlat
	)

	pressed.bg_color = Color(
		0.02,
		0.18,
		0.3,
		1.0
	)


	button.add_theme_stylebox_override(
		"normal",
		normal
	)

	button.add_theme_stylebox_override(
		"hover",
		hover
	)

	button.add_theme_stylebox_override(
		"pressed",
		pressed
	)


# --------------------------------------------------
# TOPBAR POPUPS
# --------------------------------------------------

func toggle_token_popup() -> void:
	level_popup.hide()

	if token_popup.visible:
		token_popup.hide()
		return

	position_popup_below(
		token_popup,
		token_card
	)

	token_popup.show()


func toggle_level_popup() -> void:
	token_popup.hide()

	if level_popup.visible:
		level_popup.hide()
		return

	position_popup_below(
		level_popup,
		level_card
	)

	level_popup.show()


func position_popup_below(
	popup: Control,
	card: Control
) -> void:
	await get_tree().process_frame

	var card_pos: Vector2 = (
		card.global_position
	)

	var card_size: Vector2 = (
		card.size
	)

	var popup_width: float = (
		popup.size.x
	)

	popup.global_position = Vector2(
		card_pos.x
		+ card_size.x / 2.0
		- popup_width / 2.0,

		card_pos.y
		+ card_size.y
		+ 6.0
	)


# --------------------------------------------------
# STATS OVERLAY
# --------------------------------------------------

func open_stats() -> void:
	token_popup.hide()
	level_popup.hide()

	update_stats_overlay()

	stats_overlay.show()
	stats_overlay.move_to_front()


func close_stats() -> void:
	stats_overlay.hide()


func update_stats_overlay() -> void:
	stats_income_label.text = (
		"Tokens / sec: "
		+ format_number(
			get_total_income()
		)
	)


	stats_earned_label.text = (
		"Total earned: "
		+ format_number(
			stats.total_earned
		)
		+ "\nOffline earned: "
		+ format_number(
			stats.offline_earned
		)
	)


	stats_spent_label.text = (
		"Total spent: "
		+ format_number(
			stats.total_spent
		)
	)


	stats_slots_label.text = (
		"Unlocked slots: "
		+ str(
			get_unlocked_slot_count()
		)
		+ " / "
		+ str(
			slots.size()
		)
	)


	stats_level_label.text = (
		"Total level: "
		+ str(
			get_total_level()
		)
	)


	stats_unlock_spend_label.text = (
		"Slot unlocks: "
		+ format_number(
			stats.slot_unlock_spent
		)
	)


	var machine_text: String = ""

	for machine: MachineData in machines:
		var spent: float = (
			stats.get_machine_spending(
				machine.machine_name
			)
		)

		machine_text += (
			machine.machine_name
			+ ": "
			+ format_number(
				spent
			)
			+ "\n"
		)


	stats_machine_spend_label.text = (
		machine_text.strip_edges()
	)


func get_unlocked_slot_count() -> int:
	var count: int = 0

	for slot: SlotData in slots:
		if slot.unlocked:
			count += 1

	return count


# --------------------------------------------------
# MACHINES
# --------------------------------------------------

func create_machine_data() -> void:
	machines.append(
		MachineData.new(
			"Laptop",
			1.0,
			5.0,
			150.0,
			10,
			preload(
				"res://assets/machines/laptop.png"
			)
		)
	)

	machines.append(
		MachineData.new(
			"PC",
			15.0,
			50.0,
			1_500.0,
			10,
			preload(
				"res://assets/machines/pc.png"
			)
		)
	)

	machines.append(
		MachineData.new(
			"Workstation",
			150.0,
			500.0,
			15_000.0,
			10,
			preload(
				"res://assets/machines/workstation.png"
			)
		)
	)

	machines.append(
		MachineData.new(
			"Server",
			1_500.0,
			5_000.0,
			0.0,
			10,
			preload(
				"res://assets/machines/server.png"
			)
		)
	)


# --------------------------------------------------
# SLOTS
# --------------------------------------------------

func create_slots() -> void:
	for i: int in range(
		slot_unlock_costs.size()
	):
		var slot := SlotData.new()

		if i == 0:
			slot.unlocked = true

		slots.append(
			slot
		)


		var slot_ui := (
			MachineSlot.new()
		)

		slot_ui.setup(
			i
		)

		slot_ui.action_pressed.connect(
			on_slot_button_pressed
		)

		slot_grid.add_child(
			slot_ui
		)


func on_slot_button_pressed(
	slot_index: int
) -> void:
	var slot: SlotData = (
		slots[
			slot_index
		]
	)

	if not slot.unlocked:
		unlock_slot(
			slot_index
		)

		return


	upgrade_slot(
		slot
	)


func unlock_slot(
	slot_index: int
) -> void:
	var slot: SlotData = (
		slots[
			slot_index
		]
	)

	var cost: float = (
		slot_unlock_costs[
			slot_index
		]
	)


	if tokens < cost:
		message_label.text = (
			"Not enough Tokens."
		)

		return


	tokens -= cost


	stats.add_slot_spending(
		cost
	)


	slot.unlocked = true
	slot.machine_tier = 0
	slot.machine_level = 1


	message_label.text = (
		"Slot "
		+ str(
			slot_index + 1
		)
		+ " unlocked!"
	)


	update_ui()


# --------------------------------------------------
# UPGRADES
# --------------------------------------------------

func upgrade_slot(
	slot: SlotData
) -> void:
	var machine: MachineData = (
		machines[
			slot.machine_tier
		]
	)


	if (
		slot.machine_level
		< machine.max_level
	):
		upgrade_machine_level(
			slot,
			machine
		)

		return


	upgrade_machine_tier(
		slot,
		machine
	)


func upgrade_machine_level(
	slot: SlotData,
	machine: MachineData
) -> void:
	var cost: float = (
		get_level_upgrade_cost(
			slot
		)
	)


	if tokens < cost:
		message_label.text = (
			"Not enough Tokens."
		)

		return


	tokens -= cost


	stats.add_machine_spending(
		machine.machine_name,
		cost
	)


	slot.machine_level += 1


	if slot.machine_level == 5:
		message_label.text = (
			machine.machine_name
			+ " Level 5! Production x2!"
		)

	elif slot.machine_level == 10:
		message_label.text = (
			machine.machine_name
			+ " Level 10! Production x4!"
		)

	else:
		message_label.text = (
			machine.machine_name
			+ " upgraded to Level "
			+ str(
				slot.machine_level
			)
		)


	update_ui()


func upgrade_machine_tier(
	slot: SlotData,
	machine: MachineData
) -> void:
	if (
		slot.machine_tier
		>= machines.size() - 1
	):
		message_label.text = (
			"Maximum machine reached."
		)

		return


	var cost: float = (
		machine.tier_upgrade_cost
	)


	if tokens < cost:
		message_label.text = (
			"Not enough Tokens."
		)

		return


	tokens -= cost


	stats.add_machine_spending(
		machine.machine_name,
		cost
	)


	slot.machine_tier += 1
	slot.machine_level = 1


	var new_machine: MachineData = (
		machines[
			slot.machine_tier
		]
	)


	message_label.text = (
		"Upgraded to "
		+ new_machine.machine_name
		+ "!"
	)


	update_ui()


# --------------------------------------------------
# COSTS
# --------------------------------------------------

func get_level_upgrade_cost(
	slot: SlotData
) -> float:
	var machine: MachineData = (
		machines[
			slot.machine_tier
		]
	)


	var multiplier: float = pow(
		1.35,
		slot.machine_level - 1
	)


	return (
		machine.base_upgrade_cost
		* multiplier
	)


# --------------------------------------------------
# MILESTONES
# --------------------------------------------------

func get_milestone_multiplier(
	level: int
) -> float:
	var multiplier: float = 1.0


	if level >= 5:
		multiplier *= 2.0


	if level >= 10:
		multiplier *= 2.0


	return multiplier


func get_milestone_text(
	level: int
) -> String:
	if level >= 10:
		return "x4 production"


	if level >= 5:
		return "x2 production"


	return "Level 5: x2"


# --------------------------------------------------
# INCOME
# --------------------------------------------------

func get_slot_income(
	slot: SlotData
) -> float:
	if not slot.unlocked:
		return 0.0


	var machine: MachineData = (
		machines[
			slot.machine_tier
		]
	)


	var level_multiplier: float = (
		1.0
		+ float(
			slot.machine_level - 1
		)
		* 0.25
	)


	var milestone_multiplier: float = (
		get_milestone_multiplier(
			slot.machine_level
		)
	)


	return (
		machine.base_income
		* level_multiplier
		* milestone_multiplier
	)


func get_total_income() -> float:
	var total: float = 0.0


	for slot: SlotData in slots:
		total += (
			get_slot_income(
				slot
			)
		)


	return total


# --------------------------------------------------
# TOTAL LEVEL
# --------------------------------------------------

func get_total_level() -> int:
	var total: int = 0


	for slot: SlotData in slots:
		if not slot.unlocked:
			continue


		for tier: int in range(
			slot.machine_tier
		):
			total += (
				machines[
					tier
				].max_level
			)


		total += (
			slot.machine_level
		)


	return total


# --------------------------------------------------
# UI
# --------------------------------------------------

func update_ui() -> void:
	update_top_bar()


	for i: int in range(
		slots.size()
	):
		update_slot_ui(
			i
		)


func update_top_bar() -> void:
	token_label.text = (
		format_number(
			tokens
		)
	)


	total_level_label.text = (
		str(
			get_total_level()
		)
	)


func update_slot_ui(
	slot_index: int
) -> void:
	var slot: SlotData = (
		slots[
			slot_index
		]
	)


	var slot_ui: MachineSlot = (
		slot_grid.get_child(
			slot_index
		)
		as MachineSlot
	)


	if not slot.unlocked:
		slot_ui.show_locked(
			format_number(
				slot_unlock_costs[
					slot_index
				]
			)
		)

		return


	var machine: MachineData = (
		machines[
			slot.machine_tier
		]
	)


	var button_text: String


	if (
		slot.machine_level
		< machine.max_level
	):
		button_text = (
			"Upgrade\n"
			+ format_number(
				get_level_upgrade_cost(
					slot
				)
			)
			+ " Tokens"
		)


	elif (
		slot.machine_tier
		< machines.size() - 1
	):
		var next_machine: MachineData = (
			machines[
				slot.machine_tier
				+ 1
			]
		)


		button_text = (
			"Upgrade: "
			+ next_machine.machine_name
			+ "\n"
			+ format_number(
				machine.tier_upgrade_cost
			)
			+ " Tokens"
		)


	else:
		button_text = "MAX"


	slot_ui.show_machine(
		machine,
		slot.machine_level,

		format_number(
			get_slot_income(
				slot
			)
		),

		get_milestone_text(
			slot.machine_level
		),

		button_text
	)


# --------------------------------------------------
# NUMBER FORMAT
# --------------------------------------------------

func format_number(
	value: float
) -> String:
	if value >= 1_000_000_000_000.0:
		return "%.2fT" % (
			value
			/ 1_000_000_000_000.0
		)


	if value >= 1_000_000_000.0:
		return "%.2fB" % (
			value
			/ 1_000_000_000.0
		)


	if value >= 1_000_000.0:
		return "%.2fM" % (
			value
			/ 1_000_000.0
		)


	if value >= 1_000.0:
		return "%.2fK" % (
			value
			/ 1_000.0
		)


	if value >= 100.0:
		return "%.0f" % value


	if value >= 10.0:
		return "%.1f" % value


	return "%.2f" % value
