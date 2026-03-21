namespace Onion.Pool {
    public static class IPoolableExtensions {
        public static void Release<T>(this T data) where T : class, IPoolable, new() {
            DataPool<T>.Release(data);   
        }
    }
}