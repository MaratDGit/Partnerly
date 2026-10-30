# Продолжение работы

Обновлено: 2026-10-30.

## Текущее состояние

- Ветка: `Cursor-Agent` (tracking `origin/Cursor-Agent`).
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- UserPermissions / политики — `7e8173b` (на origin). Адатив четырёх страниц — `3f8568b` (на origin). User Groups send + попап — `e27dcad` (на origin).
- Дерево рефералов — коммит `f1e14ea`. Веб-инспектор — `bdc34e4`. Поиск в навбаре, Statistics+Dashboards, фото ролей — `57bf563`.
- Расширение System Settings — закоммичено на `Cursor-Agent`, **push ещё нет**. Миграция на локальной БД **применена**.

## Последняя задача

Коммит пакета System Settings (настройки, lookup ролей/auto-assign, timezone DataTable). Push владелец не просил.

## Уже сделано недавно

- Поиск в навбаре; Statistics+Dashboards; фото ролей.
- Исполнитель тикета видит ViewCase.
- Веб-инспектор: live Chrome + extension, iframe HTML.
- Дерево рефералов, адаптив, User Groups send, профиль/email/loader.

## Важные нюансы

- Секреты: user-secrets + локальный `secrets.json`. В Git Google OAuth пустой. SMTP/Google/OpenAI **не** в SystemSettings.
- Миграция `20261030074752_ExpandSystemSettingsAndUserLockout` создана и **применена** на локальной БД 2026-10-30. Коммит сделан 2026-10-30; push ещё нет.
- Автоназначение тикетов на Super по email больше не работает, пока в настройках не выбран пользователь или роль.
- Null в новых bool трактуется как прежнее поведение (вкл.), кроме admin-писем (выкл.) и auto-assign (нет назначения).
- Язык: cookie важнее настройки; настройка важнее Accept-Language.
- Часовой пояс по умолчанию `Asia/Yerevan`. DataTable колонки и Date/DateRange-фильтры, Insights, экспорт и навбар читают его. Relative «N минут назад» — длительность, без пояса; старше 7 дней — календарная дата в поясе настроек.
- Права: политики на CRUD; видимость — `IUserVisibilityService`. Тикет: автор из видимых **или** `AssignedTo`.
- БД: не запускать миграции без разрешения. Seeder **не удаляет** лишние RolePermissions на уже заполненной БД.
- Не путать канонические файлы: журнал = `docs/ai/WORK_LOG.md`.

## Незавершённое и препятствия

- Нет автотестов.
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер в этой сессии недоступен — System Settings и перевязанные сценарии (Google off, lockout, вложения, timezone) нужно проверить после рестарта приложения.
- Push этой задачи владелец не просил.

## Рекомендуемый следующий шаг

1. Открыть System Settings, задать автоназначение тикетов и при необходимости остальные значения.
2. Проверить вход, Google, письма, тикеты, вложения, AI и даты в списках.
3. Push по просьбе владельца.

Перед кодом снова прочитать `AGENTS.md` и этот файл.
