using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Spelling;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Controls.Utils
{
    internal class SpellChecker : IDisposable
    {
        private readonly ISpellChecker _spellChecker;
        private DelayedAction<SpellChecker> _delayedSpellCheckAction;
        private string? _text;

        public Action<SpellCheckError[]>? SpellCheckCompleted { get; set; }

        public SpellChecker(ISpellChecker spellChecker)
        {
            _spellChecker = spellChecker;
            _delayedSpellCheckAction = new(100, 500, SpellCheck, this);
        }

        public void SpellCheck(string text)
        {
            _text = text;
            _delayedSpellCheckAction.Invoke();
        }

        private static void SpellCheck(SpellChecker spellChecker)
        {
            if (string.IsNullOrWhiteSpace(spellChecker._text))
                return;

            Dispatcher.UIThread.AddTimer(new DispatcherTimer());
            var errors = spellChecker._spellChecker.SpellCheck(spellChecker._text); 
            spellChecker.SpellCheckCompleted?.Invoke(errors);

        }
        public void Dispose()
        {
            _delayedSpellCheckAction.Dispose();
        }

        /// <summary>
        /// Class to aid in the culling of events within a specified amount of time with a maximum delay.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        class DelayedAction<TState> : IDisposable
        {
            private readonly int _cullingInterval;
            private readonly int _maxCullingDelay;
            private readonly Action<TState> _action;
            private readonly Timer _timer;
            private DateTime? _startTime;
            private readonly SemaphoreSlim _semaphore = new(1, 1);
            private readonly TState _state;

            private int _invokeQueued = 0;
            public bool InvokeQueued => _invokeQueued == 1;

            /// <summary>
            /// Creates a DelayedActionArgument class.
            /// </summary>
            /// <param name="cullingInterval">The interval calls can be made in and override the last call passed arguments</param>
            /// <param name="maxCullingDelay">Maximum delay that calls will be culled.</param>
            /// <param name="action">Action to be invoked.</param>
            public DelayedAction(int cullingInterval, int maxCullingDelay, Action<TState> action, TState state)
            {
                _cullingInterval = cullingInterval;
                _maxCullingDelay = maxCullingDelay;
                _action = action ?? throw new ArgumentNullException(nameof(action));
                _state = state;
                Dispatcher.UIThread.AddTimer();
                _timer = new Timer(TimerCallback, this, Timeout.Infinite, Timeout.Infinite);
            }

            private static void TimerCallback(object state)
            {
                var delayedAction = Unsafe.As<DelayedAction<TState>>(state);
                Interlocked.Exchange(ref delayedAction._invokeQueued, 0);
                delayedAction._startTime = null;
                delayedAction._action.Invoke(delayedAction._state);
            }

            /// <summary>
            /// Called to invoke the action with the specified parameters unless this invoke is culled.
            /// </summary>
            public void Invoke()
            {
                _semaphore.Wait();
                try
                {
                    // Only set the arguments if they 
                    Interlocked.Exchange(ref _invokeQueued, 1);
                    
                    // Check if we have exceeded the maximum delay time.
                    if (_maxCullingDelay == 0
                        || (DateTime.UtcNow - (_startTime ??= DateTime.UtcNow)).TotalMilliseconds >= _maxCullingDelay)
                    {
                        // Stop the timer.
                        _timer.Change(0, Timeout.Infinite);
                        return;
                    }

                    _timer.Change(_cullingInterval, Timeout.Infinite);
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            public void Dispose()
            {
                _timer.Dispose();
                _semaphore.Dispose();
            }
        }
    }
}
