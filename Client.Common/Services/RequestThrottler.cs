namespace Client.Common.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// 全局请求限流器，使用 SemaphoreSlim 限制并发请求数
    /// </summary>
    public static class RequestThrottler
    {
        // 最大并发请求数
        private const int MaxConcurrentRequests = 3;
        
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);

        /// <summary>
        /// 在限流控制下执行异步操作（有返回值）
        /// </summary>
        public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            await Semaphore.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// 在限流控制下执行异步操作（无返回值）
        /// </summary>
        public static async Task ExecuteAsync(Func<Task> action)
        {
            await Semaphore.WaitAsync();
            try
            {
                await action();
            }
            finally
            {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// 获取当前可用的请求槽位数
        /// </summary>
        public static int AvailableSlots => Semaphore.CurrentCount;
    }
}
