# Журнал технических решений

Писать только подтверждённое. Дата — из миграции, коммита или сессии. Если дата неизвестна — так и указать.

---

## TR-2026-11-03-09 — Overlay попапов светлее через CSS-переменные

- **Дата:** 2026-11-03
- **Суть:** фон за системными попапами (modal-backdrop, lookup, create-automation, plans, login, My Settings, OpenNewCase, Agents delete) берёт `--partnerly-glass-overlay` / `--partnerly-glass-overlay-blur`. Light `rgba(238,240,248,0.26)`, dark `rgba(10,12,28,0.36)`, blur 6px — страница за попапом читается.
- **Причина:** владелец: прежний фон (≈0.48/0.58 + blur 14px) слишком хмурый.
- **Затрагивает:** `partnerly-glass.css` и fallback в page-css попапов.
- **Последствия:** один рычаг на все glass-оверлеи. Браузер не проверялся. Коммит не делался.

---

## TR-2026-11-03-08 — Drawflow pan смотрит на classList[0]

- **Дата:** 2026-11-03
- **Суть:** перетаскивание пустого места двигает весь граф только если первый CSS-класс контейнера — `parent-drawflow` или клик попал в `.drawflow`. `#drawflow` поэтому начинается с `parent-drawflow`, затем `automation-canvas`.
- **Причина:** Drawflow 0.0.59 в `click()` берёт `ele_selected.classList[0]`, не `classList.contains`.
- **Затрагивает:** `_BuilderPanel.cshtml`, холст билдера.
- **Последствия:** класс `automation-canvas` нельзя ставить первым. Коммит не делался.

---

## TR-2026-11-03-07 — Пользовательский гид по действиям автоматизации

- **Дата:** 2026-11-03
- **Суть:** как настраивать каждый триггер и каждое действие описано на `/Documentation` (футер) и дублируется в правой панели редактора процесса для выбранного блока. Источник полей — схемы билдера и обработчики, не черновик DocsDraftNote.
- **Причина:** владелец: документация в системе, чтобы было понятно, как настраивать каждое действие.
- **Затрагивает:** `_DocsContent`, `_BuilderI18n`, `builder.js`, resx en/ru/hy, стили Documentation и билдера.
- **Последствия:** имена событий в справке совпадают с `AutomationEventBridge` (UserRegistered, SupportTicketCreated, SupportTicketStatusChanged). Create/Update/Delete в тексте ограничены тем, что реально умеют коннекторы. Коммит не делался. Браузер не проверялся.
- **Альтернативы:** только docs/ai для агента — отклонено, нужна документация в приложении.

---

## TR-2026-11-03-06 — My Settings как glass-попап, save on close

- **Дата:** 2026-11-03
- **Суть:** My Settings больше не страница со sticky buttons. Навбар открывает glass-оверлей. Close / Escape / клик по фону делают тот же POST, что Save; ошибки оставляют попап открытым.
- **Причина:** владелец явно: попап, без sticky, закрытие = сохранение.
- **Затрагивает:** `DashboardController.Settings`, `_MySettingsPopup`, `open-my-settings.js`, `page-mysettings.css`, `_NavbarPartial`, `_CommonMasterLayout`.
- **Последствия:** прямая ссылка `/Dashboard/Settings` тоже попап. Логика пароля (пусто = не менять) не менялась. Header — один заголовок Settings; левое меню без квадратных иконок; поля в одну колонку; скролл только в секции полей. Браузер не проверялся.
- **Альтернативы:** оставить sticky Save внутри попапа — отклонено явно.

---

## TR-2026-11-03-05 — OpenNewCase как glass-попап

- **Дата:** 2026-11-03
- **Суть:** Создание обращения в поддержку — стеклянный оверлей, не двухколоночная страница. Футер Support перехватывает переход и открывает попап на текущей странице (AJAX GET/POST). Прямой URL `/SupportTickets/OpenNewCase` показывает тот же попап. Карточка адаптивна: desktop ≤520px, tablet, phone sheet с safe-area.
- **Причина:** владелец: страница OpenNewCase должна быть попапом как другие, с адаптивкой на все устройства.
- **Затрагивает:** `SupportTicketsController.OpenNewCase`, `_OpenNewCasePopup`, `open-support-case.js`, `page-newsupportticket.css`, `_Footer`, `_CommonMasterLayout`.
- **Последствия:** без JS футер по-прежнему ведёт на `/SupportTickets/OpenNewCase`. Право `support_tickets.create` не менялось. Браузер не проверялся.
- **Альтернативы:** оставить полноэкранную страницу со glass-карточкой — отклонено явно.

---

## TR-2026-11-03-04 — Индикатор агента = IsActive; Delete — glass-попап

- **Дата:** 2026-11-03
- **Суть:** Точка слева от имени в меню Agents светится при `IsActive=true` и тухнет на паузе. Delete из context-меню открывает glass-попап `#partnerlyAgentsDelete` (на любой странице, не только хаб) и удаляет через `DeleteConfirmed`.
- **Причина:** владелец явно сменил смысл индикатора с live-run на active/paused и отказался от `window.confirm`.
- **Затрагивает:** `_VerticalMenu`, `partnerly-layout.css`.
- **Последствия:** live-run больше не красит точку. Попап живёт в layout, чтобы Delete работал вне `/Automation`. Браузер не проверялся.
- **Альтернативы:** переиспользовать hub overlay `_DeletePanel` — отклонено, `hub.js` есть только на Index.

---

## TR-2026-11-03-03 — Agents context menu: rename / pause / start / delete

- **Дата:** 2026-11-03
- **Суть:** Правый клик по имени агента в левом меню открывает glass-меню. Rename сохраняется с blur. Pause/Start задают `IsActive` явно через `ToggleActive(isActive)`. Delete подтверждается и зовёт `DeleteConfirmed`.
- **Причина:** владелец: кнопки в submenu агентов, и они должны реально работать.
- **Затрагивает:** `IAutomationAuthoringService.ToggleActiveAsync`, `AutomationAuthoringService`, `AutomationController.Rename/ToggleActive`, `_VerticalMenu`, `partnerly-layout.css`, `hub.js.setActive`, resx en/ru/hy.
- **Последствия:** пауза отменяет schedule/interval/start wakeup; старт published снова `ArmPublishedAsync`. Индикатор слева — `IsActive` (см. TR-2026-11-03-04), не live-run. Браузер не проверялся.
- **Альтернативы:** только toggle без желаемого состояния — отклонено, UI показывает Pause или Start.

