// Learn more about F# at http://fsharp.org

open System
open System.Threading

[<EntryPoint>]
let main argv =
    // nuget add package System.Runtime.Loader
    // btw - it's totally different in mono (use Mono.Posix)
    // We can use `kill -SIGTERM $PID` to stop cleanly.
    use wait = new ManualResetEventSlim()
    use cts = new CancellationTokenSource ()
    System.Runtime.Loader.AssemblyLoadContext.Default.add_Unloading(
        fun ctx -> 
            // Might do some nice stuff here, like stopping 
            // services, closing sockets, etc.
            printfn "Shutting down nicely!"
            cts.Cancel ()
            wait.Set ()
        )
    let loop = async {
        while true do
            printfn "Hello World from F#!"
            do! Async.Sleep 5000
            // In real life we would be starting our service.
    }
    Async.Start (loop, cts.Token)
    wait.Wait()
    0 // return an integer exit code


(*
    With Unloading handled, your application catches SIGTERM.
    Not coincidentally, systemd sends SIGTERM when you say
    `systemctl stop fshello`

    Your application will cleanly unload.
    systemctl subcommands:
        - start - start the service now
        - stop - stop the service now
        - enable - enable it to start automatically
        - disable - disable automatic start (doesn't actually stop it)
        - reload - reloads the configuration (not the service)
        - status - show the service status
    
    Logs (stdio):
        journalctl -u fshello
        journalctl -u fshello --vacuum-size=0
*)
