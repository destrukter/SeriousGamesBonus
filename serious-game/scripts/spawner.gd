extends Node

@onready var lanes: Lanes = $"../Lanes"
@onready var obstacles = $"../Obstacles"

@export var obstacle: PackedScene

var temp_timer = 0.0

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass
		
func spawn_obstacle() -> void:
	var lane = lanes.get_random_lane()
	var inst: Obstacle= obstacle.instantiate()
	inst.position = Vector3(lanes.get_center(lane), 0, -5)
	obstacles.add_child(inst)

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	temp_timer += delta
	if temp_timer > 1:
		temp_timer -= 1
		spawn_obstacle()