---

## TR-2026-11-03-02 — Редактор процесса — страница, не overlay

- **Дата:** 2026-11-03
- **Суть:** Drawflow-редактор открывается как `/Automation/Builder/{id}`, не glass-popup на хабе. Кнопки тулбара и блоки палитры — мелкие стеклянные. Settings / Connections / детали run остаются overlay на этой странице.
- **Причина:** владелец: «редактор процесса пусть не будет попапом».
- **Затрагивает:** `AutomationController.Builder`, `Builder.cshtml`, `_BuilderPanel`, `hub.js`, `create-popup.js`, `builder.js`, `_VerticalMenu`, `automation-builder.css`.
- **Последствия:** старый `/Automation?edit={id}` редиректит на Builder. Хаб больше не грузит Drawflow. Браузер не проверялся.
- **Альтернативы:** оставить wide overlay — отклонено явно.

---

## TR-2026-11-03-01 — Glass попапы и DataTable chrome, не app-shell

- **Дата:** 2026-11-03
- **Суть:** Системные попапы и DataTable (фильтр, колонки, экспорт, bulk-delete, lookup, note, DeletePartial, кнопки тулбара/колонок, badge) используют тот же glass, что create-automation: полупрозрачный фон, blur 16px, радиус 28px, индиго-бордер. Слой `partnerly-glass.css` грузится последним после `partnerly-data-table.css`.
- **Причина:** владелец явно попросил стекло на всех DataTable-попапах/кнопках/badge и на всех системных попапах. Ранее общий glass app-shell откатывался — сайдбар и карточки страниц не трогаем.
- **Затрагивает:** `partnerly-glass.css`, `_CommonMasterLayout`, `_DeleteConfirmation`, hover transform у `.data-table-grid-action`.
- **Последствия:** миграций нет. Гостевой лендинг (`_LandingLayout`) слой не подключает. Браузер не проверялся.
- **Альтернативы:** править каждый page-css по отдельности — отклонено, один поздний слой перекрывает #fff/#151c2c.

---

## TR-2026-11-02-13 — Statistics Agents = живые Automation KPI

- **Дата:** 2026-11-02
- **Суть:** Вкладка Agents на Statistics больше не рисует случайные «игры». Числа и графики из `IAutomationStore` с фильтром владельца / `automation.manage`. Success rate = succeeded / (succeeded + failed). Очередь = queued + running + waiting.
- **Причина:** владелец: карточки и KPI секции Agents на реальных данных автоматизации; карточки не сдвигать при hover.
- **Затрагивает:** `DashboardController` Index/GetStats/GetGamesByPeriod, `DashboardViewModel`, `Views/Dashboard/Index.cshtml`, hover CSS (dashboard, dashboards, profile, referral, layout).
- **Последствия:** JSON ключи `gamesPlayed`/`victories`/`defeats`/`winRate` сохранены для существующего `dashboard.js`. Без права `automation.view` — нули. Браузер не проверялся.
- **Альтернативы:** отдельная вкладка Automation рядом с Agents — отклонено, секция уже называлась Agents.

---

## TR-2026-11-02-12 — Automation hub: одна панель, таблица, overlay вместо страниц

- **Дата:** 2026-11-02
- **Суть:** Экран автоматизации — Index-хаб. Список без DataTable, действия строки открывают glass popup, билдер Drawflow и подключения — в overlay того же стиля. Левое меню содержит только панель Automation.
- **Причина:** владелец: убрать пункты History/Templates/Connections/My Automations; не водить пользователя по страницам; таблица как у Grok.
- **Затрагивает:** `_VerticalMenu`, `AutomationController` (panel actions + AJAX JSON), `Index.cshtml`, partials `_BuilderPanel` / `_HistoryPanel` / `_SettingsPanel` / `_ConnectionsPanel` / `_RunDetailsPanel` / `_DeletePanel` / `_HubOverlay`, `hub.js`, `builder.js` init/destroy, `create-popup.js`, `automation-builder.css`, resx en/ru/hy.
- **Последствия:** миграций нет. Старые URL редиректят на Index с query (`edit`, `history`, `settings`, `run`, `connections`, `delete`). DataTable-опции List/History остаются в коде, хаб их не рендерит. Браузер не проверялся.
- **Альтернативы:** оставить DataTable на List — отклонено, владелец явно запретил DataTable на этом экране. Полноэкранные страницы билдера — отклонено, нужен popup.

---

## TR-2026-11-02-11 — Automation UX: glass create popup, delay amount/unit, no sticky

- **Дата:** 2026-11-02
- **Суть:** Создание автоматизации — стеклянный popup (имя, описание, опциональный шаблон); запись в БД только на POST Save, затем Builder. Delay в UI — число + единица; в графе `amount`/`unit`, движок ещё принимает legacy `delay`/`startAfter`. Число без двоеточия = секунды (`TimeSpan.TryParse("90")` иначе даёт 90 дней). Delay-триггер на publish ставит wakeup без `RunId`; republish отменяет только такие, не delay-шаги живых run. Sticky на билдере снят, кнопки в glass-toolbar.
- **Причина:** владелец: пользователь не должен гадать JSON; после сохранения сразу панель; sticky с автоматизации убрать.
- **Затрагивает:** `DelayDuration`, `DelayActionHandler`, `ScheduleArmer`, `AutomationWorker`, dispatcher startAfter, `AutomationController` Create GET/POST, Views Automation + `_CreateAutomationPopup`, `builder.js`, `create-popup.js`, `automation-builder.css`, resx en/ru/hy.
- **Последствия:** миграций нет. GET `/Automation/Create` редиректит на List. Шаблоны по-прежнему код-каталог. Браузер не проверялся.
- **Альтернативы:** Bootstrap modal — отклонено, владелец просил стеклянный стиль как landing/plans. Оставить Config JSON для «продвинутых» — отклонено, пользователь не должен гадать поля.

---

## TR-2026-11-02-10 — Automation v1: child workflow, wait_until, settings/templates/HTTP

