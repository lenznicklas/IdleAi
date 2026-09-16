extends Control


const BACKGROUND_TEXTURE: Texture2D = preload(
	"res://assets/background/bg.png"
)


var tokens: float = 0.0

var machines: Array[MachineData] = []
var slots: Array[SlotData] = []

var slot_unlock_costs: Array[float] = [
	0.0,
	10.0,
	100.0,
	1_000.0,
	10_000.0,
	100_000.0
]


@onready var background: TextureRect = (
	$Background
)

@onready var token_label: Label = (
	$MarginContainer/VBoxContainer/TopBar/TokenLabel
)

@onready var income_label: Label = (
	$MarginContainer/VBoxContainer/TopBar/IncomeLabel
)

@onready var total_level_label: Label = (
	$MarginContainer/VBoxContainer/TopBar/TotalLevelLabel
)

@onready var slot_grid: GridContainer = (
	$MarginContainer/VBoxContainer/RoomPanel/RoomVBox/ScrollContainer/SlotGrid
)

@onready var message_label: Label = (
	$MarginContainer/VBoxContainer/MessageLabel
)


func _ready() -> void:
	setup_background()

	create_machine_data()
	create_slots()

	update_ui()

	message_label.text = (
		"Upgrade your first machine."
	)


func _process(delta: float) -> void:
	tokens += (
		get_total_income()
		* delta
	)

	update_top_bar()


# -------------------------
# BACKGROUND
# -------------------------

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


# -------------------------
# MACHINES
# -------------------------

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


# -------------------------
# SLOTS
# -------------------------

func create_slots() -> void:
	for i: int in range(
		slot_unlock_costs.size()
	):
		var slot := SlotData.new()

		if i == 0:
			slot.unlocked = true

		slots.append(slot)

		var slot_ui := MachineSlot.new()

		slot_ui.setup(i)

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
		slots[slot_index]
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
		slots[slot_index]
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

	slot.unlocked = true
	slot.machine_tier = 0
	slot.machine_level = 1

	message_label.text = (
		"Slot "
		+ str(slot_index + 1)
		+ " unlocked!"
	)

	update_ui()


# -------------------------
# UPGRADES
# -------------------------

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


# -------------------------
# COSTS
# -------------------------

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


# -------------------------
# MILESTONES
# -------------------------

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


# -------------------------
# INCOME
# -------------------------

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


# -------------------------
# TOTAL LEVEL
# -------------------------

func get_total_level() -> int:
	var total: int = 0

	for slot: SlotData in slots:
		if not slot.unlocked:
			continue

		var machine: MachineData = (
			machines[
				slot.machine_tier
			]
		)

		total += (
			slot.machine_tier
			* machine.max_level
			+ slot.machine_level
		)

	return total


# -------------------------
# UI
# -------------------------

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
		"Tokens: "
		+ format_number(
			tokens
		)
	)

	income_label.text = (
		"+"
		+ format_number(
			get_total_income()
		)
		+ " / sec"
	)

	total_level_label.text = (
		"Level: "
		+ str(
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


# -------------------------
# NUMBER FORMAT
# -------------------------

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
