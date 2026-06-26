using JoelG.ENA4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

namespace KatieSaveHelper.Features.Util
{
    // Wrapper for the vanilla "Static Coroutine" system that keeps track of the routines that are active so that they can be cancelled later if needed
    public class StaticCoroutine
    {
        private static readonly FieldInfo instanceField = AccessTools.Field(typeof(StaticCoroutineRunner), "instance");
        private static readonly MethodInfo ensureInstanceMethod = AccessTools.Method(typeof(StaticCoroutineRunner), "EnsureInstance");
        private static StaticCoroutineRunner Instance
        {
            get
            {
                try
                {
                    ensureInstanceMethod?.Invoke(null, null);
                    return (StaticCoroutineRunner)instanceField.GetValue(null);
                }
                catch (Exception e)
                {
                    KatieLogger.Error($"StaticCoroutine.Instance failed: {e}");
                    return null;
                }
            }
        }
        private static readonly List<StaticCoroutine> _activeRoutines = new List<StaticCoroutine>();
        public static IReadOnlyList<StaticCoroutine> ActiveRoutines => _activeRoutines;
        public Coroutine Routine { get; private set; }
        public IEnumerator Enumerator { get; private set; }
        public bool FastForwardRequested { get; private set; } = false;
        public bool Finished { get; private set; } = false;
        private string _identifier = null;
        private string _groupIdentifier = null;
        public string Identifier => _identifier ?? Enumerator.GetType().Name;
        public string GroupIdentifier => _groupIdentifier ?? "None";
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private TaskCompletionSource<object> _tcs;
        private TaskCompletionSource<object> CompletionTcs
        {
            get
            {
                if (_tcs == null)
                    _tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                return _tcs;
            }
        }
        public CancellationToken CancelToken => _cts.Token;

        private StaticCoroutine(string identifier, string groupIdentifier = null)
        {
            _identifier = identifier;
            _groupIdentifier = groupIdentifier;
        }

        // Start a static coroutine with the desired enumerator method
        public static StaticCoroutine Start(IEnumerator enumerator, string identifier = null, string groupIdentifier = null)
        {
            var sc = new StaticCoroutine(identifier, groupIdentifier);

            sc.Enumerator = enumerator;
            sc.Routine = Instance.StartCoroutine(sc.Run());
            return sc;
        }

        // Start a static coroutine with the desired enumerator method, inserting the new static coroutine instance as one of it's arguments
        public static StaticCoroutine Start(Func<StaticCoroutine, IEnumerator> enumeratorFactory, string identifier = null, string groupIdentifier = null)
        {
            var sc = new StaticCoroutine(identifier, groupIdentifier);
            
            IEnumerator enumerator = enumeratorFactory(sc);
            sc.Enumerator = enumerator;
            try
            {
                sc.Routine = Instance.StartCoroutine(sc.Run());
            }
            catch (Exception ex)
            {
                sc.Routine = null;
                KatieLogger.Error($"Error while starting Static Coroutine: {ex}");
            }
            return sc;
        }

        private IEnumerator Run()
        {
            if (_activeRoutines.Contains(this)) yield break;

            _activeRoutines.Add(this);
            Finished = false;

            if (KatieConfig.Settings.debugMode.Value) KatieLogger.Info($"Started routine '{Identifier}'");
            try
            {
                while (!FastForwardRequested && Enumerator.MoveNext())
                    yield return Enumerator.Current;

                // If fast forward is requested, continue to move the enumerator until it's done, without returning any yields
                while (FastForwardRequested && Enumerator.MoveNext()) { }
            }
            finally
            {
                End();
                if (KatieConfig.Settings.debugMode.Value) KatieLogger.Info($"Finished routine '{Identifier}'");
            }
        }

        private void End(bool cancelled = false)
        {
            if (Finished) return;

            _activeRoutines.Remove(this);
            Finished = true;
            if (cancelled)
                CompletionTcs.TrySetCanceled();
            else
                CompletionTcs.TrySetResult(null);
        }

        // Check if any static coroutines are currently active, that optionally match a condition
        public static bool AnyActive(Func<StaticCoroutine, bool> predicate = null)
        {
            if (_activeRoutines.Count == 0)
                return false;

            if (predicate == null)
                return _activeRoutines.Count > 0;

            return _activeRoutines.Any(predicate);
        }

        public static Task Await(StaticCoroutine routine, CancellationToken token = default)
        {
            if (routine == null)
                return Task.CompletedTask;

            if (routine.Finished)
                return Task.CompletedTask;

            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);

            if (token.CanBeCanceled)
            {
                var registration = token.Register(() =>
                {
                    routine.RequestCancel();
                    routine.CompletionTcs.TrySetCanceled(token);
                });

                var t = routine.CompletionTcs.Task;
                return t.ContinueWith(_ =>
                {
                    registration.Dispose();
                    return t; // preserve original completion
                }, TaskScheduler.Default).Unwrap();
            }

            return routine.CompletionTcs.Task;
        }