- **Дата:** 2026-11-02
- **Суть:** `execute_workflow` — fire-and-forget enqueue child с `Depth+1`, тот же `OwnerUserId`, published+active; чужой/удалённый id как 404. Parent не ждёт конца child. `wait_until` не попадает в completed, пока condition false; `until` — permanent fail. `StepResult.Retryable=false` для SSRF, IDOR, timeout; transient (нет коннектора) по-прежнему retry + backoff 2^(attempt-1) сек, max 60. Шаблоны — код-каталог, не INSERT в `AutomationTemplates` (без разрешения на данные БД). HTTP connection payload через тот же `ISecretProtector`.
- **Причина:** владелец попросил закрыть процесс автоматизации, не PHASE 8 отчёт.
- **Затрагивает:** `ExecuteWorkflowActionHandler`, `WaitUntilActionHandler`, `StepResult`, authoring settings/templates, store connections, UI Templates/Settings/Connections, меню.
- **Последствия:** миграций нет. Child не блокирует parent. Google/Telegram connections по-прежнему вне v1.
- **Альтернативы:** синхронный Advance child в том же tick — отклонено, HTTP-запрос/worker не должен исполнять дерево вглубь; depth живёт на run.

---

## TR-2026-11-02-09 — Automation PHASE 7: IDOR 404, cancel wakeup, SSRF без retry-обхода

- **Дата:** 2026-11-02
- **Суть:** Чужой id по-прежнему `KeyNotFoundException` (как 404), не 403. Удалённое определение скрыто и от `automation.manage`. `CancelAsync` отменяет pending/leased wakeup этого run; `AdvanceAsync` не трогает already cancelled/succeeded/failed. SSRF на литералах IP режется `PublicHttpGuard.IsAllowedAsync` до HTTP-клиента; неудачный шаг всё ещё ретраится до `RetryLimit` (как create_record без коннектора). Dry-run не резолвит/не вызывает `IEmailSender`.
- **Причина:** PHASE 7 ARCHITECTURE §18–19; wakeup после cancel иначе мог Advance waiting-run.
- **Затрагивает:** `AutomationRunService`, `IAutomationStore` (`CancelPendingWakeupsForRunAsync`), `ExecuteWorkflowActionHandler` (depth), `Partnerly.Automation.Tests`.
- **Последствия:** `execute_workflow` child enqueue всё ещё stub («PHASE 5»). Браузер PHASE 6 не проверялся. Миграций нет.
- **Альтернативы:** не ретраить permanent SSRF — отклонено в v1, единый retry для любого StepFailed.

---

## TR-2026-11-02-08 — Automation PHASE 6: Razor UI + Drawflow + HMAC webhook

- **Дата:** 2026-11-02
- **Суть:** UI в `Automation/Web` + `Views/Automation`. Список — существующий DataTable. Билдер — вендорный Drawflow (MIT), граф по-прежнему `nodes[]+edges[]`; x/y только сохраняются. Входящий webhook без cookie: HMAC `X-Partnerly-Signature`, неизвестный publicId → 404. Секрет в `WebhookSecretHash` через Data Protection (поле не переименовывали, миграции нет).
- **Причина:** архитектура PHASE 6; React Flow по-прежнему отказ.
- **Затрагивает:** `Automation/Web`, `Views/Automation`, `_VerticalMenu`, SharedResource en/ru/hy, `wwwroot/vendor/drawflow`, `AutomationWebhookIngestService`.
- **Последствия:** шаблоны и HTTP connections в v1 — страницы-заглушки. Drawflow-маппинг branch/switch портов упрощённый (`true`/`false`, `default`/`case:0`). Браузерная проверка UI в этой сессии не выполнялась.
- **Альтернативы:** CDN Drawflow без вендора — отклонено, UI не должен падать без jsdelivr.

---

## TR-2026-11-02-07 — Automation PHASE 5: коннекторы и события Partnerly

- **Дата:** 2026-11-02
- **Суть:** Коннекторы вызывают существующие сервисы. Видимость — owner-based дубль правил Super/Admin/Employee/User, не `IUserVisibilityService` (он завязан на CurrentUser HTTP). CUD-триггеры с `SaveChanges` interceptor после успешного commit; EventBus даёт отдельные `event` имена (`UserRegistered`, `SupportTicketCreated`, `SupportTicketStatusChanged`), чтобы не дублировать `entity.created`. Interceptor молчит при `RecursionGuard.Current > 0`. `IAutomationChangeSink` optional в `AppDbContext` (LogService создаёт контекст без sink). `EntityRegistry` Scoped с коннекторами в приложении; в тестах — пустой Singleton. Email/HTTP/notification резолвятся из `context.Services`, чтобы dry-run тесты не требовали `IEmailSender`. Исходящий HTTP без auto-redirect.
- **Причина:** архитектура PHASE 5; цикл AppDbContext → interceptor → store → AppDbContext снят через `IServiceProvider` в interceptor.
- **Затрагивает:** `Partnerly/Automation/Connectors`, `Integration`, `Actions`, `Helpers/PublicHttpGuard`, `AppDbContext`, `PermissionCodes`, `PermissionSeeder`, `Program.cs`, `WebPageInspectorService`.
- **Последствия:** UI Automation ещё нет (PHASE 6). Входящий webhook controller — PHASE 6. Права `automation.*` появятся у Employee/User при следующем старте seeder (только добавление недостающих). Миграций в PHASE 5 нет.
- **Альтернативы:** вешать dispatcher на существующие EventBus handlers — отклонено, EventBus уже умеет несколько handlers.

---

## TR-2026-11-02-06 — Automation PHASE 4: SQL wakeup + Cronos

- **Дата:** 2026-11-02
- **Суть:** Состояние Automation в SQL Server. Worker `IHostedService` арендует `AutomationWakeups` (batch 10, lease 30 с, poll 1 с). Cron — пакет Cronos 0.8.4, TZ через существующий `TimeZoneHelper` (`Asia/Yerevan` → Windows `Caucasus Standard Time`). Тесты оставляют in-memory store; приложение — `AutomationStore` + worker. Authoring не исполняет граф в HTTP: только `queued` + wakeup `start`.
- **Причина:** архитектура PHASE 4; Hangfire по-прежнему не берём.
- **Затрагивает:** `Partnerly/Automation/Persistence`, `Scheduler`, `Hosting/AutomationWorker`, `AppDbContext`, Cronos 0.8.4, миграция `20261102134353_AddAutomationTables`.
- **Последствия:** миграция применена только на локальной БД. EnsureCreated существующую БД не дополнит (KI-002). Другие стенды не трогались. Сущности Automation **не** реализуют `IAuditableEntity`, чтобы worker SaveChanges не перетирал аудит текущим HTTP-пользователем.
- **Альтернативы:** Hangfire — отклонено в TR-2026-11-02-04.

