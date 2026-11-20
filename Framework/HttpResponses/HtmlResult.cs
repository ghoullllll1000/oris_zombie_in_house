using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Framework.HttpResponses
{
    public class HtmlResult : IHttpResponseTypeResult
    {
        private readonly object _html;

        public HtmlResult(string html)
        {
            _html = html;
        }

        public byte[] Execute(HttpListenerContext context)
        {
            string html = _html?.ToString() ?? string.Empty;
            context.Response.ContentType = "text/html; charset=utf-8";
            return Encoding.UTF8.GetBytes(html);
        }
    }
}
