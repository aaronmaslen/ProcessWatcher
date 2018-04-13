open System

open FSharp.Text.RegexProvider

type ResolutionRegex = Regex< @"^(?<XRes>\d{3,})x(?<YRes>\d{3,})">

type Parameter =
| Resolution of int * int
| ExecutablePath of string
| RunningDirectory of string
| MonitorChildProcesses of bool
| Error of string

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
            
            | _ -> (Error <| sprintf "Unknown param -%s" name, valueIndex)
        
        let argSeq = arg |> Seq.singleton |> Seq.append argSeq

        let next = nameIndex + 1
        if next >= argNames.Length then
            if valueIndex < argValues.Length
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

    0 // return an integer exit code