---

## TR-2026-11-02-05 — Automation PHASE 3: in-memory ядро

- **Дата:** 2026-11-02
- **Суть:** PHASE 3 — движок в `Partnerly/Automation/` с `InMemoryAutomationStore`. Registry хранит `Type` handler’ов (Transient из scope), не экземпляры. Переменные — `{{path}}` через regex, не Handlebars (Handlebars уже в приложении; подключить на UI-шаблонах). Cron-выражения бросают `NotSupportedException` до PHASE 4/Cronos. Чужой id → `KeyNotFoundException` (как 404). Dry-run пропускает email/HTTP/CUD. Delay ставит wakeup, не `Task.Delay`. `JsonNode.DeepClone`/`DeepEquals` на net6.0 нет — helper `JsonNodes`. В `Program.cs` access-сервис с `Guid.Empty` до PHASE 5 (HTTP authoring ещё нет).
- **Причина:** архитектура PHASE 3 запрещает миграцию и UI; ядро должно проверяться unit-тестами.
- **Затрагивает:** `Partnerly/Automation/*`, `Partnerly.Automation.Tests`, `Program.cs` (`AddPartnerlyAutomation()`).
- **Последствия:** unit-тесты остаются на in-memory. Приложение с PHASE 4 использует EF (TR-2026-11-02-06). KI-011 закрыт только для Automation.
- **Альтернативы:** сразу EF InMemory — отклонено, миграция и worker — PHASE 4.

---

## TR-2026-11-02-04 — Automation: SQL wakeup, не Hangfire; граф + Drawflow

- **Дата:** 2026-11-02
- **Суть:** Universal Automation Platform живёт в `Partnerly/Automation/`. Состояние run/wakeup в SQL Server; воркер `IHostedService` с арендой строк. Hangfire/Quartz не используются. Workflow — JSON граф nodes/edges. Редактор Drawflow (vanilla JS). Cronos для cron+TZ. Коннекторы User/Ticket/Notification/Group/EmailTemplate. User и Employee владеют своими автоматизациями.
- **Причина:** Hangfire — очередь джобов, не state machine; React Flow ломает Razor-стек.
- **Затрагивает:** `docs/ai/Automation/*`. Код ядра — PHASE 3 (TR-2026-11-02-05). Миграции — PHASE 4.
- **Последствия:** миграция `Automation*` только локально по явному «да». Прод не трогать. EnsureCreated существующую БД не дополнит (KI-002).
- **Альтернативы:** Hangfire + continuations — отклонено; xyflow/React — отклонено.

---

## TR-2026-11-02-03 — Apple Sign In как Google

- **Дата:** 2026-11-02
- **Суть:** Sign in with Apple зеркалит Google: Challenge → `/signin-apple` → `AppleCallback` → вход или `AppleRegister`. `Users.AppleId`, флаг `EnableAppleLogin`. Ключи не в Git: `Authentication:Apple:ClientId` плюс либо `ClientSecret`, либо `TeamId`+`KeyId`+`PrivateKey` (.p8) в user-secrets / `secrets.json`.
- **Причина:** владелец попросил Apple на Login/Register, ключей ещё нет.
- **Затрагивает:** `AspNet.Security.OAuth.Apple` 6.0.15, `Program.cs`, `AccountController`, лендинг-попапы, System Preferences, миграция `20261102092927_AddAppleSignIn`.
- **Последствия:** миграция применена на локальной БД 2026-11-02. Return URL в Apple Developer: `{BaseUrl}/signin-apple`. Другие стенды не трогались.
- **Альтернативы:** только ClientId+ClientSecret без генерации JWT — поддерживается, если секрет уже выпущен.

---

## TR-2026-11-02-02 — DataTable: логические ширины + добор до контейнера

- **Дата:** 2026-11-02
- **Суть:** у колонок две ширины: логическая (`column.width` из кода, localStorage или ручного resize) и отображаемая. Свободное место контейнера делится пропорционально логическим ширинам видимых колонок, кроме Selected (всегда 60px) и Actions. Сохранение пишет только логические ширины, иначе добор накапливался бы. Selected всегда первая, `Reorderable = false`, `Resizable = false`.
- **Причина:** `table { width:100% }` растягивал Selected; `syncTableWidth` = сумма колонок оставлял пустое место справа.
- **Затрагивает:** `resize.js` (`applyLayout`), `reorder.js`, `columnSettings.js`, `_DataTable.cshtml`, `DataTableColumn.SelectedColumn`, `partnerly-data-table.css`.
- **Последствия:** смена видимости колонок и ширины контейнера (`ResizeObserver`) пересчитывают добор. Миграция не нужна.
- **Альтернативы:** `table-layout:auto` — отклонено, ломает resize и ellipsis.

---

## TR-2026-11-02-01 — Системные уведомления по коду

- **Дата:** 2026-11-02
- **Суть:** системные уведомления (регистрация, новый тикет, started/closed/reopened) хранят в `Message` код `$N:{resxKey}|arg`, а не локализованную строку. Показ идёт через `NotificationMessage.Format` / JS-шаблоны на культуре текущего запроса (язык получателя, когда он открывает сайт).
- **Причина:** отправитель на hy, получатель на en видел армянский текст — `Messages.Get` брал культуру запроса отправителя.
- **Затрагивает:** `NotificationMessage`, события регистрации и тикетов, навбар SignalR, `NotificationController` Data/Export/Edit, dashboard, site search.
- **Последствия:** массовая отправка из User Groups и Create в Notifications не кодируются. Миграция не нужна. Уже сохранённый текст в БД не меняется.
- **Альтернативы:** колонка Language у User — отклонено, без миграции; культура только в cookie.

---

## TR-2026-11-01-01 — DataTable: массовое удаление

