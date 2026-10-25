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
- `UserVisibility` / `CanViewUserAsync` — видимость записей по роли.
- Старый `HasPermision` / RoleType удалён (2026-10-25). Права — через политики `Permission:`.

Контроллеры **без** `_BaseController`: `AccountController`, `HomeController`, `AIController` (`[Authorize]` + `ai.use` на Ask), `LanguageController`.

Политики `Permission:*` стоят на CRUD/Data/Export модулей Users, Roles, UserGroups, SupportTickets, Notifications, EmailTemplates, Logs, SystemPreferences, Attachments (список), AI. MarkAsRead / Upload к своему тикету — без глобального manage. Dashboard / MyProfile — только `[Authorize]`.

## Сервисы и репозитории

Паттерн: интерфейс `IFooService` / `IFooRepository`, реализация в одноимённых классах. Базовый `Repository<T>`: GetById (Guid), GetAll, Add, Update, Delete, SaveChanges, `QueryEntity` (проекция выбранных полей через Expression).

`IRepository<T>.GetByIdAsync(Guid?)` не подходит для `SystemSettings` (ключ `int`) — для настроек отдельный репозиторий.

Результаты операций часто обёрнуты в `ServiceResult<T>`.

## DataTable

Не клиентский DataTables.net как единственный источник правды, а серверная модель:

- Идентификаторы таблиц: `Models/DataTable/DataTableNames`.
- Описание колонок/действий: статический `CreateTableOptions` в ViewModel, регистрация в singleton `DataTableRegistry`.
- Права UI: `Permission` на action/column/export. Фильтр в `_DataTable.cshtml` по `ViewData["Permissions"]` — **не** менять зарегистрированные options (singleton общий для всех пользователей).
- StickyButtons: `Permission` на `FormActionBase`; фильтр в `_StickyButtons.cshtml`. Удаление вложений в модалке — `attachments.manage` (или `DeletePermission`).
- Запрос: `DataTableQueryService` + `DataTablePropertyResolver`.
- UI: `Views/Shared/DataTable/_DataTable.cshtml`, `_DataTablePartial.cshtml`, JS в `wwwroot/js/datatable/`.
- Inline-правка: `IInlineUpdateService`.
- Экспорт: `DataTableExcelExporter`, `DataTableCsvExporter`.

Идентификаторы таблиц должны быть уникальны (`DataTableNames`). Конфликт тикетов и email-шаблонов исправлен 2026-10-25.

## Авторизация и права

### Аутентификация

- Схема по умолчанию: cookie (`LoginPath=/Account/Login`, `AccessDeniedPath=/Account/AccessDenied`).
- Google: схема `"Google"`, ClientId/Secret из конфигурации.
- Claims при входе собираются в `AccountController` (`NameIdentifier` = `User.Id`, Role, Email, кастомные claim имени, фото, рефкода, RoleId, LoginTime).
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
