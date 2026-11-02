# PHASE 1 — Анализ Partnerly и предложение архитектуры Automation

Дата: 2026-11-02.  
Срез кода: `origin/Cursor-Agent` @ `a84fd0d`.  
Код приложения **не менялся**. Жду проверки владельца перед PHASE 2/3.

Спецификация: [MASTER-SPECIFICATION.md](./MASTER-SPECIFICATION.md).

---

## Краткий вывод

Partnerly уже даёт слои, права, DataTable, почту, SignalR, EventBus, таймзоны и SSRF-фильтр. Этого достаточно, чтобы **не** строить второй портал.

Чего нет: устойчивого планировщика, графа workflow, изоляции автоматизаций по владельцу, тестового проекта. `OnlineStatusService` — таймер в памяти; `EventBus` — in-process, события теряются при рестарте в момент publish. Длительные Delay через `Task.Delay` здесь недопустимы.

Предложение: отдельный движок в `Partnerly/Automation/`, состояние в SQL Server, воркер `IHostedService` с арендой строк. Hangfire **не** брать. UI — Razor + DataTable + реальный визуальный редактор Drawflow (vanilla JS). После этого отчёта реализация не начинается, пока нет «да».

---

## 1. Что уже есть и что переиспользовать

| Возможность | Факт | Как использовать |
|---|---|---|
| Слои | Controller → Service → Repository → `AppDbContext` | Тонкий MVC снаружи; логика только в `Automation/` |
| DI | Scoped сервисы, Singleton EventBus/DataTableRegistry, один HostedService | `AddPartnerlyAutomation()` в `Program.cs` |
| Права | `Permission:{code}`, seeder только добавляет | Новые коды `automation.*` |
| Видимость | `IUserVisibilityService` | Entity-операции только через него + whitelist полей |
| Current user | Claims, без БД на каждое свойство | OwnerUserId сверять на сервере, не из JSON клиента |
| Почта | `IEmailSender` + Handlebars-шаблоны | Action Send Email вызывает сервис, не SMTP напрямую |
| Уведомления | `INotificationService` + `NotificationHub` | Send Notification / live-статус |
| SignalR | `/notificationHub`, user id = `NameIdentifier` | Отдельный `/automationHub`, тот же `IUserIdProvider` |
| Логи | `ILogService` | Аудит publish/run/cancel |
| Локализация | resx en/ru/hy | Все подписи Builder/меню в SharedResource |
| Таймзона | `SystemSettings.TimeZoneId` + `TimeZoneHelper` (IANA→Windows) | Расписания в TZ владельца, DueAt в UTC |
| Списки | свой DataTable | My Automations / History — не второй грид |
| Вкладки | `<partnerly-tabs>` | Dashboard: обзор / история / шаблоны |
| HTTP SSRF | `WebPageInspectorService.IsPublicHttpUrlAsync` (private) | Вынести в общий guard, вызывать из HTTP Action |
| События | `UserRegistered`, `SupportTickedCreated/StatusChanged` | Мост в Trigger Engine, EventBus не заменять |
| AI | `IOpenAIService` + `ai.use` | В v1 не звать; оставить `IAutomationAuthoringService` для будущего текста→граф |
| Секреты | user-secrets / Data Protection стек ASP.NET | Шифровать External Connections им, не plaintext |

Не дублировать: DataTable, EventBus, `Permission:`, SMTP, SignalR-хаб уведомлений, отдельную палитру CSS.

---

## 2. Решения (принимаю сам)

### 2.1 Scheduler: не Hangfire

Hangfire (.NET 6 совместим) — очередь джобов, не state machine. Delay на дни, retry шага, отмена, восстановление после рестарта всё равно требуют таблиц выполнения. Плюс свои таблицы, dashboard и лицензионный шум рядом с Partnerly UI.

**Выбор:** SQL-очередь `AutomationWakeups` + `AutomationWorker` (`IHostedService`).

- Cron: пакет **Cronos** (маленький, MIT, IANA TZ). Не NCrontab (хуже с TZ).
- Тики: раз в несколько секунд; due-строки берутся `UPDATE … SET LeaseUntil, LeaseOwner WHERE DueAtUtc <= now AND LeaseUntil < now`.
- Delay / Wait Until = шаг `Waiting` + wakeup, **не** `Task.Delay`.
- Несколько процессов Partnerly: протокол аренды готов сразу; сейчас достаточно одного инстанса.

### 2.2 Workflow: граф, не линейный список

Версия хранит JSON: `nodes[]` + `edges[]` (source, target, sourceHandle = `true`/`false`/`case:x`). Исполнитель — токен на узле, контекст переменных в JSON. Это сразу визуальный редактор и запас под AI (AI пишет тот же JSON через сервис).

### 2.3 Visual builder: Drawflow

Проект — Razor + jQuery, не SPA. React Flow / xyflow потребовали бы второй фронтенд — отказ.