- **Дата:** 2026-11-01
- **Суть:** колонка Selected во всех DataTable (Visible=false, кроме group members). Иконка удаления справа в тулбаре по `datatable.bulk_delete` (Super/Administrator через `PermissionCodes.All`). Кнопка видна, если Selected включена в гриде и есть выбранные строки. Процесс по одной записи с Start/Stop и подтверждением; итог пишется в `Log` через `ILogService.CreateLogAsync` (`BD` / Information).
- **Причина:** владелец попросил массовое удаление с прогрессом и журналом по существующим стандартам.
- **Затрагивает:** `DataTableColumn.HiddenSelected`, `PermissionCodes.DataTableBulkDelete`, `DataTablesController`, `bulkDelete.js`, `_DataTable.cshtml`, реестр колонок ViewModel.
- **Последствия:** селектор не требует поля Selected в БД (клиентский `selectedRows`). Удаление идёт существующими сервисами с теми же проверками, что и одиночное удаление. `userAccessAuditTable` без bulk delete. Миграция не нужна. Право появится после старта (`PermissionSeeder`).
- **Альтернативы:** один POST со всем списком ID — отклонён, нельзя остановить процесс и показать счётчики.

---

## TR-2026-10-31-04 — DataTable: ID по праву и колонка Note

- **Дата:** 2026-10-31
- **Суть:** колонка ID есть в таблице и в панели колонок только при праве `datatable.view_ids`. По умолчанию оно у Super, Administrator и Employee. У таблиц с полем `Note` в списке колонок есть Note (выкл. по умолчанию): иконка, заливка как у кнопки заметки на форме, клик открывает тот же вид попапа.
- **Причина:** владелец не хочет показывать ID без права; Note нужен как опциональная колонка.
- **Затрагивает:** `PermissionCodes.DataTableViewIds`, `PermissionSeeder`, `DataTableColumn.HiddenNote` / `DataTableColumnType.Note`, фильтр колонок в `_DataTable.cshtml`, `columns.js` / `render.js`, `partnerly-data-table.css`.
- **Последствия:** реестр DataTable не мутируется. JSON ответа по-прежнему содержит `Id` как KeyField. Note не на `rolePermissions`, `userPermissions`, `userAccessAudit` — у сущностей нет поля Note. Новое право появится после старта (`PermissionSeeder`).
- **Альтернативы:** прятать ID только CSS — отклонено, колонка осталась бы в настройках.

---

## TR-2026-10-31-03 — Панель колонок DataTable

- **Дата:** 2026-10-31
- **Суть:** в правом углу тулбара (справа от поиска) кнопка-шестерёнка открывает попап со всеми колонками из options. Пользователь включает/выключает колонки и меняет их порядок перетаскиванием. Actions в списке нет и остаётся последней. Стили попапа как у таблицы (фильтр-модалка: радиус, токены, чекбоксы, reset).
- **Причина:** владелец попросил настройки колонок в тулбаре.
- **Затрагивает:** `columnSettings.js`, `_DataTable.cshtml` (thead рендерит и скрытые колонки), `DataTableOptions.ColumnSettingsEnabled`, localStorage `columnVisibility`, `reorder.js` API `applyColumnOrder`.
- **Последствия:** хотя бы одна колонка должна остаться видимой. Reset возвращает порядок и видимость из исходных options.

---

## TR-2026-10-31-02 — Перестановка колонок DataTable

- **Дата:** 2026-10-31
- **Суть:** пользователь меняет порядок колонок перетаскиванием заголовка. Колонка Actions (`__actions`) не перетаскивается и остаётся последней. Ghost-чип следует за курсором, колонки сдвигаются с FLIP-анимацией. Порядок пишется в localStorage (`dataTable:{id}:columnOrder`), если `RememberState`.
- **Причина:** владелец попросил плавно менять места колонок, кроме Actions.
- **Затрагивает:** `reorder.js`, `datatable.js`, `_CommonMasterLayout`, `DataTableOptions.ColumnReorderEnabled`, `DataTableColumn.Reorderable`, `partnerly-data-table.css`.
- **Последствия:** клик по заголовку по-прежнему сортирует (порог ~6px до drag). Ресайз за правый край не начинает reorder. Экспорт с сервера идёт в исходном порядке реестра.
- **Альтернативы:** HTML5 Drag and Drop — отклонён из‑за системного ghost и конфликта с resize/sort.

---

## TR-2026-10-31-01 — Общий tab control

- **Дата:** 2026-10-31
- **Суть:** вкладки форм — `<partnerly-tabs>` / `<partnerly-tab>` и `partnerly-tabs.css`, стиль как на Edit Group. User Role Edit и User Groups используют один control.
- **Причина:** владелец: не копировать стили вкладок; сделать общий control для сайта.
- **Затрагивает:** `PartnerlyTabsTagHelper`, `partnerly-tabs.css` / `.js`, `UserRoleEdit.cshtml`, `UserGroups/Form.cshtml`, `_CommonMasterLayout`.
- **Последствия:** System Preferences пока на своих `.system-settings-tabs`.
- **Альтернативы:** копировать CSS Edit Group на User Role — отклонено.

---

## TR-2026-10-30-07 — Auth-попап без сборки из точек

- **Дата:** 2026-10-30
- **Суть:** Login/Signup на `/` проявляется прозрачностью; тексты лендинга гаснут. Светящиеся точки остаются фоном и соединяются с ближайшими соседями короткой линией, которая растёт с двух концов.
- **Причина:** владелец: точки не должны превращаться в попап; попап и тексты — плавное появление/исчезновение; точки как будто стремятся присоединиться друг к другу.
- **Затрагивает:** `landing-dots.js`, `landing-auth.js`, `landing.css`. Flip панелей не менялся.
- **Последствия:** `gatherToAuth` снят. `prefers-reduced-motion` — без линий и без fade.
- **Альтернативы:** сбор точек в силуэт карточки — предыдущая версия, снята.

---

## TR-2026-10-30-06 — Кэш SystemSettings в памяти

- **Дата:** 2026-10-30
- **Суть:** `GetCurrentAsync()` читает строку Id=1 из `IMemoryCache` (10 минут), копия на выдачу, чтобы запросы не делили tracked-сущность. Create/Update сразу перезаписывают кэш. `MaintenanceMiddleware` берёт настройки через сервис, а не отдельным SQL.
- **Причина:** владелец попросил не ходить в БД на каждый запрос; настройки — самая частая и редко меняющаяся строка.
- **Затрагивает:** `SystemSettingsService`, `Program.cs` (`AddMemoryCache`), `MaintenanceMiddleware`, `EmailConfirmationTokenService`.
- **Последствия:** счётчик `TaskTickedID` (`GenerateNextCodeAsync`) кэш не сбрасывает — номер тикета по-прежнему читается из БД. Права пользователя пока не кэшируются.
- **Альтернативы:** Redis — не брался, один процесс.

