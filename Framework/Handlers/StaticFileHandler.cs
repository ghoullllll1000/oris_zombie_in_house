using System.Net;
using System.Text;

namespace Framework.Handlers
{
    class StaticFileHandler : Handler
    {
        public override async Task HandleRequest(HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            HttpListenerRequest request = context.Request;

            string localPath = request.Url?.LocalPath ?? "/";
            string requestUrl = localPath.Trim('/');

            bool isFile = !string.IsNullOrEmpty(requestUrl) && requestUrl.Contains('.');

            if (isFile)
            {
                try
                {
                    string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "static");
                    string fullRootPath = Path.GetFullPath(rootPath) + Path.DirectorySeparatorChar;
                    string filePath = Path.GetFullPath(Path.Combine(rootPath, requestUrl));

                    if (!filePath.StartsWith(fullRootPath, StringComparison.OrdinalIgnoreCase))
                    {
                        response.StatusCode = 403;
                        await WriteResponseAsync(response, "Доступ запрещён.");
                        return;
                    }

                    FileInfo fileInfo = new FileInfo(filePath);

                    if (!fileInfo.Exists)
                    {
                        response.StatusCode = 404;
                        filePath = Path.Combine(rootPath, "404.html");
                    }
                    else
                    {
                        response.StatusCode = 200;
                    }

                    byte[] buffer = await File.ReadAllBytesAsync(filePath);
                    response.ContentType = GetContentType(filePath);
                    response.ContentLength64 = buffer.Length;
                    using Stream output = response.OutputStream;

                    await output.WriteAsync(buffer);
                    await output.FlushAsync();

                    response.Close();
                }
                catch (Exception ex)
                {
                    try
                    {
                        response.StatusCode = 500;
                        await WriteResponseAsync(response, $"Ошибка при обработке запроса: {ex.Message}.");
                    }
                    catch
                    {
                    }
                }
            }
            else if (Successor != null)
            {
                await Successor.HandleRequest(context);
            }
        }
        private string GetContentType(string filePath)
        {
            string result;

            switch (new FileInfo(filePath).Extension)
            {
                case ".html":
                    result = "text/html; charset=utf-8";
                    break;
                case ".htm":
                    result = "text/html; charset=utf-8";
                    break;
                case ".css":
                    result = "text/css; charset=utf-8";
                    break;
                case ".js":
                    result = "text/javascript; charset=utf-8";
                    break;
                case ".png":
                    result = "image/png";
                    break;
                case ".ico":
                    result = "image/x-icon";
                    break;
                case ".svg":
                    result = "image/svg+xml";
                    break;
                case ".jpg":
                    result = "image/jpeg";
                    break;
                case ".jpeg":
                    result = "image/jpeg";
                    break;
                case ".gif":
                    result = "image/gif";
                    break;
                case ".webp":
                    result = "image/webp";
                    break;
                default:
                    result = "text/html; charset=utf-8";
                    break;
            }

            return result;
        }
        
        private async Task WriteResponseAsync(HttpListenerResponse response, string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;

            await output.WriteAsync(buffer);
            await output.FlushAsync();

            response.Close();
        }
    }
}
