# Отчёт: изменения Partnerly на ветке Cursor-Agent

## Метаданные

- **Тип:** проект (работы AI-агента с владельцем)
- **Период:** с 2026-10-25 по 2026-10-26
- **Ветка Git:** `Cursor-Agent` (tracking `origin/Cursor-Agent`)
- **HEAD:** `3f8568b` — Fix sidebar flash and fit account pages on short desktops.
- **Автор отчёта:** AI-агент по запросу владельца
- **Источники:** `WORK_LOG.md`, `TECHNICAL_SOLUTIONS.md`, `KNOWN_ISSUES.md`, git log/stat, предыдущий отчёт `docs/reports/2026-10-25-pervonachalnyj-analiz.md`
- **Дата составления:** 2026-10-26 (Asia/Yerevan)

Период **до 2026-10-25** в журнале не реконструировался. Старый DataTable, dark mode, Google OAuth и прочее на ветке — работа до этих сессий; в этом отчёте не расписывается построчно.

## Краткое резюме

На ветке `Cursor-Agent` за 25–26 октября сделаны пять коммитов агента: секреты убраны из Git, появилась постоянная документация `docs/ai/`, почищен мёртвый код и i18n, права доведены до политик + DataTable/StickyButtons + видимость по ролям, экран AccessControl удалён, затем починены сайдбар и адаптив Dashboard / Profile / Settings / Support (включая обрезку на 1280×800).

Сборки `dotnet build Partnerly.sln` в `%TEMP%` для блоков прав и чистки — 0 ошибок. CSS/layout в браузере и на живых ролях **не проверялись**. Автотестов в решении нет. Схема БД не менялась, миграции не запускались.

На `origin/Cursor-Agent` запушены коммиты до `7e8173b`. Коммит `3f8568b` (UI/адаптив) локально, **не запушен**.

## Коммиты агента

| Хеш | Дата (локальное время git) | Сообщение | На origin |
|-----|----------------------------|-----------|-----------|
| `db8244d` | 2026-10-25 11:54 | Keep API keys out of git and load local secrets.json optionally. | да |
| `4f71f30` | 2026-10-25 12:48 | Add persistent AI agent docs so project context survives new sessions. | да |
| `6b4b973` | 2026-10-25 13:37 | Remove unused code and stop tracking user uploads. | да |
| `7e8173b` | 2026-10-25 15:46 | Enforce role and user permissions across screens and drop AccessControl. | да |
| `3f8568b` | 2026-10-26 15:49 | Fix sidebar flash and fit account pages on short desktops. | **нет** (ветка ahead 1) |

Перед `db8244d` на той же ветке есть `30c622d` (2026-10-25 11:25, «added the permissions part -1») — это не запись журнала агента; локальный `Belote-Dev` указывает на другой хеш того же сообщения (`a8a20fa`) из‑за rewrite истории секретов.

## Завершённые задачи

### 2026-10-25 — Ветка, секреты, push

- **Результат:** ветка от `Belote-Dev`; из незапушенной истории убраны OpenAI/Google ключи; `secrets.json` в `.gitignore`; optional `AddJsonFile("secrets.json")`; Google OAuth в `appsettings.json` — пустые плейсхолдеры; `git push -u origin Cursor-Agent` прошёл.
- **Проверка:** сборка в той сессии после rewrite не запускалась.
- **Журнал:** WORK_LOG «Ветка Cursor-Agent, очистка секретов, push». TR-2026-10-25-02.

### 2026-10-25 — Документация агента

- **Результат:** `AGENTS.md`, `.cursor/rules/`, `docs/ai/*`, шаблон отчётов, первый отчёт.
- **Проверка:** сборка не запускалась (только markdown).
- **Журнал:** WORK_LOG «Постоянная память AI-агента». TR-2026-10-25-01.

### 2026-10-25 — Чистка кода, i18n, uploads вне Git

- **Результат:** удалены `ClaimAuthorizeAttribute`, `RoleTypeAttribute`, `_ValidationScriptsPartial`, jquery-validation; сняты EPPlus и datatables.net-dt; правки en/ru/hy (смысл + дубли ключей); `uploads` не трекаются, файлы на диске не удалялись; сущности Payment/Transaction оставлены.
- **Проверка:** `dotnet build` в temp — 0 ошибок. Сборка в `bin/` падала из‑за занятого `Partnerly.exe`. Браузер не проверялся.
- **Журнал:** WORK_LOG «Чистка…», «Правка локализации…», «Смысл текстов…». Закрыты KI-006, KI-007.

