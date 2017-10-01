// Learn more about F# at http://fsharp.org

open System
open System.Threading
open Suave
open Suave.Operators
open Suave.Successful
open Suave.Filters

/// Handler for /info - for demo purposes, so we can easily get the PID and a
/// little other infomation.
let info =
    use p = System.Diagnostics.Process.GetCurrentProcess ()
    let assemblies = AppDomain.CurrentDomain.GetAssemblies() |> Array.map(fun a -> sprintf "\t%s" a.FullName) |> String.concat "\n"
    sprintf "PID: %d\nMemory: %d\nCPU: %O\nAssemblies:\n%s\n" p.Id p.WorkingSet64 p.TotalProcessorTime assemblies

let cts = new CancellationTokenSource ()

/// Don't normally create an endpoint that will kill the service.  This is here
/// so we can create chaos and demonstrate resiliency.
let die () = 
    // lsof -i tcp:8080 # shows running socket
    // curl -X POST http://localhost:8080/die
    cts.Cancel ()
    System.Environment.Exit (0)
    "shutting down"

/// Don't normally create an endpoint that will zombie the service either.
/// This one kills the Suave listener, but the process still runs, illustrating
/// the need for an HTTP health check.
let zombie () = 
    cts.Cancel ()
    "shutting down"

let rec fib (n:uint32) =
    match n with
    | 0u | 1u -> n
    | _ -> fib (n-1u) + fib (n-2u)

/// Nice CPU eating service in case we want to show it die in the middle of
/// processing a request.
let fibHandler num =
    fun (ctx:HttpContext) ->
        async {
            let result = fib num |> sprintf "%d"
            return! OK result ctx
        }
        
let app : WebPart = 
    choose [
        path "/healthcheck" >=> GET >=> OK "OK"
        path "/info" >=> GET >=> request (fun r -> OK info)
        path "/die" >=> POST >=> request (fun r -> OK (die ()))
        path "/zombie" >=> POST >=> request (fun r -> OK (zombie ()))
        GET >=> pathScan "/fib/%d" fibHandler
        RequestErrors.NOT_FOUND "nobody here"
    ]

[<EntryPoint>]
let main argv =
    let config = {
        defaultConfig with
            cancellationToken = cts.Token
            bindings = [HttpBinding.create HTTP (System.Net.IPAddress.Parse("0.0.0.0")) 8080us]
    }
    let listening, server = startWebServerAsync config app
    Async.Start (server, cts.Token)

    // How we wait is platform-specific
    // dotnet catches SIGTERM:
    #if !NET462
    use wait = new ManualResetEventSlim ()
    System.Runtime.Loader.AssemblyLoadContext.Default.add_Unloading(
        fun ctx ->
            printfn "Shutting down..."
            wait.Set ()
        )
    wait.Wait ()

    #else
    // mono needs to be told to catch it (no System.Runtime.Loader).
    let signals = [| new Mono.Unix.UnixSignal(Mono.Unix.Native.Signum.SIGTERM) |]
    Mono.Unix.UnixSignal.WaitAny (signals, -1) |> ignore
    #endif

    // finally after a SIGTERM we fall through and cancel the service.
    cts.Cancel ()
    0
