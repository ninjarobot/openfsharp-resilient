MONO_PATH=$(shell which mono)
MONO_LIBS=$(shell dirname $(MONO_PATH))/../lib/mono/4.5/

IMAGE_TAG?=latest

.$PHONY:
	all

all: dotnet mono

dotnet:
	dotnet publish -f netcoreapp2.0 -c Release

mono:
	FrameworkPathOverride=$(MONO_LIBS) dotnet publish -f net462 -c Release

docker: all
	docker build -t fsserver-dotnet:$(IMAGE_TAG) -f docker/dotnet/Dockerfile .
	docker build -t fsserver-mono:$(IMAGE_TAG) -f docker/mono/Dockerfile .