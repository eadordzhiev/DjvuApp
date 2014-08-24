using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DjvuApp.Misc
{
    public class TasksQueue
    {
        private class TaskToken
        {
            public CancellationToken CancellationToken { get; private set; }

            public int Priority { get; private set; }

            private readonly Func<Task> _function;

            public TaskToken(Func<Task> function, int priority, CancellationToken cancellationToken)
            {
                _function = function;
                Priority = priority;
                CancellationToken = cancellationToken;
            }

            public async Task ExecuteAsync()
            {
                await _function();
            }
        }

        private readonly List<TaskToken> _tasks = new List<TaskToken>();
        private bool _isRunning = false;

        private async Task LoopAsync()
        {
            _isRunning = true;

            while (true)
            {
                TaskToken task;
                lock (_tasks)
                {
                    _tasks.RemoveAll(item => item.CancellationToken.IsCancellationRequested);

                    if (_tasks.Count == 0)
                    {
                        _isRunning = false;
                        return;
                    }

                    var maxPriority = _tasks.Max(item => item.Priority);
                    task = _tasks.First(item => item.Priority == maxPriority);
                }

                await task.ExecuteAsync();

                lock (_tasks)
                {
                    _tasks.Remove(task);
                }
            }
        }

        public async void EnqueueToCurrentThreadAsync([NotNull] Func<Task> function, int priority, CancellationToken cancellationToken)
        {
            var token = new TaskToken(function, priority, cancellationToken);

            lock (_tasks)
            {
                _tasks.Add(token);
            }

            if (!_isRunning)
            {
                await LoopAsync();
            }
        }
    }
}