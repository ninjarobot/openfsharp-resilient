// Learn more about F# at http://fsharp.org

open System

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    Console.ReadLine () |> ignore // no console, this line gets skipped.
    printfn "Shutting down now!"
    0 // return an integer exit code
