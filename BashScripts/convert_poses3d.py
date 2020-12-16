from numpy import load
from numpy import savetxt

import sys
import json

input_path = sys.argv[1] + ".npy"
output_path = sys.argv[1] + "_poses3d.json"

data = load(input_path)
lists = data.tolist()
json_str = json.dumps(lists)

output_file = open(output_path, "w")
output_file.write(json_str)


