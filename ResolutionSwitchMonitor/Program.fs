open System

open FSharp.Text.RegexProvider

open ProcessWatcher.Common
open DisplayModeSwitch.DisplayMode
open DisplayModeSwitch

type ResolutionRegex = Regex< @"^(?<XRes>\d{3,})x(?<YRes>\d{3,})">

type Parameter =
| Resolution of int * int
| ExecutablePath of string
| RunningDirectory of string
| MonitorChildProcesses of bool
| WaitForProcess of string
| Error of string
| Help

let (|Head|_|) (head : string) (s : string) =
    if s.StartsWith head then s.Substring head.Length |> Some
    else None

let resMatch s =
    let r = ResolutionRegex().TypedMatch s
    if r.Success then Resolution (r.XRes.Value |> int, r.YRes.Value |> int) |> Some
    else None

let parseArgs (argv : string[]) =
    let getArgs =
        Array.map
            (fun s ->
                match s with
                | Head "--" s -> ([| s |], [| |])
                | Head "-" s -> (seq { for c in s -> c |> string } |> Seq.toArray, [| |])
                | _ -> ([| |], [| s |])
            )
        >> Array.unzip
        >> (fun (a, b) -> (Array.collect id a, Array.collect id b))

    let (argNames, argValues) = getArgs argv

    let rec args nameIndex valueIndex argSeq =
        let name = argNames.[nameIndex]
        let value = if valueIndex >= argValues.Length then None else argValues.[valueIndex] |> Some

        let (arg, nextValue) =
            match name with
            | "c"
            | "monitor-child-processes" -> (MonitorChildProcesses true, valueIndex)
            | "no-monitor-child-processes" -> (MonitorChildProcesses false, valueIndex)
            | "r" ->
                match value with
                | None -> (Error <| sprintf "Missing resolution value", valueIndex)
                | Some s ->
                    match resMatch s with
                    | Some r -> (r, valueIndex + 1)
                    | None -> (Error <| sprintf "%s does not look like a resolution" s, valueIndex + 1)
            | Head "resolution=" s ->
                match resMatch s with
                | Some r -> (r, valueIndex)
                | None -> (Error <| sprintf "%s does not look like a resolution" s, valueIndex)
            | "d" ->
                match value with
                | None -> (Error <| sprintf "Missing running directory", valueIndex)
                | Some d -> (RunningDirectory d, valueIndex + 1)
            | Head "running-directory=" d -> (RunningDirectory d, valueIndex)
            | "x" ->
                match value with
                | None -> (Error <| sprintf "Missing executable path", valueIndex)
                | Some x -> (ExecutablePath x, valueIndex + 1)
            | Head "executable-path=" x -> (ExecutablePath x, valueIndex + 1)
            | "w" ->
                match value with
                | None -> (Error <| sprintf "Missing process name", valueIndex)
                | Some p -> (WaitForProcess p, valueIndex + 1)
            | Head "wait-for-process=" p -> (WaitForProcess p, valueIndex)
            | "h"
            | "help" -> (Help, valueIndex)
            | _ -> (Error <| sprintf "Unknown param -%s" name, valueIndex)
        
        let argSeq = arg |> Seq.singleton |> Seq.append argSeq

        let next = nameIndex + 1
        if next >= argNames.Length then
            if valueIndex = argValues.Length
                && argSeq |> (not << Seq.exists (fun a -> match a with ExecutablePath _ -> true | _ -> false))
            then
                argValues.[valueIndex]
                |> ExecutablePath
                |> Seq.singleton
                |> Seq.append argSeq
            else argSeq
        else args (nameIndex + 1) nextValue argSeq

    args 0 0 Seq.empty

[<EntryPoint>]
let main argv =
    let args = parseArgs argv

    for a in args do
        match a with
        | Resolution (x, y) -> printfn "Resolution: %ix%i" x y
        | ExecutablePath x -> printfn "Execute: %s" x
        | RunningDirectory d -> printfn "Run in: %s" d
        | MonitorChildProcesses true -> printfn "Monitor child processes"
        | MonitorChildProcesses false -> printfn "Do not monitor child processes"
        | Error e -> printfn "Error: %s" e
        | WaitForProcess p -> printfn "Wait for process: %s" p
        | Help ->
            printfn "%s"
                ("Usage: ResolutionSwitchMonitor [-r resolution] [-d <running directory>"
                + " [-c [-w <wait for process>]] [-x] <executable path>")
        
    if args |> Seq.exists (fun a -> match a with ExecutablePath _ -> true | _ -> false) then
        let exe =
           args
           |> Seq.rev
           |> Seq.pick (fun a -> match a with ExecutablePath x -> Some x | _ -> None)
        
        let watchChildren =
            if args |> Seq.exists (fun a -> match a with MonitorChildProcesses _ -> true | _ -> false) then
                args
                |> Seq.rev
                |> Seq.pick (fun a -> match a with MonitorChildProcesses c -> Some c | _ -> None)
            else false

        let proc = ProcessWatcher (exe, true, watchChildren)
        proc.Start()

        if args |> Seq.exists (fun a -> match a with Resolution _ -> true | _ -> false) then
            let (x, y) =
                args
                |> Seq.rev
                |> Seq.pick (fun a -> match a with Resolution (x,y) -> (x,y) |> Some | _ -> None)

            let dev =
                GetDisplayDevices()
                |> Seq.where
                    (fun d ->
                        printfn "Device Detected: %s, Active: %b, Primary: %b" d.DeviceString d.Active d.PrimaryDevice
                        d.Active
                        && d.PrimaryDevice
                    )
                |> Seq.head

            let mode =
                GetDisplayModes dev
                |> Seq.where
                    (fun m ->
                        m.FixedOutput.IsNone
                        || m.FixedOutput.Value = FixedOutputMode.Default
                    )
                |> Seq.where
                    (fun m ->
                        match m.Resolution with
                        | Some (mx, my) -> mx = (x |> uint32) && my = (y |> uint32)
                        | None -> false
                    )
                |> Seq.tryHead

            if mode.IsSome then
                let x, y = mode.Value.Resolution.Value
                printfn "Mode: %ux%u" x y
                let result =
                    SetDisplayMode dev mode.Value (SetDisplayModeOptions.Temporary |> Seq.singleton)
                    |> function
                    | Ok _ -> "OK"
                    | Result.Error RestartNeeded -> "failed. Restart needed"
                    | Result.Error u -> "failed. Code: " + (u |> string)
                printfn
                    "Setting mode %s" result

        proc.AllProcessesEndedEvent |> (Async.AwaitEvent >> Async.RunSynchronously)
        0
    else
        printfn "Missing executable path"
        -1
