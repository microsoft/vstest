// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    /// <summary>
    /// Abstract class having common parallel manager implementation
    /// </summary>
    internal abstract class ParallelOperationManager<T> : IParallelOperationManager, IDisposable
    {
        #region ConcurrentManagerInstanceData

        protected Func<T> CreateNewConcurrentManager { get; set; }

        /// <summary>
        /// Gets a value indicating whether hosts are shared.
        /// </summary>
        protected bool SharedHosts { get; private set; }

        protected T[] concurrentManagerInstances;

        /// <summary>
        /// Singleton Instance of this class
        /// </summary>
        protected static T instance = default(T);

        /// <summary>
        /// Default number of Processes
        /// </summary>
        private int currentParallelLevel = 0;

        #endregion

        #region Concurrency Keeper Objects

        /// <summary>
        /// LockObject to iterate our sourceEnumerator in parallel
        /// We can use the sourceEnumerator itself as lockObject, but since its a changing object - it's risky to use it as one
        /// </summary>
        protected object sourceEnumeratorLockObject = new object();

        #endregion

        protected ParallelOperationManager(Func<T> createNewManager, int parallelLevel, bool sharedHosts)
        {
            this.CreateNewConcurrentManager = createNewManager;
            this.SharedHosts = sharedHosts;

            // Update Parallel Level
            this.UpdateParallelLevel(parallelLevel);
        }

        /// <summary>
        /// Updates the Concurrent Executors according to new parallel setting
        /// </summary>
        /// <param name="newParallelLevel">Number of Parallel Executors allowed</param>
        public void UpdateParallelLevel(int newParallelLevel)
        {
            if (this.concurrentManagerInstances == null)
            {
                // not initialized yet
                // create rest of concurrent clients other than default one
                this.concurrentManagerInstances = new T[newParallelLevel];
                for (int i = 0; i < newParallelLevel; i++)
                {
                    this.concurrentManagerInstances[i] = this.CreateNewConcurrentManager();
                }
            }
            else if (this.currentParallelLevel != newParallelLevel)
            {
                var newManagerInstances = new List<T>();

                // If number of concurrent clients is less than the new level
                // Create more concurrent clients and update the list
                if (this.currentParallelLevel < newParallelLevel)
                {
                    newManagerInstances.AddRange(this.concurrentManagerInstances);
                    for (int i = 0; i < newParallelLevel - this.currentParallelLevel; i++)
                    {
                        newManagerInstances.Add(this.CreateNewConcurrentManager());
                    }
                }
                else
                {
                    // If number of concurrent clients is more than the new level
                    // Dispose off the extra ones
                    for (int i = 0; i < newParallelLevel; i++)
                    {
                        newManagerInstances.Add(this.concurrentManagerInstances[i]);
                    }

                    for (int i = newParallelLevel; i < this.currentParallelLevel; i++)
                    {
                        this.DisposeInstance(this.concurrentManagerInstances[i]);
                    }
                }

                // Update the current concurrent executor collection
                this.concurrentManagerInstances = newManagerInstances.ToArray();
            }

            // Update current parallel setting to new one
            this.currentParallelLevel = newParallelLevel;
        }

        public void Dispose()
        {
            if (this.concurrentManagerInstances != null)
            {
                foreach (var managerInstance in this.concurrentManagerInstances)
                {
                    this.DisposeInstance(managerInstance);
                }
            }

            instance = default(T);
        }

        protected void DoActionOnAllManagers(Action<T> action, bool doActionsInParallel = false)
        {
            if (this.concurrentManagerInstances != null && this.concurrentManagerInstances.Length > 0)
            {
                var actionTasks = new Task[this.concurrentManagerInstances.Length];
                for (int i = 0; i < this.concurrentManagerInstances.Length; i++)
                {
                    // Read the array before firing the task - beware of closures
                    var client = this.concurrentManagerInstances[i];
                    if (doActionsInParallel)
                    {
                        actionTasks[i] = Task.Run(() => action(client));
                    }
                    else
                    {
                        this.DoManagerAction(() => action(client));
                    }
                }

                if (doActionsInParallel)
                {
                    this.DoManagerAction(() => Task.WaitAll(actionTasks));
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
                // we can safely ignore that as user is just cancelling the test run and we don't care about additional parallel executors
                // as we will be disposing them off soon ansyway
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("AbstractParallelOperationManager: Exception while invoking an action on Proxy Manager instance: {0}", ex);
                }
            }
        }
        
        /// <summary>
        /// Fetches the next data object for the concurrent executor to work on
        /// </summary>
        /// <param name="source">sourcedata to work on - sourcefile or testCaseList</param>
        /// <returns>True, if data exists. False otherwise</returns>
        protected bool TryFetchNextSource<Y>(IEnumerator enumerator, out Y source)
        {
            source = default(Y);
            var hasNext = false;
            lock (this.sourceEnumeratorLockObject)
            {
                if (enumerator.MoveNext())
                {
                    source = (Y)enumerator.Current;
                    hasNext = source != null;
                }
            }

            return hasNext;
        }

        #region AbstractMethods

        protected abstract void DisposeInstance(T managerInstance);

        #endregion
    }
}