---

## TR-2026-10-30-05 — Контраст текста в dark mode

- **Дата:** 2026-10-30
- **Суть:** тёмная тема Partnerly задаёт `data-theme` / `data-bs-theme`, но `core.css` (Sneat/Bootstrap light-first) почти не содержит `[data-bs-theme=dark]` палитры. Подписи, body и `.text-muted` остаются `#384551` / `#646e78` / `#a7acb2`.
- **Причина:** владелец: в dark на формах, Documentation и других страницах текст «хмурый».
- **Затрагивает:** токены в `partnerly-layout.css`, helper-классы в `site.css`, legal-copy в `landing.css`, floating labels в Settings / System Preferences / auth.
- **Последствия:** страницы без своих dark-override тоже читают новые `--partnerly-text*`. Vendor `core.css` не менялся.
- **Альтернативы:** полноценный Bootstrap dark rebuild `core.css` — не брался, слишком широко.

---

## TR-2026-10-30-04 — Account-формы как попап на лендинге

- **Дата:** 2026-10-30
- **Суть:** вход, регистрация, забытый пароль и сообщения об успехе показываются стеклянным диалогом поверх `/`, а не страницами `_BlankLayout`.
- **Причина:** владелец попросил убрать отдельные account-страницы и выдержать стиль главной.
- **Затрагивает:** `Home/Index`, `_LandingAuthModal` и partials, `landing-auth.js`, `landing.css`, GET/POST `AccountController` Login/Register/ForgotPassword, успех ConfirmEmail и ChangePassword. Cookie `LoginPath` по-прежнему `/Account/Login` (302 на `/?auth=login`).
- **Последствия:** реферальная ссылка `/Account/Register?reff=` открывает попап регистрации. Google Challenge и Apple-заглушка не менялись. ConfirmForgotPassword, GoogleRegister, AccessDenied остаются страницами. Неиспользуемые `Views/Account/Login|Register|Forgot*` и экраны успеха удалены 2026-10-30.
- **Альтернативы:** полноценный SPA-auth — не брался, POST и antiforgery остаются MVC.

---

## TR-2026-10-30-03 — Гостевой search как чат без AI

- **Дата:** 2026-10-30
- **Суть:** на `/` ввод в поиск (Enter или кнопка) превращает лендинг в чат: поле уезжает вниз, заголовок и Login/Sign up скрываются. Ответ — локальная заглушка, не OpenAI. Под каждым ответом иконки Google и Apple.
- **Причина:** владелец попросил чат-сценарий сейчас, AI подключить позже.
- **Затрагивает:** `Home/Index`, `landing-search.js`, `landing.css`, GET `Account/GoogleLogin`, заглушка `Account/AppleLogin` (редирект на Login — Apple Sign In ещё нет).
- **Последствия:** AI не вызывается. Google-иконка ведёт на существующий Challenge. Apple — на Login, пока нет OAuth.
- **Альтернативы:** сразу звать `IOpenAIService` — отложено по просьбе владельца.

---

## TR-2026-10-30-02 — Гостевой лендинг без меню

- **Дата:** 2026-10-30
- **Суть:** неавторизованный `/` — полноэкранный лендинг без пунктов меню. Навбар: имя приложения (не логотип), язык и тема как у авторизованных, кнопка Login. Центр — canvas-нейросеть, которая сливается в мозг.
- **Причина:** владелец попросил очистить домашнюю для гостей и сделать современный визуал.
- **Затрагивает:** `_LandingLayout`, `Home/Index`, `Home/Privacy`, `landing.css`, `landing-dots.js`. Авторизованный пользователь по-прежнему уходит на Dashboard.
- **Последствия:** старые блоки features/видео/карточки сняты. Имя без анимации (Cormorant Garamond). Фон — мелкие мерцающие точки близких indigo/violet. Account-страницы без favicon-логотипа. Тема на лендинге та же, `partnerly-theme`. Коммит не делался.
- **Альтернативы:** нейросеть Canvas — снята по просьбе владельца.

---

## TR-2026-10-30-01 — Операционные настройки в SystemSettings

- **Дата:** 2026-10-30
- **Суть:** параметры, которые раньше были зашиты в коде, читаются из единственной строки `SystemSettings` (Id=1). Google-схема в DI остаётся; Challenge и кнопки входа режутся флагом. SMTP/Google/OpenAI секреты не переносились. Часовой пояс — IANA id, без нового NuGet (`TimeZoneHelper`).
- **Причина:** владелец попросил вынести перечисленные опции в System Settings и перевязать все hardcode-связки.
- **Затрагивает:** сущность и UI SystemPreferences; User lockout-поля; UserService, AccountController, EmailSender, NotificationService, SupportTicketService, AttachmentService, event handlers, AIController, layouts, DataTable JS/экспорт/фильтры дат, Insights.
- **Последствия:** миграция `20261030074752_ExpandSystemSettingsAndUserLockout` создана и применена на локальной БД 2026-10-30. Тикеты больше не назначаются на Super по email, если оба auto-assign поля пустые. Null в новых bool/int трактуется как прежнее поведение (кроме автоназначения). DataTable Date/DateRange сравнивает календарный день настроек в UTC, JSON ячеек не конвертируется (клиент форматирует). Коммит на ветке `Cursor-Agent` 2026-10-30; push ещё нет.
- **Альтернативы:** отдельная таблица key/value — не бралась, чтобы не плодить механизм рядом с однострочными настройками.

---

## TR-2026-10-29-03 — Глобальный поиск в навбаре

- **Дата:** 2026-10-29
- **Суть:** title страницы в навбаре заменён полем поиска (десктоп и планшет ≥768px, на телефоне скрыто). Запрос идёт на `/Search/Query`; сервис ищет разделы меню (локализованные названия + английские ключи) и данные модулей, которые пользователю можно видеть. Клиент — `fetch`, не jQuery, чтобы не включать `#globalLoader`.
- **Причина:** владелец просил искать по сайту (секции и данные), а не показывать заголовок текущей страницы.
- **Затрагивает:** `SearchController`, `ISiteSearchService`, `_NavbarPartial`, `site-search.js`, `navbar.css`, i18n `SiteSearch*`.
- **Последствия:** нового permission нет — доступ к данным тот же, что у списков (`*.view` + `VisibleUserIds`). Миграций и пакетов нет.
- **Альтернативы:** полнотекстовый SQL / отдельный индекс — не брались, чтобы не трогать схему и пакеты.

