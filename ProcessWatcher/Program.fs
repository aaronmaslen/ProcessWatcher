// Learn more about F# at http://fsharp.org

open System

open ProcessWatcher.Common

[<EntryPoint>]
let main argv =
    let watcher = new ProcessWatcher.Common.ProcessWatcher("", true, false)
    0 // return an integer exit code
