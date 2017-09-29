- title : Resilient F# in the Linux Ecosystem
- description : Keeping F# code running reliably in a Linux environment, with systemd, docker, consul, and kubernetes
- author : Dave Curylo
- theme : night
- transition : default

***

## Resilient F# in the Linux Ecosystem

<br />
<br />

#### Deploy, run, and maintain self-supporting F# application code

<br />
<br />

Dave Curylo

* [@i\_no\_see\_pound](http://www.twitter.com/i_no_see_pound)
* [ninjarobot on GitHub](https://github.com/ninjarobot)
* dave.curylo on fsharp.slack.com

***

## About me

* Maintain official Docker images for fsharp and swipl
* Develop F# software for Virtustream within Dell-EMC
* Languages nerd really enjoying F# and Prolog

***

##### Resilient: _able to withstand or recover quickly from difficult conditions_

<br />
<br />

Unlikely events happen all the time.

***

### Ecosystem

| Component | Purpose |
| --------- | ------------|
| systemd | keeps processes running by tracking the PID |
| journald | structured logging, for when things go wrong |
| docker | standardized, immutable infrastructure containers |
| kubernetes | distributed scheduler |

***

### systemd

_A modern solution for process management._

* Like Windows Services, you can
    - specify startup command
    - auto or manual startup
    - set restart thresholds

* Additionally applies resource constraints (cgroups)
    - CPU
    - Memory
    - Storage and network I/O

---

### Signals, `systemd` and you!

When `systemd` should shutdown your application, it will send a `SIGTERM`.

There are different ways to handle this depending on the runtime. 

* `dotnet` - handle `AssemblyLoadContext.Unloading` event.
* `mono` - you must use Mono.Posix and wait for signals.

---

    open System
    open System.Threading

    [<EntryPoint>]
    let main argv =
        use wait = new ManualResetEventSlim()
        use cts = new CancellationTokenSource ()
        System.Runtime.Loader.AssemblyLoadContext.Default.add_Unloading(
            fun ctx -> 
                printfn "Shutting down nicely!"
                cts.Cancel ()
                wait.Set ()
            )
        let pretendService = async {
            while true do
                printfn "Hello World from F#!"
                do! Async.Sleep 5000
                // In real life we would be starting our service.
        }
        Async.Start (pretendService, cts.Token)
        wait.Wait()
        0

---

### SIGTERM on mono

On mono, System.Runtime.Loader isn't available, you need to catch the specific signal.

        let signals = 
            [|
                new Mono.Unix.UnixSignal(
                    Mono.Unix.Native.Signum.SIGTERM
                )
            |]
        Mono.Unix.UnixSignal.WaitAny (signals, -1) |> ignore

---

### Signals?  **REALLY???**

Yeah.  You probably wish you could just block on Console.ReadLine.  But you don't get a terminal attached, so there is no console.

You get one of two things, depending on how you write your application and which runtime:

* It exits immediately, blowing right through that ReadLine.
* It eats up an entire CPU core in a tight loop reading from a console that isn't there.

---

### Registering services

Once you have a nice application, ready for systemd and Unix signals, you need to tell systemd about it:

```
# deploy to /etc/systemd/system
[Unit]
Description=Hello F# service

[Service]
Type=simple # "real" unix daemons will fork - use "forking"
User=dave
ExecStart=/usr/bin/dotnet /home/dave/src/openfs/simpleservice/bin/Debug/netcoreapp2.0/simpleservice.dll
Restart=always # it will restart however it stops
RestartSec=20s # if it restarts too fast, systemd will stop restarting, throttle it here.
```

Copy to `/etc/systemd/system/fshello.service`, then `systemctl enable fshello`. 

---

We can find the process that is listening on 8080:

    lsof -i tcp:8080

and send SIGTERM to tell it to exit

    kill $PID

---

### Service control

systemctl subcommands:

* start - start the service now
* restart - restart the service now
* stop - stop the service now
* enable - enable it to start automatically
* disable - disable automatic start (doesn't actually stop it)
* reload - reloads the configuration (not the service)
* status - show the service status

---

### Limitations of `systemd`

* Use `systemd` if you package your services to deploy directly onto the operating system
    - Someone needs `root` access to set you up
* PID tracking is _not_ a health check.  Just because it's running doesn't mean it's healthy.
* Only keeps instance of a service running - it has no real means to scale your application up.
* Not cluster aware.


***

### Docker

Image - built from a recipe (a Dockerfile) and uploaded to a registry.  Images contain filesystem layers and can be based off other images, meaning they share the same layers.

Container - a process started on top of a copy of an image's filesystem using `cgroups` to limit the memory, CPU, network, and disk for the process.

Dockerfile - a set of instructions for building an image.  Each instruction is executed by creating a container from the base image (`FROM`) and applying each instruction.  After each instruction, the a new image layer is saved, eventually (if no errors), creating a new image with multiple layers.

---

### Docker daemon

Docker has a daemon for running containers - interact with it using `docker run`.  Since Dockerfiles can contain a health check which is stored in image metadata, `dockerd` will perform those health checks, although they are only really used by Swarm.

---

### Dockerfile

    FROM microsoft/dotnet
    ADD ./bin/Release/netcoreapp2.0/publish /app
    EXPOSE 8080
    CMD ["/usr/bin/dotnet", "/app/httpservice.dll"]
    HEALTHCHECK --interval=5s CMD ["curl", "http://localhost:8080/healthcheck"]

### docker run

Run a container, and you can also find that systemd plays a role here:

    docker run -p 8080:8080 -d fsserver-mono:0.0.3

    systemctl status

***

### Kubernetes: Cluster Scheduling

* How should this job run?
    - Always 3 instances
    - Up to 246 MiB RAM, up to 80% of a CPU core
* Where should the instances run?
    - Prefer certain hosts in the cluster
    - Require certain resources (network port, persistent storage, etc.)

Cluster schedulers "schedule" a job to run on the worker nodes in the cluster, placing jobs based on requested resources and available, balanced capacity.

---

### Minikube time!

[Download minikube](https://kubernetes.io/docs/tasks/tools/install-minikube/) - a small Go executable that bootstraps a little Kubernetes environment using your local hypervisor.

Point your local docker client at the minikube registry to push images there.


    export DOCKER_TLS_VERIFY="1"
    export DOCKER_HOST="tcp://172.16.106.141:2376"
    export DOCKER_CERT_PATH="/home/dave/src/openfs/certs"
    export DOCKER_API_VERSION="1.23"

Now when you build images, you're doing it in minikube's registry.

---
    apiVersion: apps/v1beta1
    kind: Deployment
    metadata:
    name: fsharp-mono-deployment
    spec:
    replicas: 3
    template:
        metadata:
        labels:
            app: fsserver-mono
        spec:
        containers:
        - name: fsserver-mono
            image: fsserver-mono
            imagePullPolicy: Never
            ports:
            - containerPort: 8080
            livenessProbe:
            httpGet:
                path: /healthcheck
                port: 8080
            initialDelaySeconds: 5
            timeoutSeconds: 1

---

### Register the deployment

    kubectl create -f fsserver-mono.yaml

    kubectl expose deployment fsharp-mono-deployment

    minikube service fsharp-mono-deployment


### Do some damage

    kubectl get services

    curl http://172.16.106.141:30262/healthcheck

    curl -X POST http://172.16.106.141:30262/zombie

    curl http://172.16.106.141:30262/info

    curl http://172.16.106.141:30262/die

    curl http://172.16.106.141:30262/info

***

### Summary

* Include healthcheck API's - HTTP if possible
* Be aware of target runtime differences
* Join FSharp Software Foundation

*** 
