open System

open ProcessWatcher.Common

[<EntryPoint>]
let main argv =
    let watcher = new ProcessWatcher(argv.[0], true, true)
    watcher.ProcessEvent.Add (fun (eventType, name) -> Console.WriteLine("{0} {1}", name, eventType))
    watcher.Start()
    Async.AwaitEvent (watcher.AllProcessesEndedEvent) |> Async.RunSynchronously
    0 // return an integer exit code
