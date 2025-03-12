using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Prosim2GSX.UI.EFB.ViewModels
{
    /// <summary>
    /// Service for data binding between the EFB UI and the ServiceModel.
    /// </summary>
    public class EFBDataBindingService
    {
        private readonly List<BaseViewModel> _viewModels = new();
        private readonly DispatcherTimer _updateTimer = new();
        private readonly object _syncLock = new();
        private ServiceModel _serviceModel;
        private bool _isPaused;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBDataBindingService"/> class.
        /// </summary>
        public EFBDataBindingService()
        {
            // Initialize the update timer
            _updateTimer.Interval = TimeSpan.FromSeconds(1);
            _updateTimer.Tick += UpdateTimer_Tick;
        }

        /// <summary>
        /// Gets a value indicating whether updates are paused.
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Initializes the data binding service with a ServiceModel.
        /// </summary>
        /// <param name="serviceModel">The ServiceModel to bind to.</param>
        public void Initialize(ServiceModel serviceModel)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _updateTimer.Start();
        }

        /// <summary>
        /// Registers a view model with the data binding service.
        /// </summary>
        /// <param name="viewModel">The view model to register.</param>
        public void RegisterViewModel(BaseViewModel viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            lock (_syncLock)
            {
                if (!_viewModels.Contains(viewModel))
                {
                    _viewModels.Add(viewModel);
                    viewModel.Initialize();
                }
            }
        }

        /// <summary>
        /// Unregisters a view model from the data binding service.
        /// </summary>
        /// <param name="viewModel">The view model to unregister.</param>
        public void UnregisterViewModel(BaseViewModel viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            lock (_syncLock)
            {
                if (_viewModels.Contains(viewModel))
                {
                    _viewModels.Remove(viewModel);
                    viewModel.Cleanup();
                }
            }
        }

        /// <summary>
        /// Synchronizes data between the ServiceModel and the registered view models.
        /// </summary>
        public void SynchronizeData()
        {
            if (_serviceModel == null || _isPaused)
            {
                return;
            }

            lock (_syncLock)
            {
                foreach (var viewModel in _viewModels)
                {
                    try
                    {
                        // Call the view model's update method if it has one
                        if (viewModel is IUpdatableViewModel updatableViewModel)
                        {
                            updatableViewModel.Update(_serviceModel);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue updating other view models
                        System.Diagnostics.Debug.WriteLine($"Error updating view model {viewModel.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Sets the update frequency for data synchronization.
        /// </summary>
        /// <param name="interval">The update interval.</param>
        public void SetUpdateFrequency(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentException("Update interval must be greater than zero.", nameof(interval));
            }

            _updateTimer.Interval = interval;
        }

        /// <summary>
        /// Pauses data updates.
        /// </summary>
        public void PauseUpdates()
        {
            _isPaused = true;
        }

        /// <summary>
        /// Resumes data updates.
        /// </summary>
        public void ResumeUpdates()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Starts a background task to update data asynchronously.
        /// </summary>
        /// <param name="updateAction">The update action to perform.</param>
        /// <param name="interval">The update interval.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartBackgroundUpdateAsync(Func<Task> updateAction, TimeSpan interval)
        {
            if (updateAction == null)
            {
                throw new ArgumentNullException(nameof(updateAction));
            }

            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentException("Update interval must be greater than zero.", nameof(interval));
            }

            // Cancel any existing background update task
            StopBackgroundUpdate();

            // Create a new cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // Start the background update task
            await Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await updateAction();
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue updating
                        System.Diagnostics.Debug.WriteLine($"Error in background update: {ex.Message}");
                    }

                    try
                    {
                        await Task.Delay(interval, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Task was canceled, exit the loop
                        break;
                    }
                }
            }, token);
        }

        /// <summary>
        /// Stops the background update task.
        /// </summary>
        public void StopBackgroundUpdate()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        /// <summary>
        /// Cleans up resources used by the data binding service.
        /// </summary>
        public void Cleanup()
        {
            _updateTimer.Stop();
            StopBackgroundUpdate();

            lock (_syncLock)
            {
                foreach (var viewModel in _viewModels)
                {
                    viewModel.Cleanup();
                }

                _viewModels.Clear();
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            SynchronizeData();
        }
    }

    /// <summary>
    /// Interface for view models that can be updated with a ServiceModel.
    /// </summary>
    public interface IUpdatableViewModel
    {
        /// <summary>
        /// Updates the view model with data from the ServiceModel.
        /// </summary>
        /// <param name="serviceModel">The ServiceModel to update from.</param>
        void Update(ServiceModel serviceModel);
    }
}
