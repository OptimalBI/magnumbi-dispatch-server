using System;
using MagnumBI.Dispatch.Web.Models;
using Xunit;

namespace OptimalBI.Collections.Tests {
    public class PriorityQueueTests : IDisposable {
        public void Dispose() {
        }

        private readonly JobTimeoutModel j1 =
            new JobTimeoutModel("123", DateTime.Now + TimeSpan.FromMinutes(5), new JobRequest());

        private readonly JobTimeoutModel j2 =
            new JobTimeoutModel("456", DateTime.Now + TimeSpan.FromMinutes(10), new JobRequest());

        private readonly JobTimeoutModel j3 =
            new JobTimeoutModel("789", DateTime.Now + TimeSpan.FromMinutes(15), new JobRequest());

        private PriorityLinkedList<JobTimeoutModel> CreateSimpleQueue() {
            PriorityLinkedList<JobTimeoutModel> pq = new PriorityLinkedList<JobTimeoutModel>();
            pq.Enqueue(this.j3);
            pq.Enqueue(this.j2);
            pq.Enqueue(this.j1);
            return pq;
        }

        [Fact]
        public void TestClear1() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            pq.Clear();
            Assert.Equal(0, pq.Count);
            Assert.False(pq.Contains(this.j1));
            Assert.False(pq.Contains(this.j2));
            Assert.False(pq.Contains(this.j3));
        }

        [Fact]
        public void TestClear2() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            pq.Dequeue();
            pq.Dequeue();
            pq.Dequeue();
            Assert.Equal(0, pq.Count);
            Assert.False(pq.Contains(this.j1));
            Assert.False(pq.Contains(this.j2));
            Assert.False(pq.Contains(this.j3));
        }

        [Fact]
        public void TestClear3() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            pq.Remove(this.j1);
            pq.Remove(this.j2);
            pq.Remove(this.j3);
            Assert.Equal(0, pq.Count);
            Assert.False(pq.Contains(this.j1));
            Assert.False(pq.Contains(this.j2));
            Assert.False(pq.Contains(this.j3));
        }

        [Fact]
        public void TestContains() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            Assert.True(pq.Contains(this.j1));
            Assert.True(pq.Contains(this.j2));
            Assert.True(pq.Contains(this.j3));
            JobTimeoutModel j4 = new JobTimeoutModel("abc", DateTime.Now + TimeSpan.FromMinutes(15), new JobRequest());
            Assert.False(pq.Contains(j4));
        }

        [Fact]
        public void TestCount1() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            Assert.Equal(3, pq.Count);
            pq.Dequeue();
            Assert.Equal(2, pq.Count);
            pq.Dequeue();
            Assert.Equal(1, pq.Count);
            pq.Dequeue();
            Assert.Equal(0, pq.Count);
            pq.Dequeue();
            Assert.Equal(0, pq.Count);
            pq.Enqueue(this.j1);
            Assert.Equal(1, pq.Count);
        }

        [Fact]
        public void TestCount2() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            Assert.Equal(3, pq.Count);
            pq.Remove(this.j1);
            Assert.Equal(2, pq.Count);
            pq.Remove(this.j2);
            Assert.Equal(1, pq.Count);
            pq.Remove(this.j3);
            Assert.Equal(0, pq.Count);
            pq.Remove(this.j1);
            Assert.Equal(0, pq.Count);
            pq.Enqueue(this.j1);
            Assert.Equal(1, pq.Count);
        }

        [Fact]
        public void TestDequeue() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            JobTimeoutModel d1 = pq.Dequeue();
            Assert.Equal(2, pq.Count);
            JobTimeoutModel d2 = pq.Dequeue();
            Assert.Equal(1, pq.Count);
            JobTimeoutModel d3 = pq.Dequeue();
            Assert.Equal(0, pq.Count);
            Assert.Equal(this.j1, d1);
            Assert.Equal(this.j2, d2);
            Assert.Equal(this.j3, d3);
        }

        [Fact]
        public void TestEmpty() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            pq.Dequeue();
            pq.Dequeue();
            pq.Dequeue();
            Assert.Equal(0, pq.Count);
        }

        [Fact]
        public void TestPeek() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            JobTimeoutModel front = pq.Peek();
            Assert.Equal(this.j1, front);
            Assert.Equal(3, pq.Count);
            pq.Clear();
            front = pq.Peek();
            Assert.Null(front);
            Assert.Equal(0, pq.Count);
        }

        [Fact]
        public void TestRemove1() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            pq.Remove(this.j1);
            JobTimeoutModel d1 = pq.Dequeue();
            JobTimeoutModel d2 = pq.Dequeue();
            Assert.Equal(d1, this.j2);
            Assert.Equal(d2, this.j3);
            // queue should be empty now
            Assert.Null(pq.Dequeue());
        }

        [Fact]
        public void TestRemove2() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            pq.Remove(this.j2);
            JobTimeoutModel d1 = pq.Dequeue();
            JobTimeoutModel d2 = pq.Dequeue();
            Assert.Equal(d1, this.j1);
            Assert.Equal(d2, this.j3);
            // queue should be empty now
            Assert.Null(pq.Dequeue());
        }

        [Fact]
        public void TestRemove3() {
            PriorityLinkedList<JobTimeoutModel> pq = this.CreateSimpleQueue();
            pq.Remove(this.j3);
            JobTimeoutModel d1 = pq.Dequeue();
            JobTimeoutModel d2 = pq.Dequeue();
            Assert.Equal(d1, this.j1);
            Assert.Equal(d2, this.j2);
            // queue should be empty now
            Assert.Null(pq.Dequeue());
        }
    }
}