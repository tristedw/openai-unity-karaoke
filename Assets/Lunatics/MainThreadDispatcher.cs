using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lunatics
{
    /// <summary>
    /// Executes actions from unity main thread. Public methods are thread-safe.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher instance;
        private static readonly List<Action> actionsList = new List<Action>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("Found another dispatcher instance. Destroyed");
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Adds action to the queue for execution from the main Unity thread. Thread-safe.
        /// </summary>
        /// <param name="action">Action to execute from Unity main thread</param>
        public static void Enqueue(Action action)
        {
            if (action == null) return;

            lock (actionsList)
            {
                actionsList.Add(action);
            }
        }

        private void Update()
        {
            Action[] actions;
            lock (actionsList)
            {
                if (actionsList.Count == 0) return;
                actions = actionsList.ToArray();
                actionsList.Clear();
            }

            foreach (var action in actions)
            {
                ExecuteAction(action);
            }
        }

        private static void ExecuteAction(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}