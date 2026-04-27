using System;

namespace Prosim2GSX.Commands
{
    // Base type for any error originating inside the CommandRegistry.
    // Controllers map subclasses to HTTP status codes:
    //   CommandNotFoundException   → 404
    //   CommandValidationException → 400
    //   anything else (incl. raw)  → 500
    public class CommandException : Exception
    {
        public CommandException(string message) : base(message) { }
        public CommandException(string message, Exception inner) : base(message, inner) { }
    }

    public class CommandNotFoundException : CommandException
    {
        public string CommandName { get; }

        public CommandNotFoundException(string name)
            : base($"Command '{name}' is not registered.")
        {
            CommandName = name;
        }
    }

    // Handlers raise this when the inbound request is malformed, references
    // missing state (e.g. profile name not in the collection), or fails a
    // domain rule that the client could plausibly fix and retry.
    public class CommandValidationException : CommandException
    {
        public CommandValidationException(string message) : base(message) { }
    }
}
