# PulsePresence

Показывает в статусе Discord («Слушает …», «Играет в …», «Смотрит …», activity type 2/0/3) трек, который играет в любом приложении Windows. Один `PulsePresence.exe` со значком в трее, темным меню и окном настроек в стиле Discord Dark Theme с живым превью карточки.

## Как устроено (src/)

- `Program.cs`: версия (`Version = 2.0.0`), один экземпляр (мьютекс `Local\PulsePresence`, брошенный забирается; после обновления ждём старую копию до 15 с).
- `Engine.cs`: отдельный STA-поток, раз в секунду `Media.Read` → `Pick` → `Activity.Build` → `Discord.SetActivity`. Отправка только при изменении и не чаще раза в 4 с (лимит Discord 5/20 с). Перечитывает `config.json` по FileSystemWatcher.
- `WinRT.cs`, `Media.cs`: медиасессии через рефлексию по типам `..., ContentType=WindowsRuntime`, потому что встроенный csc не читает Windows.winmd (его нет без Windows SDK). Ожидание операций через `WindowsRuntimeSystemExtensions.AsTask`, поток обложки через `AsStreamForRead`, оба вызываются рефлексией.
- `Sources.cs`: имя источника (`names` из настроек → имя из меню «Пуск» через Shell.Application для `_crx_` и `!` → правила) и ссылка поиска для кнопки.
- `Covers.cs`: асинхронная неблокирующая выгрузка обложек на litterbox.catbox.moe (72h) без зависания основного цикла движка. Поддерживает отключение выгрузки (`uploadCovers: false`) для полной приватности.
- `Activity.cs`: строки, `status_display_type`, тип активности (`activityType`), обложка, полоска, кнопка «Слушать на …».
- `Discord.cs`: `\\.\pipe\discord-ipc-N`, кадр op/длина/JSON, ping→pong. `IsAlive` через PeekNamedPipe: иначе перезапуск Discord не виден, пока не сменится трек.
- `TrayApp.cs`: NotifyIcon и темное контекстное меню с кастомным рендерером `DarkMenuRenderer`; `SettingsForm.cs`: современное темное окно настроек с DWM Immersive Dark Mode и живым Discord Live Preview; `Startup.cs`: чистый автозапуск через реестр HKCU Run, без WMI-манипуляций и без мусора на Рабочем столе; `Updater.cs`: GitHub releases/latest, замена exe через переименование в `.old`.
- `Text.cs`: строки по-русски на русской Windows, иначе по-английски. Лог (`%APPDATA%\PulsePresence\log.txt`) по-английски.

## Важно

- Код на C# 5: `build.ps1` собирает встроенным `Framework64\v4.0.30319\csc.exe`. Нельзя `$""`, `?.`, `nameof`, `=>`-члены, инициализаторы автосвойств, `out var`.
- Иконка `assets/icon.ico`: многослойный значок со звуковой волной и плеером, маленькие размеры — классические DIB для совместимости с System.Drawing.Icon.
- README.md на английском, README.ru.md на русском.

