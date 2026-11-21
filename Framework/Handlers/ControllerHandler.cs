using Framework.Attributes;
using Framework.Controllers;
using Framework.Core;
using Framework.HttpResponses;
using System.Net;
using System.Reflection;
using System.Web;
using System.Threading.Tasks;

namespace Framework.Handlers
{
    internal class ControllerHandler : Handler
    {
        private string connectionString = string.Empty;
        public ControllerHandler(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public override async Task HandleRequest(HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            HttpListenerRequest request = context.Request;

            string[] pathParts = request.Url!.LocalPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (pathParts.Length >= 2)
            {
                string controllerName = pathParts[0];
                string methodName = pathParts[1];

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

                if (controllerType != null)
                {
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

                    if (targetMethod != null)
                    {
                        
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

                        ControllerBase controllerInstance = Activator.CreateInstance(controllerType)! as ControllerBase;
                        controllerInstance.SetContext(context);
                        controllerInstance.SetConnectionString(connectionString);

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
                            if (result is RedirectResult)
                            {
                                context.Response.Close();
                            }
                            else
                            {
                                await HttpResponseWriter.WriteBytesAsync(context.Response, buffer);
                                context.Response.Close();
                            }
                        }
                        else if (result is string fileName)
                        {
                            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "static", fileName);
                            if (File.Exists(filePath))
                            {
                                byte[] bytes = await File.ReadAllBytesAsync(filePath);
                                context.Response.ContentType = "text/html; charset=utf-8";
                                await HttpResponseWriter.WriteBytesAsync(context.Response, bytes, 200);
                            }
                            else
                            {
                                await HttpResponseWriter.WriteHtmlAsync(context.Response, fileName, 200);
                            }
                        }
                        else
                        {
                            string html = result?.ToString() ?? "<h1>Метод контроллера выполнен</h1>";
                            await HttpResponseWriter.WriteHtmlAsync(context.Response, html, 200);
                        }

                        context.Response.Close();
                        return;
                    }
                    else
                    {
                        await SendNotFound(response);
                        return;
                    }
                }
                else
                {
                    await SendNotFound(response);
                    return;
                }
            }

            if (Successor != null)
            {
                await Successor.HandleRequest(context);
            }
            else
            {
                await SendNotFound(response);
                return;
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
    }
}