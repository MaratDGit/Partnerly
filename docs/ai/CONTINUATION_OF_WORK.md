# Продолжение работы

Обновлено: 2026-10-30.

## Текущее состояние

- Ветка: `Cursor-Agent` (tracking `origin/Cursor-Agent`).
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- UserPermissions / политики — `7e8173b` (на origin). Адатив четырёх страниц — `3f8568b` (на origin). User Groups send + попап — `e27dcad` (на origin).
- Дерево рефералов — коммит `f1e14ea`. Веб-инспектор — `bdc34e4`. Поиск в навбаре, Statistics+Dashboards, фото ролей — `57bf563`.
- Расширение System Settings — закоммичено на `Cursor-Agent`, **push ещё нет**. Миграция на локальной БД **применена**.
- Гостевой лендинг/чат — коммит `bd0a764`. Account-попап и очистка неиспользуемых views/ключей — **не закоммичены**.

## Последняя задача

На странице сброса пароля (`ConfirmForgotPassword` / ChangePassword) глаз не переключал видимость — не было `password-toggle-icon`. Исправлено. Коммит/push по-прежнему по просьбе владельца.

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
- Браузер в этой сессии недоступен — проверить попап на `/`: Login, Sign up, Forgot, ошибки валидации, Google, язык/тема при открытом диалоге.
- Account-попап и, возможно, System Settings — без push, пока владелец не попросит.

## Рекомендуемый следующий шаг

1. Открыть `/` без сессии: Login/Sign up как попап, стиль стекла как у поиска.
2. Проверить `/?auth=login`, регистрацию, forgot, реферал `reff`.
3. Коммит/push по просьбе владельца.

Перед кодом снова прочитать `AGENTS.md` и этот файл.