---

## TR-2026-10-29-02 — OK заметки и автоширина заголовков DataTable

- **Дата:** 2026-10-29
- **Суть:** попап заметки закрывается по OK на всех формах; в тулбаре DataTable справа от фильтра — кнопка подгона колонок под полный текст заголовка плюс иконки sort/filter. Колонка `__actions` подгоняется по ширине кнопок в строке, заголовок для неё не учитывается.
- **Причина:** JS писал только в `#hiddenNote` (его нет на большинстве форм) и падал до `modal.hide()`. Заголовки таблицы режутся `max-width:0` + ellipsis при `table-layout:fixed`.
- **Затрагивает:** `_StickyButtons.cshtml`, `_DataTable.cshtml`, `resize.js`, i18n `DataTableFitColumns`.
- **Последствия:** ширины после подгона пишутся в тот же localStorage, что и ручной resize. Миграций нет.
- **Альтернативы:** подгон по содержимому ячеек тела — не бралось, владелец просил только заголовки.

---

## TR-2026-10-29-01 — Назначенный видит тикет по уведомлению

- **Дата:** 2026-10-29
- **Суть:** `ViewCase` и связанные действия тикета больше не режутся только `CanViewUser(автор)`. Исполнитель (`AssignedTo` = текущий пользователь) видит и открывает тикет, вложения, список и экспорт. Ссылка View в уведомлении ведёт на `/SupportTickets/ViewCase/{id}`.
- **Причина:** назначенный пользователь получал Access Denied, если автор тикета вне его видимости (роль User видит только себя; Employee не видит Admin/Super).
- **Затрагивает:** `IUserVisibilityService.CanViewSupportTicketAsync`, `SupportTicketsController`, `AttachmentsController`, проекция `AssignedTo` в `QueryEntityExt`.
- **Последствия:** миграций нет. Право `support_tickets.view` по-прежнему нужно. Создание тикета за другого пользователя по-прежнему через `CanViewUser`.
- **Альтернативы:** отдельное permission «свои assigned» — не бралось, чтобы не плодить коды.

---

## TR-2026-10-28-02 — Модульные дашборды под Statistics

- **Дата:** 2026-10-28, уточнено 2026-10-29
- **Суть:** KPI Users / Tasks / Roles / Groups / Notifications через `IInsightsDashboardService` и `VisibleUserIds()`, секции по `*.view`. С 2026-10-29 это не отдельная страница, а вкладки на `/Dashboard` вместе с Games (первая). Пункт меню Dashboards снят; `/Dashboard/Dashboards` редирект на Index. В Roles: Users by role с фото (макс. 5, hover — имя и email) и Permissions by role. Журнал и email-шаблоны на этом экране не показываются. Без нового permission. Chart.js только у Games.
- **Причина:** владельцу нужна отдельная страница дашбордов по модулям, в дизайне сайта, light/dark.
- **Затрагивает:** `DashboardController.Index` (+ редирект Dashboards), `InsightsDashboardService`, `Index.cshtml`, `_InsightsPanels.cshtml`, меню, i18n en/ru/hy.
- **Последствия:** страница скроллится классом `dashboards-page`. View `Dashboards.cshtml` остаётся в репозитории, контроллер на него не смотрит. Миграций нет.
- **Альтернативы:** подменю Statistics, отдельные permission-коды, Chart.js — не брались.

---

## TR-2026-10-28-01 — Инспектор: HTML всех iframe

- **Дата:** 2026-10-28
- **Суть:** `observe.js` внедряется в каждый frame вкладки (`webNavigation` + `all frames`). Background склеивает HTML/текст/карточки. Same-origin iframe дополнительно читается через `contentDocument`. Cross-origin с opaque/sandbox может не открыться.
- **Причина:** владелец хочет полный HTML, включая страницы внутри iframe.
- **Затрагивает:** расширение v1.1.0, карточка Iframes, `web-inspector.js`.
- **Последствия:** нужно Reload unpacked и подтвердить право webNavigation.
- **Альтернативы:** только same-origin walk — недостаточно для чужих доменов в iframe.

---

## TR-2026-10-27-03 — Веб-инспектор: realtime через Chrome extension

- **Дата:** 2026-10-27
- **Суть:** страница Partnerly не может читать DOM чужого окна. Live-поток — unpacked-расширение `tools/partnerly-web-inspector-extension` (MutationObserver + history). Экран делится: окно сайта слева (~52% монитора, `chrome.windows` popup), Partnerly справа с индикаторами, которые вспыхивают на каждое изменение. HTML/текст идут из живого Chrome, не с серверного fetch. Настоящий always-on-top OS Chrome API не даёт — вместо этого окна стоят рядом и оба видны.
- **Причина:** владелец хочет realtime каждого изменения сайта на странице Partnerly.
- **Затрагивает:** расширение, `web-inspector.js/css`, `Views/WebInspector/Index.cshtml`, i18n.
- **Последствия:** без установленного расширения live нет. Нужно Load unpacked. Новых NuGet-пакетов нет.
- **Альтернативы:** Playwright/CDP — не брались.

---

## TR-2026-10-27-02 — Веб-инспектор: живой сайт в окне Chrome

- **Дата:** 2026-10-27
- **Суть:** «Открыть» сразу вызывает `window.open` (отдельное окно Chrome, не iframe). X-Frame-Options / sandbox больше не режут живой сайт. HTML, видимый текст и извлечённые данные по-прежнему приходят серверным fetch в вкладки Partnerly. Браузер **не даёт** читать DOM чужого окна (same-origin). Если Chrome режет popup — сообщение разрешить всплывающие окна.
- **Причина:** владелец хочет полноценный сайт в другом окне Chrome и данные на экране Partnerly.
- **Затрагивает:** `web-inspector.js`, `Index.cshtml`, CSS, i18n.
- **Последствия:** снимок сервера может отличаться от живого окна (логин, SPA). Без новых пакетов и без расширения Chrome.
- **Альтернативы:** расширение браузера или Playwright — не брались.

---

## TR-2026-10-27-01 — Веб-инспектор: серверный fetch, не iframe

