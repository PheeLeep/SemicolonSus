namespace SemicolonSus {

    /// <summary>
    /// A class contains the delegates for the events.
    /// </summary>
    internal class Delegates {

        /// <summary>
        /// Represent the method on showing the progress of the ongoing operation.
        /// </summary>
        /// <param name="current">The current number of items are finished.</param>
        /// <param name="total">The total number of items.</param>
        /// <param name="text">The string message from the ongoing operation.</param>
        internal delegate void ProgressDetailsUpdateDelegate(int current, int total, string text);
        
        /// <summary>
        /// Represents the method when the operation was succeed or cancelled.
        /// </summary>
        /// <param name="type">The type of the operation been made.</param>
        /// <param name="isCancelled">Shows if the operation is cancel.</param>
        /// <param name="ex">Shows an <see cref="System.Exception"/> if it was cancelled prematurely.</param>
        internal delegate void OperationCompletedDelegate(TrollObjectsHolder.OperationType type, bool isCancelled, System.Exception ex);
    }
}
