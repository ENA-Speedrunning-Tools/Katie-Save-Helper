using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KatieSaveHelper
{

    public class TrackedTaskBase
    {
        // Internal list of all tracked tasks (generic or non-generic)
        internal static readonly List<TrackedTaskBase> _activeTasks = new List<TrackedTaskBase>();
        public static IReadOnlyList<TrackedTaskBase> ActiveTasks => _activeTasks;

        public string Identifier { get; protected set; }
        public string GroupIdentifier { get; protected set; }
        public CancellationTokenSource CancellationSource { get; protected set; }
        public CancellationToken CancelToken => CancellationSource.Token;
        public Task Task { get; protected set; }

        protected TrackedTaskBase() { }

        protected void Register()
        {
            lock (_activeTasks)
                _activeTasks.Add(this);
        }

        protected void Unregister()
        {
            lock (_activeTasks)
                _activeTasks.Remove(this);
        }

        public void Cancel()
        {
            if (!CancellationSource.IsCancellationRequested)
                CancellationSource.Cancel();
        }

        public async Task Await()
        {
            try
            {
                await Task;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                KatieLogger.Error($"Error while awaiting task '{Identifier}' : {ex}");
            }
        }

        public static void CancelAll(Func<TrackedTaskBase, bool> predicate = null)
        {
            lock (_activeTasks)
            {
                foreach (var t in (predicate == null ? _activeTasks.ToList() : _activeTasks.Where(predicate).ToList()))
                    t.Cancel();
            }
        }
    }

    // Non-generic tracked task (void-returning or async Task)
    public class TrackedTask : TrackedTaskBase
    {
        private TrackedTask() { }

        public static TrackedTask Start(Func<TrackedTask, Task> task, string identifier = null, string groupIdentifier = null, CancellationTokenSource cts = null)
        {
            var tt = new TrackedTask
            {
                CancellationSource = cts ?? new CancellationTokenSource(),
                Identifier = identifier ?? task.Method.Name,
                GroupIdentifier = groupIdentifier ?? "None"
            };

            tt.Register();

            tt.Task = Task.Run(async () =>
            {
                try
                {
                    await task(tt);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    tt.Unregister();
                }
            }, tt.CancelToken);

            return tt;
        }

        public static TrackedTask Start(Func<Task> task, string identifier = null, string groupIdentifier = null, CancellationTokenSource cts = null)
            => Start(_ => task(), identifier, groupIdentifier, cts);

        public static TrackedTask FromCanceled(CancellationToken token, string identifier = null, string groupIdentifier = null)
        {
            var tt = new TrackedTask
            {
                CancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token),
                Identifier = identifier ?? "CanceledTask",
                GroupIdentifier = groupIdentifier ?? "None"
            };

            tt.CancellationSource.Cancel();
            tt.Task = Task.FromCanceled(token);

            return tt;
        }

    }

    // Generic tracked task with result
    public class TrackedTask<T> : TrackedTaskBase
    {
        public T Result { get; private set; }

        public TrackedTask() { }

        public static TrackedTask<T> Start(Func<TrackedTask<T>, Task<T>> task, string identifier = null, string groupIdentifier = null, CancellationTokenSource cts = null)
        {
            var tt = new TrackedTask<T>
            {
                CancellationSource = cts ?? new CancellationTokenSource(),
                Identifier = identifier ?? task.Method.Name,
                GroupIdentifier = groupIdentifier ?? "None"
            };

            tt.Register();

            tt.Task = Task.Run(async () =>
            {
                try
                {
                    tt.Result = await task(tt);
                    return tt.Result;
                }
                catch (OperationCanceledException)
                {
                    return default;
                }
                finally
                {
                    tt.Unregister();
                }
            }, tt.CancelToken);

            return tt;
        }

        public static TrackedTask<T> Start(Func<TrackedTask<T>, T> task, string identifier = null, string groupIdentifier = null, CancellationTokenSource cts = null)
        {
            var tt = new TrackedTask<T>
            {
                CancellationSource = cts ?? new CancellationTokenSource(),
                Identifier = identifier ?? task.Method.Name,
                GroupIdentifier = groupIdentifier ?? "None"
            };

            tt.Register();

            tt.Task = Task.Run(() =>
            {
                try
                {
                    tt.Result = task(tt);
                    return tt.Result;
                }
                catch (OperationCanceledException)
                {
                    return default;
                }
                finally
                {
                    tt.Unregister();
                }
            }, tt.CancelToken);

            return tt;
        }

        public static TrackedTask<T> Start(Func<Task<T>> task, string identifier = null, string groupIdentifier = null, CancellationTokenSource cts = null)
        {
            var tt = new TrackedTask<T>
            {
                CancellationSource = cts ?? new CancellationTokenSource(),
                Identifier = identifier ?? task.Method.Name,
                GroupIdentifier = groupIdentifier ?? "None"
            };

            tt.Register();

            tt.Task = Task.Run(async () =>
            {
                try
                {
                    tt.Result = await task();
                    return tt.Result;
                }
                catch (OperationCanceledException)
                {
                    return default;
                }
                finally
                {
                    tt.Unregister();
                }
            }, tt.CancelToken);

            return tt;
        }

        public static TrackedTask<T> Start(Func<T> task, string identifier = null, string groupIdentifier = null, CancellationTokenSource cts = null)
        {
            var tt = new TrackedTask<T>
            {
                CancellationSource = cts ?? new CancellationTokenSource(),
                Identifier = identifier ?? task.Method.Name,
                GroupIdentifier = groupIdentifier ?? "None"
            };

            tt.Register();

            tt.Task = Task.Run(() =>
            {
                try
                {
                    tt.Result = task();
                    return tt.Result;
                }
                catch (OperationCanceledException)
                {
                    return default;
                }
                finally
                {
                    tt.Unregister();
                }
            }, tt.CancelToken);

            return tt;
        }

        public static TrackedTask<T> FromCanceled(CancellationToken token, string identifier = null, string groupIdentifier = null)
        {
            var tt = new TrackedTask<T>
            {
                CancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token),
                Identifier = identifier ?? "CanceledTask",
                GroupIdentifier = groupIdentifier ?? "None"
            };

            tt.CancellationSource.Cancel();
            tt.Task = Task.FromCanceled<T>(token);

            return tt;
        }
    }

}
