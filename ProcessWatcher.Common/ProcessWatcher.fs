namespace ProcessWatcher.Common
open System
open System.Diagnostics
open System.IO
open System.Linq
open System.Management
open System.Reactive.Linq

module Wmi =
    let private wmiEventQuery = new WqlEventQuery("__InstanceOperationEvent", "TargetInstance ISA 'Win32_Process'")
    let private watcher = new ManagementEventWatcher(wmiEventQuery)
    let WatcherEvent = watcher.EventArrived.Select(fun wea -> wea.NewEvent)
    let CreateEvent = WatcherEvent.Where(fun e -> e.ClassPath.ClassName = "__InstanceCreationEvent")
    let DeleteEvent = WatcherEvent.Where(fun e -> e.ClassPath.ClassName = "_InstanceDeletionEvent")

type ProcessEventType =
| Start
| Exit

type ProcessWatcher(processPath, start, watchChildren) =
    let processName = Path.GetFileName(processPath)
    let mutable pids : uint32 list = list.Empty
    let processEvent = new Event<_>()
    let createEvent = Wmi.CreateEvent
                         .Select(fun ce -> ce.["TargetInstance"] :?> ManagementBaseObject)
                         .Where(fun targetInstance ->
                                    targetInstance.["Name"].ToString() = processName ||
                                    List.contains (targetInstance.["ProcessId"] :?> uint32) pids ||
                                    if watchChildren then
                                        List.contains (targetInstance.["ParentProcessId"] :?> uint32) pids
                                    else false)
    let deleteEvent = Wmi.DeleteEvent
                         .Select(fun ce -> ce.["TargetInstance"] :?> ManagementBaseObject)
                         .Where(fun targetInstance -> 
                                    targetInstance.["Name"].ToString() = processName ||
                                    List.contains (targetInstance.["ProcessId"] :?> uint32) pids)
    do
        if start then
            let p : Process = Process.Start(processName)
            pids <- List.append pids (List.singleton (uint32 p.Id))

        createEvent.Add(fun targetInstance ->
                            let pid = targetInstance.["ProcessId"] :?> uint32
                            if not (List.contains pid pids) then
                                pids <- List.append pids (List.singleton pid))

        createEvent.Add(fun targetInstance -> processEvent.Trigger (Start, targetInstance.["Name"].ToString()))

        deleteEvent.Add(fun targetInstance -> processEvent.Trigger (Exit, targetInstance.["Name"].ToString()))

    [<CLIEvent>]
    member __.ProcessEvent = processEvent.Publish