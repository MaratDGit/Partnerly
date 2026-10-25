# Журнал работ Partnerly

Новые записи — сверху. Не отмечать успех без проверки.

---

## 2026-10-25 — StickyButtons по правам

- **Суть:** те же коды, что и у DataTable, на Back/Save/Create/Delete/Note/вложения/меню форм.
- **Сделано:** `FormActionBase.Permission`; `_StickyButtons.cshtml` режет кнопки по `ViewData["Permissions"]`. Проставлено в Users, Roles, UserRoleEdit, UserGroups, Tickets (включая ViewCase), Notifications, EmailTemplates, Logs, SystemPreferences. MySettings без кода — свой профиль. Удалить вложение — `attachments.manage`; на тикете скрепка видна при `support_tickets.view` (User может смотреть/грузить свои файлы, Delete API по-прежнему manage).
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildSticky` — 0 ошибок.
- **Браузер:** не проверялся.

---

## 2026-10-25 — DataTable: кнопки и ссылки по правам

- **Суть:** `CreateTableOptions` общий (singleton registry). Права нельзя писать в зарегистрированные options — утекут между пользователями.
- **Сделано:** `Permission` на `DataTableAction` / `DataTableColumn` / `DataTableExportOptions`. `_DataTable.cshtml` режет toolbar, row actions, export, UrlTemplate и inline по `ViewData["Permissions"]` и отдаёт урезанный JSON в JS. Коды проставлены в CreateTableOptions Users, Roles, UserPermissions, UserGroups, Tickets, Notifications, EmailTemplates, Logs, Attachments.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildDt` — 0 ошибок, 97 предупреждений.
- **Браузер:** не проверялся.

---

## 2026-10-25 — Нет уведомления создателю при смене статуса тикета

- **Суть:** обработчик `SupportTickedStatusChangedEvent` писал `UserId = (Guid)Ticket.UserId` — при пустом UserId падал; SignalR-сбой после Save мог оборвать цепочку; Create не обновлял unread. Inline-сохранение не публиковало событие статуса.
- **Сделано:** получатель = UserId ?? CreatedBy; ошибки в лог; unread после создания; inline вызывает `UpdateSupportTicketAsync(..., oldStatus)`.
- **Сборка:** ниже.

---

## 2026-10-25 — Loading error Logs / Notifications / Attachments

- **Суть:** DataTable `QueryEntity` проецирует только колонки таблицы. Фильтр по `CreatedBy`/`UserId` после проекции ломал EF (поля нет в SELECT).
- **Исправление:** эти поля добавляются в проекцию; `VisibleUserIds()` берётся до лямбды Where.
- **Сборка:** ниже в этой сессии.

---

## 2026-10-25 — Super скрыт от всех, кроме Super

- **Требование:** Super видит всё; его не видит никто (Admin/Employee/User).
- **Было:** Admin видел пользователей с ролью SUPER.
- **Стало:** Admin в `UserVisibilityService` не видит Super так же, как других Admin. Списки, карточки, тикеты, логи, вложения, lookup идут через тот же фильтр.
- **Сборка:** не запускалась в этом шаге (правка одного сервиса). Браузер не проверялся.

---

## 2026-10-25 — Политики на действиях и видимость по ролям

- **Требование:** права на каждом действии, где нужно; Super — все; Admin — все коды, но без просмотра других Admin; Employee — просмотр кроме Admin/Super; User — только своя информация.
- **Сделано:** `PermissionCodes` + seeder (Super и Admin = все коды; Employee view+тикеты; User `support_tickets.view/create`); `IUserVisibilityService`; политики на Users/Roles/UserGroups/Tickets/Notifications/EmailTemplates/Logs/SystemPreferences/Attachments/AI; фильтры DataTable/lookup; меню по `ViewData["Permissions"]`. Seeder только добавляет RolePermissions. Схема БД не менялась.
- **Сборка:** `dotnet build` в temp — 0 ошибок, 98 предупреждений. Браузер и живой seeder не проверялись.
- **KI:** 004, 005, 013 закрыты в коде; на существующей БД лишние старые права могут остаться, пока не переназначить вручную.


## 2026-10-25 — Снятие AccessControl в пользу Roles

- **Требование:** перенести из AccessControl то, чего не было в логике role/user permissions, затем удалить контроллер и связанные файлы.
- **Перенесено:** проверка активного права; журнал `AccessControlAuditLogs`; политики `roles.manage_permissions` / `users.manage_roles` на inline-операциях; список прав без удалённых.
- **Удалено:** `AccessControlController`, `AccessControlService`, `IAccessControlService`, Views/AccessControl. Таблица аудита и сущность оставлены.
- **Сборка:** `dotnet build` в temp — 0 ошибок. Браузер не проверялся.

---

## 2026-10-25 — UserPermissions на UserRoleEdit

- **Требование:** индивидуальные права пользователя в `RolesController`, DataTable на `UserRoleEdit` по образцу `rolePermissions` на форме роли.
- **Действия:** `UserPermissionExt` + `QueryUserPermissionsExt`; add/delete/update grant в `UserService`/`UserRepository`; `UserRolesViewModel.CreateTableOptions` для `userPermissionsTable` (как `RolesViewModel` для rolePermissions); lookup исключает уже назначенные права; `IsGranted` редактируется inline. Проверка `HasPermissionAsync` уже учитывает `UserPermission`. Схема БД не менялась.
- **Сборка:** `dotnet build` в temp — 0 ошибок. UI в браузере под учётной записью не проверялся.

---