### 2026-10-25 — Права, видимость, AccessControl снят

- **Результат:** `PermissionCodes` + seeder Super/Admin = все коды; `IUserVisibilityService` (Super видит всех и скрыт; Admin не видит других Admin и Super; Employee не видит Admin/Super; User — себя); политики на Users/Roles/UserGroups/Tickets/Notifications/EmailTemplates/Logs/SystemPreferences/Attachments/AI; UserPermissions DataTable на `UserRoleEdit`; AccessControl-контроллер/сервис/views удалены, таблица аудита оставлена; уведомление смены статуса тикета — получатель `UserId ?? CreatedBy`; DataTable QueryEntity больше не ломает Logs/Notifications/Attachments.
- **Проверка:** `dotnet build` в temp — 0 ошибок, ~97–98 предупреждений. Браузер и живой seeder не проверялись.
- **Журнал:** WORK_LOG блоки UserPermissions, AccessControl, политики, Super скрыт, loading error, уведомление тикета. KI-004, KI-005, KI-013 закрыты в коде.

### 2026-10-25 — DataTable и StickyButtons по правам

- **Результат:** `Permission` на действиях/колонках/export DataTable; фильтр в `_DataTable.cshtml` по `ViewData["Permissions"]` без мутации singleton registry; то же для `FormActionBase.Permission` и `_StickyButtons.cshtml`.
- **Проверка:** DataTable — temp build 0 ошибок, 97 предупреждений; StickyButtons — temp build 0 ошибок. Браузер не проверялся.
- **Журнал:** WORK_LOG «DataTable: кнопки…», «StickyButtons по правам». Вошло в `7e8173b`.

### 2026-10-25 / 2026-10-26 — Сайдбар и адаптив четырёх страниц

- **Результат:** FOUC свёрнутого меню (класс из localStorage сразу после `<body>`); иконка Support в collapsed больше не съезжает влево; Bootstrap-сетка и брейкпоинты Dashboard/Profile/Settings/Support; графики Dashboard слушают `partnerly-layout-change`; повторный CSS (перенос текста, карточки команды, подписи вкладок Settings); на 1280×800 скролл у страниц без `.card` и компактные отступы/графики.
- **Проверка:** сборка не запускалась (CSS/разметка/JS). Браузер не проверялся (в т.ч. Nest Hub Max).
- **Журнал:** WORK_LOG записи 2026-10-25 адаптив/меню и 2026-10-26 Nest Hub Max. Коммит `3f8568b`.

## Изменения по модулям

| Модуль | Суть | Файлы (основные) |
|--------|------|------------------|
| Секреты / запуск | ключи не в Git; local secrets.json | `.gitignore`, `Program.cs`, `appsettings.json` (плейсхолдеры; файл секретов не в коммите) |
| Документация | память агента, KI/TR, отчёты | `AGENTS.md`, `docs/ai/*`, `.cursor/rules/*`, `docs/reports/*` |
| Авторизация | политики `Permission:{code}`, seeder, видимость | `PermissionCodes.cs`, `PermissionSeeder`, `PermissionService`, `UserVisibilityService`, контроллеры |
| Роли / пользователи | UserPermissions на UserRoleEdit; AccessControl удалён | `RolesController`, `UserRolesViewModel`, удалены `AccessControl*` |
| DataTable | права на кнопки/ссылки/export при рендере | `_DataTable.cshtml`, `CreateTableOptions` во ViewModels |
| StickyButtons | те же коды на Back/Save/Create/Delete | `_StickyButtons.cshtml`, `FormActionsViewModel` |
| Поддержка | статус → уведомление создателю; адаптив OpenNewCase | `SupportTickedStatusChangedEvent`, `SupportTicketService`, `page-newsupportticket.css` |
| Меню | пункты по правам; collapsed без вспышки и сдвига Support | `_VerticalMenu.cshtml`, `_CommonMasterLayout.cshtml`, `partnerly-layout.css/js` |
| Dashboard / профиль / настройки | адаптив и скролл на коротком десктопе | `dashboard.css/js`, `page-myprofile.css`, `page-mysettings.css`, views |
| Локализация | en/ru/hy смысл и ключи | `SharedResource.*.resx` |
| Чистка | мёртвый код, пакеты, uploads из индекса | `Partnerly.csproj`, атрибуты, `wwwroot/uploads` в gitignore |
| БД / миграции | **не менялись** | — |

