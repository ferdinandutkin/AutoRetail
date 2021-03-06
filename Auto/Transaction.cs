using Auto.Interfaces;
using Auto.IO;
using Microsoft.Extensions.Logging;
using Shared.Logging;

namespace Auto;

internal class Transaction
{

    public delegate void CommandQueueSizeChangedHandler(int newSize);
  
    public event CommandQueueSizeChangedHandler CommandQueueSizeChanged;

    private readonly List<IRollbackCommand> _executedCommands = new();

    private readonly List<IRollbackCommand> _commands = new();

    private readonly List<IRollbackCommand> _failedCommands = new();

    private readonly ILogger _logger;

    private readonly IIOProvider _ioProvider;
    internal Transaction(IIOProvider ioProvider, ILogger? logger = null)
    {
        _logger = logger ?? Logger.Instance;
        _ioProvider = ioProvider;

    }

    private void Rollback()
    {
        _logger.LogInformation("Starting rollback");
        _ioProvider.Write("Starting rollback");

        foreach (var executedCommand in _executedCommands)
        {
            try
            {
                executedCommand.Rollback();
            }
            catch (Exception e)
            {
                _logger?.LogError($"Exception {e} was caught in {nameof(Rollback)} method");
                _ioProvider.Write($"Exception with message \"{e.Message}\" was caught while trying to rollback");
            }

        }

        foreach (var failedCommand in _failedCommands)
        {
            _commands.Remove(failedCommand);
        }
        _failedCommands.Clear();


    }
    public void Execute()
    {

        foreach (var command in _commands)
        {
            try
            {
                command.Execute();
                _executedCommands.Add(command);
            }
            catch (Exception e)
            {

                _ioProvider.Write($"Exception with message \"{e.Message}\" was caught while trying to execute all commands");

                _logger.LogError($"Exception {e} was caught in {nameof(Execute)} method");



                _failedCommands.Add(command);
                Rollback();
                return;
            }
        }
        _commands.Clear();
        _logger.LogTrace("Transaction completed successfully");
        _ioProvider.Write("Transaction completed successfully");

    }

    internal void PushCommand(IRollbackCommand command)
    {
        _commands.Add(command);
       
        CommandQueueSizeChanged?.Invoke(_commands.Count);
    }
}
