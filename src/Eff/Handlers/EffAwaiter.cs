﻿using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Nessos.Effects.Handlers
{
    /// <summary>
    ///   Base awaiter class for Eff awaitables.
    /// </summary>
    public abstract class EffAwaiter : ICriticalNotifyCompletion
    {
        internal EffAwaiter() { }

        /// <summary>
        ///   Awaiter identifier for debugging purposes.
        /// </summary>
        public abstract string Id { get; }

        public string CallerMemberName { get; set; } = "";
        public string CallerFilePath { get; set; } = "";
        public int CallerLineNumber { get; set; } = 0;

        /// <summary>
        ///   Returns true if the awaiter has been completed with a result value.
        /// </summary>
        public bool HasResult { get; internal set; }

        /// <summary>
        ///   Gets the exception result for the awaiter.
        /// </summary>
        public Exception? Exception { get; internal set; }

        /// <summary>
        ///   Gets a state machine awaiting on the current awaiter instance.
        /// </summary>
        [DisallowNull]
        public IEffStateMachine? StateMachine { get; internal set; }

        /// <summary>
        ///   Returns true if the awaiter has been completed with an exception value.
        /// </summary>
        public bool HasException => !(Exception is null);

        /// <summary>
        ///   Returns true if the awaiter has been completed with either a result or an exception.
        /// </summary>
        public bool IsCompleted => HasResult || HasException;

        /// <summary>
        ///   Sets an exception value for the awaiter.
        /// </summary>
        public abstract void SetException(Exception exception);

        /// <summary>
        ///   Processes the awaiter using the provided effect handler.
        /// </summary>
        public abstract Task Accept(IEffectHandler handler);

        /// <summary>
        ///   Clears any results from the awaiter instance.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        ///   Configures the EffAwaiter instance with supplied parameters.
        /// </summary>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        /// <returns>An EffAwaiter instance with callsite metadata.</returns>
        public EffAwaiter ConfigureAwait(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            CallerMemberName = callerMemberName;
            CallerFilePath = callerFilePath;
            CallerLineNumber = callerLineNumber;
            return this;
        }

        /// <summary>
        ///   For use by EffMethodBuilder
        /// </summary>
        public EffAwaiter GetAwaiter() => this;

        /// <summary>
        ///   For use by EffMethodBuilder
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult()
        {
            if (!(Exception is null))
            {
                ExceptionDispatchInfo.Capture(Exception).Throw();
                return;
            }

            if (!HasResult)
            {
                throw new InvalidOperationException($"Awaiter of type {Id} has not been completed.");
            }
        }

        void INotifyCompletion.OnCompleted(Action continuation) => throw new NotSupportedException("Eff awaitables should only be awaited in Eff methods.");
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) => throw new NotSupportedException("Eff awaitables should only be awaited in Eff methods.");
    }

    /// <summary>
    ///   Base awaiter class for Eff awaitables.
    /// </summary>
    public abstract class EffAwaiter<TResult> : EffAwaiter
    {
        [AllowNull]
        private TResult _result = default;

        /// <summary>
        ///   Gets either the result value or throws the exception that have been stored in the awaiter.
        /// </summary>
        public TResult Result => GetResult();

        /// <summary>
        ///   Sets a result value for the awaiter.
        /// </summary>
        public void SetResult(TResult value)
        {
            Exception = null;
            _result = value;
            HasResult = true;
        }

        public sealed override void SetException(Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            HasResult = false;
            _result = default;
            Exception = exception;
        }

        /// <summary>
        ///   For use by EffMethodBuilder
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new TResult GetResult()
        {
            if (!(Exception is null))
            {
                ExceptionDispatchInfo.Capture(Exception).Throw();
                return default!;
            }

            if (!HasResult)
            {
                throw new InvalidOperationException($"Awaiter of type {Id} has not been completed.");
            }

            return _result;
        }

        /// <summary>
        ///   For use by EffMethodBuilder
        /// </summary>
        public new EffAwaiter<TResult> GetAwaiter() => this;

        /// <summary>
        ///   Configures the EffAwaiter instance with supplied parameters.
        /// </summary>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        /// <returns>An EffAwaiter instance with callsite metadata.</returns>
        public new EffAwaiter<TResult> ConfigureAwait(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            CallerMemberName = callerMemberName;
            CallerFilePath = callerFilePath;
            CallerLineNumber = callerLineNumber;
            return this;
        }

        public override void Clear()
        {
            _result = default;
            HasResult = false;
            Exception = null;
        }
    }

    /// <summary>
    ///   Awaiter for abstract Effects.
    /// </summary>
    public class EffectAwaiter<TResult> : EffAwaiter<TResult>
    {
        public EffectAwaiter(Effect<TResult> effect)
        {
            Effect = effect;
        }

        public Effect<TResult> Effect { get; }

        public override string Id => Effect.GetType().Name;
        public override Task Accept(IEffectHandler handler) => handler.Handle(this);
    }

    /// <summary>
    ///   Awaiter adapter for TPL tasks.
    /// </summary>
    public class TaskAwaiter<TResult> : EffAwaiter<TResult>
    {
        public TaskAwaiter(ValueTask<TResult> task)
        {
            Task = task;
        }

        public ValueTask<TResult> Task { get; }

        public override string Id => nameof(TaskAwaiter);
        public override Task Accept(IEffectHandler handler) => handler.Handle(this);
    }
}