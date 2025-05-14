using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Prosim2GSX.ViewModels.Commands
{
    /// <summary>
    /// A command that supports asynchronous operations, providing
    /// proper handling of async methods in commands
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Func<object, bool> _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Gets whether the command is currently executing
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                _isExecuting = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// Event that is raised when the ability to execute the command changes
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Creates a new async command that can always execute
        /// </summary>
        /// <param name="execute">The asynchronous execution logic</param>
        public AsyncRelayCommand(Func<object, Task> execute) : this(execute, null)
        {
        }

        /// <summary>
        /// Creates a new async command
        /// </summary>
        /// <param name="execute">The asynchronous execution logic</param>
        /// <param name="canExecute">The execution status logic</param>
        public AsyncRelayCommand(Func<object, Task> execute, Func<object, bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether this command can be executed in its current state
        /// </summary>
        /// <param name="parameter">Data used by the command. 
        /// If the command does not require data, this parameter can be set to null.</param>
        /// <returns>True if the command can be executed; otherwise, false</returns>
        public bool CanExecute(object parameter)
        {
            return !IsExecuting && (_canExecute == null || _canExecute(parameter));
        }

        /// <summary>
        /// Executes the asynchronous command on the current command target
        /// </summary>
        /// <param name="parameter">Data used by the command. 
        /// If the command does not require data, this parameter can be set to null.</param>
        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                return;

            IsExecuting = true;

            try
            {
                await _execute(parameter);
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// Method used to raise the CanExecuteChanged event to force a UI update
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