        // Immediately stop the coroutine instance
        public void Stop()
        {
            StaticCoroutineRunner instance = (StaticCoroutineRunner)instanceField.GetValue(null);
            if (instance != null && Routine != null)
            {
                instance.StopCoroutine(Routine);
                End(cancelled: true);
            }
        }

        // Request to fast forward the coroutine when it reaches it's next yield
        public void RequestFastForward()
        {
            FastForwardRequested = true;
        }

        // Let the coroutine know that it's cancellation has been requested
        public void RequestCancel()
        {
            _cts.Cancel();
        }

        // Stop all active coroutines, that optionally match a condition
        public static void StopAll(Func<StaticCoroutine, bool> predicate = null)
        {
            if (predicate == null)
            {
                foreach (var routine in _activeRoutines.ToList())
                {
                    routine.Stop();
                }
                return;
            }

            foreach (var routine in _activeRoutines.ToList())
            {
                if (predicate(routine))
                {
                    routine.Stop();
                }
            }
        }

        // Request to fast forward all active coroutines, that optionally match a condition
        public static void RequestFastForwardAll(Func<StaticCoroutine, bool> predicate = null)
        {
            if (predicate == null)
            {
                foreach (var routine in _activeRoutines.ToList())
                {
                    routine.RequestFastForward();
                }
                return;
            }

            foreach (var routine in _activeRoutines.ToList())
            {
                if (predicate(routine))
                {
                    routine.RequestFastForward();
                }
            }
        }

        // Request to cancel all active coroutines, that optionally match a condition
        public static void RequestCancelAll(Func<StaticCoroutine, bool> predicate = null)
        {
            if (predicate == null)
            {
                if (KatieConfig.Settings.debugMode.Value)
                    KatieLogger.Info($"Requesting to cancel all active routines");

                foreach (var routine in _activeRoutines.ToList())
                {
                    routine.RequestCancel();
                }
                return;
            }

            foreach (var routine in _activeRoutines.ToList())
            {
                if (predicate(routine))
                {
                    if (KatieConfig.Settings.debugMode.Value)
                        KatieLogger.Info($"Requesting to cancel routine '{routine.Identifier}'");
                    routine.RequestCancel();
                }
            }
        }

        public static bool AnyDupesActive(StaticCoroutine routine) => 
            AnyActive(sc => sc.Identifier == routine.Identifier && sc != routine);

        public static bool AnyGroupDupesActive(StaticCoroutine routine) =>
            AnyActive(sc => sc.GroupIdentifier == routine.GroupIdentifier && sc != routine);

        public static void RequestCancelDupes(StaticCoroutine routine) =>
            RequestCancelAll(sc => sc.Identifier == routine.Identifier && sc != routine);

        public static void RequestCancelGroupDupes(StaticCoroutine routine) =>
            RequestCancelAll(sc => sc.GroupIdentifier == routine.GroupIdentifier && sc != routine);

        public static void RequestFastForwardDupes(StaticCoroutine routine) =>
            RequestFastForwardAll(sc => sc.Identifier == routine.Identifier && sc != routine);

        public static void RequestFastForwardGroupDupes(StaticCoroutine routine) =>
            RequestFastForwardAll(sc => sc.GroupIdentifier == routine.GroupIdentifier && sc != routine);

        public static IEnumerator WaitForCancelAll(Func<StaticCoroutine, bool> predicate = null, CancellationToken token = default)
        {
            RequestCancelAll(predicate);
            while (AnyActive(predicate))
            {
                yield return null;
                if (token.IsCancellationRequested) yield break;
            }
        }

        public static IEnumerator WaitForCancelDupes(StaticCoroutine scWrapper, CancellationToken token = default)
        {
            RequestCancelDupes(scWrapper);
            while (AnyDupesActive(scWrapper))
            {
                yield return null;
                if (token.IsCancellationRequested) yield break;
            }
        }

        public static IEnumerator WaitForCancelGroupDupes(StaticCoroutine scWrapper, CancellationToken token = default)
        {
            RequestCancelGroupDupes(scWrapper);
            while (AnyGroupDupesActive(scWrapper))
            {
                yield return null;
                if (token.IsCancellationRequested) yield break;
            }
        }

        public static IEnumerator WaitForFastForwardAll(Func<StaticCoroutine, bool> predicate = null, CancellationToken token = default)
        {
            RequestFastForwardAll(predicate);
            while (AnyActive(predicate))
            {
                yield return null;
                if (token.IsCancellationRequested) yield break;
            }
        }

        public static IEnumerator WaitForFastForwardDupes(StaticCoroutine scWrapper, CancellationToken token = default)
        {
            RequestFastForwardDupes(scWrapper);
            while (AnyDupesActive(scWrapper))
            {
                yield return null;
                if (token.IsCancellationRequested) yield break;
            }
        }

        public static IEnumerator WaitForFastForwardGroupDupes(StaticCoroutine scWrapper, CancellationToken token = default)
        {
            RequestFastForwardGroupDupes(scWrapper);
            while (AnyGroupDupesActive(scWrapper))
            {
                yield return null;
                if (token.IsCancellationRequested) yield break;
            }
        }
    }
}
