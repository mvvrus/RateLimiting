namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    public class ActiveSessionLimitingMiddleware
    {
        RequestDelegate _next;

        public ActiveSessionLimitingMiddleware(RequestDelegate next) 
        {
            _next=next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);
            IActiveSession? active_session = context.GetActiveSession();
            if (active_session != null && active_session.IsAvailable) {
                ActiveSessionLeaseInfo? lease_info = active_session.Properties[ActiveSessionLeaseInfo.KEY] as ActiveSessionLeaseInfo;
                if(lease_info!= null) {
                    if(Volatile.Read(ref lease_info._lease)==null && lease_info.WasLeaseEverRejected) 
                        await active_session.Terminate(context);
                }
            }
        }
    }
}
