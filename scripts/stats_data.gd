class_name StatsData
extends RefCounted


var total_earned: float = 0.0
var offline_earned: float = 0.0

var total_spent: float = 0.0
var slot_unlock_spent: float = 0.0

var machine_spending: Dictionary = {}


func add_earned(
	amount: float
) -> void:
	if amount <= 0.0:
		return

	total_earned += amount


func add_offline_earned(
	amount: float
) -> void:
	if amount <= 0.0:
		return

	offline_earned += amount
	total_earned += amount


func add_slot_spending(
	amount: float
) -> void:
	if amount <= 0.0:
		return

	total_spent += amount
	slot_unlock_spent += amount


func add_machine_spending(
	machine_name: String,
	amount: float
) -> void:
	if amount <= 0.0:
		return

	total_spent += amount

	if not machine_spending.has(
		machine_name
	):
		machine_spending[
			machine_name
		] = 0.0

	machine_spending[
		machine_name
	] += amount


func get_machine_spending(
	machine_name: String
) -> float:
	if not machine_spending.has(
		machine_name
	):
		return 0.0

	return float(
		machine_spending[
			machine_name
		]
	)


func to_dict() -> Dictionary:
	return {
		"total_earned":
			total_earned,

		"offline_earned":
			offline_earned,

		"total_spent":
			total_spent,

		"slot_unlock_spent":
			slot_unlock_spent,

		"machine_spending":
			machine_spending
	}


func load_from_dict(
	data: Dictionary
) -> void:
	total_earned = float(
		data.get(
			"total_earned",
			0.0
		)
	)

	offline_earned = float(
		data.get(
			"offline_earned",
			0.0
		)
	)

	total_spent = float(
		data.get(
			"total_spent",
			0.0
		)
	)

	slot_unlock_spent = float(
		data.get(
			"slot_unlock_spent",
			0.0
		)
	)

	var saved_machine_spending: Variant = (
		data.get(
			"machine_spending",
			{}
		)
	)

	if (
		saved_machine_spending
		is Dictionary
	):
		machine_spending = (
			saved_machine_spending
			as Dictionary
		)

	else:
		machine_spending = {}
