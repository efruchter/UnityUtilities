using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Assertions;
// ReSharper disable DelegateSubtraction

namespace Patterns.Observer
{
    public interface IMessageReceiver<in T>
    {
        void OnMessageReceived(T message);
    }

    /// <summary>
    /// A message bus for implementing an Observer pattern. Call GetBus() to access the global bus,
    /// or GetBus(GameObject) to use a bus on a gameobject.
    /// </summary>
    public static class MessageBus
    {
        private static MessageBusBehaviour _globalBusBehaviour;
        private static bool _globalBusBehaviourInitialized;

        private static void InitGlobalBus()
        {
            Assert.IsTrue(Application.isPlaying, "Global MessageBus cannot be used unless the game is running.");

            if (_globalBusBehaviourInitialized)
            {
                return;
            }

            _globalBusBehaviourInitialized = true;
            _globalBusBehaviour = new GameObject("[Global MessageBus]").AddComponent<MessageBusBehaviour>();
            UnityEngine.Object.DontDestroyOnLoad(_globalBusBehaviour.gameObject);
        }

        /// <summary>
        /// Get the global bus.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IBus<T> GetBus<T>()
        {
            if (!Application.isPlaying)
            {
                return new DummyBusImpl<T>();
            }

            InitGlobalBus();
            return _globalBusBehaviour != null ? _globalBusBehaviour.GetBus<T>() : new DummyBusImpl<T>();
        }

        /// <summary>
        /// Get the bus attached to a GameObject, or create one.
        /// </summary>
        /// <typeparam name="T">The Message type</typeparam>
        /// <param name="go">Gameobject that the bus lives on.</param>
        /// <returns>If the object is alive, a valid bus. If the object is dead, a dummy bus.</returns>
        public static IBus<T> GetBus<T>(GameObject go)
        {
            if (go == null)
            {
                return new DummyBusImpl<T>();
            }

            var localBus = go.GetComponent<MessageBusBehaviour>() ?? go.AddComponent<MessageBusBehaviour>();

            return localBus.GetBus<T>();
        }
    }

    /// <summary>
    /// Component for linking a bus with a GameObject's lifecycle.
    /// </summary>
    public class MessageBusBehaviour : MonoBehaviour
    {
        private readonly Dictionary<Type, object> _busses = new Dictionary<Type, object>();

        public IBus<T> GetBus<T>()
        {
            var key = typeof(T);

            object o;
            if (_busses.TryGetValue(key, out o))
            {
                Assert.IsTrue(o is IBus<T>, "Retrieved IBus was not a valid type in MessageBusBehaviour.GetBus. Fatal.");
                return o as IBus<T>;
            }

            DelegateBusImpl<T> bus = new DelegateBusImpl<T>();
            _busses[key] = bus;
            return bus;
        }
    }

    public interface IBus<T>
    {
        /// <summary>
        /// The amount of buffered messages. 1 by default.
        /// </summary>
        int GetMessageBufferLength();

        /// <summary>
        /// Set how many Buffered messages are stored.
        /// </summary>
        /// <param name="length"></param>
        void SetMessageBufferLength(int length);

        /// <summary>
        /// Subscribe to future messages. If any buffered messages exist, recieve them instantly.
        /// </summary>
        /// <param name="function"></param>
        void Subscribe(Action<T> function);

        /// <summary>
        /// Subscribe to future messages. If any buffered messages exist, recieve them instantly.
        /// </summary>
        /// <param name="messageReceiver"></param>
        void Subscribe(IMessageReceiver<T> messageReceiver);

        /// <summary>
        /// Subscribe to the next message only.
        /// </summary>
        /// <param name="function"></param>
        void SubscribeOnce(Action<T> function);

        /// <summary>
        /// Subscribe to the next message only.
        /// </summary>
        /// <param name="messageReceiver"></param>
        void SubscribeOnce(IMessageReceiver<T> messageReceiver);

        /// <summary>
        /// Release your subscription.
        /// </summary>
        /// <param name="function"></param>
        void Unsubscribe(Action<T> function);

        /// <summary>
        /// Release your subscription.
        /// </summary>
        /// <param name="messageReceiver"></param>
        void Unsubscribe(IMessageReceiver<T> messageReceiver);

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
        int GetListenerCount();
    }

