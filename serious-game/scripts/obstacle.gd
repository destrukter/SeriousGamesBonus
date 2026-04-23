extends RigidBody3D
class_name Obstacle

@export var obstacle_speed = 3.0

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	self.set_axis_velocity(obstacle_speed * Vector3.BACK)
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass
