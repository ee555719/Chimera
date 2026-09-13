// SPDX-License-Identifier: AGPL-3.0-or-later
using Chimera.Abstractions.Interfaces;

namespace HelloWorld;

public class Plugin : IStartupTask, IMenuContribution
{
    public string Id => "com.example.hello";
    public string Header => "Hello World";
    public string? Icon => null;
    public int Order => 100;
    public System.Windows.Input.ICommand Command { get; }

    public Plugin()
    {
        Command = new RelayCommand(Execute);
    }

    public Task ExecuteAsync(IPluginContext context)
    {
        context.Logger.Info("HelloWorld plugin loaded!");
        return Task.CompletedTask;
    }

    private void Execute()
    {
        System.Windows.MessageBox.Show("Hello from Chimera plugin!", "Hello World", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}

public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
