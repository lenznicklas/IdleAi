class_name SlotData
extends RefCounted


var unlocked: bool = false
var machine_tier: int = 0
var machine_level: int = 1


func to_dict() -> Dictionary:
	return {
		"unlocked": unlocked,
		"machine_tier": machine_tier,
		"machine_level": machine_level
	}


func load_from_dict(
	data: Dictionary
) -> void:
	unlocked = bool(
		data.get(
			"unlocked",
			false
		)
	)

	machine_tier = int(
		data.get(
			"machine_tier",
			0
		)
	)

	machine_level = int(
		data.get(
			"machine_level",
			1
		)
	)
