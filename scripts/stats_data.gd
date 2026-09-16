class_name StatsData
extends RefCounted


var total_earned: float = 0.0
var total_spent: float = 0.0

var slot_unlock_spent: float = 0.0

var machine_spending: Dictionary = {}


func add_earned(amount: float) -> void:
	if amount <= 0.0:
		return

	total_earned += amount


func add_slot_spending(amount: float) -> void:
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

	if not machine_spending.has(machine_name):
		machine_spending[machine_name] = 0.0

	machine_spending[machine_name] += amount


func get_machine_spending(
	machine_name: String
) -> float:
	if not machine_spending.has(machine_name):
		return 0.0

	return float(
		machine_spending[machine_name]
	)
