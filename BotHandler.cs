using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TgBotCheker
{
    internal sealed class BotHandler
    {
        private readonly ITelegramBotClient _bot;

        private const string BtnCheckSub = "✅ Я подписался на все каналы";

        public BotHandler(ITelegramBotClient bot)
        {
            _bot = bot;
        }

        // ─── Точка входа ──────────────────────────────────────────────────────

        public async Task HandleUpdateAsync(
            ITelegramBotClient _,
            Update update,
            CancellationToken ct)
        {
            if (update.Message is { } msg)
                await OnMessage(msg, ct);
            else
                Console.Error.WriteLine($"[{Now}] [IGNORED] тип обновления: {update.Type}");
        }

        // ─── Обработка сообщений ──────────────────────────────────────────────

        private async Task OnMessage(Message msg, CancellationToken ct)
        {
            if (msg.Text is null) return;

            var text   = msg.Text.Trim();
            var chatId = msg.Chat.Id;
            var userId = msg.From?.Id ?? chatId;

            Log($"[MSG] user={userId} text=\"{text}\"");

            if (text == "/start")
            {
                await HandleStart(chatId, userId, ct);
                return;
            }

            if (text == BtnCheckSub)
            {
                await HandleCheckSubscription(chatId, userId, ct);
                return;
            }

            // Любое другое сообщение — напоминаем подписаться
            await HandleStart(chatId, userId, ct);
        }

        // ─── /start ───────────────────────────────────────────────────────────

        private async Task HandleStart(long chatId, long userId, CancellationToken ct)
        {
            var missing = await SubscriptionChecker.GetMissingChannels(_bot, userId, ct);

            if (missing.Count == 0)
            {
                await SendWelcome(chatId, ct);
                return;
            }

            await SendSubscriptionRequest(chatId, missing, ct);
        }

        // ─── Проверка по кнопке ───────────────────────────────────────────────

        private async Task HandleCheckSubscription(long chatId, long userId, CancellationToken ct)
        {
            Log($"[CHECK] user={userId}");

            var missing = await SubscriptionChecker.GetMissingChannels(_bot, userId, ct);

            if (missing.Count == 0)
            {
                Log($"[ACCESS_GRANTED] user={userId}");
                await SendWelcome(chatId, ct);
                return;
            }

            // Показываем только те каналы, которых ещё не хватает
            Log($"[ACCESS_DENIED] user={userId} осталось={missing.Count}");
            await SendSubscriptionRequest(chatId, missing, ct,
                header: $"❌ Вы ещё не подписались на {PluralChannels(missing.Count)}:\n");
        }

        // ─── Отправка сообщений ───────────────────────────────────────────────

        private async Task SendSubscriptionRequest(
            long chatId,
            List<RequiredChannel> channels,
            CancellationToken ct,
            string? header = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine(header ??
                $"Чтобы получить доступ, подпишитесь на {PluralChannels(channels.Count)} ниже:");
            sb.AppendLine("👇👇👇");
            sb.AppendLine();

            for (int i = 0; i < channels.Count; i++)
                sb.AppendLine($"{i + 1}. {channels[i].DisplayName} — {channels[i].InviteLink}");

            sb.AppendLine();
            sb.Append($"✅ После подписки нажмите кнопку «{BtnCheckSub}» ниже.");

            await _bot.SendMessage(
                chatId,
                sb.ToString(),
                replyMarkup: CheckButton(),
                cancellationToken: ct);
        }

        private async Task SendWelcome(long chatId, CancellationToken ct)
        {
            await _bot.SendMessage(
                chatId,
                SubscriptionConfig.WelcomeMessage,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct);
        }

        // ─── Клавиатура ───────────────────────────────────────────────────────

        private static ReplyKeyboardMarkup CheckButton() =>
            new(new[] { new[] { new KeyboardButton(BtnCheckSub) } })
            {
                ResizeKeyboard = true,
            };

        // ─── Вспомогательные ──────────────────────────────────────────────────

        private static string PluralChannels(int count) => count switch
        {
            1 => "1 канал",
            2 or 3 or 4 => $"{count} канала",
            _ => $"{count} каналов",
        };

        private static void Log(string line) =>
            Console.WriteLine($"[{Now}] {line}");

        private static string Now =>
            DateTime.Now.ToString("HH:mm:ss");
    }
}
