using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;

namespace TgBotCheker
{
    internal static class Program
    {
        private static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("Остановка...");
                cts.Cancel();
            };

            try
            {
                var token = Environment.GetEnvironmentVariable("BOT_TOKEN")
                    ?? throw new InvalidOperationException("BOT_TOKEN не задан");

                var bot     = new TelegramBotClient(token);
                var handler = new BotHandler(bot);

                var me = await bot.GetMe(cts.Token);
                Console.WriteLine($"Бот @{me.Username} запущен.");

                bot.StartReceiving(
                    updateHandler: handler.HandleUpdateAsync,
                    errorHandler:  (_, ex, source, ct) => HandlePollingError(ex, source, ct),
                    receiverOptions: new ReceiverOptions { AllowedUpdates = [] },
                    cancellationToken: cts.Token
                );

                _ = RunWebServer(cts.Token);
                _ = RunSelfPing(cts.Token);

                Console.WriteLine("Для остановки нажмите Ctrl+C.");
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Бот остановлен.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static async Task HandlePollingError(
            Exception exception,
            HandleErrorSource source,
            CancellationToken ct)
        {
            if (exception is ApiRequestException { Message: var msg } && msg.Contains("Conflict"))
            {
                Console.WriteLine("[Конфликт] Другой экземпляр активен. Ожидание 10 с...");
                await Task.Delay(10_000, ct);
            }
            else
            {
                Console.WriteLine($"[ОШИБКА POLLING] {exception.Message} (источник: {source})");
            }
        }

        private static async Task RunWebServer(CancellationToken ct)
        {
            var port     = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{port}/");
            listener.Start();
            Console.WriteLine($"Веб-сервер запущен на порту {port}.");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var context  = await listener.GetContextAsync();
                    var response = context.Response;
                    var body     = Encoding.UTF8.GetBytes("OK");
                    response.ContentLength64 = body.Length;
                    await response.OutputStream.WriteAsync(body, ct);
                    response.OutputStream.Close();
                }
                catch (OperationCanceledException) { break; }
                catch { /* игнорируем разовые ошибки соединения */ }
            }

            listener.Stop();
        }
        private static async Task RunSelfPing(CancellationToken ct)
        {
            var port    = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            var url     = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL")
                          ?? $"http://localhost:{port}";
            var client  = new System.Net.Http.HttpClient();

            // Ждём пока веб-сервер поднимется
            await Task.Delay(5_000, ct);

            Console.WriteLine($"Self-ping запущен → {url}");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await client.GetAsync(url, ct);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PING] OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PING] Ошибка: {ex.Message}");
                }

                // Пинг каждые 10 минут — Render засыпает после 15
                await Task.Delay(TimeSpan.FromMinutes(10), ct);
            }
        }
    }
}
