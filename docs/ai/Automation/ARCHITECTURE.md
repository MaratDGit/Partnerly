# PHASE 2 — Архитектура Partnerly Automation

Дата: 2026-11-02.  
Опирается на: [MASTER-SPECIFICATION.md](./MASTER-SPECIFICATION.md), [PHASE-1-ANALYSIS.md](./PHASE-1-ANALYSIS.md).  
Срез кода: `a84fd0d` + docs PHASE 1. **Код приложения в этом этапе не пишется.**

Владелец сказал начинать следующий этап. Решения PHASE 1 считаются принятыми:

- Планировщик — SQL wakeup + `IHostedService`, не Hangfire.
- Cron — пакет Cronos (PHASE 4).
- Редактор — Drawflow, не React.
- User и Employee — свои автоматизации (`OwnerUserId`). Super/Admin — ещё `automation.manage`.
- Миграции — PHASE 4, только локальная БД, не прод.

---

## 1. Цель модуля

Универсальный движок workflow внутри монолита Partnerly: пользователь собирает процесс без кода. Движок не знает таблиц Users/Tickets — только коннекторы. Дальше можно вынести в SaaS, не ломая контракт GraphJson и таблиц `Automation*`.

Не заменяет EventBus, DataTable, `Permission:`, почту, SignalR.

---

## 2. Границы

| Внутри `Partnerly/Automation/` | Вне, минимум |
|---|---|
| Движок, registry, коннекторы, EF-сущности, worker, HMAC, переменные | `Program.cs`: `AddPartnerlyAutomation()`, hub map, EventBus subscribe |
| Тонкие контроллеры `Automation/Web` | `AppDbContext.OnModelCreating` — apply configs |
| ViewModels | `PermissionCodes` + seeder |
| | `_VerticalMenu.cshtml`, resx en/ru/hy |
| | Views `Views/Automation/`, css/js под wwwroot |
| | Вынос SSRF-проверки URL в `Partnerly/Helpers/PublicHttpGuard.cs` (общий с инспектором) |
| | `Partnerly.Automation.Tests` в `.sln` (PHASE 7, каркас PHASE 3) |

Существующие сервисы Partnerly **вызываются**, не копируются.

---

## 3. Каталоги и типы

Пространство имён: `Partnerly.Automation.*`. Один основной тип на файл.

```text
Partnerly/Automation/
  Hosting/
    AutomationServiceCollectionExtensions.cs
    AutomationWorker.cs                    # IHostedService
  Abstractions/
    ITriggerHandler.cs
    IActionHandler.cs
    IConditionEvaluator.cs
    IEntityConnector.cs
    IAutomationAuthoringService.cs         # UI + будущий AI
    IAutomationRunService.cs
    IAutomationScheduler.cs
    IAutomationAccessService.cs
    IVariableBinder.cs
    ISecretProtector.cs
    IPublicHttpGuard.cs                    # обёртка над helper
    AutomationTypeCodes.cs                 # строки триггеров/действий
    AutomationStatuses.cs
    EntityFieldDescriptor.cs
    ConditionNode.cs                       # AST
    WorkflowGraph.cs                       # DTO JSON
    TriggerContext.cs
    ActionContext.cs
    StepResult.cs
  Registry/
    TriggerRegistry.cs                     # singleton
    ActionRegistry.cs
    EntityRegistry.cs
  Engine/
    WorkflowExecutor.cs
    StepRunner.cs
    VariableBinder.cs
    GraphValidator.cs
    RecursionGuard.cs                      # AsyncLocal depth
  Conditions/
    ConditionEvaluator.cs
  Triggers/
    ManualTriggerHandler.cs
    ScheduleTriggerHandler.cs
    IntervalTriggerHandler.cs
    EntityCreatedTriggerHandler.cs
    EntityUpdatedTriggerHandler.cs
    EntityDeletedTriggerHandler.cs
    WebhookTriggerHandler.cs
    EventTriggerHandler.cs
    DelayTriggerHandler.cs
  Actions/
    SendEmailActionHandler.cs
    SendNotificationActionHandler.cs
    CreateRecordActionHandler.cs
    UpdateRecordActionHandler.cs
    FindRecordActionHandler.cs
    DeleteRecordActionHandler.cs
    DelayActionHandler.cs
    WaitUntilActionHandler.cs
    ConditionalBranchActionHandler.cs
    SwitchActionHandler.cs
    SetVariableActionHandler.cs
    HttpRequestActionHandler.cs
    WebhookActionHandler.cs
    ExecuteWorkflowActionHandler.cs
  Scheduler/
    CronScheduleCalculator.cs
    WakeupLeaser.cs
    ScheduleArmer.cs                       # после publish
  Persistence/
    Entities/…                             # см. §4
    Configurations/…                       # IEntityTypeConfiguration
    AutomationSaveChangesInterceptor.cs    # CUD → wakeup, после Save
    IAutomationStore.cs
    AutomationStore.cs                     # репозиторий модуля
  Security/
    AutomationAccessService.cs
    WebhookSignature.cs
    SecretProtector.cs                     # IDataProtectionProvider
    AutomationHttpGuard.cs
  Connectors/
    ConnectorBase.cs
    UserConnector.cs
    SupportTicketConnector.cs
    NotificationConnector.cs
    UserGroupConnector.cs
    EmailTemplateConnector.cs
  Integration/
    AutomationEventBridge.cs               # IEventHandler существующих событий
    AutomationHub.cs
  Authoring/
    AutomationAuthoringService.cs
    AutomationRunService.cs
  Web/
    AutomationController.cs
    AutomationHooksController.cs           # анонимный webhook, своя проверка HMAC
    ViewModels/…
```

