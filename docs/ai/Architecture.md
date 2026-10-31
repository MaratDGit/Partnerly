# Архитектура Partnerly

Сверка с кодом: 2026-10-25. Предположения помечены отдельно.

## Общая схема

Монолит ASP.NET Core 6 MVC. Точка входа — `Partnerly/Program.cs` (minimal hosting).

Типичный запрос к админскому экрану:

1. Cookie-аутентификация.
2. `[Authorize]` на `_BaseController` (если контроллер его наследует).
3. При наличии `[Authorize(Policy = "Permission:...")]` — `PermissionPolicyProvider` создаёт политику, `PermissionAuthorizationHandler` вызывает `IPermissionService.HasPermissionAsync`.
4. Контроллер ходит в сервис, сервис — в репозиторий / `AppDbContext`.
5. Razor View или JSON / файл экспорта.

## Слои

```text
Controller  →  I*Service  →  I*Repository : Repository<T>  →  AppDbContext
```

Регистрация в `Program.cs`: почти все сервисы и репозитории **Scoped**; `IEmailSender` и обработчики событий — **Transient**; `IEventBus`, `IDataTableRegistry`, `IUserIdProvider`, `IAuthorizationPolicyProvider` — **Singleton**.

`AppDbContext` зависит от `ICurrentUserService` (для аудита в `SaveChangesAsync`).

## Контроллеры

Базовый класс `_BaseController`:

- `[Authorize]`.
- Общие зависимости: EventBus, UserService, CurrentUser, LogService, localizer, SystemSettings, DataTableQueryService, DataTableRegistry.
- `OnActionExecutionAsync` заполняет ViewBag (фото, имя, рефкод) и текст тикера / maintenance; `ViewData["Permissions"]` — эффективные коды прав текущего пользователя для меню.
- `PrepareViewModelAsync` подставляет имена авторов.
- `ExportTableAsync` — Excel/CSV.
- `UserVisibility` / `CanViewUserAsync` — видимость записей по роли. Тикет: `CanViewSupportTicketAsync` — автор из видимых **или** текущий пользователь = `AssignedTo`.
- Старый `HasPermision` / RoleType удалён (2026-10-25). Права — через политики `Permission:`.

Контроллеры **без** `_BaseController`: `AccountController`, `HomeController`, `AIController` (`[Authorize]` + `ai.use` на Ask), `LanguageController`, `SearchController` (`[Authorize]`, поиск по секциям и данным).

Политики `Permission:*` стоят на CRUD/Data/Export модулей Users, Roles, UserGroups, SupportTickets, Notifications, EmailTemplates, Logs, SystemPreferences, Attachments (список), AI, Web inspector (`web_inspector.use`). MarkAsRead / Upload к своему тикету — без глобального manage. Dashboard / MyProfile / ReferralTree — только `[Authorize]`. Statistics (`DashboardController.Index`) и бывшие модульные дашборды — одна страница: вкладки Games (первая) / Users / Tasks / Roles / Groups / Notifications; KPI модулей через `IInsightsDashboardService` по `*.view` и `VisibleUserIds()`. `/Dashboard/Dashboards` редирект на Index. В Roles: превью фото в Users by role (макс. 5) и Permissions by role. Дерево рефералов: `DashboardController.ReferralTree` + `GetReferralTree`; корень — текущий пользователь или тот, кого можно видеть (`CanViewUser` / своя downline). Super скрыт от остальных. Схема БД не меняется (`User.ReferrerId`). Веб-инспектор: `WebInspectorController` + unpacked Chrome extension `tools/partnerly-web-inspector-extension` — живое окно сайта слева, realtime DOM/индикаторы справа. Серверный fetch для live-потока не используется. Право `web_inspector.use`.

## Сервисы и репозитории

Паттерн: интерфейс `IFooService` / `IFooRepository`, реализация в одноимённых классах. Базовый `Repository<T>`: GetById (Guid), GetAll, Add, Update, Delete, SaveChanges, `QueryEntity` (проекция выбранных полей через Expression).

`IRepository<T>.GetByIdAsync(Guid?)` не подходит для `SystemSettings` (ключ `int`) — для настроек отдельный репозиторий.

Результаты операций часто обёрнуты в `ServiceResult<T>`.

## DataTable

Не клиентский DataTables.net как единственный источник правды, а серверная модель:

