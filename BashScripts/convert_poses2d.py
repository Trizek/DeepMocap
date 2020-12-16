from numpy import load
from numpy import savetxt

import sys
import json

input_path = sys.argv[1] + ".npz"
output_path = sys.argv[2] + ".json"

data = load(input_path, allow_pickle=True)
array = data["keypoints"]

output_file = open(output_path, "w")

def get_pose_str(pose):
	res = ""
	n = 0
	for val in pose:
		if n > 0:
			res = "{0}, {1}".format(res, val)
		else:
			res = "{0}".format(val)
		n = n + 1
	return res

print("[", file=output_file)
frame_index=0
for frame in array:
	if frame_index > 0:
		print("  ,", file=output_file)
	frame_index = frame_index + 1
	print("  [", file=output_file)

	instance_index=0
	for instance in frame[1]:
		if instance_index > 0:
			print("    ,", file=output_file)
		instance_index = instance_index + 1
		print("    {", file=output_file)
		print("      \"x\": [{0}],".format(get_pose_str(instance[0])), file=output_file)
		print("      \"y\": [{0}],".format(get_pose_str(instance[1])), file=output_file)
		print("      \"confidence\": [{0}]".format(get_pose_str(instance[3])), file=output_file)
		print("    }", file=output_file)

	print("  ]", file=output_file)
print("]", file=output_file)