- **Дата:** 2026-10-27
- **Суть:** экран `/WebInspector` в отдельной секции меню Tools. Сервер сам качает HTML (http/https), разбирает видимый текст, ссылки, картинки, формы, таблицы и отдаёт JSON + превью в `sandbox` iframe (`srcdoc`). Чужой сайт в iframe не читается из‑за CORS/`X-Frame-Options`. Локальные/приватные адреса блокируются (SSRF). Право `web_inspector.use` (Super/Admin все коды, Employee — добавлено в seeder). Без headless-браузера и без новых пакетов: SPA на чистом JS может открыться пустой.
- **Причина:** владельцу нужна страница «как браузер» и доступ к HTML для дальнейшей автоматизации.
- **Затрагивает:** `WebInspectorController`, `IWebPageInspectorService`, меню, `PermissionCodes`, i18n.
- **Последствия:** нужен рестарт, чтобы seeder добавил код права. Миграций нет.
- **Альтернативы:** настоящий headless Chrome / Playwright — новые пакеты, не брались.

---

## TR-2026-10-26-01 — Дерево рефералов без нового права и без новых пакетов

- **Дата:** 2026-10-26
- **Суть:** экран `/Dashboard/ReferralTree` в меню Main сразу под Statistics. Данные из `User.ReferrerId`; SVG-связи и pan/zoom на своём JS/CSS, без D3/npm. Lookup пользователя — только при `users.view`. Super не показывается, если смотрящий не Super. Потомки выбранного корня в дереве не режутся `CanViewUser` (иначе User не увидит свою сеть). Корень: сам пользователь, либо `CanViewUser`, либо узел из своей downline.
- **Причина:** владельцу нужен наглядный граф приглашений на каждого пользователя.
- **Затрагивает:** `UserService.GetReferralTreeAsync`, `DashboardController`, меню, i18n, `referral-tree.css/js`.
- **Последствия:** отдельного permission-кода нет; User видит своё дерево, Admin/Employee — чужие по правилам видимости корня. Миграций нет.
- **Альтернативы:** вложенное submenu Statistics, отдельный пакет визуализации — не выбраны, чтобы не плодить механизмы.

---

## TR-2026-10-25-01 — Документация постоянной памяти агента

- **Дата:** 2026-10-25
- **Суть:** Канонические инструкции в `AGENTS.md`, детали в `docs/ai/`, правила Cursor в `.cursor/rules/`.
- **Причина:** Восстановление контекста между сессиями и учёт работ.
- **Затрагивает:** только документацию и правила агента, не бизнес-логику.
- **Последствия:** агент обязан обновлять журнал после значимых задач.
- **Альтернативы:** держать знания только в чате — отвергнуто владельцем.

---

## TR-2026-10-25-02 — Секреты вне Git-истории ветки Cursor-Agent

- **Дата:** 2026-10-25
- **Суть:** OpenAI key и Google OAuth убраны из незапушенных коммитов; `secrets.json` в gitignore; загрузка optional `secrets.json` в `Program.cs`; значения в user-secrets. Ветка запушена на `origin/Cursor-Agent`.
- **Причина:** GitHub push protection отклонил push из‑за секретов в истории.
- **Затрагивает:** история 35 незапушенных коммитов (переписана до push), `appsettings.json` (пустые ClientId/ClientSecret), `.gitignore`, `Program.cs`.
- **Последствия:** локальный запуск зависит от user-secrets / `secrets.json`. Ключи ранее светились в отклонённом push — ротация рекомендована, не подтверждена.
- **Альтернативы:** unblock секретов на GitHub — не выбран.

---

## TR-2026-10-21 — Система Permission вместо Role.Type

- **Дата:** миграции `20261021145957_AddRolePermissionsSystem`, `20261024211314_deleteRoleType`
- **Суть:** таблицы Permissions / RolePermissions / UserPermissions / AccessControlAuditLogs; политики `Permission:{code}`; seeder кодов прав.
- **Причина:** в коде закомментирован старый RoleType/`ClaimAuthorize`; поле Type у роли удалено. Точная дискуссия выбора **не сохранилась**.
- **Затрагивает:** авторизация, Roles, Users, DbContext. UI матриц AccessControl снят 2026-10-25 в пользу DataTable в Roles.
- **Последствия:** покрытие политиками расширено 2026-10-25 (`PermissionCodes`, seeder Super+Admin = все коды, видимость записей по роли). `HasPermision` удалён. Seeder только добавляет RolePermissions, не снимает лишние. Кнопки DataTable фильтруются при рендере, не в singleton registry.
- **Альтернативы:** неизвестны (не выдумывать).

---

## TR-неизвестная-дата — Свои User/Role и cookie, не ASP.NET Identity

- **Дата:** неизвестна (уже в `InitialClean` и AccountController)
- **Суть:** сущности User/Role, BCrypt, cookie claims, опционально GoogleId.
- **Причина:** не задокументирована авторами.
- **Затрагивает:** весь вход и авторизация.
- **Последствия:** нет Identity stores, roles claims вручную; смена роли требует `LastRoleUpdateTime` + middleware.
- **Альтернативы:** не зафиксированы.

---

## TR-неизвестная-дата — Собственный DataTable вместо отдельного SPA

- **Дата:** неизвестна (серия коммитов datatable в истории ветки)
- **Суть:** серверные `DataTableOptions`, QueryService, Razor partial, JS-модули, Excel/CSV.
- **Причина:** не задокументирована.
- **Затрагивает:** все списки админки.
- **Последствия:** новый список обязан регистрировать уникальный table id. Пакет `datatables.net-dt` установлен, но основной поток — свой.
- **Альтернативы:** не зафиксированы.

---

## TR-неизвестная-дата — EnsureCreated при старте плюс миграции в репозитории

- **Дата:** неизвестна (`DbInitializer` + папка Migrations)
- **Суть:** при старте `EnsureCreated()`, параллельно ведутся EF-миграции.
- **Причина:** не задокументирована.
- **Затрагивает:** развёртывание и схему.
- **Последствия:** риск расхождения БД разработчика с миграциями. Менять это без разрешения нельзя.
- **Альтернативы:** только `Migrate()` — не внедрять без решения владельца.

---

## TR-неизвестная-дата — Внутрипроцессный EventBus

- **Дата:** неизвестна
- **Суть:** in-memory подписки, обработчики из DI, отдельный scope на publish.
- **Причина:** не задокументирована.
- **Затрагивает:** регистрация пользователя, тикеты (письма/уведомления).
- **Последствия:** нет гарантии доставки после рестарта, нет распределённой шины.
- **Альтернативы:** не зафиксированы.
