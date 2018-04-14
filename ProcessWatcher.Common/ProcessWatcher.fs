namespace ProcessWatcher.Common

open System
open System.Diagnostics
open System.IO
open System.Linq
open System.Management
open System.Reactive.Linq

module Wmi =
    let private defaultWmiEventQuery =
        new WqlEventQuery(
            "__InstanceOperationEvent",
            TimeSpan(0,0,0,0,1000),
            "TargetInstance ISA 'Win32_Process'"
        )
    let mutable private watcher = new ManagementEventWatcher(defaultWmiEventQuery)
    let WatcherEvent = watcher.EventArrived.Select(fun wea -> wea.NewEvent)
    let CreateEvent = WatcherEvent.Where(fun e -> e.ClassPath.ClassName = "__InstanceCreationEvent")
    let DeleteEvent = WatcherEvent.Where(fun e -> e.ClassPath.ClassName = "__InstanceDeletionEvent")
    let mutable private started = false
    let Start(span) =
        if not started then
            watcher <-
                match span with
                | Some span ->
                    let query =
                        new WqlEventQuery(
                            "__InstanceOperationEvent",
                            span,
                            "TargetInstance ISA 'Win32_Process'"
                        )
                    new ManagementEventWatcher(query)
                | None -> watcher

            watcher.Start()
            started <- true
    let Stop() =
        if started then
            watcher.Stop()
            started <- false
    let Property<'T> name (mbe : ManagementBaseObject) = mbe.[name] :?> 'T
    let TargetInstance = Property<ManagementBaseObject> "TargetInstance"
    let Pid = Property<uint32> "ProcessId"
    let ParentPid = Property<uint32> "ParentProcessId"
    let ProcessName = Property<string> "Name"



type ProcessEventType =
| Start
| Exit

type ProcessWatcher(processInfo : ProcessStartInfo, start, watchChildren) =
    let processName = processInfo.FileName
    let mutable pids : (uint32 * bool) list = list.Empty
    let processEvent = new Event<_>()
    let endEvent = new Event<_>()
    let createEvent =
        Wmi
            .CreateEvent
            .Select(Wmi.TargetInstance)
            .Where(fun targetInstance ->
                pids.Any(fun (pid, _) ->
                    (Wmi.Pid targetInstance) = pid
                ) || ((Wmi.ProcessName targetInstance) = processName)
                || if watchChildren
                    then
                        pids.Any(fun (pid, _) ->
                            (Wmi.ParentPid targetInstance) = pid
                        )
                    else false
            )
    let deleteEvent =
        Wmi
            .DeleteEvent
            .Select(Wmi.TargetInstance)
            .Where(fun targetInstance ->
                pids.Any(fun (pid, _) ->
                    (Wmi.Pid targetInstance) = pid
                ) || ((Wmi.ProcessName targetInstance) = processName)
            )
    new (processPath, start, watchChildren) =
        let psi = new ProcessStartInfo(processPath)
        ProcessWatcher (psi, start, watchChildren)
    member __.Start(?span) =
        createEvent.Add(fun targetInstance ->
            let pid = Wmi.Pid targetInstance
            if not (pids.Any(fun (p, _) -> p = pid)) then
                pids <- List.append pids (List.singleton (pid, true))
        )

        deleteEvent.Add(fun targetInstance ->
            let pid = Wmi.Pid targetInstance
            if pids.Any(fun (p, _) -> p = pid) then
                pids <-
                    List.append
                        (List.where(fun (p, _) -> p <> pid) pids)
                        (List.singleton(pid, false))
        )

        createEvent.Add(fun targetInstance -> 
            processEvent
                .Trigger(
                    Start,
                    Wmi.Pid targetInstance,
                    Wmi.ProcessName targetInstance
                )
        )

        deleteEvent.Add(fun targetInstance ->
            processEvent
                .Trigger(
                    Exit,
                    Wmi.Pid targetInstance,
                    Wmi.ProcessName targetInstance
                )
        )

        deleteEvent.Add(fun _ -> if not (pids.Any(fun (_, running) -> running)) then endEvent.Trigger())

        Wmi.Start(span)

        if start then
            let p : Process = Process.Start(processInfo)
            pids <- List.append pids (List.singleton ((uint32 p.Id), true))

    [<CLIEvent>]
    member __.ProcessEvent = processEvent.Publish

    [<CLIEvent>]
    member __.AllProcessesEndedEvent = endEvent.Publish