#!/bin/bash

while getopts i: flag
do
	case "${flag}" in
		i) input_file=${OPTARG};;
	esac
done

if [[ -z "$input_file" ]]; then
	echo "Format: extract_poses.sh -i <input_video>";
	exit 1
fi

dir=$(pwd)
input_dir=$(pwd)/"Temp"
input_filename=$(basename "$input_file")
input_basename=$(basename "$input_filename" .mp4)
input_path=$input_dir/$input_filename
output_dir=$dir/extracted_poses
temp_dir=$dir/Temp

echo "Start Cleaning..."
rm -Rf $temp_dir
rm -Rf $output_dir
cd ~/VideoPose3D/VideoPose3D/inference
rm -Rf input_directory
rm -Rf output_directory
cd ~/VideoPose3D/VideoPose3D/data
rm -Rf data_2d_custom_myvideos.npz
echo "Cleaning done"

echo "Start Copy Video..."
cd $dir
mkdir $temp_dir
cp $input_file "$temp_dir"/
echo "Copy Video done"

echo "Start 2D Inference..."
cd ~/VideoPose3D/VideoPose3D/inference
sudo python3 infer_video_d2.py --cfg COCO-Keypoints/keypoint_rcnn_R_101_FPN_3x.yaml --output-dir $temp_dir --image-ext mp4 $input_dir
echo "2D Inference done"

echo "Start Asset preprocess..."
cd ~/VideoPose3D/VideoPose3D/data
sudo python3 prepare_data_2d_custom.py -i $temp_dir -o myvideos
mkdir $output_dir
cd $dir
python3 convert_poses2d.py $temp_dir/"$input_filename" $output_dir/"$input_basename"_poses2d
echo "Asset preprocess done"

echo "Start 3D Inference"
cd ~/VideoPose3D/VideoPose3D
sudo python3 run.py -d custom -k myvideos -arc 3,3,3,3,3 -c checkpoint --evaluate pretrained_h36m_detectron_coco.bin --render --viz-subject $input_filename --viz-action custom --viz-camera 0 --viz-video $input_path --viz-output "$output_dir/"$input_basename"_Viz.mp4" --viz-size 6 --viz-export $output_dir/$input_basename
cd $dir
python3 convert_poses3d.py $output_dir/$input_basename
rm -Rf $output_dir/"$input_basename".npy
rm -Rf Temp
echo "3D Inference done"



