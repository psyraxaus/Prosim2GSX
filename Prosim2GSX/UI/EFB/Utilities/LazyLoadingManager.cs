using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Prosim2GSX.UI.EFB.Utilities
{
    /// <summary>
    /// Manages lazy loading of resources to improve application startup time and responsiveness.
    /// </summary>
    public class LazyLoadingManager
    {
        private static readonly LazyLoadingManager _instance = new();
        private readonly Queue<LazyLoadTask> _taskQueue = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly DispatcherTimer _timer;
        private bool _isProcessing;

        /// <summary>
        /// Gets the singleton instance of the LazyLoadingManager.
        /// </summary>
        public static LazyLoadingManager Instance => _instance;

        /// <summary>
        /// Initializes a new instance of the LazyLoadingManager class.
        /// </summary>
        private LazyLoadingManager()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += OnTimerTick;
        }

        /// <summary>
        /// Enqueues a task to be executed lazily.
        /// </summary>
        /// <param name="task">The action to execute.</param>
        /// <param name="priority">The priority of the task.</param>
        public void EnqueueTask(Action task, LazyLoadPriority priority = LazyLoadPriority.Normal)
        {
            EnqueueTask(new LazyLoadTask(task, priority));
        }

        /// <summary>
        /// Enqueues a task to be executed lazily.
        /// </summary>
        /// <param name="task">The task to enqueue.</param>
        private async void EnqueueTask(LazyLoadTask task)
        {
            await _semaphore.WaitAsync();
            try
            {
                _taskQueue.Enqueue(task);
                if (!_isProcessing)
                {
                    _isProcessing = true;
                    _timer.Start();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Handles the timer tick event to process the next task in the queue.
        /// </summary>
        private async void OnTimerTick(object sender, EventArgs e)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_taskQueue.Count == 0)
                {
                    _timer.Stop();
                    _isProcessing = false;
                    return;
                }

                var task = _taskQueue.Dequeue();
                
                // Execute the task on a background thread
                await Task.Run(() =>
                {
                    try
                    {
                        task.Action();
                    }
                    catch (Exception ex)
                    {
                        // Log the exception but don't let it crash the application
                        System.Diagnostics.Debug.WriteLine($"Error in lazy loading task: {ex.Message}");
                    }
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Clears all pending tasks from the queue.
        /// </summary>
        public async Task ClearQueueAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                _taskQueue.Clear();
                if (_isProcessing)
                {
                    _timer.Stop();
                    _isProcessing = false;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the number of pending tasks in the queue.
        /// </summary>
        public async Task<int> GetPendingTaskCountAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _taskQueue.Count;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Represents a task to be executed lazily.
        /// </summary>
        private class LazyLoadTask
        {
            /// <summary>
            /// Gets the action to execute.
            /// </summary>
            public Action Action { get; }

            /// <summary>
            /// Gets the priority of the task.
            /// </summary>
            public LazyLoadPriority Priority { get; }

            /// <summary>
            /// Initializes a new instance of the LazyLoadTask class.
            /// </summary>
            /// <param name="action">The action to execute.</param>
            /// <param name="priority">The priority of the task.</param>
            public LazyLoadTask(Action action, LazyLoadPriority priority)
            {
                Action = action;
                Priority = priority;
            }
        }
    }

    /// <summary>
    /// Defines the priority levels for lazy loading tasks.
    /// </summary>
    public enum LazyLoadPriority
    {
        /// <summary>
        /// High priority tasks are executed before normal and low priority tasks.
        /// </summary>
        High,

        /// <summary>
        /// Normal priority tasks are executed after high priority tasks but before low priority tasks.
        /// </summary>
        Normal,

        /// <summary>
        /// Low priority tasks are executed after high and normal priority tasks.
        /// </summary>
        Low
    }
}
