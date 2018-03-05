namespace ProcessWatcher.Common
open System
open System.Diagnostics
open System.IO
open System.Linq
open System.Management
open System.Reactive.Linq

module Wmi =
    let private wmiEventQuery = new WqlEventQuery("__InstanceOperationEvent",
                                                  TimeSpan(0,0,0,0,100),
                                                  "TargetInstance ISA 'Win32_Process'")
    let private watcher = new ManagementEventWatcher(wmiEventQuery)
    let WatcherEvent = watcher.EventArrived.Select(fun wea -> wea.NewEvent)
    let CreateEvent = WatcherEvent.Where(fun e -> e.ClassPath.ClassName = "__InstanceCreationEvent")
    let DeleteEvent = WatcherEvent.Where(fun e -> e.ClassPath.ClassName = "__InstanceDeletionEvent")
    let mutable private started = false
    let Start() =
        if not started then
            watcher.Start()
            started <- true
    let Stop() =
        if started then
            watcher.Stop()
            started <- false

type ProcessEventType =
| Start
| Exit

type ProcessWatcher(processPath, start, watchChildren) =
    let processName = Path.GetFileName(processPath)
    let mutable pids : (uint32 * bool) list = list.Empty
    let processEvent = new Event<_>()
    let endEvent = new Event<_>()
    let createEvent = Wmi.CreateEvent
                         .Select(fun ce -> ce.["TargetInstance"] :?> ManagementBaseObject)
                         .Where(fun targetInstance ->
                                    pids.Any(fun (pid, _) -> (targetInstance.["ProcessId"] :?> uint32) = pid) ||
                                    targetInstance.["Name"].ToString() = processName ||
                                    if watchChildren then
                                        pids.Any(fun (pid, _) -> (targetInstance.["ParentProcessId"] :?> uint32) = pid)
                                    else false)
    let deleteEvent = Wmi.DeleteEvent
                         .Select(fun ce -> ce.["TargetInstance"] :?> ManagementBaseObject)
                         .Where(fun targetInstance ->
                                    pids.Any(fun (pid, _) -> (targetInstance.["ProcessId"] :?> uint32) = pid) ||
                                    targetInstance.["Name"].ToString() = processName)
    member __.Start() =
        createEvent.Add(fun targetInstance ->
                            let pid = targetInstance.["ProcessId"] :?> uint32
                            if not (pids.Any(fun (p, _) -> p = pid)) then
                                pids <- List.append pids (List.singleton (pid, true)))

        deleteEvent.Add(fun targetInstance ->
                            let pid = targetInstance.["ProcessId"] :?> uint32
                            if pids.Any(fun (p, _) -> p = pid) then
                                pids <- List.append
                                                (List.where (fun (p, _) -> p <> pid) pids)
                                                (List.singleton (pid, false)))

        createEvent.Add(fun targetInstance -> processEvent.Trigger (Start, targetInstance.["Name"].ToString()))

        deleteEvent.Add(fun targetInstance -> processEvent.Trigger (Exit, targetInstance.["Name"].ToString()))

        deleteEvent.Add(fun _ -> if not (pids.Any(fun (_, running) -> running)) then endEvent.Trigger())

        Wmi.Start()

        if start then
            let p : Process = Process.Start(processPath)
            pids <- List.append pids (List.singleton ((uint32 p.Id), true))

    [<CLIEvent>]
    member __.ProcessEvent = processEvent.Publish

    [<CLIEvent>]
    member __.AllProcessesEndedEvent = endEvent.Publish