Resilient F# in the Linux Ecosystem
========

Notes, examples, and slide content from the OpenFSharp 2017 talk.

* [HTTP service](httpservice/Program.fs) with health checking and clean shutdown
* `systemd` [service definition](httpservice/fsservice.service) to register a .NET Core service
* [`Dockerfile`](httpservice/docker/dotnet/Dockerfile) including a health check
* [Slides](slides/slides/index.md)
* Bonus: [embedding mono with mkbundle](alpine-microcontainer/Dockerfile)