Регистрация: каждый handler — Transient; registry — Singleton и резолвит handler по коду из scope. Worker и interceptor — Singleton с `IServiceScopeFactory`. Store, authoring, run, access — Scoped.

Новый action = класс `IActionHandler` + строка в `AutomationTypeCodes` + UI-метаданные (`Describe()`). `WorkflowExecutor` не меняется.

---

## 4. Модель данных

Соглашения как у Partnerly: `Guid Id`, аудит `CreatedBy/Date`, `UpdatedBy/Date`, `Note` там, где сущность пользовательская. Даты в UTC. Статусы — **строки**, не EF-enum.

### 4.1 `Automations` ← класс `AutomationDefinition`

| Поле | Тип | Примечание |
|---|---|---|
| Id | uniqueidentifier PK | |
| OwnerUserId | uniqueidentifier NOT NULL | FK Users, Restrict |
| Name | nvarchar(200) NOT NULL | |
| Description | nvarchar(2000) NULL | |
| IsActive | bit NOT NULL default 1 | |
| IsDeleted | bit NOT NULL default 0 | мягкое удаление |
| PublishedVersionId | uniqueidentifier NULL | FK Versions, Restrict |
| DraftVersionId | uniqueidentifier NULL | текущий черновик |
| TimeZoneId | nvarchar(100) NOT NULL | копия/override; default из SystemSettings |
| MaxConcurrentRuns | int NOT NULL default 1 | |
| MaxRunsPerHour | int NOT NULL default 60 | |
| StepTimeoutSeconds | int NOT NULL default 60 | |
| RunTimeoutSeconds | int NOT NULL default 3600 | Delay/Wait не входят в wall timeout |
| RetryLimit | int NOT NULL default 3 | на шаг |
| Note, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate | как IAuditableEntity | |

Индекс: `(OwnerUserId, IsDeleted)`, `(IsActive, IsDeleted)`.

### 4.2 `AutomationVersions` ← `AutomationVersion`

| Поле | Тип |
|---|---|
| Id | uniqueidentifier PK |
| AutomationId | uniqueidentifier NOT NULL FK Cascade |
| Number | int NOT NULL | 1, 2, … уникален с AutomationId |
| GraphJson | nvarchar(max) NOT NULL |
| GraphSchemaVersion | int NOT NULL default 1 |
| IsPublished | bit NOT NULL |
| PublishedAt | datetime2 NULL |
| Note, CreatedBy, CreatedDate | |

