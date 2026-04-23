extends Node3D

@onready var lanes: Lanes = $"../Lanes"
var current_lane: int = 1

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.

func move_to_lane(lane: int) -> void:
	self.current_lane = lane
	self.position = Vector3(lanes.get_center(lane), self.position.y, self.position.z)

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	if Input.is_action_just_pressed("go_left") and !lanes.is_leftmost(current_lane):
		self.move_to_lane(current_lane - 1)
	elif Input.is_action_just_pressed("go_right") and !lanes.is_rightmost(current_lane):
		self.move_to_lane(current_lane + 1)
