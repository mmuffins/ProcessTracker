namespace ProcessTrackerService.Core.Interfaces
{
    public abstract class APIResponseMessage
    {
        public bool Success { get; }
        public string Message { get; }
        public int StatusCode { get; }

        protected APIResponseMessage(int statuscode, bool success = false, string message = null)
        {
            Success = success;
            Message = message;
            StatusCode = statuscode;
        }
    }
}