Индекс уникальный `(AutomationId, Number)`.

Черновик редактируется in-place в DraftVersion. Publish копирует граф в новую published-версию **или** помечает текущий draft published и открывает новый draft-клон. Выбор: publish помечает draft `IsPublished=true`, ставит `PublishedVersionId`, клонирует JSON в новый draft с Number+1. Запущенные run держат старый VersionId.

### 4.3 `AutomationTriggers` ← `AutomationTrigger`

Снимок триггеров опубликованной версии (для индексов планировщика и webhook). Граф тоже содержит trigger-узлы; таблица — проекция.

| Поле | Тип |
|---|---|
| Id | uniqueidentifier PK |
| VersionId | uniqueidentifier NOT NULL FK Cascade |
| Type | nvarchar(50) NOT NULL | см. коды |
| ConfigJson | nvarchar(max) NOT NULL |
| WebhookPublicId | nvarchar(40) NULL unique | только webhook |
| WebhookSecretHash | nvarchar(200) NULL | HMAC key hash; сырой секрет не хранить |
| IsEnabled | bit NOT NULL |

Индекс `(Type, IsEnabled)`, unique filter на WebhookPublicId.

### 4.4 `AutomationWakeups` ← `AutomationWakeup`

Очередь due-работы. Не аудитная.

| Поле | Тип |
|---|---|
| Id | uniqueidentifier PK |
| Kind | nvarchar(30) NOT NULL | `schedule` / `interval` / `delay` / `wait` / `retry` / `recover` |
| DueAtUtc | datetime2 NOT NULL |
| AutomationId | uniqueidentifier NULL |
| VersionId | uniqueidentifier NULL |
| TriggerId | uniqueidentifier NULL |
| RunId | uniqueidentifier NULL | продолжение шага |
| NodeId | nvarchar(64) NULL |
| PayloadJson | nvarchar(max) NULL | trigger context / wait condition |
| LeaseUntil | datetime2 NULL |
| LeaseOwner | nvarchar(100) NULL | Environment.MachineName + pid |
| Attempt | int NOT NULL default 0 |
| Status | nvarchar(20) NOT NULL | `pending` / `leased` / `done` / `cancelled` |

Индекс `(Status, DueAtUtc)` include LeaseUntil. Аренда: `UPDATE … SET Status=leased, LeaseUntil=@t, LeaseOwner=@o WHERE Id=@id AND Status=pending AND DueAtUtc<=@now`.

Lease 30 секунд; heartbeat во время run продлевает. Зависший `leased` с `LeaseUntil < now` возвращается в `pending` (recover).

### 4.5 `AutomationRuns` ← `AutomationRun`

| Поле | Тип |
|---|---|
| Id | uniqueidentifier PK |
| AutomationId | uniqueidentifier NOT NULL |
| VersionId | uniqueidentifier NOT NULL |
| OwnerUserId | uniqueidentifier NOT NULL | денормализация для IDOR истории |
| TriggerType | nvarchar(50) NOT NULL |
| TriggerReason | nvarchar(200) NULL | human/log key |
| Status | nvarchar(20) NOT NULL | `queued` / `running` / `waiting` / `succeeded` / `failed` / `cancelled` |
| IsDryRun | bit NOT NULL | тестовый запуск: без email/HTTP/CUD |
| Depth | int NOT NULL default 0 | Execute Workflow |
| IdempotencyKey | nvarchar(80) NULL | unique filter where not null |
| ContextJson | nvarchar(max) NOT NULL | переменные |
| Error | nvarchar(max) NULL |
| StartedAt | datetime2 NULL |
| EndedAt | datetime2 NULL |
| HeartbeatAt | datetime2 NULL |
| CancelledBy | uniqueidentifier NULL |
| RetryCount | int NOT NULL default 0 |
| CreatedDate | datetime2 NOT NULL |

Индекс `(OwnerUserId, CreatedDate DESC)`, `(AutomationId, Status)`, unique `IdempotencyKey` where not null.

### 4.6 `AutomationRunSteps` ← `AutomationRunStep`

