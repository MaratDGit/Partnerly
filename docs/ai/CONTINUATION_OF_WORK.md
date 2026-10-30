# Продолжение работы

Обновлено: 2026-10-30.

## Текущее состояние

- Ветка: `Cursor-Agent` (tracking `origin/Cursor-Agent`).
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- UserPermissions / политики — `7e8173b` (на origin). Адатив четырёх страниц — `3f8568b` (на origin). User Groups send + попап — `e27dcad` (на origin).
- Дерево рефералов — коммит `f1e14ea`. Веб-инспектор — `bdc34e4`. Поиск в навбаре, Statistics+Dashboards, фото ролей — `57bf563`.
- Расширение System Settings — закоммичено на `Cursor-Agent`, **push ещё нет**. Миграция на локальной БД **применена**.
- Гостевой лендинг/чат — `bd0a764`. Account-попап — `51205de`. Privacy/Docs/dark-текст — `fd84fb8`. Кэш SystemSettings, цикл логотипа, flip auth и сборка попапа из точек — `3f1b869` на `Cursor-Agent`. **Push ещё нет.**

## Последняя задача

Имя сайта при первой загрузке пишется буква за буквой (clip-path), не fade. Попап без морфа точек, линии между точками, ближе буквы названия — в рабочем дереве, не закоммичено.

## Уже сделано недавно

- Поиск в навбаре; Statistics+Dashboards; фото ролей.
- Исполнитель тикета видит ViewCase.
- Веб-инспектор: live Chrome + extension, iframe HTML.
- Дерево рефералов, адаптив, User Groups send, профиль/email/loader.
- Лендинг: точки, логотип-ветка, чат-поиск без AI, иконки входа.

## Важные нюансы

- Секреты: user-secrets + локальный `secrets.json`. В Git Google OAuth пустой. SMTP/Google/OpenAI **не** в SystemSettings.
- Миграция `20261030074752_ExpandSystemSettingsAndUserLockout` создана и **применена** на локальной БД 2026-10-30.
- Cookie `LoginPath` = `/Account/Login` → 302 на `/?auth=login`.
- Реферал: `/Account/Register?reff=` открывает попап регистрации.
- Сброс пароля по письму (`ConfirmForgotPassword`), GoogleRegister и AccessDenied — ещё отдельные страницы.
- AI на гостевой поиск не включать, пока владелец не попросит.
- Не путать канонические файлы: журнал = `docs/ai/WORK_LOG.md`.

## Незавершённое и препятствия

- Нет автотестов.
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер в этой сессии недоступен — проверить dark: формы (Settings, System Preferences, тикет), Documentation, Privacy-попап, таблицы.
- Браузер: попап на `/` — Login, Sign up, Forgot, ошибки валидации, Google, язык/тема при открытом диалоге.
- Privacy/Docs, зум точек и dark-текст закоммичены; push — по просьбе владельца.

## Рекомендуемый следующий шаг

1. В браузере: Login/Sign up на `/` — попап fade, тексты гаснут, точки соединяются линиями; закрытие возвращает тексты; flip Login↔Register.
2. Коммит и push — по просьбе владельца.

Перед кодом снова прочитать `AGENTS.md` и этот файл.
