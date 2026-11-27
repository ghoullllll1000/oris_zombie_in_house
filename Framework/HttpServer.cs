using Framework.Authentication;
using Framework.Handlers;
using System.Net;

namespace Framework
{
    public class HttpServer
    {
        private readonly HttpListener server;
        private bool isRunning;
        private Settings settings;
        private IUserSessionService userSessionService;

        public HttpServer(Settings settings, IUserSessionService userSessionService)
        {
            server = new HttpListener();

            string uriPrefix = $"http://{settings.Host}:{settings.Port}/";

            server.Prefixes.Add(uriPrefix);

            this.settings = settings;
            this.userSessionService = userSessionService;
        }

        public async Task Start()
        {
            server.Start();
            isRunning = true;

            Console.WriteLine("Сервер запущен и слушает:");

            foreach (string prefix in server.Prefixes)
            {
                Console.WriteLine(prefix);
            }

            while (isRunning)
            {
                try
                {
                    HttpListenerContext context = await server.GetContextAsync();
                    await HandleRequest(context);
                }
                catch (HttpListenerException) when (!isRunning)
                {
                    break;
                }
                catch (ObjectDisposedException) when (!isRunning)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при получении запроса: {ex.Message}");
                }
            }

            Console.WriteLine("Сервер завершил работу.");
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            Handler staticFileHandler = new StaticFileHandler();
            Handler controllerHandler = new ControllerHandler(settings.ConnectionString, userSessionService);
            staticFileHandler.Successor = controllerHandler;

            Console.WriteLine("Получен запрос.");

            await staticFileHandler.HandleRequest(context);

            Console.WriteLine($"Запрос обработан: {context.Request.Url}");
        }

        public void Stop()
        {
            if (!isRunning)
                return;

            isRunning = false;

            server.Stop();

            Console.WriteLine("Сервер остановлен.");
        }
    }
}