| Поле | Тип |
|---|---|
| Id | uniqueidentifier PK |
| RunId | uniqueidentifier NOT NULL FK Cascade |
| NodeId | nvarchar(64) NOT NULL |
| ActionType | nvarchar(50) NOT NULL |
| Status | nvarchar(20) NOT NULL | `pending` / `running` / `succeeded` / `failed` / `skipped` / `waiting` |
| Attempt | int NOT NULL |
| InputJson | nvarchar(max) NULL |
| OutputJson | nvarchar(max) NULL |
| Error | nvarchar(max) NULL |
| StartedAt / EndedAt | datetime2 NULL |

Уникальный `(RunId, NodeId, Attempt)`. Повтор failed run создаёт новые Attempt, successful steps не переигрываются.

### 4.7 `AutomationConnections` ← `AutomationConnection`

| Поле | Тип |
|---|---|
| Id | uniqueidentifier PK |
| OwnerUserId | uniqueidentifier NOT NULL |
| Name | nvarchar(200) NOT NULL |
| Kind | nvarchar(50) NOT NULL | `http` / `webhook` ; позже google/telegram… |
| EncryptedPayload | nvarchar(max) NOT NULL | Data Protection purpose `Automation.Connection` |
| IsDeleted | bit |
| аудит | |

Секреты в JSON внутри payload: `{ "baseUrl", "headers", "secret" }`. В API наружу — только Name/Kind/Id, не секрет.

### 4.8 `AutomationTemplates` ← `AutomationTemplate`

Системные пресеты GraphJson (`OwnerUserId` NULL). Копирование создаёт AutomationDefinition владельца.

Связи:

```text
User 1—* AutomationDefinition
AutomationDefinition 1—* AutomationVersion
AutomationDefinition 0..1—0..1 PublishedVersion / DraftVersion
AutomationVersion 1—* AutomationTrigger
AutomationDefinition 1—* AutomationRun
AutomationRun 1—* AutomationRunStep
AutomationWakeup → Automation / Run (без Cascade delete run)
```

---

## 5. Контракт GraphJson (schema 1)

```json
{
  "schemaVersion": 1,
  "nodes": [
    {
      "id": "n1",
      "kind": "trigger",
      "type": "schedule",
      "name": "Every Monday 09:00",
      "x": 80, "y": 40,
      "config": { "scheduleKind": "weekly", "weekDays": [1], "time": "09:00" }
    },
    {
      "id": "n2",
      "kind": "action",
      "type": "find_record",
      "name": "findUser",
      "config": { "entity": "user", "filters": [{ "field": "lastActivity", "op": "less_than", "value": "{{execution.nowMinusDays.30}}" }] }
    },
    {
      "id": "n3",
      "kind": "action",
      "type": "branch",
      "config": { "condition": { "op": "and", "children": [ { "op": "is_not_empty", "left": "{{steps.findUser.result}}" } ] } }
    }
  ],
  "edges": [
    { "id": "e1", "from": "n1", "to": "n2" },
    { "id": "e2", "from": "n2", "to": "n3" },
    { "id": "e3", "from": "n3", "to": "n4", "port": "true" },
    { "id": "e4", "from": "n3", "to": "n5", "port": "false" }
  ]
}
```

Правила валидации (`GraphValidator`):

- Ровно один `kind=trigger` в v1 (несколько триггеров = несколько автоматизаций или PHASE позже; таблица Triggers может держать один). Уточнение: **v1 допускает несколько trigger-узлов** (OR: любой стреляет один run). Все проецируются в `AutomationTriggers`.
- Каждый action достижим от хотя бы одного trigger.
- Нет исходящих рёбер у terminal; branch имеет ports `true`/`false`; switch — `case:{value}` и `default`.
- `name` узла — ключ `steps.{name}` в переменных; уникален, `[A-Za-z][A-Za-z0-9_]*`.
- Неизвестные type — ошибка публикации.

Drawflow хранит x/y; сервер их не интерпретирует, только сохраняет.

---

## 6. Коды типов

`AutomationTypeCodes`:

