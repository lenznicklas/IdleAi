class_name MachineSlot
extends Control


signal action_pressed(slot_index: int)


const EMPTY_TEXTURE: Texture2D = preload(
	"res://assets/machines/empty.png"
)

const BORDER_TEXTURE: Texture2D = preload(
	"res://assets/background/border.png"
)


var slot_index: int = 0

var title_label: Label
var machine_texture: TextureRect
var level_label: Label
var income_label: Label
var milestone_label: Label
var action_button: Button


func setup(index: int) -> void:
	slot_index = index

	custom_minimum_size = Vector2(
		0.0,
		380.0
	)

	size_flags_horizontal = (
		Control.SIZE_EXPAND_FILL
	)

	size_flags_vertical = (
		Control.SIZE_FILL
	)

	create_ui()

func create_ui() -> void:
	var margin := MarginContainer.new()

	margin.name = "Margin"

	margin.set_anchors_and_offsets_preset(
		Control.PRESET_FULL_RECT
	)

	margin.add_theme_constant_override(
		"margin_left",
		12
	)

	margin.add_theme_constant_override(
		"margin_right",
		12
	)

	margin.add_theme_constant_override(
		"margin_top",
		12
	)

	margin.add_theme_constant_override(
		"margin_bottom",
		12
	)

	add_child(margin)


	var content := VBoxContainer.new()

	content.name = "Content"

	content.size_flags_horizontal = (
		Control.SIZE_EXPAND_FILL
	)

	content.size_flags_vertical = (
		Control.SIZE_EXPAND_FILL
	)

	margin.add_child(content)


	create_title(content)
	create_machine_image(content)
	create_level(content)
	create_income(content)
	create_milestone(content)
	create_button(content)
	create_border()

func create_title(
	content: VBoxContainer
) -> void:
	title_label = Label.new()

	title_label.name = "Title"

	title_label.custom_minimum_size = Vector2(
		0.0,
		35.0
	)

	title_label.horizontal_alignment = (
		HORIZONTAL_ALIGNMENT_CENTER
	)

	title_label.vertical_alignment = (
		VERTICAL_ALIGNMENT_CENTER
	)

	title_label.clip_text = true

	content.add_child(
		title_label
	)


func create_machine_image(
	content: VBoxContainer
) -> void:
	machine_texture = TextureRect.new()

	machine_texture.name = "MachineTexture"

	machine_texture.custom_minimum_size = Vector2(
		0.0,
		150.0
	)

	machine_texture.size_flags_horizontal = (
		Control.SIZE_EXPAND_FILL
	)

	machine_texture.size_flags_vertical = (
		Control.SIZE_EXPAND_FILL
	)

	machine_texture.expand_mode = (
		TextureRect.EXPAND_IGNORE_SIZE
	)

	machine_texture.stretch_mode = (
		TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	)

	machine_texture.mouse_filter = (
		Control.MOUSE_FILTER_IGNORE
	)

	content.add_child(
		machine_texture
	)


func create_level(
	content: VBoxContainer
) -> void:
	level_label = Label.new()

	level_label.name = "Level"

	level_label.custom_minimum_size = Vector2(
		0.0,
		25.0
	)

	level_label.horizontal_alignment = (
		HORIZONTAL_ALIGNMENT_CENTER
	)

	level_label.clip_text = true

	content.add_child(
		level_label
	)


func create_income(
	content: VBoxContainer
) -> void:
	income_label = Label.new()

	income_label.name = "Income"

	income_label.custom_minimum_size = Vector2(
		0.0,
		25.0
	)

	income_label.horizontal_alignment = (
		HORIZONTAL_ALIGNMENT_CENTER
	)

	income_label.clip_text = true

	content.add_child(
		income_label
	)


func create_milestone(
	content: VBoxContainer
) -> void:
	milestone_label = Label.new()

	milestone_label.name = "Milestone"

	milestone_label.custom_minimum_size = Vector2(
		0.0,
		30.0
	)

	milestone_label.horizontal_alignment = (
		HORIZONTAL_ALIGNMENT_CENTER
	)

	milestone_label.clip_text = true

	content.add_child(
		milestone_label
	)


func create_button(
	content: VBoxContainer
) -> void:
	action_button = Button.new()

	action_button.name = "ActionButton"

	action_button.custom_minimum_size = Vector2(
		0.0,
		65.0
	)

	action_button.size_flags_horizontal = (
		Control.SIZE_EXPAND_FILL
	)

	action_button.clip_text = true

	action_button.pressed.connect(
		_on_action_pressed
	)

	content.add_child(
		action_button
	)


func create_border() -> void:
	var border := NinePatchRect.new()

	border.name = "Border"

	border.texture = BORDER_TEXTURE

	border.set_anchors_and_offsets_preset(
		Control.PRESET_FULL_RECT
	)

	border.mouse_filter = (
		Control.MOUSE_FILTER_IGNORE
	)

	border.patch_margin_left = 20
	border.patch_margin_top = 20
	border.patch_margin_right = 20
	border.patch_margin_bottom = 20

	border.draw_center = false

	add_child(border)

	border.move_to_front()


func _on_action_pressed() -> void:
	action_pressed.emit(
		slot_index
	)


func show_locked(
	unlock_cost: String
) -> void:
	title_label.text = (
		"Slot "
		+ str(slot_index + 1)
	)

	machine_texture.texture = (
		EMPTY_TEXTURE
	)

	level_label.text = "LOCKED"

	income_label.text = ""

	milestone_label.text = ""

	action_button.text = (
		"Unlock\n"
		+ unlock_cost
		+ " Tokens"
	)


func show_machine(
	machine: MachineData,
	level: int,
	income: String,
	milestone: String,
	button_text: String
) -> void:
	title_label.text = (
		machine.machine_name
	)

	machine_texture.texture = (
		machine.texture
	)

	level_label.text = (
		"Level "
		+ str(level)
		+ " / "
		+ str(machine.max_level)
	)

	income_label.text = (
		"+"
		+ income
		+ " Tokens/s"
	)

	milestone_label.text = (
		milestone
	)

	action_button.text = (
		button_text
	)
