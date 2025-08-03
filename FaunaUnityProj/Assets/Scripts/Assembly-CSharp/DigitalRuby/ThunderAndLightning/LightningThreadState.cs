using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	public class LightningThreadState
	{
		internal readonly int mainThreadId;

		internal readonly bool multiThreaded;

		private Thread lightningThread;

		private AutoResetEvent lightningThreadEvent = new AutoResetEvent(initialState: false);

		private readonly Queue<Action> actionsForBackgroundThread = new Queue<Action>();

		private readonly Queue<KeyValuePair<Action<bool>, ManualResetEvent>> actionsForMainThread = new Queue<KeyValuePair<Action<bool>, ManualResetEvent>>();

		public bool Running = true;

		private bool isTerminating;

		private bool UpdateMainThreadActionsOnce(bool inDestroy)
		{
			KeyValuePair<Action<bool>, ManualResetEvent> keyValuePair;
			lock (actionsForMainThread)
			{
				if (actionsForMainThread.Count == 0)
				{
					return false;
				}
				keyValuePair = actionsForMainThread.Dequeue();
			}
			try
			{
				keyValuePair.Key(inDestroy);
			}
			catch
			{
			}
			if (keyValuePair.Value != null)
			{
				keyValuePair.Value.Set();
			}
			return true;
		}

		private void BackgroundThreadMethod()
		{
			Action action = null;
			while (Running)
			{
				try
				{
					if (!lightningThreadEvent.WaitOne(500))
					{
						continue;
					}
					while (true)
					{
						lock (actionsForBackgroundThread)
						{
							if (actionsForBackgroundThread.Count == 0)
							{
								break;
							}
							action = actionsForBackgroundThread.Dequeue();
							goto IL_0051;
						}
						IL_0051:
						action();
					}
				}
				catch (ThreadAbortException)
				{
				}
				catch (Exception ex2)
				{
					Debug.LogErrorFormat("Lightning thread exception: {0}", ex2);
				}
			}
		}

		public LightningThreadState(bool multiThreaded)
		{
			mainThreadId = Thread.CurrentThread.ManagedThreadId;
			this.multiThreaded = multiThreaded;
			lightningThread = new Thread(BackgroundThreadMethod)
			{
				IsBackground = true,
				Name = "LightningBoltScriptThread"
			};
			lightningThread.Start();
		}

		public void TerminateAndWaitForEnd(bool inDestroy)
		{
			isTerminating = true;
			while (true)
			{
				if (UpdateMainThreadActionsOnce(inDestroy))
				{
					continue;
				}
				lock (actionsForBackgroundThread)
				{
					if (actionsForBackgroundThread.Count == 0)
					{
						break;
					}
				}
			}
		}

		public void UpdateMainThreadActions()
		{
			if (multiThreaded)
			{
				while (UpdateMainThreadActionsOnce(inDestroy: false))
				{
				}
			}
		}

		public bool AddActionForMainThread(Action<bool> action, bool waitForAction = false)
		{
			if (isTerminating)
			{
				return false;
			}
			if (Thread.CurrentThread.ManagedThreadId == mainThreadId || !multiThreaded)
			{
				action(obj: true);
				return true;
			}
			ManualResetEvent manualResetEvent = (waitForAction ? new ManualResetEvent(initialState: false) : null);
			lock (actionsForMainThread)
			{
				actionsForMainThread.Enqueue(new KeyValuePair<Action<bool>, ManualResetEvent>(action, manualResetEvent));
			}
			manualResetEvent?.WaitOne(10000);
			return true;
		}

		public bool AddActionForBackgroundThread(Action action)
		{
			if (isTerminating)
			{
				return false;
			}
			if (!multiThreaded)
			{
				action();
			}
			else
			{
				lock (actionsForBackgroundThread)
				{
					actionsForBackgroundThread.Enqueue(action);
				}
				lightningThreadEvent.Set();
			}
			return true;
		}
	}
}
