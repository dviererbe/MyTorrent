using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UserClient
{
  public class MyCommand : ICommand
  {
	public event EventHandler CanExecuteChanged
	{
	  add { CommandManager.RequerySuggested += value; }
	  remove { CommandManager.RequerySuggested -= value; }
	}

	private readonly Predicate<object> _canExecute = null;
	private readonly Action<object> _execute = null;

	public MyCommand(Action<object> execute, Predicate<object> canExecute = null)
	{
	  this._execute = execute;
	  this._canExecute = canExecute;
	}

	public bool CanExecute(object parameter)
	{
	  return this._canExecute?.Invoke(parameter) != false;
	}
	public void Execute(object parameter)
	{
	  this._execute?.Invoke(parameter);
	}
  }

  public class NetworkInfo
  {
	public long FragmentSize { get; set; }
	public string HashAlgorithm { get; set; }
	public HashSet<string> TorrentList { get; set; }
  }
}
