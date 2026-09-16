class_name SaveManager
extends Node


signal autosave_requested


const SAVE_PATH: String = (
	"user://idle_ai_save.json"
)


var autosave_timer: Timer


func start_autosave(
	interval_seconds: float
) -> void:
	if autosave_timer != null:
		autosave_timer.queue_free()

	autosave_timer = Timer.new()

	autosave_timer.wait_time = (
		interval_seconds
	)

	autosave_timer.one_shot = false
	autosave_timer.autostart = true

	autosave_timer.timeout.connect(
		_on_autosave_timeout
	)

	add_child(
		autosave_timer
	)


func _on_autosave_timeout() -> void:
	autosave_requested.emit()


func save_data(
	data: Dictionary
) -> bool:
	var file := FileAccess.open(
		SAVE_PATH,
		FileAccess.WRITE
	)

	if file == null:
		push_error(
			"Could not open save file."
		)

		return false

	var json_string: String = (
		JSON.stringify(
			data,
			"\t"
		)
	)

	file.store_string(
		json_string
	)

	file.close()

	return true


func load_data() -> Dictionary:
	if not FileAccess.file_exists(
		SAVE_PATH
	):
		return {}

	var file := FileAccess.open(
		SAVE_PATH,
		FileAccess.READ
	)

	if file == null:
		push_error(
			"Could not open save file."
		)

		return {}

	var json_string: String = (
		file.get_as_text()
	)

	file.close()

	var parsed: Variant = (
		JSON.parse_string(
			json_string
		)
	)

	if parsed == null:
		push_error(
			"Save file contains invalid JSON."
		)

		return {}

	if not parsed is Dictionary:
		push_error(
			"Save file has an invalid format."
		)

		return {}

	return parsed as Dictionary


func has_save() -> bool:
	return FileAccess.file_exists(
		SAVE_PATH
	)


func delete_save() -> void:
	if not has_save():
		return

	DirAccess.remove_absolute(
		ProjectSettings.globalize_path(
			SAVE_PATH
		)
	)
