namespace TgBotCheker
{
    /// <summary>
    /// Один обязательный канал.
    /// </summary>
    public sealed class RequiredChannel
    {
        /// <summary>
        /// Username (@handle) для публичного канала
        /// ИЛИ числовой ID вида -100XXXXXXXXXX для приватного.
        /// Бот должен быть администратором канала.
        /// </summary>
        public required string ChannelId { get; init; }

        /// <summary>
        /// Название канала для отображения пользователю.
        /// </summary>
        public required string DisplayName { get; init; }

        /// <summary>
        /// Ссылка для вступления (invite link или t.me/username).
        /// </summary>
        public required string InviteLink { get; init; }
    }

    /// <summary>
    /// ЕДИНСТВЕННОЕ место для настройки бота.
    /// Добавляйте или убирайте каналы — всё остальное подстраивается автоматически.
    /// Работает с любым количеством каналов: хоть 1, хоть 10.
    /// </summary>
    public static class SubscriptionConfig
    {
        public static readonly RequiredChannel[] Channels =
        [
            new()
            {
                ChannelId   = "@your_channel_1",         // ← заменить на свой
                DisplayName = "Канал 1",
                InviteLink  = "https://t.me/your_channel_1",
            },
            new()
            {
                ChannelId   = "@your_channel_2",         // ← заменить на свой
                DisplayName = "Канал 2",
                InviteLink  = "https://t.me/+Eq4SDuc_uBk0N2Qy",
            },
            new()
            {
                ChannelId   = "@your_channel_3",         // ← заменить на свой
                DisplayName = "Канал 3",
                InviteLink  = "https://t.me/+vLQ_XNbiMeRlM2Fi",
            },

            // Чтобы добавить ещё канал — скопируйте блок ниже:
            // new()
            // {
            //     ChannelId   = "@your_channel_4",
            //     DisplayName = "Канал 4",
            //     InviteLink  = "https://t.me/+XXXXXXXXXX",
            // },
        ];

        /// <summary>
        /// Текст после успешной проверки подписки.
        /// </summary>
        public const string WelcomeMessage =
            "✅ Отлично! Вы подписались на все каналы.\n\n" +
            "Добро пожаловать! Теперь у вас есть полный доступ.";
    }
}
