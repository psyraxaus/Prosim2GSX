using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Prosim2GSX.ViewModels.Commands
{
    /// <summary>
    /// A command that asynchronously delegates execution to another method
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Predicate<object> _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Creates a new instance of AsyncRelayCommand
        /// </summary>
        /// <param name="execute">The method to execute when the command is invoked</param>
        /// <param name="canExecute">The method to determine if the command can be executed</param>
        public AsyncRelayCommand(Func<object, Task> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Event raised when the can execute state changes
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines if the command can be executed
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute(parameter));
        }

        /// <summary>
        /// Executes the command asynchronously
        /// </summary>
        public async void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    _isExecuting = true;
                    RaiseCanExecuteChanged();
                    await _execute(parameter);
                }
                finally
                {
                    _isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Raises the CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
