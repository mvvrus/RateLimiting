using MVVRus.Threading.AsyncSynchroPrimitives;

namespace MVVRus.Threading.AsyncSynchroPrimitiveTests
{
    public class AsyncManualResetEventTests
    {
        [Fact(Timeout = 1000)]
        public async void SetAndWaitTest()
        {
            AsyncManualResetEvent t = new AsyncManualResetEvent();

            Task<Boolean> wait_result=t.WaitAsync(10000);
            Assert.False(wait_result.IsCompleted);
            //t.Set();
            t.Dispose();
            Boolean wait_completed = await wait_result;
            Assert.True(wait_completed);
        }
    }
}