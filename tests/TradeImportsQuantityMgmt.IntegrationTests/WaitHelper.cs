namespace TradeImportsQuantityMgmt.IntegrationTests
{
    public static class WaitHelper
    {
        public static async Task<bool> WaitUntilAsync(
            Func<Task<bool>> condition,
            TimeSpan timeout,
            TimeSpan? pollInterval = null,
            CancellationToken cancellationToken = default
        )
        {
            var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);

            using var timeoutCts = new CancellationTokenSource(timeout);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            while (!linkedCts.Token.IsCancellationRequested)
            {
                try
                {
                    if (await condition())
                    {
                        return true;
                    }

                    await Task.Delay(interval, linkedCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            return false;
        }
    }
}
