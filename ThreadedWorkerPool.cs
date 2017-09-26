using System.Collections.Generic;
using System.Threading;
using UnityEngine.Assertions;

namespace Kazoo.Threading {
	/// <summary>
	/// Threaded Worker pool that consumes inputs and produces outputs. Thread workers may share data
	/// via an internal mutex.
	/// </summary>
	public class ThreadedWorkerPool<TI, TO> {
		// External Interface
		private readonly object _externalThreadLock = new object();
		private bool _externalMarkedForDeath;
		private readonly Queue<TI> _externalInputBuffer = new Queue<TI>();
		private readonly Queue<TO> _externalOutputBuffer = new Queue<TO>();

		/// <summary>
		/// The state of threaded work results.
		/// FatalError is serious and should be treated as an app failure.
		/// </summary>
		public enum ThreadedWorkResultType {
			NoOutput,
			OutputProduced,
			FatalError
		}

		/// <summary>
		/// The results of threaded work.
		/// </summary>
		public struct ThreadedWorkProduct {
			public ThreadedWorkResultType ResultType;
			public TO Output;

			public ThreadedWorkProduct(TO output, ThreadedWorkResultType resultType) {
				Output = output;
				ResultType = resultType;
			}

			/// <summary>
			/// Build an ouput that is assumed to have valid output.
			/// </summary>
			public ThreadedWorkProduct(TO output) {
				ResultType = ThreadedWorkResultType.OutputProduced;
				Output = output;
			}
		}

		/// <summary>
		/// A worker function to be performed in some number of threads.
		/// </summary>
		public delegate ThreadedWorkProduct ThreadedWork(TI input);

		/// <summary>
		/// The thread worker. Threads can call into the ThreadedWork function at any time.
		/// </summary>
		public interface IThreadableWorker {
			/// <summary>
			/// A work function to process a piece of info.
			/// The callee is responsible for synchronizing on it's shared resources.
			/// </summary>
			/// <returns>The results of this computation.</returns>
			ThreadedWorkProduct ThreadedWork(TI input);

			/// <summary>
			/// How many milliseconds do we wait after each work check?
			/// Be generous, share the CPU!
			/// </summary>
			int WorkCheckCooldownMilliseconds { get; }
		}

		/// <summary>
		/// Create a thread pool.
		/// </summary>
		/// <param name="poolCount">How many threads in the pool. These will always be running.</param>
		/// <param name="workerTemplate">The shared thread worker that implements the work.</param>
		public ThreadedWorkerPool(int poolCount, IThreadableWorker workerTemplate) {
			Assert.IsFalse(_externalMarkedForDeath, "Pool is in an invalid state of death.");
			_externalMarkedForDeath = false;
			for (int i = 0; i < poolCount; i++) {
				Thread thread = new Thread(() => WorkThread(workerTemplate));
				thread.Start();
			}
		}

		/// <summary>
		/// Halt all workers. Output can be consumed, but no more input will be.
		/// </summary>
		public void StopWorkers() {
			lock (_externalThreadLock) {
				Assert.IsFalse(_externalMarkedForDeath, "You may not stop a worker pool that is already marked for stopping.");
				_externalMarkedForDeath = true;
			}
		}

		private void WorkThread(IThreadableWorker worker) {
			int workCheckCooldown = worker.WorkCheckCooldownMilliseconds;
			Assert.IsTrue(workCheckCooldown >= 0, "Thread worker cooldown must be 0 milliseconds or greater.");
			while (!SynchronizedShouldDie()) {
				bool readyToWork;
				lock (_externalThreadLock) {
					readyToWork = _externalInputBuffer.Count > 0;
				}
				if (readyToWork) {
					TI input = default(TI);
					bool workAvilabale = false;
					lock (_externalThreadLock) {
						if (_externalInputBuffer.Count > 0) {
							input = _externalInputBuffer.Dequeue();
							workAvilabale = true;
						}
					}
					if (workAvilabale) {
						ThreadedWorkProduct result = worker.ThreadedWork(input);
						Assert.IsFalse(result.ResultType == ThreadedWorkResultType.FatalError,
							"A worker thread has experienced a FatalError. No recovery.");
						if (result.ResultType == ThreadedWorkResultType.OutputProduced) {
							lock (_externalThreadLock)
							{
								_externalOutputBuffer.Enqueue(result.Output);
							}
						}
					}
				}
				Thread.Sleep(workCheckCooldown);
			}
		}

		private bool SynchronizedShouldDie() {
			lock (_externalThreadLock) {
				return _externalMarkedForDeath;
			}
		}

		/// <summary>
		/// Submit input to be consumed by the workers.
		/// </summary>
		/// <param name="input">Input.</param>
		public void SubmitInput(TI input) {
			lock (_externalThreadLock) {
				if (!_externalMarkedForDeath) {
					_externalInputBuffer.Enqueue(input);
				}
			}
		}

		/// <summary>
		/// Check if output is on the buffer waiting for retrieval.
		/// </summary>
		/// <returns>true if buffer is non empty.</returns>
		public bool OutputExists() {
			lock (_externalThreadLock) {
				return _externalOutputBuffer.Count > 0;
			}
		}

		/// <summary>
		/// Synchronized dequeue of output buffer entries.
		/// </summary>
		/// <returns>An element of the output buffer.</returns>
		public TO DequeueOutput() {
			lock (_externalThreadLock) {
				Assert.IsTrue(_externalOutputBuffer.Count > 0, "No output in buffer. Be sure to check if OutputExists.");
				return _externalOutputBuffer.Dequeue();
			}
		}
	}
}
