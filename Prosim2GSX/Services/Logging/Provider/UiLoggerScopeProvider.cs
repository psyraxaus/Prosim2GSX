using System;
using System.Text;
using System.Threading;

namespace Prosim2GSX.Services.Logging.Provider
{
    /// <summary>
    /// Null implementation of IDisposable for use when scope is not supported
    /// </summary>
    internal sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope()
        {
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Scope provider for UI logging
    /// </summary>
    internal class UiLoggerScopeProvider
    {
        private readonly AsyncLocal<ScopeNode> _current = new AsyncLocal<ScopeNode>();
        private readonly bool _includeScopes;

        /// <summary>
        /// Initializes a new instance of the UiLoggerScopeProvider class
        /// </summary>
        public UiLoggerScopeProvider(bool includeScopes)
        {
            _includeScopes = includeScopes;
        }

        /// <summary>
        /// Pushes a new scope onto the stack
        /// </summary>
        public IDisposable Push(object state)
        {
            if (!_includeScopes)
                return NullScope.Instance;

            var parent = _current.Value;
            var node = new ScopeNode(this, parent, state);
            _current.Value = node;

            return node;
        }

        /// <summary>
        /// Gets formatted scope information
        /// </summary>
        public string GetScopeInformation()
        {
            if (!_includeScopes)
                return null;

            var current = _current.Value;
            if (current == null)
                return null;

            var builder = new StringBuilder();

            var separator = string.Empty;
            while (current != null)
            {
                if (current.State != null)
                {
                    builder.Insert(0, current.State);
                    builder.Insert(0, separator);
                    separator = " => ";
                }
                current = current.Parent;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Node in the scope tree
        /// </summary>
        private class ScopeNode : IDisposable
        {
            private readonly UiLoggerScopeProvider _provider;

            public ScopeNode Parent { get; }
            public object State { get; }

            public ScopeNode(UiLoggerScopeProvider provider, ScopeNode parent, object state)
            {
                _provider = provider;
                Parent = parent;
                State = state;
            }

            public void Dispose()
            {
                var current = _provider._current.Value;
                if (current == this)
                {
                    // This is the current node, so pop it by restoring the parent
                    _provider._current.Value = Parent;
                }
            }
        }
    }
}
