using System;

namespace OptimalBI.Collections {
    /// <summary>
    ///     Node data class for PriorityLinkedList.
    /// </summary>
    /// <typeparam name="T">Type of item to store in this node.</typeparam>
    internal class PriorityLinkedListNode<T> where T : IComparable<T> {
        internal PriorityLinkedListNode<T> Left;
        internal PriorityLinkedListNode<T> Right;
        internal T Value;

        /// <inheritdoc />
        public PriorityLinkedListNode(T value,
            PriorityLinkedListNode<T> left = null,
            PriorityLinkedListNode<T> right = null) {
            this.Value = value;
            this.Left = left;
            this.Right = right;
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"{this.Value}; Left: {this.Left}; Right: {this.Right}";
        }
    }
}