**Drawflow** (vanilla JS, MIT): узлы, связи, drag, ветвления. Обёртка CSS под токены Partnerly (`#696cff` / dark `#8185ff`, radius 12px, Public Sans, Boxicons). Граф сериализуется в тот же JSON. Не картинка-заглушка.

Тестовый запуск: POST enqueue dry-run без side-effect флага на действиях записи / HTTP (preview log only) — отдельно от Publish.

### 2.4 Переменные

Синтаксис `{{path}}` как в спецификации. Интерполяция через Handlebars **только** по белому списку путей, собранному коннектором сущности. В контекст не попадают `PasswordHash`, OAuth id, SMTP, ключи.

Примеры: `{{trigger.user.email}}`, `{{ticket.status}}`, `{{execution.startedAt}}`, `{{steps.findUser.result.id}}`.

### 2.5 Тесты

Первый тестовый проект: `Partnerly.Automation.Tests` (xUnit). Это требует PHASE 3+, не сейчас. KI-011 закрывается только для Automation.

---

## 3. Папка `Partnerly/Automation/`

```text
Automation/
  Hosting/AutomationServiceCollectionExtensions.cs   # AddPartnerlyAutomation()
  Abstractions/     ITriggerHandler, IActionHandler, IConditionEvaluator,
                    IEntityConnector, IAutomationAuthoringService
  Registry/         Trigger/Action/Entity registries (singleton, handlers by code)
  Engine/           Executor, StepRunner, VariableBinder, Idempotency
  Conditions/       Nested AND/OR/NOT + операторы
  Triggers/         Manual, Schedule, Interval, Entity CUD, Webhook, Event, Delay
  Actions/          Email, Notification, CRUD via connector, Delay, WaitUntil,
                    Branch, Switch, SetVariable, HttpRequest, Webhook, RunWorkflow
  Scheduler/        CronClock, WakeupLeaser, AutomationWorker (hosted)
  Persistence/      сущности EF + IEntityTypeConfiguration
  Security/         OwnerGuard, WebhookSignature, PublicHttpGuard, SecretProtector
  Connectors/       User, SupportTicket, Notification, UserGroup, EmailTemplate
  Web/              тонкие контроллеры + ViewModels
```

Views: `Partnerly/Views/Automation/` (стандартный Razor).  
Статика: `wwwroot/assets/js/automation/`, `wwwroot/assets/css/automation-builder.css`.  
Тесты: отдельный проект в `Partnerly.sln`.

Существующие модули внутрь Automation не переносятся.

---

## 4. Таблицы SQL Server

Префикс `Automation`. Guid PK, аудит как у остальных сущностей, мягкое удаление у определений.

| Таблица | Назначение |
|---|---|
| `Automations` | Имя, OwnerUserId, IsActive, PublishedVersionId, лимиты (MaxConcurrent, MaxPerHour), Draft/Published |
| `AutomationVersions` | AutomationId, Number, GraphJson, PublishedAt, CreatedBy |
| `AutomationTriggers` | VersionId, Type, ConfigJson (cron, entity, webhook secret hash) |
| `AutomationWakeups` | DueAtUtc, Kind (schedule / delay / wait), RunId?, LeaseUntil, LeaseOwner |
| `AutomationRuns` | VersionId, Trigger, Status, Started/Ended, Error, IdempotencyKey, CancelledBy |
| `AutomationRunSteps` | RunId, NodeId, Status, Attempt, Input/Output JSON, Error, Duration |
| `AutomationConnections` | OwnerUserId, Kind (http/webhook), EncryptedPayload (Data Protection) |
| `AutomationTemplates` | Системные шаблоны графа |

Индексы: `(OwnerUserId, IsDeleted)`, `(Wakeups.DueAtUtc, LeaseUntil)`, уникальный `Runs.IdempotencyKey` (nullable filter), `(RunId, NodeId, Attempt)`.

Миграция EF — только в PHASE 4 и только после «да» на эту архитектуру. На уже существующей БД `EnsureCreated()` **новые таблицы не создаст** (KI-002). Нужен `dotnet ef database update` на стенде разработчика, не прод.

---

## 5. Сущности Partnerly — первый коннектор

Реестр коннекторов. Новый модуль = новый класс, движок не трогать.

| Сущность | Триггер | Find | Create | Update | Delete | Поля наружу |
|---|---|---|---|---|---|---|
| User | created (событие регистрации) | да, через visibility | нет | ограниченно | нет | Email, Name, LastActivity, IsBlocked, RoleId, Referral — **не** пароль/OAuth/Balance |
| SupportTicket | created / status changed / updated | да | да, через сервис | да (status, assignee, note) | только `support_tickets.delete` | Subject, Status, AssignedTo, UserId, TicketID |
| Notification | нет | да (свои) | да, через `INotificationService` | нет | нет | Type, Message, UserId |
| UserGroup | нет | да | нет | нет | нет | Name, члены (для рассылки существующим API) |
| EmailTemplate | нет | да | нет | нет | нет | Name, Subject |

