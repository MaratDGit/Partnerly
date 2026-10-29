# Продолжение работы

Обновлено: 2026-10-28.

## Текущее состояние

- Ветка: `Cursor-Agent` (tracking `origin/Cursor-Agent`).
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- UserPermissions / политики — `7e8173b` (на origin). Адатив четырёх страниц — `3f8568b` (на origin). User Groups send + попап — `e27dcad` (на origin).
- Дерево рефералов — коммит `f1e14ea`. Веб-инспектор (iframe HTML) — `bdc34e4`. Модульные дашборды — незакоммичены.

## Последняя задача

Страница `/Dashboard/Dashboards`: KPI users/tasks/roles/groups/notifications, меню сразу под Statistics. Журнал и email-шаблоны на экране нет. Счётчики по `VisibleUserIds()`, секции по существующим `*.view`. Index с играми не трогался.

## Уже сделано недавно

- Веб-инспектор: live Chrome + extension, iframe HTML, вкладки, KPI-карточки.
- Дерево рефералов, адаптив, User Groups send, профиль/email/loader.

## Важные нюансы

- Секреты: user-secrets + локальный `secrets.json`. В Git Google OAuth пустой.
- Права: политики на CRUD; видимость — `IUserVisibilityService`.
- Дашборды: отдельного permission нет; Tasks = SupportTickets. Класс страницы `dashboards-page`, чтобы layout не прятал скролл как у inspector.
- Дерево рефералов: корень = сам / CanViewUser / своя downline; Super скрыт если смотрящий не Super.
- DataTable: id таблиц уникальны; тикеты — `supportTicketsTable`.
- БД: не запускать миграции без разрешения. Seeder **не удаляет** лишние RolePermissions на уже заполненной БД.
- Не путать канонические файлы: журнал = `docs/ai/WORK_LOG.md`.

## Незавершённое и препятствия

- Нет автотестов.
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер: дашборды в этой сессии не открывались (нет browser tools).
- Приложение могло быть запущено (сборка шла в `%TEMP%`).
- Дашборды и, возможно, более ранние правки — не закоммичены. Push не просили.

## Рекомендуемый следующий шаг

1. Проверить `/Dashboard/Dashboards` в light и dark, вкладки и ссылки «Открыть список».
2. Коммит/push — только если попросит владелец.

Перед кодом снова прочитать `AGENTS.md` и этот файл.
