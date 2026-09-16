class_name MachineData
extends RefCounted


var machine_name: String
var base_income: float
var base_upgrade_cost: float
var tier_upgrade_cost: float
var max_level: int
var texture: Texture2D


func _init(
	p_name: String,
	p_income: float,
	p_upgrade_cost: float,
	p_tier_upgrade_cost: float,
	p_max_level: int,
	p_texture: Texture2D
) -> void:

	machine_name = p_name
	base_income = p_income
	base_upgrade_cost = p_upgrade_cost
	tier_upgrade_cost = p_tier_upgrade_cost
	max_level = p_max_level
	texture = p_texture
