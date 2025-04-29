using System;
using System.Net.Http;

namespace RemoteDesktopWPF.Services
{
    public static class ApiService
    {
        private static readonly HttpClientHandler handler = new HttpClientHandler
        {
            // Bypass SSL validation for development only
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        public static readonly HttpClient Client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7191/")
        };
    }
}