**Triggers:** `manual`, `schedule`, `interval`, `entity.created`, `entity.updated`, `entity.deleted`, `webhook`, `event`, `delay`.

**Actions:** `send_email`, `send_notification`, `create_record`, `update_record`, `find_record`, `delete_record`, `delay`, `wait_until`, `branch`, `switch`, `set_variable`, `http_request`, `webhook`, `execute_workflow`.

**ScheduleKind** в config: `once`, `interval_minutes`, `hourly`, `daily`, `weekly`, `monthly`, `cron`, `specific_date`. Все считают next fire через Cronos + `TimeZoneHelper.Resolve(definition.TimeZoneId)` → DueAtUtc.

Interval trigger: `{ "minutes": 15 }`. Delay trigger: `{ "delay": "1.00:00:00" }` от момента arm (publish или событие-родитель — в v1 от publish).

Entity trigger config: `{ "entity": "user"|"support_ticket", "fields": ["status"] }` (`fields` для updated/changed).

Event trigger: `{ "event": "user.registered"|"ticket.created"|"ticket.status_changed" }` — мост из существующих классов событий.

---

## 7. Условия

`ConditionNode` — дерево, не плоский список.

```text
ConditionNode
  Op: equals | not_equals | greater_than | less_than | contains
      | is_empty | is_not_empty | changed | and | or | not
  Left: string (путь/литерал после bind)
  Right: string?
  Children: ConditionNode[]
```

`changed` имеет смысл только в entity.updated: сравнивает `trigger.before.{field}` и `trigger.after.{field}`.

Типы: string, number, bool, datetime, guid, null. Сравнение datetime в UTC. Неизвестный путь → условие false, в dry-run — warning в step output.

Не использовать DataTableConditionalOperator как runtime (другой смысл), но набор операторов сознательно близкий.

---

## 8. Переменные

Контекст run — JSON object:

```text
trigger.*          # payload триггера (уже отфильтрованный коннектором)
execution.startedAt
execution.runId
execution.dryRun
execution.nowMinusDays.{n}   # заранее вычисленные ключи 1,7,14,30,90
steps.{nodeName}.result
steps.{nodeName}.error
vars.{name}        # set_variable
connection.{id}    # без секретов
```

`IVariableBinder.Interpolate(template, ctx)` — Handlebars, без helper, которые ходят в БД. Перед bind — `EntityRegistry.FilterBag`. Попытка `{{user.passwordHash}}` не резолвится.

Валидация публикации: каждый `{{…}}` в config должен быть известен схемам trigger/entity/steps или `vars.*` / `execution.*`.

---

## 9. Исполнение

HTTP/UI **только** создаёт `AutomationRun` (`queued`) + `AutomationWakeup` due=now. Дальше worker.

```text
loop:
  recover expired leases
  lease due wakeups (batch 10)
  foreach wakeup:
    open scope
    if schedule/interval/delay-without-run → start new run from published version
    if wait/delay/retry → resume run at NodeId
    WorkflowExecutor.AdvanceUntilBlockOrEnd
    persist steps + context
    if waiting → insert wakeup DueAtUtc, status=waiting
    if succeeded/failed/cancelled → wakeup done
    SignalR владельцу
```

`AdvanceUntilBlockOrEnd`:

1. Проверить CancelledBy / run timeout / depth (max 3 для execute_workflow).
2. Взять готовые узлы (все входящие edges удовлетворены; для branch — выбран port).
3. StepRunner: retry до RetryLimit, backoff в wakeup (не sleep).
4. Delay/WaitUntil: step `waiting`, выход из цикла.
5. Идемпотентность: succeeded nodeId не выполняется снова.
6. Dry-run: email/notification/HTTP/CUD пишут OutputJson `skipped:dryRun`, find/branch/set_variable работают.

Не `Task.Delay` длиннее 1 секунды. Не выполнять цикл в MVC action.

Рекурсия CUD: `RecursionGuard.Depth` (AsyncLocal). Interceptor не эмитит trigger, если Depth>0, кроме явного `entity.updated` с `reentry: true` в config (v1 — всегда skip при Depth>0).

