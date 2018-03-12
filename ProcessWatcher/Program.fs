open System

open ProcessWatcher.Common

[<EntryPoint>]
let main argv =
    let watcher = new ProcessWatcher(argv.[0], true, true)
    watcher.ProcessEvent.Add (fun (eventType, pid, name) -> Console.WriteLine("{0} ({1}) {2}", name, pid, eventType))
    watcher.Start()
    watcher.AllProcessesEndedEvent |> (Async.AwaitEvent >> Async.RunSynchronously)
    0 // return an integer exit code
