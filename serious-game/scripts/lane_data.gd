extends Node 
class_name Lanes

var rng = RandomNumberGenerator.new()


@export var num_lanes = 3
@export var lane_width = 1.2

func get_random_lane() -> int:
	return rng.randi_range(0, num_lanes - 1)

func get_center(lane: int) -> float:
	return 0.5 + (lane - (float(num_lanes) / 2.0)) * lane_width

func is_leftmost(lane: int) -> bool:
	return lane == 0
	
func is_rightmost(lane: int) -> bool:
	return lane == num_lanes - 1

#3
#-lane_width 	0 = 0 - 1.5
#0				1 = 1 - 1.5
#+lane_width		2 = 2 - 1.5
#
#4
#-1.5lane_width -1.5 = 0 - 2
#-0.5lane_width -0.5 = 1 - 2
#+0.5lane_width
#+1.5lane_width