Heartbeat: каждые 10 с на running run. Старт worker: `running` с HeartbeatAt старше 2 минут → wakeup `recover`.

---

## 10. Планировщик

После Publish: `ScheduleArmer` удаляет pending wakeups этой automation с Kind schedule/interval и вставляет следующий DueAtUtc.

После успешного schedule run: считается next fire, новый wakeup. Пропуски (процесс лежал): catch-up **один** раз (не шторм).

TimeZone: поле определения, по умолчанию `SystemSettings.TimeZoneId` (`Asia/Yerevan`). Cronos `GetNextOccurrence(utc, tz)`.

Event-based scheduling = entity/event trigger + optional delay в config (`startAfter`).

---

## 11. Коннекторы (v1)

`IEntityConnector`: `Key`, `DisplayName`, `Fields`, `Can(op)`, `FindAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `ToBag(entity)`.

Все запросы через существующие сервисы + `IUserVisibilityService`. Owner автоматизации ≠ обход Super-скрытия: коннектор работает от **системного** scope, но поля Super-пользователя не отдаются не-Super владельцу автоматизации (фильтр как visibility для OwnerUserId). Если owner — User, Find User видит только себя и свою downline по тем же правилам, что дерево рефералов? **Нет:** слишком широко. v1: User-владелец может Find только записи, которые `CanViewUser(Owner)` видел бы в UI. Admin/Super — как их visibility.

Поля:

- **user** (key `user`): Email, FirstName, LastName, LastActivity, IsBlocked, EmailConfirmed, RoleId, MyReferralCode, ReferrerId, IsOnlayn. Ops: find, update (IsBlocked нет для User-роли; LastActivity нет). Trigger created. Нет create/delete, нет PasswordHash/GoogleId/AppleId/Balance.
- **support_ticket**: TicketID, Subject, Message, Status, UserId, AssignedTo, Note. Ops: find, create, update, delete если у **владельца автоматизации** есть `support_tickets.delete`. Triggers: created, updated, deleted (deleted = IsDeleted).
- **notification**: Type, Message, UserId, Link. Ops: find (свои получателю), create через `INotificationService`.
- **user_group**: Name. Ops: find. Рассылка — action send_notification/email по членам, не произвольный SQL.
- **email_template**: Name, Subject. Ops: find. Send Email ссылается на Name шаблона.

Нет: Payment, Transaction, Log write, SystemSettings, Permission*, tokens, Attachment как entity (вложения письма — через `IEmailSender`).

---

## 12. Внешние HTTP / webhook

`http_request`: method GET/POST/PUT/PATCH/DELETE, url из config или connection baseUrl, headers без хост-перезаписи, timeout 20 с, max response 256 KB. URL через `PublicHttpGuard` (тот же алгоритм, что `IsPublicHttpUrlAsync`: только http/https, не loopback, не RFC1918, не link-local, не metadata).

`webhook` action: POST signed body на URL владельца, тот же guard.

Входящий: `POST /Automation/hooks/{webhookPublicId}` — контроллер без cookie auth, проверка HMAC-SHA256 заголовка `X-Partnerly-Signature`. Тело → trigger context (json, max 64 KB). SSRF не применяется к входящему; rate limit по PublicId (MaxRunsPerHour определения).

---

## 13. Права и IDOR

Коды (добавить в `PermissionCodes.All`):

```
automation.view
automation.create
automation.edit
automation.delete
automation.publish
automation.run
automation.history
automation.connections
automation.manage
```

Seeder: Super/Admin = All (как сейчас). Employee и User = view, create, edit, delete, publish, run, history, connections — **без manage**.

`IAutomationAccessService.Require(id, code)`: не manage → `OwnerUserId == CurrentUser.UserId`. Manage (Super/Admin) — любая запись. 404 на чужие, не 403 (не светить id).

Webhook incoming не использует cookie; лимит и HMAC достаточны. Запуск от имени владельца автоматизации, не вызывающего.

---

## 14. SignalR

Хаб `AutomationHub` на `/automationHub`, тот же `CustomUserIdProvider`. Клиент после login подключается как к уведомлениям.

События **только** `Clients.User(ownerId)`:

- `automation.run.started`
- `automation.step.started` / `completed` / `failed`
- `automation.run.completed` / `failed` / `cancelled`
- payload: runId, automationId, nodeId, status — без чужих данных

Проверка: user id хаба == OwnerUserId (или manage не транслируем всем админам в v1 — только владельцу, чтобы не плодить подписки).

---

## 15. UI (PHASE 6, контракт сейчас)

Контроллер `AutomationController` : `_BaseController`.

| Action | Право | UI |
|---|---|---|
| Index | view | Dashboard KPI + вкладки |
| List | view | DataTable My Automations |
| Builder / BuilderSave | create/edit | Drawflow |
| Publish | publish | |
| ToggleActive | edit | |
| Copy | create | |
| Delete | delete | |
| Run / Cancel | run | |
| History / RunDetails | history | DataTable + шаги |
| Templates | view | |
| Connections | connections | |
| Settings | manage | лимиты по умолчанию (опционально v1: страница-заглушка «используются поля определения») |

Меню: секция **Automation** после Tools, пункт Dashboard + My Automations (остальное с дашборда). Иконка `bx-bot`.

Тексты только resx. StickyButtons на builder. Токены как у сайта, без site-wide glass.

Dry-run: кнопка Test в builder → `IsDryRun=true`.

---

## 16. Авторство / AI-запас

`IAutomationAuthoringService`:

- `CreateDraftAsync(owner, name)`
- `UpdateGraphAsync(id, graph)`
- `ValidateAsync(id)`
- `PublishAsync(id)`
- `CopyAsync(id)`
- `EnqueueRunAsync(id, reason, dryRun)`

UI и будущий AI Assistant вызывают **только** это. AI не получает DbContext и не исполняет код. PHASE 3–6 не подключают `IOpenAIService`.

---

## 17. Логирование

`ILogService` на create/publish/delete/run/cancel. Не писать GraphJson целиком и секреты. Ошибки шага — в `AutomationRunStep.Error` + SignalR.

---

## 18. Тесты (каркас PHASE 3, полнота PHASE 7)

Проект `Partnerly.Automation.Tests` (xUnit, net6.0). Без БД на unit:

- ConditionEvaluator (AND/OR/NOT, changed, empty)
- VariableBinder whitelist / отказ passwordHash
- GraphValidator
- CronScheduleCalculator TZ Asia/Yerevan
- PublicHttpGuard (localhost, 10.x, metadata)
- WebhookSignature
- RecursionGuard
- Два пользователя: AccessService (IDOR)

С in-memory EF (PHASE 7): run delay → wakeup, retry, cancel, recover lease, dry-run не зовёт email.

Не выключать тесты ради зелёной сборки.

---

## 19. Порядок реализации после этого документа

| Фаза | Что |
|---|---|
| 3 | Папка Automation, абстракции, registry, evaluator, executor на in-memory store, тесты ядра. Без миграции, без UI. `dotnet build` + unit tests. |
| 4 | EF configs, миграция, worker, Cronos, wakeup lease. Применить миграцию **только** если владелец явно ок на локальную БД. |
| 5 | Коннекторы, EventBridge, interceptor, email/notification, hub, права, seeder, PublicHttpGuard extract. |
| 6 | Меню, DataTable, Drawflow, хуки. |
| 7 | IDOR/SSRF/два пользователя, retry/cancel/recover. |
| 8 | Отчёт. |

PHASE 3 можно начинать по этому файлу без новых таблиц (интерфейсы + in-memory). Миграцию не создавать, пока не начат PHASE 4.

---

## 20. Явно не делаем в v1

- Hangfire / Quartz.
- React SPA.
- Произвольный SQL и C#.
- AI-генерация workflow.
- Google/Microsoft/Telegram/Slack OAuth connections (только каркас http/webhook).
- Payment/Balance.
- Выполнение workflow внутри HTTP request.
- `Task.Delay` на часы.
- Прод-миграции.
- Правка vendor CSS.
- Исправление KI-003/010 «заодно».
