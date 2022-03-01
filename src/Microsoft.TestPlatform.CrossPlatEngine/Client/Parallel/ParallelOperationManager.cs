// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;

using ObjectModel;

/// <summary>
/// Manages work that is done on multiple testhosts in parallel.
/// </summary>
internal sealed class ParallelOperationManager<TManager, TEventHandler, TWorkload> : IDisposable
{
    private readonly Func<TestRuntimeProviderInfo, TManager> _createNewManager;
    private readonly int _maxParallelLevel;

    /// <summary>
    /// Holds all active managers, so we can do actions on all of them, like initialize, run, cancel or close.
    /// </summary>
    // TODO: make this ConcurrentDictionary and use it's concurrent api, if we have the need.
    private readonly IDictionary<TManager, TEventHandler> _managerToEventHandlerMap = new ConcurrentDictionary<TManager, TEventHandler>();

    /// <summary>
    /// Default number of Processes
    /// </summary>
    private TEventHandler _eventHandler;
    private Func<TEventHandler, TManager, TEventHandler> _getEventHandler;
    private Action<TManager, TEventHandler, TWorkload> _runWorkload;
    private readonly List<ProviderSpecificWorkload<TWorkload>> _workloads = new();
    private readonly List<Slot> _managerSlots = new();

    private readonly object _lock = new();

    public ParallelOperationManager(Func<TestRuntimeProviderInfo, TManager> createNewManager, int parallelLevel)
    {
        _createNewManager = createNewManager;
        _maxParallelLevel = parallelLevel;
        ClearSlots();
    }

    private void ClearSlots()
    {
        lock (_lock)
        {
            _managerSlots.Clear();
            _managerSlots.AddRange(Enumerable.Range(0, _maxParallelLevel).Select(_ => new Slot()));
        }
    }

    public void StartWork(
        List<ProviderSpecificWorkload<TWorkload>> workloads!!,
        TEventHandler eventHandler!!,
        Func<TEventHandler, TManager, TEventHandler> getEventHandler!!,
        Action<TManager, TEventHandler, TWorkload> runWorkload!!)
    {
        _eventHandler = eventHandler;
        _getEventHandler = getEventHandler;
        _runWorkload = runWorkload;

        _workloads.AddRange(workloads);

        lock (_lock)
        {
            ClearSlots();
            RunWorkInParallel();
        }
    }

    private bool RunWorkInParallel()
    {
        // TODO: Right now we don't re-use shared hosts, but if we did, this is the place
        // where we should find a workload that fits the manager if any of them is shared.
        // Or tear it down, and start a new one.

        List<SlotWorkloadPair> workToRun = new();
        lock (_lock)
        {
            if (_workloads.Count == 0)
                return true;

            var availableSlots = _managerSlots.Where(slot => slot.IsAvailable).ToList();
            var availableWorkloads = _workloads.Where(workload => workload != null).ToList();
            var amount = Math.Min(availableSlots.Count, availableWorkloads.Count);
            var workloadsToRun = availableWorkloads.Take(amount).ToList();

            for (int i = 0; i < amount; i++)
            {
                var slot = availableSlots[i];
                slot.IsAvailable = false;
                var workload = availableWorkloads[i];
                workToRun.Add(new SlotWorkloadPair(slot, workload));
                _workloads.Remove(workload);
            }
        }

        foreach (var pair in workToRun)
        {
            try
            {
                var manager = _createNewManager(pair.Workload.Provider);
                var eventHandler = _getEventHandler(_eventHandler, manager);
                pair.Slot.EventHandler = eventHandler;
                pair.Slot.Manager = manager;
                pair.Slot.ManagerInfo = pair.Workload.Provider;
                pair.Slot.Work = pair.Workload.Work;

                _runWorkload(manager, eventHandler, pair.Workload.Work);
            }
            finally
            {
                // clean the slot or something, to make sure we don't keep it reserved.
            }
        }

        return false;
    }

    public bool RunNextWork(TManager completedManager)
    {
        lock (_lock)
        {
            var completedSlot = _managerSlots.Where(s => ReferenceEquals(completedManager, s.Manager)).ToList();
            if (!completedSlot.Any())
            {
                // yikes, we should have found it there
            }

            var slot = completedSlot.Single();
            slot.IsAvailable = true;

            return RunWorkInParallel();
        }
    }

    /// <summary>
    /// Remove and dispose a manager from concurrent list of manager.
    /// </summary>
    /// <param name="manager">Manager to remove</param>
    public void RemoveManager(TManager manager)
    {
        _managerToEventHandlerMap.Remove(manager);
    }

    /// <summary>
    /// Add a manager in concurrent list of manager.
    /// </summary>
    /// <param name="manager">Manager to add</param>
    /// <param name="handler">eventHandler of the manager</param>
    public void AddManager(TManager manager, TEventHandler handler)
    {
        _managerToEventHandlerMap.Add(manager, handler);
    }

    public void DoActionOnAllManagers(Action<TManager> action, bool doActionsInParallel = false)
    {
        lock (_lock)
        {
            int i = 0;
            var actionTasks = new Task[_managerToEventHandlerMap.Count];
            foreach (var manager in _managerSlots.Select(s => s.Manager))
            {
                if (manager == null)
                    continue;

                // Read the array before firing the task - beware of closures
                if (doActionsInParallel)
                {
                    actionTasks[i] = Task.Run(() => action(manager));
                    i++;
                }
                else
                {
                    DoManagerAction(() => action(manager));
                }
            }

            if (doActionsInParallel)
            {
                DoManagerAction(() => Task.WaitAll(actionTasks));
            }
        }

    }

    private void DoManagerAction(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            // Exception can occur if we are trying to cancel a test run on an executor where test run is not even fired
            // we can safely ignore that as user is just canceling the test run and we don't care about additional parallel executors
            // as we will be disposing them off soon anyway
            EqtTrace.Warning("AbstractParallelOperationManager: Exception while invoking an action on Proxy Manager instance: {0}", ex);
        }
    }

    internal void StopAllManagers()
    {
        ClearSlots();
    }

    public void Dispose()
    {
        ClearSlots();
    }

    private class Slot
    {
        public bool IsAvailable { get; set; } = true;

        public TManager? Manager { get; set; }

        public TestRuntimeProviderInfo? ManagerInfo { get; set; }

        public TEventHandler? EventHandler { get; set; }

        public TWorkload? Work { get; set; }
    }

    private class SlotWorkloadPair
    {
        public SlotWorkloadPair(Slot slot, ProviderSpecificWorkload<TWorkload> workload)
        {
            Slot = slot;
            Workload = workload;
        }
        public Slot Slot { get; }
        public ProviderSpecificWorkload<TWorkload> Workload { get; }
    }
}
