using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Onion.Pool {
    public static class DataPool {
        public static CancellationToken exitToken { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            exitToken = Application.exitCancellationToken;
        }
    }

    public static class DataPool<T> where T : class, IPoolable, new() {
        private static readonly Stack<T> _pool = new();
        private static readonly HashSet<T> _active = new();
        private static readonly object _lock = new();

        private static bool _trimAuto = true;
        private static bool _isTrimming = false;
        private static int _trimMinSize = 0;
        private static float _trimRatio = 0.2f;
        private static float _trimInterval = 5.0f;

        private static int _peakCapacity = 0;

        public static bool trimAuto {
            get { lock (_lock) { return _trimAuto; } }
            set { 
                lock (_lock) { 
                    _trimAuto = value; 
                
                    if (_trimAuto && !_isTrimming) {
                        _isTrimming = true;
                        _ = TrimAsync();
                    }
                } 
            }
        }

        public static int trimMinSize {
            get { lock (_lock) { return _trimMinSize; } }
            set { lock (_lock) { _trimMinSize = Mathf.Max(0, value); } }
        }

        public static float trimRatio {
            get { lock (_lock) { return _trimRatio; } }
            set { lock (_lock) { _trimRatio = Mathf.Clamp01(value); } }
        }

        public static float trimInterval {
            get { lock (_lock) { return _trimInterval; } }
            set { lock (_lock) { _trimInterval = Mathf.Max(0.1f, value); } }
        }

        public static T Get() {
            T data;

            lock (_lock) {
                if (trimAuto && !_isTrimming) {
                    _isTrimming = true;

                    _ = TrimAsync();
                }

                data = _pool.Count > 0 
                    ? _pool.Pop() 
                    : new T();

                _active.Add(data);

                if (_active.Count + _pool.Count > _peakCapacity) {
                    _peakCapacity = _active.Count + _pool.Count;
                }
            }

            return data;
        }

        public static void Release(T data) {
            data.Clear();

            lock (_lock) {
                if (!_active.Remove(data)) {
                    Debug.LogWarning($"Attempted to release an object that is not active: {typeof(T).Name}");
                    return;
                }

                _pool.Push(data);
            }
        }

        private static async Awaitable TrimAsync() {
            var token = DataPool.exitToken;

            while (!token.IsCancellationRequested) {
                float interval;
                lock (_lock) {
                    if (!_trimAuto) {
                        _isTrimming = false;

                        return;
                    }

                    interval = _trimInterval;
                }

                try {
                    await Awaitable.WaitForSecondsAsync(interval, token);

                    if (trimAuto) Trim();
                } catch (OperationCanceledException) {
                    break;
                }
            }

            lock (_lock) {
                _isTrimming = false;
            }
        }

        private static void Trim() {
            lock (_lock) {
                int targetSize = Mathf.Max(_trimMinSize, Mathf.CeilToInt(_peakCapacity * (1 - _trimRatio)));
                int trimCount = _active.Count + _pool.Count - targetSize;

                if (trimCount <= 0) {
                    _peakCapacity = targetSize;
                    return;
                }

                trimCount = Mathf.Min(trimCount, _pool.Count);
                for (int i = 0; i < trimCount; i++) {
                    _pool.Pop();
                }

                _peakCapacity = _active.Count + _pool.Count;
            }
        }
    }
}