## Принятые технические решения

- **TR-2026-10-25-01** — канонические инструкции в `AGENTS.md` / `docs/ai/`.
- **TR-2026-10-25-02** — секреты вне истории `Cursor-Agent`.
- Ранее существовавшие: Permission вместо Role.Type (TR-2026-10-21), свои User/Role, DataTable, EnsureCreated, EventBus — не новые, но политики и фильтр DataTable уточнены в TR-2026-10-21 (последствия 2026-10-25).

Новых TR за 2026-10-26 не заводилось (CSS/layout без смены архитектуры).

## Известные проблемы

**Закрыты в коде (2026-10-25), runtime не подтверждён:** KI-001 (для `Cursor-Agent`), KI-004, KI-005, KI-006, KI-007, KI-013.

**Открыты:**

| Код | Суть |
|-----|------|
| KI-002 | `EnsureCreated()` рядом с миграциями |
| KI-003 | seed-пользователь на Admin, не Super |
| KI-008 | Payment/Transaction без UI |
| KI-009 | прочие поля в `appsettings` (SMTP и т.п.) |
| KI-010 | middleware после `MapControllerRoute` (гипотеза) |
| KI-011 | нет автотестов |
| KI-012 | лишний `using OpenXml` в `SupportTicket.cs` |

Seeder **только добавляет** RolePermissions. На уже заполненной БД старые лишние права сами не снимутся.

## Сборка и тесты

| Что | Результат |
|-----|-----------|
| `dotnet build Partnerly.sln` в `%TEMP%` (чистка, политики, DataTable, StickyButtons) | 0 ошибок; предупреждения ~97–98 |
| Сборка в `Partnerly/bin` | в одной сессии не вышла: файл `Partnerly.exe` был занят |
| CSS/layout 26 октября | сборка не запускалась |
| Автотесты | проекта тестов нет |
| Браузер (роли, меню, адаптив, Nest Hub Max) | **не проверялось** |

## Риски и ручные действия

1. Перезапустить приложение, чтобы seeder дописал новые коды Super/Admin/Employee/User.
2. Проверить в браузере четыре роли: меню, AccessDenied, DataTable, StickyButtons, тикет Start/Close.
3. Ctrl+F5 и проверка Dashboard / My Profile / My Settings / Support на телефоне, планшете и Nest Hub Max 1280×800.
4. По желанию: ротация ключей после старого отклонённого GitHub push; не пушить `Belote-Dev` без той же очистки истории.
5. Push `3f8568b` — только по просьбе владельца.
6. Миграции и продакшен-БД — не трогать без разрешения.

## Следующие шаги

1. Ручная проверка прав и адаптив в браузере.
2. По просьбе — `git push` коммита UI.
3. Открытые KI по приоритету владельца: KI-003 (seed Super), KI-002 (EnsureCreated), KI-008 (платежи), тесты (KI-011).

## Приложения

### Расхождения журнала и git

- `CONTINUATION_OF_WORK.md` в `3f8568b` всё ещё писал, что sidebar/адаптив «незакоммичены», хотя они вошли в этот же коммит. Фактическое состояние: working tree clean, ahead 1 от origin.
- Журнал до 2026-10-25 не покрывает всю историю ветки (`git log` длиннее пяти коммитов агента).
- Локальный `Belote-Dev` и `Cursor-Agent` разошлись по хешам из‑за `filter-branch` секретов; сравнивать ветки по сообщению коммита, не по одному SHA.

### Что сознательно не делалось

- Коммит/push без просьбы владельца (кроме уже запрошенных: push секретов+docs+прав 25 октября; коммит UI 26 октября без push).
- Подключение к БД, миграции, удаление таблиц/данных.
- Удаление загруженных файлов с диска.
- Удаление сущностей Payment/Transaction.
