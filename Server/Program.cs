using Framework;
using Framework.Authentication;
using Server.Services.Authentication;
using System.Text.Json;

if (!File.Exists("settings.json"))
{
    Console.WriteLine("Ошибка: файл settings.json не найден.");
    Console.ReadLine();
    return;
}

string settingJson = await File.ReadAllTextAsync("settings.json");

Settings? setting = JsonSerializer.Deserialize<Settings>(settingJson);

if (setting == null)
{
    Console.WriteLine("Ошибка: не удалось прочитать settings.json.");
    Console.ReadLine();
    return;
}

IUserSessionService userSessionService = new UserSessionService();

HttpServer server = new HttpServer(setting, userSessionService);

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
