using System;
using System.Net;

namespace Framework.HttpResponses
{
    public class RedirectResult : IHttpResponseTypeResult
    {
        private readonly string _url;

        public RedirectResult(string url)
        {
            _url = url;
        }

        public byte[] Execute(HttpListenerContext context)
        {
            context.Response.StatusCode = 302;
            context.Response.RedirectLocation = _url;
            return Array.Empty<byte>();
        }
    }
}