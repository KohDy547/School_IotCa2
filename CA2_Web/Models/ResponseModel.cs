using System;
using System.Net;

namespace CA2_Web.Models
{
    public class Response
    {
        public HttpStatusCode HttpStatus { get; set; }

        public string Message { get; set; }
        public object Payload { get; set; }
        public Exception ExceptionPayload { get; set; } = null;

        public bool HasError => ExceptionPayload != null;
    }

    public class CaptchaResponse : Response
    {
        public string CaptchaCode { get; set; }
        public byte[] CaptchaByteData { get; set; }
        public string CaptchBase64Data => Convert.ToBase64String(CaptchaByteData);
        public DateTime Timestamp { get; set; }
    }
}
