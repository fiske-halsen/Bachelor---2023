namespace EMSuite.PhoneNotification.Models
{
    public class Result<T>
    {
        public T Value { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
