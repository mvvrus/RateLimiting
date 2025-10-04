using MVVRus.Threading.AsyncSynchroPrimitives;

namespace MVVRus.Threading.AsyncSynchroPrimitiveTests
{
    public class AsyncManualResetEventTests
    {
        [Fact(Timeout = 5000)]
        public async void SetAndUnlimitedWaitTest()
        {
            AsyncManualResetEvent under_test = new AsyncManualResetEvent();
            Task wait_result=under_test.WaitAsync();
            Assert.NotSame(wait_result, await Task.WhenAny(wait_result, Task.Delay(100)));
            under_test.Set();
            await wait_result;
        }

        [Fact(Timeout = 5000)]
        public async void SetAndLimitedWaitTest()
        {
            AsyncManualResetEvent under_test = new AsyncManualResetEvent();
            Task<Boolean> wait_result = under_test.WaitAsync(500);
            Assert.NotSame(wait_result, await Task.WhenAny(wait_result, Task.Delay(100)));
            under_test.Set();
            Assert.True(await wait_result);

            under_test = new AsyncManualResetEvent();
            wait_result = under_test.WaitAsync(500);
            Assert.NotSame(wait_result, await Task.WhenAny(wait_result, Task.Delay(100)));
            Assert.False(await wait_result);
        }

        [Fact(Timeout = 5000)]
        public async void ResetWhileWaitingTest()
        {
            AsyncManualResetEvent under_test = new AsyncManualResetEvent();
            Task wait_result, wait_result2, delay_task;
            wait_result = under_test.WaitAsync();
            delay_task = Task.Delay(100);
            Assert.Same(delay_task, await Task.WhenAny(wait_result, delay_task));

            under_test.Reset();
            wait_result2 = under_test.WaitAsync();
            delay_task = Task.Delay(100);
            Assert.Same(delay_task, await Task.WhenAny(wait_result2, delay_task));

            under_test.Set();
            await Task.Yield();
            await wait_result;
            await wait_result2;
        }

        [Fact(Timeout = 5000)]
        public async void ResetAfterSetTest()
        {
            AsyncManualResetEvent under_test = new AsyncManualResetEvent();
            Task wait_result, delay_task;
            wait_result = under_test.WaitAsync();
            delay_task = Task.Delay(100);
            Assert.Same(delay_task, await Task.WhenAny(wait_result, delay_task));
            under_test.Set();
            await wait_result;

            under_test.Reset();
            wait_result = under_test.WaitAsync();
            delay_task = Task.Delay(100);
            Assert.Same(delay_task, await Task.WhenAny(wait_result, delay_task));

            under_test.Set();
            await Task.Yield();
            await wait_result;
        }

        [Fact(Timeout = 5000)]
        public async void CancelUnlimitedWaitTest()
        {
            using(CancellationTokenSource cts=new CancellationTokenSource()) {
                AsyncManualResetEvent under_test = new AsyncManualResetEvent();
                Task wait_result = under_test.WaitAsync(cts.Token);
                cts.Cancel();
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await wait_result);
            }        
        }

        [Fact(Timeout = 5000)]
        public async void CancelLimitedWaitTest()
        {
            using(CancellationTokenSource cts = new CancellationTokenSource()) {
                AsyncManualResetEvent under_test = new AsyncManualResetEvent();
                Task wait_result = under_test.WaitAsync(10000, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await wait_result);
            }
        }

        [Fact(Timeout = 5000)]
        public async void DisposeWhileWaitingTest()
        {
            AsyncManualResetEvent under_test = new AsyncManualResetEvent();
            Task wait_result = under_test.WaitAsync();
            under_test.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(async ()=> await wait_result);
        }

    }
}