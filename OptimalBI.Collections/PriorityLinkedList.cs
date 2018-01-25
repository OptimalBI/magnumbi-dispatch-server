using System;
using System.Collections;
using System.Collections.Generic;

namespace OptimalBI.Collections {
    /// <summary>
    ///     A priority queue which uses a linked list as the internal data structure.
    ///     An item x is of higher priority than y if x goes before y in their sorting order.
    ///     In other words, if CompareTo(x,y) returns -1, x will be considered to be of higher priority than y.
    /// </summary>
    /// <typeparam name="T">Type of items to store in this priority queue.</typeparam>
    public class PriorityLinkedList<T> : IEnumerable<T> where T : IComparable<T> {
        private PriorityLinkedListNode<T> head;
        public int Count { get; set; }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() {
            PriorityLinkedListNode<T> node = this.head;
            while (node != null) {
                yield return node.Value;
                node = node.Right;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        /// <summary>
        ///     Add item to ordered position in list.
        /// </summary>
        /// <param name="item">Item to add. May not be null.</param>
        public void Enqueue(T item) {
            if (item == null) {
                throw new NullReferenceException("Item cannot be null.");
            }
            // add item to front
            if (this.head == null) {
                this.head = new PriorityLinkedListNode<T>(item);
                this.Count++;
                return;
            }
            PriorityLinkedListNode<T> next = this.head;
            PriorityLinkedListNode<T> prev = null;
            while (next != null) {
                if ((prev == null || item.CompareTo(prev.Value) > 0) && item.CompareTo(next.Value) <= 0) {
                    // add item between prev and next
                    PriorityLinkedListNode<T> node = new PriorityLinkedListNode<T>(item, prev, next);
                    // reassign other node values
                    if (prev != null) {
                        prev.Right = node;
                    }
                    next.Left = node;
                    if (next == this.head) {
                        this.head = node;
                    }
                    this.Count++;
                    return;
                }
                prev = next;
                next = next.Right;
            }
            // add to end of queue
            if (prev != null) {
                prev.Right = new PriorityLinkedListNode<T>(item, prev, null);
                this.Count++;
            }
        }

        /// <summary>
        ///     Remove and return item at front of queue.
        /// </summary>
        /// <returns>Item at front of queue</returns>
        public T Dequeue() {
            if (this.head == null) {
                return default(T);
            }
            PriorityLinkedListNode<T> head = this.head;
            this.head = head.Right;
            if (this.head != null) {
                this.head.Left = null;
            }
            this.Count--;
            return head.Value;
        }

        /// <summary>
        ///     Returns the item at the front of the queue without removing it.
        /// </summary>
        /// <returns>Item at front of queue</returns>
        public T Peek() {
            if (this.head == null) {
                return default(T);
            }
            return this.head.Value;
        }

        /// <summary>
        ///     Determines whether the given item is contained in the queue.
        /// </summary>
        /// <param name="item">Item to check for</param>
        /// <returns>True iff there exists an item in the queue with reference equality to item</returns>
        public bool Contains(T item) {
            PriorityLinkedListNode<T> next = this.head;
            while (next != null) {
                if (ReferenceEquals(next.Value, item)) {
                    return true;
                }
                next = next.Right;
            }
            return false;
        }

        /// <summary>
        ///     Removes the first (highest priority) instance of a given item from the queue.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True iff the item was found and removed</returns>
        public bool Remove(T item) {
            PriorityLinkedListNode<T> next = this.head;
            // Empty queue
            if (next == null) {
                return false;
            }
            // Item is at front
            if (ReferenceEquals(item, this.head.Value)) {
                this.head = this.head.Right;
                if (this.head != null) {
                    this.head.Left = null;
                }
                this.Count--;
                return true;
            }
            // Item is not at front
            while (next != null) {
                if (ReferenceEquals(next.Value, item)) {
                    PriorityLinkedListNode<T> left = next.Left;
                    PriorityLinkedListNode<T> right = next.Right;
                    if (left != null) {
                        left.Right = right;
                    }
                    if (right != null) {
                        right.Left = left;
                    }
                    this.Count--;
                    return true;
                }
                next = next.Right;
            }
            return false;
        }

        /// <summary>
        ///     Empties the queue.
        /// </summary>
        public void Clear() {
            this.head = null;
            this.Count = 0;
        }
    }
}