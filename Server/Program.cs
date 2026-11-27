using Framework;
using Framework.Authentication;
using Server.Services.Authentication;

string? port = Environment.GetEnvironmentVariable("ZOMBIE_PORT");
if (port == null)
{
    Console.WriteLine("Переменная окружения ZOMBIE_PORT не задана, используется значение по умолчанию = '9999'");
    port = "9999";
}

string? host = Environment.GetEnvironmentVariable("ZOMBIE_HOST");
if (host == null)
{
    Console.WriteLine("Переменная окружения ZOMBIE_HOST не задана, используется значение по умолчанию = 'localhost'");
    host = "localhost";
}

string? connectionString = Environment.GetEnvironmentVariable("ZOMBIE_DB_CONNECTION_STRING");
if (connectionString == null)
{
    Console.WriteLine("Ошибка: Переменная окружения ZOMBIE_DB_CONNECTION_STRING не задана, завершение работы сервера");
    return;
}

Console.WriteLine($"ZOMBIE_PORT={port}");
Console.WriteLine($"ZOMBIE_HOST={host}");
Console.WriteLine($"ZOMBIE_DB_CONNECTION_STRING=\"{connectionString}\"");

Settings? settings = new Settings
{
    Port = port,
    Host = host,
    ConnectionString = connectionString
};

IUserSessionService userSessionService = new UserSessionService();

HttpServer server = new HttpServer(settings, userSessionService);

Task serverTask = server.Start();

Console.WriteLine("Введите stop для остановки сервера.");

while (true)
{
    string? command = Console.ReadLine();

    if (command?.Trim().Equals(
        "stop",
        StringComparison.OrdinalIgnoreCase) == true)
    {
        server.Stop();
        break;
    }
}

await serverTask;

Console.WriteLine("Программа завершена.");
