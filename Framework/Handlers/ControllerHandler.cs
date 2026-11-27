using Framework.Attributes;
using Framework.Authentication;
using Framework.Controllers;
using Framework.Core;
using Framework.HttpResponses;
using System.Net;
using System.Reflection;
using System.Web;

namespace Framework.Handlers
{
    internal class ControllerHandler : Handler
    {
        private readonly string connectionString = string.Empty;
        private readonly IUserSessionService userSessionService;

        public ControllerHandler(string connectionString, IUserSessionService userSessionService)
        {
            this.connectionString = connectionString;
            this.userSessionService = userSessionService;
        }

        private bool IsAuthorized(HttpListenerContext context, Type controllerType, MethodInfo method)
        {
            bool requiresAuthorization =
                controllerType.IsDefined(typeof(AuthorizeAttribute), true) ||
                method.IsDefined(typeof(AuthorizeAttribute), true);

            if (!requiresAuthorization)
            {
                return true;
            }

            string? sessionId = context.Request.Cookies["sessionId"]?.Value;
            return userSessionService.GetUserId(sessionId).HasValue;
        }

        public override async Task HandleRequest(HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            HttpListenerRequest request = context.Request;

            string[] pathParts = request.Url!.LocalPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            string? controllerName = null;
            string? methodName = null;

            if (pathParts.Length == 0 && request.HttpMethod == "GET")
            {
                controllerName = "home";
                methodName = "index";
            }
            else if (pathParts.Length >= 2)
            {
                controllerName = pathParts[0];
                methodName = pathParts[1];
            }

            if (controllerName != null && methodName != null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var types = assemblies.SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null)!;
                    }
                });

                Type? controllerType = null;

                foreach (var type in types)
                {
                    var attr = type.GetCustomAttribute<HttpControllerAttribute>();
                    if (attr != null && attr.Route.Equals(controllerName, StringComparison.OrdinalIgnoreCase))
                    {
                        controllerType = type;
                        break;
                    }
                }

                if (controllerType == null)
                {
                    await SendNotFound(response);
                    return;
                }

                MethodInfo? targetMethod = null;
                foreach (var method in controllerType.GetMethods())
                {
                    var getAttr = method.GetCustomAttribute<GetAttribute>();
                    if (getAttr != null && getAttr.Route.Equals(methodName, StringComparison.OrdinalIgnoreCase) && request.HttpMethod == "GET")
                    {
                        targetMethod = method;
                        break;
                    }

                    var postAttr = method.GetCustomAttribute<PostAttribute>();
                    if (postAttr != null && postAttr.Route.Equals(methodName, StringComparison.OrdinalIgnoreCase) && request.HttpMethod == "POST")
                    {
                        targetMethod = method;
                        break;
                    }
                }

                if (targetMethod == null)
                {
                    await SendNotFound(response);
                    return;
                }

                System.Collections.Specialized.NameValueCollection formData = new();
                if (request.HttpMethod == "POST" && request.HasEntityBody)
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    string bodyString = await reader.ReadToEndAsync();
                    var parsedBody = HttpUtility.ParseQueryString(bodyString);
                    formData.Add(parsedBody);
                }

                var parameters = targetMethod.GetParameters();
                object[] args = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    string paramName = param.Name!;
                    string? rawValue = null;

                    if (param.IsDefined(typeof(FormDataAttribute)))
                    {
                        rawValue = formData[paramName];
                    }
                    else if (param.IsDefined(typeof(QueryDataAttribute)) || request.HttpMethod == "GET")
                    {
                        rawValue = request.QueryString[paramName];
                    }
                    else
                    {
                        rawValue = request.QueryString[paramName] ?? formData[paramName];
                    }

                    if (!string.IsNullOrEmpty(rawValue))
                    {
                        try
                        {
                            Type targetType = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
                            args[i] = Convert.ChangeType(rawValue, targetType);
                        }
                        catch
                        {
                            args[i] = param.ParameterType.IsValueType
                                ? Activator.CreateInstance(param.ParameterType)!
                                : string.Empty;
                        }
                    }
                    else
                    {
                        args[i] = param.ParameterType.IsValueType
                            ? Activator.CreateInstance(param.ParameterType)!
                            : string.Empty;
                    }
                }

                ControllerBase controllerInstance = (Activator.CreateInstance(controllerType) as ControllerBase)!;
                controllerInstance.SetContext(context);
                controllerInstance.SetConnectionString(connectionString);
                controllerInstance.SetSessionService(userSessionService);

                if (!IsAuthorized(context, controllerType, targetMethod))
                {
                    await SendUnauthorized(response);
                    return;
                }

                object? result = targetMethod.Invoke(controllerInstance, args);

                if (result is Task task)
                {
                    await task;

                    var resultProperty = task.GetType().GetProperty("Result");
                    result = resultProperty?.GetValue(task);
                }

                if (result is IHttpResponseTypeResult responseResult)
                {
                    var buffer = responseResult.Execute(context);
                    if (result is not RedirectResult)
                    {
                        await HttpResponseWriter.WriteBytesAsync(response, buffer);
                    }
                }
                else if (result is string fileName)
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "static", fileName);
                    if (File.Exists(filePath))
                    {
                        byte[] bytes = await File.ReadAllBytesAsync(filePath);
                        response.ContentType = "text/html; charset=utf-8";
                        await HttpResponseWriter.WriteBytesAsync(response, bytes, 200);
                    }
                    else
                    {
                        await HttpResponseWriter.WriteHtmlAsync(response, fileName, 200);
                    }
                }
                else
                {
                    string html = result?.ToString() ?? "<h1>Метод контроллера выполнен</h1>";
                    await HttpResponseWriter.WriteHtmlAsync(response, html, 200);
                }

                response.Close();
                return;
            }

            if (Successor != null)
            {
                await Successor.HandleRequest(context);
            }
            else
            {
                await SendNotFound(response);
            }
        }

        private async Task SendNotFound(HttpListenerResponse response)
        {
            response.StatusCode = 404;
            response.ContentType = "text/html; charset=utf-8";

            string errorFilePath = Path.Combine(Directory.GetCurrentDirectory(), "static", "404.html");

            if (File.Exists(errorFilePath))
            {
                byte[] buffer = await File.ReadAllBytesAsync(errorFilePath);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer);
                await response.OutputStream.FlushAsync();
            }

            response.Close();
        }

        private async Task SendUnauthorized(HttpListenerResponse response)
        {
            response.StatusCode = 401;
            response.ContentType = "text/html; charset=utf-8";

            string errorFilePath = Path.Combine(Directory.GetCurrentDirectory(), "static", "401.html");

            if (File.Exists(errorFilePath))
            {
                byte[] buffer = await File.ReadAllBytesAsync(errorFilePath);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer);
                await response.OutputStream.FlushAsync();
            }

            response.Close();
        }
    }
}