- Идентификаторы таблиц: `Models/DataTable/DataTableNames`.
- Описание колонок/действий: статический `CreateTableOptions` в ViewModel, регистрация в singleton `DataTableRegistry`.
- Права UI: `Permission` на action/column/export. Фильтр в `_DataTable.cshtml` по `ViewData["Permissions"]` — **не** менять зарегистрированные options (singleton общий для всех пользователей). Колонка `Id` дополнительно требует `datatable.view_ids` (seed: Super, Administrator, Employee).
- StickyButtons: `Permission` на `FormActionBase`; фильтр в `_StickyButtons.cshtml`. Удаление вложений в модалке — `attachments.manage` (или `DeletePermission`).
- Запрос: `DataTableQueryService` + `DataTablePropertyResolver`.
- UI: `Views/Shared/DataTable/_DataTable.cshtml`, `_DataTablePartial.cshtml`, JS в `wwwroot/js/datatable/`. Тулбар: справа от фильтра кнопка `data-table-fit-columns` (`resize.js` / `fitHeaderColumns`) подгоняет ширину колонок под полный текст заголовка и иконки sort/filter; колонка `__actions` — по ширине всех кнопок в строке, не по тексту заголовка. Порядок колонок: `reorder.js` — перетаскивание заголовка (кроме `__actions`); сохранённый порядок в localStorage при `RememberState`. Кнопка настроек (шестерёнка) справа от поиска открывает панель колонок (`columnSettings.js`): вкл/выкл и drag-порядок; `__actions` в списке нет. Колонка Note (`DataTableColumn.HiddenNote`, тип `Note`) — выкл. по умолчанию, в таблице иконка (`bx-note`), заполненная — цвет как у `.btn-note.active`, клик открывает попап как на форме.
- Вкладки форм: `<partnerly-tabs>` / `<partnerly-tab>` (`PartnerlyTabsTagHelper`), стили `partnerly-tabs.css`. Сейчас на Edit User Role и Edit Group. System Preferences пока свой markup.
- Inline-правка: `IInlineUpdateService`.
- Экспорт: `DataTableExcelExporter`, `DataTableCsvExporter`.

Идентификаторы таблиц должны быть уникальны (`DataTableNames`). Конфликт тикетов и email-шаблонов исправлен 2026-10-25.

## Авторизация и права

### Аутентификация

- Схема по умолчанию: cookie (`LoginPath=/Account/Login`, `AccessDeniedPath=/Account/AccessDenied`). Срок cookie/сессии берётся из `SystemSettings` в `SignInUserAsync`, не из `ExpireTimeSpan` в `Program.cs`.
- Google: схема `"Google"` регистрируется всегда; Challenge и кнопки на Login/Register режутся `EnableGoogleLogin`. ClientId/Secret из конфигурации, не из SystemSettings.
- Claims при входе собираются в `AccountController` (`NameIdentifier` = `User.Id`, Role, Email, кастомные claim имени, фото, рефкода, RoleId, LoginTime).
- Операционные флаги сайта (роль новичка, почта, SignalR, AI, лимиты файлов, автоназначение тикетов, имя приложения, язык, timezone) — строка `SystemSettings` Id=1 через `ISystemSettingsService.GetCurrentAsync()`. Результат кэшируется в `IMemoryCache` на 10 минут; сброс при Create/Update. `GetSetupByIDAsync` всегда идёт в БД (форма редактирования).
- `ICurrentUserService` читает claims, не ходит в БД на каждое свойство.

Пользователи и роли — **свои** сущности, не ASP.NET Identity.

### Авторизация

1. Аутентифицирован / нет (`[Authorize]`).
2. Политика `Permission:{code}`:
   - код права из политики;
   - пользователь не удалён и не заблокирован, роль активна;
   - индивидуальное `UserPermission.IsGranted` **перекрывает** роль;
   - иначе наличие строки в `RolePermissions`.
   - UI прав роли: DataTable на `Roles/Edit` (`rolePermissionsTable`).
   - UI индивидуальных прав: DataTable на `Roles/UserRoleEdit` (`userPermissionsTable`); строка = override роли, `IsGranted` = разрешить/запретить.
   - Изменения пишутся в `AccessControlAuditLogs` из `RoleService` / `UserService` через `IPermissionService.AddAccessAudit`.
   - `AccessControlController` удалён 2026-10-25.
3. `PermissionSeeder.SeedAsync` при старте **добавляет** недостающие коды и RolePermissions (лишние строки не удаляет): Super и Admin — `PermissionCodes.All`; Employee — просмотр + работа с тикетами; User — `support_tickets.view/create`. Видимость записей — `IUserVisibilityService`: Super видит всех; Super не виден никому кроме Super; Admin не видит других Admin и Super; Employee не видит Admin и Super; User — только себя.
4. `RoleChangeMiddleware`: если `User.LastRoleUpdateTime` позже claim `LoginTime`, cookie сбрасывается.
5. `MaintenanceMiddleware`: не-Admin и не-SuperRole редирект на `/Home/Maintenance`.
6. Доступ к Super-роли/пользователю: методы `IsSuperRole` / `IsSuperUser` / `IsSystemRole` в `_BaseController`.

