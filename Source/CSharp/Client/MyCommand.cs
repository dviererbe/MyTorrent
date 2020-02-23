using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UserClient
{
  /// <summary>
  /// Implementation of ICommand as generic class with delegates
  /// </summary>
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

  /// <summary>
  /// Information class which holds the parameter for a successful communication with the network
  /// </summary>
  public class NetworkInfo
  {
	public long FragmentSize { get; set; }
	public string HashAlgorithm { get; set; }
	public HashSet<string> TorrentList { get; set; }
  }

  /// <summary>
  /// Class which holds the information of one file that was uploaded to the network
  /// </summary>
  public class SavedFileInfo
  {
	public long FileSize { get; set; }
	public string FileHash { get; set; }
	public string FileName { get; set; }
	public DateTime CreateTime { get; set; }
  }

  /// <summary>
  /// Extension to <see cref="SavedFileInfo"/> to show the data in the data grid and apply choosen actions to the file.
  /// </summary>
  public class FileTableItem : SavedFileInfo
  {
	public bool Download { get; set; }
	public bool DeleteFromNetwork { get; set; }
  }
}
