If you are trying to upgrade or change FFMPEG, you've come to the right place.

see: Dockerfile.netffmpeg

This is a slightly modified verion of this project: https://github.com/jrottenberg/ffmpeg

The modification is that instead of the image base being
ubuntu, it's the Microsoft dotnet image that we want to run
the rest of our container with. this image becomes the 
base image of several others that need ffmpeg or related tools.