# Продолжение работы

Обновлено: 2026-10-29.

## Текущее состояние

- Ветка: `Cursor-Agent` (tracking `origin/Cursor-Agent`).
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- UserPermissions / политики — `7e8173b` (на origin). Адатив четырёх страниц — `3f8568b` (на origin). User Groups send + попап — `e27dcad` (на origin).
- Дерево рефералов — коммит `f1e14ea`. Веб-инспектор — `bdc34e4`. Поиск в навбаре, Statistics+Dashboards, фото ролей — в текущем коммите.

## Последняя задача

Statistics (`/Dashboard`) объединена с Dashboards: вкладка Games первая, затем Users/Tasks/Roles/Groups/Notifications. Меню Dashboards снято. В Roles — фото в Users by role (макс. 5, tooltip имя+email) и карточка Permissions by role.

## Уже сделано недавно

- Исполнитель тикета видит ViewCase.
- Модульные дашборды под Statistics.
- Веб-инспектор: live Chrome + extension, iframe HTML.
- Дерево рефералов, адаптив, User Groups send, профиль/email/loader.

## Важные нюансы

- Секреты: user-secrets + локальный `secrets.json`. В Git Google OAuth пустой.
- Права: политики на CRUD; видимость — `IUserVisibilityService`. Тикет: автор из видимых **или** `AssignedTo`.
- Statistics = Games + модульные вкладки; отдельного permission нет; Tasks = SupportTickets. Класс `dashboards-page` на `/Dashboard`.
- Дерево рефералов: корень = сам / CanViewUser / своя downline; Super скрыт если смотрящий не Super.
- DataTable: id таблиц уникальны; тикеты — `supportTicketsTable`. Fit-columns пишет ширины в тот же localStorage, что и drag-resize.
- БД: не запускать миграции без разрешения. Seeder **не удаляет** лишние RolePermissions на уже заполненной БД.
- Не путать канонические файлы: журнал = `docs/ai/WORK_LOG.md`.

## Незавершённое и препятствия

- Нет автотестов.
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер в этой сессии недоступен — объединённую Statistics и поиск в навбаре нужно проверить после рестарта приложения.
- Приложение могло быть запущено (сборка шла в `%TEMP%`).

## Рекомендуемый следующий шаг

1. После рестарта проверить Statistics (Games первая, Roles фото/tooltip) и поиск в навбаре на десктопе/планшете.

Перед кодом снова прочитать `AGENTS.md` и этот файл.
