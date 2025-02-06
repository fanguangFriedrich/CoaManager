using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace mvvmTest.ViewModel.Common
{
    public class RelayCommand<T> : ICommand
    {
        private readonly WeakAction<T> _execute;

        private readonly WeakFunc<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, bool keepTargetAlive = false) : this(execute, null, keepTargetAlive) { }

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute, bool keepTargetAlive = false)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = new WeakAction<T>(execute, keepTargetAlive);

            if (canExecute != null)
            {
                _canExecute = new WeakFunc<T, bool>(canExecute, keepTargetAlive);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { if (_canExecute != null) { CommandManager.RequerySuggested += value; } }

            remove { if (_canExecute != null) { CommandManager.RequerySuggested -= value; } }
        }

        public void RaiseCanExecuteChanged() { CommandManager.InvalidateRequerySuggested(); }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            if (_canExecute.IsStatic || _canExecute.IsAlive)
            {
                if (parameter == null && typeof(T).IsValueType) { return _canExecute.Execute(default(T)); }

                if (parameter == null || parameter is T) { return (_canExecute.Execute((T)parameter)); }
            }

            return false;
        }

        public virtual void Execute(object parameter)
        {
            var val = parameter;

            if (parameter != null && parameter.GetType() != typeof(T))
            { if (parameter is IConvertible) { val = Convert.ChangeType(parameter, typeof(T), null); } }


            if (CanExecute(val) && _execute != null && (_execute.IsStatic || _execute.IsAlive))
            {
                if (val == null)
                {
                    if (typeof(T).IsValueType) { _execute.Execute(default(T)); }
                    else { _execute.Execute((T)val); }
                }
                else { _execute.Execute((T)val); }
            }
        }
    }

}