Не подключать в v1: Payment, Transaction, Log (запись), SystemSettings, Permission*, AccessControlAuditLog, EmailConfirmationToken, Attachment (кроме как часть email).

Произвольный SQL и пользовательский C# — запрещены.

---

## 6. Точки интеграции (минимум правок вне `Automation/`)

1. `Program.cs` — `AddPartnerlyAutomation()`, hosted worker, `MapHub<AutomationHub>("/automationHub")`, подписка моста на EventBus.  
2. `AppDbContext.OnModelCreating` — `modelBuilder.ApplyConfigurationsFromAssembly` для Automation.  
3. `PermissionCodes` + `PermissionSeeder` — новые коды, seeder только добавляет.  
4. `_VerticalMenu.cshtml` — секция Automation (рядом с Tools).  
5. `SharedResource.*.resx`.  
6. Вынести SSRF-проверку URL из private-метода инспектора в общий helper (инспектор продолжает им пользоваться).

Коды прав:

- `automation.view` / `create` / `edit` / `delete` / `publish` / `run` / `history` / `connections`
- `automation.manage` — Super/Admin: чужие записи
- User и Employee: только свои (`OwnerUserId`)
- Super/Admin: все коды, как сейчас в seeder

HTTP-запрос workflow **только ставит** wakeup/run в очередь и сразу отвечает. Исполнение в worker.

---

## 7. UI

Секция меню **Automation**:

- Dashboard — KPI: активные, ошибки, последние запуски (`<partnerly-tabs>`).
- My Automations — DataTable (копия, вкл/выкл, запуск, история).
- Builder — полноэкранный Drawflow + панель свойств блока.
- History — DataTable запусков + карточка шагов.
- Templates / Connections / Settings — вкладки или отдельные actions того же контроллера.

Тема light/dark как у сайта. StickyButtons на форме автоматизации. Без `partnerly-glass.css` на весь app-shell (владелец откатывал).

---

## 8. Безопасность и надёжность

- IDOR: все GET/POST фильтруют OwnerUserId, кроме `automation.manage`.
- Webhook: путь `/Automation/hooks/{publicId}`, секрет только HMAC, не в URL.
- HTTP Action: только http/https, `PublicHttpGuard`, таймаут, лимит тела, запрет `file://`, metadata, loopback, RFC1918 (как инспектор).
- Рекурсия: `RunWorkflow` / Entity Update → глубина и флаг `AutomationCause` на SaveChanges, иначе цикл тикет→автоматизация→тикет.
- Идемпотентность: повтор failed run пропускает Successful steps.
- Лимиты: MaxConcurrent на определение, MaxPerHour, таймаут шага и всего run.
- Отмена: CancelledAt; worker проверяет перед каждым шагом.
- Recovery: при старте Running с просроченным heartbeat → wakeup retry.
- Секреты подключений: ASP.NET Data Protection, не `appsettings`.

---

## 9. Запас под AI

Не реализовывать ассистента. Нужен `IAutomationAuthoringService`: CreateDraft, UpdateGraph, Validate, Publish, EnqueueTest — те же проверки прав, что у UI. AI позже вызывает только этот сервис. GraphJson — контракт.

---

## 10. Риски

| Риск | Суть | Как закрыть |
|---|---|---|
| KI-002 | EnsureCreated не добавит таблицы на живую БД | Миграция + явный update стенда; прод не трогать |
| EventBus in-memory | Событие в момент рестарта пропадает | Триггеры CUD дублировать из SaveChanges interceptor (после commit) |
| KI-010 | Middleware после MapRoute | Worker не зависит от HTTP middleware |
| Нет тестов | KI-011 | xUnit в PHASE 7 обязательно, два пользователя |
| Drawflow + dark | Может спорить с vendor CSS | Свои токены, не править vendor |
| Apple/локальные ключи | Не относятся | Не трогать |
| Рекурсия автоматизаций | Entity Update внутри run | depth + skip interceptor |

Сборка PHASE 1 не запускалась (документы). Браузер не проверялся.

---

## 11. Порядок после «да»

PHASE 2 — детальный `ARCHITECTURE.md` в этой папке (схемы таблиц, JSON графа, коды триггеров/действий).  
PHASE 3 — ядро + registry без UI.  
PHASE 4 — миграция + worker.  
PHASE 5 — коннекторы, email, SignalR, права.  
PHASE 6 — меню, DataTable, Drawflow.  
PHASE 7 — тесты двух пользователей, SSRF, IDOR.  
PHASE 8 — финальный отчёт.

---

## 12. Нужно от владельца

1. Согласие с отказом от Hangfire (SQL wakeup + Cronos).  
2. Согласие с Drawflow, не React.  
3. Согласие на миграцию Automation-таблиц на **локальной** БД в PHASE 4 (прод — нет).  
4. User-роль: свои автоматизации **да** (SaaS) или только Employee+Admin?

Реализацию не начинаю, пока нет проверки этого отчёта.
