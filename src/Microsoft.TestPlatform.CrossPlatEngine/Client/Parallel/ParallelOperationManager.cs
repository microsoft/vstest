// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ObjectModel;
using ObjectModel.Engine;

/// <summary>
/// Common parallel manager functionality.
/// </summary>
internal abstract class ParallelOperationManager<TParallelManager, TEventHandler, TCreationContext> : IParallelOperationManager, IDisposable
{
    #region ConcurrentManagerInstanceData

    protected Func<TCreationContext, TParallelManager> CreateNewConcurrentManager { get; }

    /// <summary>
    /// Holds all active managers, so we can do actions on all of them, like initialize, run, cancel or close.
    /// </summary>
    // TODO: make this ConcurrentDictionary and use it's concurrent api, if we have the need.
    private readonly IDictionary<TParallelManager, TEventHandler> _concurrentManagerHandlerMap = new ConcurrentDictionary<TParallelManager, TEventHandler>();

    /// <summary>
    /// Singleton Instance of this class
    /// </summary>
    protected static TParallelManager s_instance;

    /// <summary>
    /// Default number of Processes
    /// </summary>
    private int _currentParallelLevel;

    protected int MaxParallelLevel { get; private set; }

    #endregion

    #region Concurrency Keeper Objects

    /// <summary>
    /// LockObject to iterate our sourceEnumerator in parallel
    /// We can use the sourceEnumerator itself as lockObject, but since its a changing object - it's risky to use it as one
    /// </summary>
    protected object _sourceEnumeratorLockObject = new();

    #endregion

    protected ParallelOperationManager(Func<TCreationContext, TParallelManager> createNewManager, int parallelLevel)
    {
        CreateNewConcurrentManager = createNewManager;

        // Update Parallel Level
        // REVIEW: this "pre-starts" testhosts so we have a pool of them, this is the reason the number or
        // parallel hosts is capped to the amount of sources so we don't "pre-start" too many of them
        // instead we should take each source, look if it can be run by shared host, and if so try to
        // grab a free host, run new one if we are below parallel level, or wait if we are at parallel
        // level and everyone is busy if we have non-shared host we do just the two last options, run new
        // one if current count is under parallel level, or wait till we can run new one.
        // this.UpdateParallelLevel(parallelLevel);

        MaxParallelLevel = parallelLevel;
    }

    /// <summary>
    /// Remove and dispose a manager from concurrent list of manager.
    /// </summary>
    /// <param name="manager">Manager to remove</param>
    public void RemoveManager(TParallelManager manager)
    {
        _concurrentManagerHandlerMap.Remove(manager);
    }

    /// <summary>
    /// Add a manager in concurrent list of manager.
    /// </summary>
    /// <param name="manager">Manager to add</param>
    /// <param name="handler">eventHandler of the manager</param>
    public void AddManager(TParallelManager manager, TEventHandler handler)
    {
        _concurrentManagerHandlerMap.Add(manager, handler);
    }

    /// <summary>
    /// Update event handler for the manager.
    /// If it is a new manager, add this.
    /// </summary>
    /// <param name="manager">Manager to update</param>
    /// <param name="handler">event handler to update for manager</param>
    public void UpdateHandlerForManager(TParallelManager manager, TEventHandler handler)
    {
        if (_concurrentManagerHandlerMap.ContainsKey(manager))
        {
            _concurrentManagerHandlerMap[manager] = handler;
        }
        else
        {
            AddManager(manager, handler);
        }
    }

    /// <summary>
    /// Get the event handler associated with the manager.
    /// </summary>
    /// <param name="manager">Manager</param>
    public TEventHandler GetHandlerForGivenManager(TParallelManager manager)
    {
        return _concurrentManagerHandlerMap[manager];
    }

    /// <summary>
    /// Get total number of active concurrent manager
    /// </summary>
    public int GetConcurrentManagersCount()
    {
        return _concurrentManagerHandlerMap.Count;
    }

    /// <summary>
    /// Get instances of all active concurrent manager
    /// </summary>
    public IEnumerable<TParallelManager> GetConcurrentManagerInstances()
    {
        return _concurrentManagerHandlerMap.Keys.ToList();
    }


    /// <summary>
    /// Updates the Concurrent Executors according to new parallel setting
    /// </summary>
    /// <param name="newParallelLevel">Number of Parallel Executors allowed</param>
    public void UpdateParallelLevel(int newParallelLevel)
    {
        if (_concurrentManagerHandlerMap == null)
        {
            throw new Exception("ParallelOperationManager.UpdateParallelLevel: This should not be used anymore, to pre-start hosts.");
        }
        else if (_currentParallelLevel != newParallelLevel)
        {
            // If number of concurrent clients is less than the new level
            // Create more concurrent clients and update the list
            if (_currentParallelLevel < newParallelLevel)
            {
                // This path does not even seem to be used anywhere.

                throw new Exception("ParallelOperationManager.UpdateParallelLevel: This should not be used anymore, to ensure we add more hosts.");
            }
            else
            {
                // If number of concurrent clients is more than the new level
                // Dispose off the extra ones
                int managersCount = _currentParallelLevel - newParallelLevel;

                foreach (var concurrentManager in GetConcurrentManagerInstances())
                {
                    if (managersCount == 0)
                    {
                        break;
                    }
                    else
                    {
                        RemoveManager(concurrentManager);
                        managersCount--;
                    }
                }
            }
        }

        // Update current parallel setting to new one
        _currentParallelLevel = newParallelLevel;
    }

    public void Dispose()
    {
        if (_concurrentManagerHandlerMap != null)
        {
            foreach (var managerInstance in GetConcurrentManagerInstances())
            {
                RemoveManager(managerInstance);
            }
        }

        s_instance = default;
    }

    protected void DoActionOnAllManagers(Action<TParallelManager> action, bool doActionsInParallel = false)
    {
        if (_concurrentManagerHandlerMap != null && _concurrentManagerHandlerMap.Count > 0)
        {
            int i = 0;
            var actionTasks = new Task[_concurrentManagerHandlerMap.Count];
            foreach (var client in GetConcurrentManagerInstances())
            {
                // Read the array before firing the task - beware of closures
                if (doActionsInParallel)
                {
                    actionTasks[i] = Task.Run(() => action(client));
                    i++;
                }
                else
                {
                    DoManagerAction(() => action(client));
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

    /// <summary>
    /// Fetches the next data object for the concurrent executor to work on
    /// </summary>
    /// <param name="source">source data to work on - source file or testCaseList</param>
    /// <returns>True, if data exists. False otherwise</returns>
    protected bool TryFetchNextSource<TSource>(IEnumerator enumerator, out TSource source)
    {
        // TODO: If only something like a concurrent queue existed.
        source = default;
        var hasNext = false;
        lock (_sourceEnumeratorLockObject)
        {
            if (enumerator != null && enumerator.MoveNext())
            {
                source = (TSource)enumerator.Current;
                hasNext = source != null;
            }
        }

        return hasNext;
    }
}
