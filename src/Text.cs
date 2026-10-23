using System.Globalization;

namespace Nowcast
{
    // Interface strings: Russian on a Russian Windows, English elsewhere.
    static class Text
    {
        public const string AppName = "PulsePresence";

        public static readonly bool Ru = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";

        static string T(string ru, string en)
        {
            return Ru ? ru : en;
        }

        public static readonly string AlreadyRunning = T(
            "PulsePresence уже работает: его значок находится в трее (стрелка ^ в правом нижнем углу экрана у часов, нажмите на неё чтобы развернуть значки).",
            "PulsePresence is already running: its icon is in the tray (arrow ^ in the bottom right corner of the screen by the clock, click it to expand).");
        public static readonly string Running = T(
            "Программа запущена и работает в трее (стрелка ^ в правом нижнем углу экрана у часов, нажмите на неё чтобы развернуть значки).\n\nНажмите на значок PulsePresence для открытия настроек.",
            "PulsePresence is running in the system tray (arrow ^ in the bottom right corner of the screen by the clock, click it to expand icons).\n\nClick the PulsePresence icon to open settings.");
        public static readonly string Updated = T("Обновлено до версии {0}.", "Updated to version {0}.");

        public static readonly string NothingPlaying = T("Ничего не играет", "Nothing is playing");
        public static readonly string DiscordOff = T("Discord не запущен", "Discord is not running");
        public static readonly string HiddenUntil = T("Статус скрыт до {0:HH:mm}", "Status hidden until {0:t}");
        public static readonly string Starting = T("Запускается…", "Starting…");

        public static readonly string HideHour = T("Скрыть статус на час", "Hide status for an hour");
        public static readonly string ShowAgain = T("Снова показывать статус", "Show status again");
        public static readonly string Sources = T("Источники в статусе", "Sources in the status");
        public static readonly string WhatWindowsSees = T("Что видит Windows", "What Windows sees");
        public static readonly string NoSessions = T("Ни одно приложение не сообщает о треке", "No app reports a track");
        public static readonly string Playing = T("играет", "playing");
        public static readonly string Paused = T("пауза", "paused");
        public static readonly string Autostart = T("Запускать вместе с Windows", "Start with Windows");
        public static readonly string DesktopShortcut = T("Создать ярлык на рабочем столе", "Create desktop shortcut");
        public static readonly string ShortcutCreated = T("Ярлык создан на рабочем столе", "Desktop shortcut created");
        public static readonly string Settings = T("Настройки…", "Settings…");

        // The settings window
        public static readonly string StatusHeading = T("Статус в Discord", "Discord Status");
        public static readonly string InListArtistTitle = T("Исполнитель — Песня", "Artist — Track");
        public static readonly string InListTitleArtist = T("Песня — Исполнитель", "Track — Artist");
        public static readonly string InListSource = T("Откуда играет: SoundCloud, Telegram…", "Where it plays: SoundCloud, Telegram…");
        public static readonly string InListArtist = T("Исполнитель", "The artist");
        public static readonly string InListTitle = T("Название трека", "The track title");
        public static readonly string InListApp = T("Название приложения Discord", "Discord application name");
        public static readonly string ActivityTypeLabel = T("Тип активности в профиле:", "Profile activity type:");
        public static readonly string ActivityListening = T("Слушает (Listening to…)", "Listening to…");
        public static readonly string ActivityPlaying = T("Играет в (Playing…)", "Playing…");
        public static readonly string ActivityWatching = T("Смотрит (Watching…)", "Watching…");
        public static readonly string CoversOption = T("Обложка трека в статусе", "Cover art in status");
        public static readonly string UploadCoversOption = T("Загрузка обложек в сеть", "Upload covers online");
        public static readonly string ButtonsOption = T("Кнопка «Слушать на …»", "\"Listen on …\" button");
        public static readonly string ShowPausedOption = T("Не убирать статус на паузе", "Keep status when paused");
        public static readonly string SourcesNote = T("Приложения, которые сейчас играют. Снимите галочку, чтобы скрыть из статуса.", "Apps playing now. Uncheck to hide from status.");
        public static readonly string ProgramHeading = T("Параметры программы", "Program Preferences");
        public static readonly string CheckUpdatesOption = T("Проверять обновления", "Check for updates");
        public static readonly string Advanced = T("Конфигурация (config.json)", "Configuration (config.json)");
        public static readonly string Close = T("Закрыть", "Close");
        public static readonly string OpenLog = T("Открыть лог", "Open log");
        public static readonly string CheckUpdates = T("Проверить обновления", "Check for updates");
        public static readonly string UpdateTo = T("Обновить до {0}", "Update to {0}");
        public static readonly string UpToDate = T("У вас последняя версия, {0}.", "You have the latest version, {0}.");
        public static readonly string UpdateAvailable = T("Вышла версия {0}. Нажмите, чтобы обновить.", "Version {0} is out. Click to update.");
        public static readonly string UpdateFailed = T("Не получилось: {0}", "Update failed: {0}");
        public static readonly string GitHub = T("Страница на GitHub", "GitHub Repository");
        public static readonly string Exit = T("Выход", "Exit");

        public static readonly string LivePreview = T("Живое превью активности", "Live Activity Preview");
        public static readonly string PreviewSampleTitle = T("Название трека", "Track Title");
        public static readonly string PreviewSampleArtist = T("Исполнитель", "Artist Name");
        public static readonly string PreviewSampleSource = T("Плеер", "Music Player");

        public static readonly string ListenOn = T("Слушать на {0}", "Listen on {0}");
        public static readonly string FindOnYouTube = T("Найти на YouTube", "Find on YouTube");
    }
}
