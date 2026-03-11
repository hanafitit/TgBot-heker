using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace TgBotCheker
{
    /// <summary>
    /// Проверяет подписку пользователя на каждый канал из SubscriptionConfig.
    /// Возвращает список каналов, на которые пользователь ещё НЕ подписан.
    /// </summary>
    internal static class SubscriptionChecker
    {
        /// <summary>
        /// Пустой список = все проверки пройдены, пускаем пользователя.
        /// </summary>
        public static async Task<List<RequiredChannel>> GetMissingChannels(
            ITelegramBotClient bot,
            long userId,
            CancellationToken ct = default)
        {
            var missing = new List<RequiredChannel>();

            foreach (var channel in SubscriptionConfig.Channels)
            {
                bool isMember = await IsMember(bot, userId, channel.ChannelId, ct);
                if (!isMember)
                    missing.Add(channel);
            }

            return missing;
        }

        private static async Task<bool> IsMember(
            ITelegramBotClient bot,
            long userId,
            string channelId,
            CancellationToken ct)
        {
            try
            {
                var member = await bot.GetChatMember(channelId, userId, ct);

                return member.Status is
                    ChatMemberStatus.Member or
                    ChatMemberStatus.Administrator or
                    ChatMemberStatus.Creator;
            }
            catch (Exception ex)
            {
                // Если канал недоступен боту — считаем что не подписан.
                // Убедитесь что бот является администратором каждого канала!
                Console.Error.WriteLine(
                    $"[SubCheck] Не удалось проверить {channelId} для user={userId}: {ex.Message}");
                return false;
            }
        }
    }
}