## 2026-10-25 — Смысл текстов en/ru/hy

- **Требование:** исправить тексты, где перевод не совпадает со смыслом экрана.
- **Действия:** `Active` — статус, не «участники»; предупреждение при выключенных уведомлениях — «не будете получать»; кнопка восстановления пароля — «Отправить ссылку»; обращения поддержки вместо «дела»; плейсхолдеры часов в настройках берут подписи полей, а не сообщения об истекшей ссылке; HY: убраны «տոմս/հղման հղումը/Կազմակերպություն»; RU: инфинитивы кнопок, грамматика, «дело»→«обращение».
- **Сборка:** `dotnet build` в temp — 0 ошибок. Браузер не проверялся.

---

## 2026-10-25 — Правка локализации en/ru/hy

- **Требование:** исправить формулировки, убрать дубли ключей, переименовать ключи в коде при необходимости.
- **Действия:** правки в трёх `SharedResource.*.resx`; слияние `LookupSearch`→`DataTableSearch`, `Changerole`→`EditRole`, `Systemsettings`→`Settings`; переименования опечаток (`InvalidEmail`, `ConfirmDelete`, `Paid`, `IsOnline`, `Ticket*Notification` и др.); разные тексты для ложных дублей (уведомления, удаление, активность); исправлен ярлык логов UserGroup.
- **Сборка:** `dotnet build` в temp — 0 ошибок. Браузер не проверялся.
- **Не меняли:** поля БД `IsOnlayn`, код статуса `Payed = "A"`, классы событий `SupportTicked*`.

---

## 2026-10-25 — Чистка мёртвого кода и дублей

- **Требование:** удалить ненужные файлы, модели таблиц оставить, убрать дублирование.
- **Действия:** удалены `ClaimAuthorizeAttribute`, `RoleTypeAttribute`, `_ValidationScriptsPartial`, jquery-validation (не подключался); сняты пакеты EPPlus и datatables.net-dt; убраны закомментированные RoleType/CanUpdate; неиспользуемые `IPermissionService`/`ICurrentUserService` из ряда сервисов; уникальный id DataTable тикетов; uploads убраны из индекса Git (файлы на диске оставлены).
- **Не трогали:** сущности Payment/Transaction и миграции.
- **Сборка:** `dotnet build` в temp-каталог — успех (0 ошибок). Сборка в `bin/` не прошла из‑за запущенного `Partnerly.exe`. Браузер не проверялся.
- **Решения:** нет новых TR.
- **KI:** закрыты KI-006, KI-007.

---

## 2026-10-25 — Постоянная память AI-агента и документация проекта

- **Требование:** изучить код, создать `AGENTS.md`, `docs/ai/*`, правила Cursor, шаблон отчётов; не менять бизнес-логику и БД.
- **Действия:**
  - Прочитан существующий черновик `AGENTS.md` (не был в Git) и `.github/copilot-instructions.md` (Azure Copilot, к архитектуре не относится).
  - Разобраны `Program.cs`, `AppDbContext`, контроллеры, права, DataTable, миграции, UI-layout.
  - Создана структура документации и правила `.cursor/rules/`.
  - Сверка фактов по исходникам; гипотезы отдельно.
- **Созданные файлы:**
  - `AGENTS.md` (обновлён)
  - `.cursor/rules/partnerly-workflow.mdc`
  - `.cursor/rules/partnerly-security.mdc`
  - `docs/ai/REVIEW_PROJECT.md`
  - `docs/ai/Architecture.md`
  - `docs/ai/DATABASE.md`
  - `docs/ai/DEVELOPMENT_STANDARDS.md`
  - `docs/ai/TECHNICAL_SOLUTIONS.md`
  - `docs/ai/KNOWN_ISSUES.md`
  - `docs/ai/WORK_LOG.md`
  - `docs/ai/CONTINUATION_OF_WORK.md`
  - `docs/ai/REPORT_TEMPLATE.md`
  - `docs/reports/2026-10-25-pervonachalnyj-analiz.md`
- **Изменённые файлы приложения:** нет.
- **Сборка / тесты:** не запускались (изменения только в markdown/rules).
- **Решения:** TR-2026-10-25-01.
- **Осталось:** коммит документации — только по просьбе; живая БД не инспектировалась.

---

## 2026-10-25 — Ветка Cursor-Agent, очистка секретов, push

- **Требование:** ветка от Belote-Dev, затем push; GitHub отклонил секреты; очистить историю незапушенных коммитов и запушить.
- **Действия:**
  - Создана ветка `Cursor-Agent` от `Belote-Dev`.
  - `git filter-branch` только для `origin/Development..HEAD`: удалён `secrets.json` из деревьев, очищены Google ClientId/ClientSecret в исторических `appsettings.json`.
  - Ключи сохранены в `dotnet user-secrets`; локальный `secrets.json` восстановлен из бэкапа и игнорируется.
  - Коммит `db8244d` — gitignore + `AddJsonFile("secrets.json")`.
  - `git push -u origin Cursor-Agent` — успех.
- **Файлы:** `.gitignore`, `Partnerly/Program.cs`, `Partnerly/appsettings.json` (плейсхолдеры), история git.
- **Сборка / тесты:** не запускались в той сессии после rewrite.
- **Решения:** TR-2026-10-25-02.
- **Осталось:** KI-001 частично (ротация ключей, история `Belote-Dev`); KI-009.

История работ **до 2026-10-25** в этом журнале не реконструировалась по всем 74 коммитам. Для старых изменений смотреть `git log`.
