// GoalFlow.Application.Common.Result<T>
// --------------------------------------
//
// Purpose:
// Provides a functional-style result wrapper for returning outcomes from
// application use cases and domain operations. This pattern ensures that
// methods communicate success or failure explicitly, avoiding exceptions
// for expected business conditions.
//
// Key Notes:
// - `Success(T)` returns a successful result containing a value.
// - `Failure(string, string)` returns a failed result with an error code
//   and human-readable message.
// - `IsSuccess` indicates whether the operation succeeded.
// - The `Error` record provides structured error information for
//   consistent handling in higher layers (API, UI).
//
// This design helps enforce explicit error handling across the
// Application layer and supports predictable workflows.

using System;

namespace GoalFlow.Application.Common
{
    /// <summary>
    /// Represents an error with a machine-readable code and
    /// a human-readable message.
    /// </summary>
    public record Error(string Code, string Message);

    /// <summary>
    /// Represents the outcome of an operation. Wraps either a
    /// successful value or a failure with an associated error.
    /// </summary>
    /// <typeparam name="T">The type of the successful value.</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// True if the operation was successful; otherwise false.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// The value of the operation if successful; null otherwise.
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// The error details if the operation failed; null otherwise.
        /// </summary>
        public Error? Error { get; }

        private Result(bool ok, T? value, Error? error)
        {
            IsSuccess = ok;
            Value = value;
            Error = error;
        }

        /// <summary>
        /// Creates a successful result containing the provided value.
        /// </summary>
        public static Result<T> Success(T value) => new(true, value, null);

        /// <summary>
        /// Creates a failed result with an error code and message.
        /// </summary>
        public static Result<T> Failure(string code, string message) =>
            new(false, default, new Error(code, message));
    }
}