Старый подход с `Role.Type` и `[ClaimAuthorize]` закомментирован; колонка Type роли удалена миграцией `20261024211314_deleteRoleType`.

## События

`IEventBus` — словарь тип события → типы обработчиков. `PublishAsync` создаёт scope и резолвит handler из DI.

Подписки в `Program.cs`:

- `UserRegisteredEvent` → `UserRegisteredEventHandler`
- `SupportTickedCreatedEvent` → `SupportTickedCreatedEventHandler`
- `SupportTickedStatusChangedEvent` → `SupportTickedStatusChangedEventHandler`

Это не очередь сообщений и не outbox.

## Фоновые процессы и realtime

- `OnlineStatusService` (`IHostedService`) — онлайн-статус по `LastActivity` и настройке `OnlineStatusAutoRefreshMinute`.
- `UpdateLastActivityMiddleware` — обновление активности.
- `NotificationHub` (`/notificationHub`) + `CustomUserIdProvider`.
- `NotificationService` использует `IHubContext<NotificationHub>`.

## Интерфейс

- `_ViewStart.cshtml`: layout `_ContentNavbarLayout`.
- Другие layout: `_CommonMasterLayout`, `_BlankLayout`, `_LandingLayout`.
- Навбар: вместо title страницы — глобальный поиск (`_NavbarPartial` + `site-search.js`) на десктопе и планшете (≥768px); на телефоне скрыто. Запрос `GET /Search/Query` через `ISiteSearchService`: секции по эффективным правам, данные модулей по `*.view` и `VisibleUserIds()`.
- Повторяемые части: `_Toast`, `_DeleteConfirmation`, `_AuditInfo`, `_StickyButtons`, секции в `Views/Shared/Sections/`.
- Локализация: `AddLocalization` + `Resources/SharedResource.{en,ru,hy}.resx`, культуры `en`, `ru`, `hy`, default `en`.
- `RequiredLabelTagHelper` — обязательные поля в формах.
- Тема / vendor CSS: `wwwroot/assets` (шаблон админки). Свой код: `wwwroot/assets/css/site.css`, `wwwroot/js/datatable`.

`HomeController.Index` для анонима показывает лендинг, для аутентифицированного — редирект на Dashboard.

## Конфигурация, логи, ошибки

- Конфиг: `appsettings.json`, `appsettings.Development.json`, user-secrets, optional `secrets.json`.
- Исключения в Production: `/Home/Error` + HSTS. В Development стандартная страница разработчика не подключается явно в `Program.cs` (используется поведение хоста).
- Прикладной журнал: сущность `Log` через `ILogService`, не только `ILogger`.
- Валидация сущностей при SaveChanges: `ValidationHelper.ValidateEntityRequiredFields`; флаг `AppDbContext.SkipValidations`.

## Порядок middleware (факт из `Program.cs`)

```text
UseRequestLocalization
EnsureCreated + PermissionSeeder + подписки EventBus (при старте, не middleware)
UseExceptionHandler / HSTS (не Development)
UseHttpsRedirection, UseStaticFiles
UseRouting, UseAuthentication, UseAuthorization
MapControllerRoute
UpdateLastActivityMiddleware, MaintenanceMiddleware, RoleChangeMiddleware
MapHub /notificationHub
```

**Предположение:** регистрация трёх middleware после `MapControllerRoute` может влиять на то, для каких запросов они выполняются. Это нужно проверять эмпирически, не считать доказанным отказом.

## Внешние интеграции

- SMTP (`IEmailSender`).
- Google OAuth.
- OpenAI HTTP (`OpenAIService`).
- Файловая система `wwwroot/uploads`.

## Что учитывать при разработке

- Не вводить второй способ таблиц или второй способ прав, пока текущие не исчерпаны.
- Новые списки: ViewModel.`CreateTableOptions` + константа в `DataTableNames` (уникальный Id) + `_DataTablePartial`.
- Новые права: код в `PermissionSeeder` **и** атрибут политики на GET **и** POST.
- Аудит полей Created/Updated заполняется в `SaveChangesAsync` только для `IAuditableEntity`.
- Не класть секреты в `appsettings.json`.
