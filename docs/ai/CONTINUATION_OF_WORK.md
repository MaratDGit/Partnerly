# Продолжение работы

Обновлено: 2026-10-29.

## Текущее состояние

- Ветка: `Cursor-Agent` (tracking `origin/Cursor-Agent`).
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- UserPermissions / политики — `7e8173b` (на origin). Адатив четырёх страниц — `3f8568b` (на origin). User Groups send + попап — `e27dcad` (на origin).
- Дерево рефералов — коммит `f1e14ea`. Веб-инспектор (iframe HTML) — `bdc34e4`. Дашборды, assignee тикета, заметки/fit-columns — незакоммичены.

## Последняя задача

OK в попапе заметки закрывает модалку. В DataTable справа от фильтра — подгон колонок под заголовок; колонка кнопок (`__actions`) подгоняется по всем кнопкам в строке, не по тексту заголовка.

## Уже сделано недавно

- Исполнитель тикета видит ViewCase.
- Модульные дашборды под Statistics.
- Веб-инспектор: live Chrome + extension, iframe HTML.
- Дерево рефералов, адаптив, User Groups send, профиль/email/loader.

## Важные нюансы

- Секреты: user-secrets + локальный `secrets.json`. В Git Google OAuth пустой.
- Права: политики на CRUD; видимость — `IUserVisibilityService`. Тикет: автор из видимых **или** `AssignedTo`.
- Дашборды: отдельного permission нет; Tasks = SupportTickets. Класс страницы `dashboards-page`, чтобы layout не прятал скролл как у inspector.
- Дерево рефералов: корень = сам / CanViewUser / своя downline; Super скрыт если смотрящий не Super.
- DataTable: id таблиц уникальны; тикеты — `supportTicketsTable`. Fit-columns пишет ширины в тот же localStorage, что и drag-resize.
- БД: не запускать миграции без разрешения. Seeder **не удаляет** лишние RolePermissions на уже заполненной БД.
- Не путать канонические файлы: журнал = `docs/ai/WORK_LOG.md`.

## Незавершённое и препятствия

- Нет автотестов.
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер в этой сессии недоступен — заметки/fit-columns и дашборды нужно проверить после рестарта приложения.
- Приложение могло быть запущено (сборка шла в `%TEMP%`).
- Дашборды, assignee, заметки/fit — не закоммичены. Push не просили.

## Рекомендуемый следующий шаг

1. Проверить попап заметки (OK закрывает) на форме Users/Roles и кнопку подгона колонок на любом списке DataTable.
2. Коммит/push — только если попросит владелец.

Перед кодом снова прочитать `AGENTS.md` и этот файл.
