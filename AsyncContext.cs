using System.Collections.Concurrent;

namespace CEFSharpDemo.Net6.WokerService
{
    public static class AsyncContext
    {
        public static void Run(Func<Task> func)
        {
            var prevCtx = SynchronizationContext.Current;

            try
            {
                var syncCtx = new SingleThreadSynchronizationContext();

                SynchronizationContext.SetSynchronizationContext(syncCtx);

                var t = func();

                t.ContinueWith(delegate
                {
                    syncCtx.Complete();
                }, TaskScheduler.Default);

                syncCtx.RunOnCurrentThread();

                t.GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);
            }
        }
    }

    public sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object?>> queue =
            new BlockingCollection<KeyValuePair<SendOrPostCallback, object?>>();

        public override void Send(SendOrPostCallback d, object? state)
        {
            queue.Add(new KeyValuePair<SendOrPostCallback, object?>(d, state));
        }

        public void RunOnCurrentThread()
        {
            while (queue.TryTake(out var workItem, Timeout.Infinite))
            {
                workItem.Key(workItem.Value);
            }
        }

        public void Complete()
        {
            queue.CompleteAdding();
        }
    }
}