    /// <summary>
    /// Message bus backed by an array queue for buffered messages and delegates for subscribers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DelegateBusImpl<T> : IBus<T>
    {
        private event Action<T> _actions = Empty;
        private event Action<T> _singleAction = Empty;
        private readonly Queue<T> _messageBuffer = new Queue<T>();
        private int _maxBufferLength = 1;

        public int GetMessageBufferLength()
        {
            return _maxBufferLength;
        }

        public virtual void SetMessageBufferLength(int length)
        {
            _maxBufferLength = Math.Max(length, 0);
            while (_messageBuffer.Count > _maxBufferLength)
            {
                _messageBuffer.Dequeue();
            }
            _messageBuffer.TrimExcess();
        }

        public virtual void Subscribe(Action<T> function)
        {
            Assert.IsNotNull(_actions);

            _actions += function;

            foreach (var buffered in _messageBuffer)
            {
                function(buffered);
            }
        }

        public void SubscribeOnce(Action<T> function)
        {
            Assert.IsNotNull(_singleAction);

            _singleAction += function;
        }

        public virtual void Unsubscribe(Action<T> function)
        {
            Assert.IsNotNull(_actions);
            Assert.IsNotNull(_singleAction);

            _actions -= function;
            _singleAction -= function;
        }

        public void Send(T message)
        {
            Assert.IsNotNull(_actions);
            Assert.IsNotNull(_singleAction);

            _actions(message);
            _singleAction(message);

            _singleAction = Empty;
        }

        public void SendBuffered(T message)
        {
            Send(message);

            if (_maxBufferLength <= 0)
            {
                return;
            }

            if (_messageBuffer.Count == _maxBufferLength)
            {
                _messageBuffer.Dequeue();
            }

            _messageBuffer.Enqueue(message);
        }

        public void ClearMessageBuffer()
        {
            _messageBuffer.Clear();
        }

        public void ReleaseAllSubscribers()
        {
            Assert.IsNotNull(_actions);
            Assert.IsNotNull(_singleAction);

            _actions = Empty;
            _singleAction = Empty;
        }

        public int GetListenerCount()
        {
            Assert.IsNotNull(_actions);
            Assert.IsNotNull(_singleAction);

            // -1 for each list because the of Empty functions.
            return (_actions.GetInvocationList().Length - 1)
                + (_singleAction.GetInvocationList().Length - 1);
        }

        private static void Empty(T message)
        {

        }

        public void Subscribe(IMessageReceiver<T> messageReceiver)
        {
            Subscribe(messageReceiver.OnMessageReceived);
        }

        public void SubscribeOnce(IMessageReceiver<T> messageReceiver)
        {
            SubscribeOnce(messageReceiver.OnMessageReceived);
        }

        public void Unsubscribe(IMessageReceiver<T> messageReceiver)
        {
            Unsubscribe(messageReceiver.OnMessageReceived);
        }
    }

    /// <summary>
    /// Empty bus implementation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct DummyBusImpl<T> : IBus<T>
    {
        public int GetListenerCount()
        {
            return 0;
        }

        public void ClearMessageBuffer()
        {
        }

        public void Unsubscribe(Action<T> function)
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

        public void SetMessageBufferLength(int length)
        {
        }

        public void Subscribe(Action<T> function)
        {
        }

        public void SubscribeOnce(Action<T> function)
        {
        }

        public int GetMessageBufferLength()
        {
            return 0;
        }

        public void Subscribe(IMessageReceiver<T> messageReceiver)
        {
        }

        public void SubscribeOnce(IMessageReceiver<T> messageReceiver)
        {
        }

        public void Unsubscribe(IMessageReceiver<T> messageReceiver)
        {
        }
    }

    public static class MessageBusTests
    {
        public static IEnumerator Test1()
        {
            yield return null;
            int[] c = { 0 };
            MessageBus.GetBus<int>().Subscribe((i) => { c[0] += i; });

            MessageBus.GetBus<int>().Send(1);
            Assert.AreEqual(c[0], 1, "Send does not send properly.");

            c[0] = 3;
            MessageBus.GetBus<int>().SendBuffered(2);
            Assert.AreEqual(c[0], 5, "Send Buffered does not send properly.");

            IBus<int> busCache = MessageBus.GetBus<int>();
            c[0] = 0;
            busCache.Subscribe((i) => { c[0] += i; });
            Assert.AreEqual(c[0], 2, "Send Buffered does not send to future subscribers properly.");

            var go = new GameObject("test");

            MessageBus.GetBus<int>(go).Send(8);
            Assert.AreEqual(c[0], 2, "Local busses leak into Global Bus.");

            int[] d = { 0 };
            MessageBus.GetBus<int>(go).SubscribeOnce((i) => { d[0] += i; });
            MessageBus.GetBus<int>(go).Send(1);
            MessageBus.GetBus<int>(go).Send(1);
            Assert.AreEqual(d[0], 1, "Subscribe Once should only receive one message before Releasing itself.");

            yield return null;
            UnityEngine.Object.Destroy(go);
            yield return null;

            d[0] = 0;

            MessageBus.GetBus<int>(go).Subscribe((i) => { d[0] += i; });
            MessageBus.GetBus<int>(go).Send(1);

            Assert.AreEqual(go, null, "Deleted gameobject should be null.");
            Assert.AreEqual(d[0], 0, "Getting the bus of a null object should return a dummy bus.");

            busCache.SendBuffered(76);
            busCache.ClearMessageBuffer();

            c[0] = 0;

            busCache.Subscribe((i) => { c[0] += i; });
            Assert.AreEqual(c[0], 0, "Clear Message Buffer didn't clear.");

            busCache.ReleaseAllSubscribers();
            busCache.Send(9);
            Assert.AreEqual(c[0], 0, "Release all subscriber Buffer didn't clear.");
        }
    }
}
