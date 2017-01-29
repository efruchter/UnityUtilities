using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Assertions;

namespace Kazoo.Messaging
{
    /// <summary>
    /// A simple message bus for implementing an Observer pattern. Call GetBus() to access the global bus,
    /// or GetBus(GameObject) to attach a bus to a gameobject.
    /// </summary>
    public static class MessageBus
    {
        static LocalMessageBus globalBus;

        static MessageBus()
        {
            globalBus = new GameObject("[Global MessageBus]").AddComponent<LocalMessageBus>();
            Assert.AreNotEqual(globalBus, null, "Creation of an object for the Global MessageBus has failed.");
            MonoBehaviour.DontDestroyOnLoad(globalBus.gameObject);
        }

        /// <summary>
        /// Get the global bus.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IBus<T> GetBus<T>()
        {
            if (globalBus == null)
            {
                return new DummyBusImpl<T>();
            }

            return globalBus.GetBus<T>();
        }

        public static IBus<T> GetBus<T>(GameObject go)
        {
            if (go == null)
            {
                return new DummyBusImpl<T>();
            }

            LocalMessageBus localBus = go.GetComponent<LocalMessageBus>();
            if (localBus == null)
            {
                localBus = go.AddComponent<LocalMessageBus>();
            }

            return localBus.GetBus<T>();
        }
    }

    /// <summary>
    /// Component for linking a bus with a GameObject's lifecycle.
    /// </summary>
    public class LocalMessageBus : MonoBehaviour
    {
        Dictionary<Type, object> busses = new Dictionary<Type, object>();

        public IBus<T> GetBus<T>()
        {
            var key = typeof(T);
            object o;
            if (busses.TryGetValue(key, out o))
            {
                return o as IBus<T>;
            }

            IBus<T> bus = new BusImpl<T>();
            busses[key] = bus;
            return bus;
        }
    }

    public interface IBus<T>
    {
        /// <summary>
        /// The amount of buffered messages. 1 by default.
        /// </summary>
        int maxBufferLength { get; }

        /// <summary>
        /// Set the length of the message buffer.
        /// </summary>
        /// <param name="length"></param>
        void SetMaxBufferLength(int length);

        /// <summary>
        /// Subscribe to future messages. If any buffered messages exist, recieve them instantly.
        /// </summary>
        /// <param name="function"></param>
        void Subscribe(Action<T> function);

        /// <summary>
        /// Subscribe to the next message.
        /// </summary>
        /// <param name="function"></param>
        void SubscribeOnce(Action<T> function);

        /// <summary>
        /// Release yoru subscription.
        /// </summary>
        /// <param name="function"></param>
        void Release(Action<T> function);

        /// <summary>
        /// Send a message to all subscribers.
        /// </summary>
        /// <param name="message"></param>
        void Send(T message);

        /// <summary>
        /// Send a message to all subscribers and all future subscribers.
        /// </summary>
        /// <param name="message"></param>
        void SendBuffered(T message);

        /// <summary>
        /// Clear all buffered messages.
        /// </summary>
        void ClearMessageBuffer();

        /// <summary>
        /// Release all subscribers.
        /// </summary>
        void ReleaseAllSubscribers();

        /// <summary>
        /// Amount of subscribers.
        /// </summary>
        int ListenerCount { get; }
    }

    /// <summary>
    /// Basic bus backed by an array queue for buffered messages and delegates for subscribers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BusImpl<T> : IBus<T>
    {
        Action<T> actions = Empty;
        Action<T> singleAction = Empty;
        Queue<T> buffer = new Queue<T>(1);
        public int maxBufferLength { private set; get; }

        public BusImpl()
        {
            maxBufferLength = 1;
        }

        public virtual void SetMaxBufferLength(int length)
        {
            maxBufferLength = Mathf.Max(0, length);
            TrimMessageBuffer();
            buffer.TrimExcess();
        }

        void TrimMessageBuffer()
        {
            Assert.IsTrue(maxBufferLength >= 0, "Buffer Length should be non-negative.");
            while (buffer.Count > maxBufferLength)
            {
                buffer.Dequeue();
            }
        }

        public virtual void Subscribe(Action<T> function)
        {
            actions += function;

            foreach (var buffered in buffer)
            {
                function(buffered);
            }
        }

        public void SubscribeOnce(Action<T> function)
        {
            singleAction += function;
        }

        public virtual void Release(Action<T> function)
        {
            actions -= function;
            singleAction -= function;
        }

        public void Send(T message)
        {
            actions(message);

            singleAction(message);
            singleAction = Empty;
        }

        public void SendBuffered(T message)
        {
            Send(message);

            buffer.Enqueue(message);
            TrimMessageBuffer();
        }

        public void ClearMessageBuffer()
        {
            buffer.Clear();
        }

        public void ReleaseAllSubscribers()
        {
            actions = Empty;
            singleAction = Empty;
        }

        public int ListenerCount
        {
            get
            {
                // -1 for each list because the Empty function forms the starting delegate on each.
                return actions.GetInvocationList().Length - 1
                    + singleAction.GetInvocationList().Length - 1;
            }
        }

        static void Empty(T message)
        {

        }
    }

    /// <summary>
    /// Empty bus implementation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DummyBusImpl<T> : IBus<T>
    {
        public int ListenerCount
        {
            get
            {
                return 0;
            }
        }

        public int maxBufferLength
        {
            get
            {
                return 0;
            }
        }

        public void ClearMessageBuffer()
        {
        }

        public void Release(Action<T> function)
        {
        }

        public void ReleaseAllSubscribers()
        {
        }

        public void Send(T message)
        {
        }

        public void SendBuffered(T message)
        {
        }

        public void SetMaxBufferLength(int length)
        {
        }

        public void Subscribe(Action<T> function)
        {
        }

        public void SubscribeOnce(Action<T> function)
        {
        }
    }
}
