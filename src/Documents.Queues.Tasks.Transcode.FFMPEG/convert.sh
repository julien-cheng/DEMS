#!/bin/sh

INPUT=$1
OUTPUT_PATH=$PWD
filename=$(basename "$INPUT")
filename="${filename%.*}"
OUTPUT_WEBM="$filename.webm"
OUTPUT_MP4="$filename.mp4"
OUTPUT_PNG="$filename.png"
OUTPUT_META="$filename.meta"
OUTPUT_MP3="$filename.mp3"


echo ----------------------------------------$OUTPUT_MP3
ffmpeg -i "$INPUT" \
	"$OUTPUT_MP3"

echo ----------------------------------------$OUTPUT_META
ffmpeg -i "$INPUT" \
	-f ffmetadata "$OUTPUT_META"

echo ----------------------------------------$OUTPUT_MP4
ffmpeg -i "$INPUT" \
	-c:v libx264 -preset medium -b:v 555k -pass 1 -c:a aac -b:a 128k -f mp4 /dev/null && \
ffmpeg -i "$INPUT" \
	-c:v libx264 -preset medium -b:v 555k -pass 2 -c:a aac -b:a 128k "$OUTPUT_MP4"

echo ----------------------------------------$OUTPUT_WEBM
ffmpeg -i "$INPUT" \
	-c:v libvpx -b:v 1M -c:a libvorbis "$OUTPUT_WEBM"

echo ----------------------------------------$OUTPUT_PNG
ffmpeg -i "$INPUT" \
	-ss 00:00:01 -vframes 1 "$OUTPUT_PNG"



