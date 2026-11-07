# План интеграций Partnerly

Создан: 2026-11-03. Ветка: `Claude-Agent`.  
Сервисы: **Telegram**, **Google Sheets**, **Gmail**, **Google Drive**, **WhatsApp**, **Outlook** (фаза 8).  
Цель: каждая интеграция — набор **действий** и **триггеров** для модуля Automation (меню Agents), плюс экран подключения.

Статусы задач: `[ ]` не начато, `[~]` в работе, `[x]` сделано и проверено.  

> **Актуально 2026-11-06:** тесты Automation 222/222 (вместе с `Integrations/`); шаблонов агентов 14; раздел `/Documentation` по интеграциям (7.1) по-прежнему не сделан — только карточки ядра; подробный аудит — `Automation/ARCHITECTURE.md` §22.
>
> **Статус 2026-11-03:** фазы 0–6 реализованы (решения D1–D7 приняты по рекомендации). `dotnet build` — 0 ошибок; тесты 133/133 (94 старых + 39 новых в `Partnerly.Automation.Tests/Integrations`). Шаблоны агентов — 8 штук (2026-11-04). Не сделано: раздел в `/Documentation` (7.1), полный сценарий с реальными аккаунтами (7.3), D5. Реальные вызовы Telegram / Google / Meta не выполнялись — только fake HTTP в тестах.  
Перед каждой фазой перечитать `AGENTS.md`, `docs/ai/Automation/ARCHITECTURE.md` и этот файл.

---

## 1. Как это встраивается в архитектуру (факт по коду 2026-11-03)

| Что уже есть | Где | Как используем |
|---|---|---|
| Действие = `IActionHandler` (`Type`, `Describe`, `ExecuteAsync`) | `Automation/Abstractions` | Каждое действие интеграции — свой handler |
| `SideEffectActionHandlerBase` — dry-run пропускает побочные эффекты | `Automation/Actions` | Наследуем для всех действий «отправить / записать» |
| Триггер = `ITriggerHandler` + запуск через `AutomationTriggerDispatcher` | `Automation/Triggers`, `Automation/Integration` | Входящие события интеграций идут через dispatcher (лимиты `MaxConcurrentRuns`, `MaxRunsPerHour` уже работают) |
| `AutomationConnection` (`OwnerUserId`, `Kind`, `EncryptedPayload`) + `ISecretProtector` (Data Protection) | `Automation/Persistence`, `Automation/Security` | Токены Telegram / Google / WhatsApp храним здесь, `Kind` = `telegram` / `google` / `whatsapp`. **Новая таблица для учётных данных не нужна** |
| Проверка владельца подключения (`connection.OwnerUserId == run.OwnerUserId`) | `HttpRequestActionHandler` | Тот же IDOR-паттерн во всех действиях |
| Права `automation.connections` | `AutomationController` | Управление подключениями интеграций — тем же правом (не плодим второй механизм) |
| Входящий webhook `POST /Automation/hooks/{id}` с HMAC Partnerly | `AutomationHooksController` | Не подходит для Meta / Telegram (у них свой формат подписи) — нужны свои endpoint'ы |
| `PublicHttpGuard` — запрет loopback / LAN | `Helpers`, `Automation/Security` | Внешние API (api.telegram.org, googleapis.com, graph.facebook.com) проходят |

### Что мешает расширению сейчас (нужно поправить в фазе 0)

1. `AutomationTypeCodes.Actions` / `.Triggers` — статические массивы; `GraphValidator` проверяет узлы **по ним**, а не по registry. Новый тип не пройдёт валидацию.
2. `CreateActionRegistry()` / `CreateTriggerRegistry()` в `AutomationServiceCollectionExtensions` — закрытые фабрики, извне не дописать.
3. Формы свойств узлов зашиты в `wwwroot/assets/js/automation/builder.js` (объект по типам), подписи — в `_BuilderI18n.cshtml` и resx.
4. Экран подключений умеет только `Kind = "http"` (`CreateHttpConnection`).
5. Ключи Data Protection явно не сохраняются (`AddDataProtection()` без `PersistKeysTo…`). Локально на Windows ключи живут в профиле пользователя; на сервере потеря ключей = нечитаемые токены всех подключений.

---

## 2. Структура папки `Partnerly/Integrations`

Код — внутри монолита `Partnerly.csproj` (отдельный class library не создаём, как и для Automation).  
Views по MVC-конвенции остаются в `Views/Integrations` и `Views/Shared/Integrations` (как `Views/Automation`).

```text
Partnerly/Integrations/
├── Common/
│   ├── IntegrationKinds.cs                  telegram / google / whatsapp
│   ├── IIntegrationConnectionService.cs     чтение/запись payload подключения (+ ISecretProtector)
│   ├── IntegrationConnectionService.cs
│   ├── IntegrationTriggerPublisher.cs       событие интеграции → AutomationTriggerDispatcher (с фильтром владельца и connectionId)
│   ├── IntegrationPollingWorker.cs          hosted service: опрос Telegram / Gmail / Sheets / Drive
│   ├── IIntegrationPoller.cs
│   └── IntegrationServiceCollectionExtensions.cs   AddPartnerlyIntegrations()
├── Telegram/
│   ├── TelegramClient.cs                    REST к Bot API
│   ├── TelegramConnectionPayload.cs         botToken, lastUpdateId, defaultChatId
│   ├── Actions/TelegramSendMessageActionHandler.cs
│   ├── Actions/TelegramSendDocumentActionHandler.cs
│   ├── Triggers/TelegramMessageReceivedTriggerHandler.cs
│   └── TelegramPoller.cs                    getUpdates (long polling)
├── Google/
│   ├── GoogleOAuthService.cs                consent URL, обмен code → tokens, refresh
│   ├── GoogleConnectionPayload.cs           refreshToken, accessToken, expiresAt, scopes, email, курсоры
│   ├── GoogleApiClient.cs                   HttpClient + автоматический refresh
│   ├── Sheets/  (Actions, Triggers, SheetsPoller)
│   ├── Gmail/   (Actions, Triggers, GmailPoller)
│   └── Drive/   (Actions, Triggers, DrivePoller)
├── WhatsApp/
│   ├── WhatsAppClient.cs                    Meta Cloud API (Graph)
│   ├── WhatsAppConnectionPayload.cs         accessToken, phoneNumberId, appSecret, verifyToken
│   ├── WhatsAppSignature.cs                 X-Hub-Signature-256
│   ├── Actions/…
│   └── Triggers/WhatsAppMessageReceivedTriggerHandler.cs
└── Web/
    ├── IntegrationsController.cs            экран подключений, OAuth callback Google
    ├── IntegrationWebhooksController.cs     входящие webhook Telegram / WhatsApp (когда есть публичный URL)
    └── ViewModels/
```

---

## 3. Коды типов (предложение)

| Сервис | Действия (`action`) | Триггеры (`trigger`) |
|---|---|---|
| Telegram | `telegram.send_message`, `telegram.send_document` | `telegram.message_received` |
| Google Sheets | `sheets.append_row`, `sheets.find_rows`, `sheets.update_row` | `sheets.row_added` |
| Gmail | `gmail.send`, `gmail.find_messages` | `gmail.message_received` |
| Google Drive | `drive.upload_file`, `drive.create_folder`, `drive.find_files` | `drive.file_added` |
| WhatsApp | `whatsapp.send_template`, `whatsapp.send_text` | `whatsapp.message_received` |

Каждый узел имеет поле `connectionId` (выбор подключения владельца). Выход шага (`StepResult.Output`) — JSON с id созданного объекта (messageId, rowNumber, fileId) для следующих узлов.

---

## 4. Решения, которые нужны от владельца

| # | Вопрос | Рекомендация |
|---|---|---|
| D1 | NuGet-пакеты Google (`Google.Apis.Sheets.v4`, `Gmail.v1`, `Drive.v3`, `Auth`) или чистый REST через `HttpClient` | **REST** без пакетов: OAuth и нужные вызовы простые, меньше зависимостей. Пакеты — только если REST станет громоздким |
| D2 | Где хранить курсоры опроса (Telegram `lastUpdateId`, Gmail `historyId`, Sheets последняя строка, Drive `pageToken`) | **В `EncryptedPayload` подключения** — без миграции. Альтернатива — таблица `IntegrationCursors` (нужна миграция и ок владельца) |
| D3 | WhatsApp: Meta Cloud API напрямую или Twilio | **Meta Cloud API**: бесплатный тестовый номер, без посредника. Входящие только через публичный URL (туннель) |
| D4 | Google OAuth-клиент: тот же, что для входа (`Authentication:Google`), или отдельный | **Тот же проект Google Cloud**, тот же клиент, добавить redirect `…/Integrations/Google/Callback` и scopes. Секреты остаются в `secrets.json` |
| D5 | Сохранение ключей Data Protection | **Сейчас** — оставить как есть (локально работает). **Перед сервером** — `PersistKeysToFileSystem` в папку вне Git, отдельная задача |
| D6 | Право на интеграции | **Переиспользовать `automation.connections`**. Отдельные `integrations.*` — только если понадобится разделить |
| D7 | Тесты | **В существующем `Partnerly.Automation.Tests`** (папка `Integrations/`), без нового тестового проекта |

---

## 5. Пошаговые задачи

### Фаза 0 — Точки расширения Automation (без новых сервисов)

- [x] **0.1** `GraphValidator`: проверять типы узлов через `ActionRegistry.IsKnown` / `TriggerRegistry.IsKnown`, а не `AutomationTypeCodes.Actions/Triggers`. Старые массивы оставить как список встроенных.
- [x] **0.2** Registry открыть для модулей: `AddPartnerlyAutomation` принимает регистрации из DI (например, `IEnumerable<AutomationHandlerRegistration>`), встроенные типы регистрируются тем же способом.
- [x] **0.3** `AutomationTriggerDispatcher`: метод для событий интеграций — по типу триггера + `connectionId` + `OwnerUserId` (чтобы событие одного пользователя не запускало чужие агенты).
- [x] **0.4** `builder.js`: формы свойств узлов расширяемые (`window.partnerlyAutomationNodeForms[type] = [...]`), новый тип `connection` — select подключений владельца по `Kind` вместо текстового поля Guid.
- [x] **0.5** Палитра билдера и `_BuilderI18n.cshtml` берут список типов из registry (`DescribeAll`) с группой (`Telegram`, `Google`, `WhatsApp`).
- [x] **0.6** Тесты: старые 94 зелёные + валидатор принимает зарегистрированный тип и отклоняет неизвестный.
- **Файлы:** `Automation/Engine/GraphValidator.cs`, `Automation/Hosting/AutomationServiceCollectionExtensions.cs`, `Automation/Registry/*`, `Automation/Integration/AutomationTriggerDispatcher.cs`, `wwwroot/assets/js/automation/builder.js`, `Views/Shared/Automation/_BuilderI18n.cshtml`.
- **Готово, когда:** сборка, все тесты, существующие агенты открываются и сохраняются в билдере.

### Фаза 1 — Общий слой `Integrations/Common` и экран подключений

- [x] **1.1** `IntegrationKinds`, `IIntegrationConnectionService` (create / get / update payload / delete с проверкой владельца, шифрование через `ISecretProtector`).
- [x] **1.2** ~~`IntegrationTriggerPublisher`~~ — не понадобился: pollers и webhook зовут `AutomationTriggerDispatcher.EnqueueIntegrationAsync` напрямую.
- [x] **1.3** `IntegrationPollingWorker` (hosted) + `IIntegrationPoller`: раз в N секунд обходит активные подключения, у которых есть опубликованные агенты с триггером этого типа. Ошибки — в `ILogService`, без токенов в логах.
- [x] **1.4** `AddPartnerlyIntegrations()` в `Program.cs` после `AddPartnerlyAutomation`.
- [x] **1.5** `IntegrationsController` + view: список подключений пользователя (Telegram / Google / WhatsApp), «Подключить», «Отключить», статус (последний успешный вызов / ошибка). Glass-попап как у Settings / Support. Пункт в меню — под Agents или в Tools (уточнить у владельца при реализации).
- [x] **1.6** Локализация en / ru / hy в `SharedResource.*.resx`.
- **Готово, когда:** экран открывается, подключение «пустого» типа создаётся и удаляется, чужое подключение недоступно (тест IDOR).

### Фаза 2 — Telegram (работает на localhost без туннеля)

- [x] **2.1** Подключение: пользователь вставляет токен бота от @BotFather → проверка `getMe` → сохранение в `EncryptedPayload`. Токен в UI больше не показывается.
- [x] **2.2** `TelegramClient`: `sendMessage`, `sendDocument`, `getUpdates`. Named `HttpClient`, таймаут, без логирования токена (он в URL!).
- [x] **2.3** Действие `telegram.send_message`: `connectionId`, `chatId`, `text` (с переменными `{{…}}`), `parseMode`.
- [x] **2.4** Действие `telegram.send_document`: файл из вложений Partnerly (`IAttachmentService`) по id.
- [x] **2.5** Триггер `telegram.message_received` через `TelegramPoller` (`getUpdates` + `offset` из курсора). Payload: chatId, from, text, date.
- [x] **2.6** Шаблон агента: «Новый тикет → сообщение в Telegram» и «Сообщение боту → создать тикет».
- [x] **2.7** Тесты: fake `HttpMessageHandler`, dry-run не отправляет, чужой `connectionId` → Failed, курсор сдвигается.
- **Позже (нужен публичный URL):** webhook вместо polling — `setWebhook` + `X-Telegram-Bot-Api-Secret-Token`.

### Фаза 3 — Google OAuth + Sheets

- [x] **3.1** `GoogleOAuthService`: consent URL (`access_type=offline`, `prompt=consent`, state с antiforgery), callback `GET /Integrations/Google/Callback`, обмен code → tokens, refresh по сроку. Scopes запрашиваются по нужным сервисам (Sheets / Gmail / Drive) — можно добавлять позже повторным consent.
- [x] **3.2** `GoogleApiClient`: Bearer + авто-refresh, обработка 401 / отозванного доступа (подключение помечается «нужно переподключить»).
- [ ] **3.3** Google Cloud Console (делает владелец): включить Sheets / Gmail / Drive API, OAuth consent screen в режиме Testing, себя в test users, redirect `http://localhost:{порт}/Integrations/Google/Callback`.
- [x] **3.4** `sheets.append_row` (spreadsheetId, лист, значения по колонкам), `sheets.find_rows` (колонка = значение), `sheets.update_row`.
- [x] **3.5** Триггер `sheets.row_added` (`SheetsPoller`, курсор — последняя обработанная строка).
- [x] **3.6** Шаблон: «Регистрация пользователя → строка в таблице».
- [x] **3.7** Тесты: refresh токена, append / find на fake handler, IDOR.

### Фаза 4 — Gmail

- [x] **4.1** `gmail.send` от имени подключённого аккаунта (MIME, UTF-8, HTML). Не путать с `send_email` (SMTP Partnerly) — оба остаются.
- [x] **4.2** `gmail.find_messages` (поисковый запрос Gmail, лимит результатов).
- [x] **4.3** Триггер `gmail.message_received` (`GmailPoller` через `users.history.list`, курсор `historyId`; фильтр — запрос / метка).
- [x] **4.4** Тесты.
- **Важно:** scope чтения Gmail — «restricted». В режиме Testing работает для test users; для публичного режима Google требует проверку приложения. Для localhost не мешает.

### Фаза 5 — Google Drive

- [x] **5.1** `drive.upload_file` — вложение Partnerly → папка Drive (multipart / resumable для больших), выход: fileId, webViewLink.
- [x] **5.2** `drive.create_folder`, `drive.find_files`.
- [x] **5.3** Триггер `drive.file_added` (`DrivePoller` через `changes.list`, курсор `pageToken`; фильтр — папка).
- [x] **5.4** Шаблон: «Новое вложение тикета → копия в Drive».
- [x] **5.5** Тесты. Scope — `drive.file` (только файлы, созданные приложением) по умолчанию; полный `drive` — только если владелец решит.

### Фаза 6 — WhatsApp (Meta Cloud API)

- [ ] **6.1** Владелец: Meta for Developers → приложение → WhatsApp → тестовый номер, получатели для теста, временный / системный токен.
- [x] **6.2** Подключение: accessToken, phoneNumberId, appSecret, verifyToken (всё в `EncryptedPayload`).
- [x] **6.3** `whatsapp.send_template` (вне 24-часового окна — только одобренные шаблоны), `whatsapp.send_text` (внутри окна).
- [x] **6.4** `IntegrationWebhooksController`: `GET` verify (`hub.challenge`), `POST` с проверкой `X-Hub-Signature-256` → триггер `whatsapp.message_received`.
- [ ] **6.5** Локально входящие — через Cloudflare Tunnel / ngrok; исходящие работают без туннеля.
- [x] **6.6** Тесты: подпись, verify, отправка на fake handler.

### Фаза 7 — Завершение

- [x] **7.1** `/Documentation` и `_DocsContent.cshtml`: раздел «Интеграции» (по сервису: действия, триггеры, поля результата) и «Если карточка упала», en / ru / hy (2026-11-06, проверено в браузере; армянский не вычитан носителем). Описание идёт по группам действий, а не отдельной карточкой на каждое.
- [x] **7.2** Шаблоны агентов в `AutomationTemplateCatalog` для каждой интеграции.
- [~] **7.3** Проверка в браузере (2026-11-03: страница `/Integrations` desktop/mobile/light/dark, диалоги, ошибка неверного токена Telegram с реальным API, палитра и свойства узла в билдере — ок; полный путь «подключить → агент → запуск» с реальными аккаунтами — нет): подключение, создание агента, запуск, история шагов.
- [x] **7.4** Обновить `Architecture.md`, `Automation/ARCHITECTURE.md`, `REVIEW_PROJECT.md`, `KNOWN_ISSUES.md`, `TECHNICAL_SOLUTIONS.md` (сверено по файлам 2026-11-06; `DATABASE.md` для интеграций не нужен — D2 без таблицы).
- [ ] **7.5** D5 — сохранение ключей Data Protection перед выкладкой на сервер.

---

### Фаза 8 — Outlook (Microsoft Graph), добавлена 2026-11-05

- [x] **8.1** `Kind = microsoft`, `MicrosoftOAuthService` (consent URL, обмен code, refresh с ротацией), `MicrosoftApiClient` (Bearer, авто-refresh, scope), экран подключения «Outlook» и callback `/Integrations/Microsoft/Callback`.
- [x] **8.2** Почта: `outlook.send`, `outlook.find_messages`, триггер `outlook.message_received` (`OutlookPoller`, курсор `outlookCursor`).
- [x] **8.3** Календарь: `outlook.create_event`, `outlook.find_events`.
- [x] **8.4** Формы билдера, переменные, подписи en / ru / hy, 2 шаблона агента, фильтр «Outlook» на хабе.
- [x] **8.5** Тесты (fake HTTP): `Partnerly.Automation.Tests/Integrations/OutlookIntegrationTests.cs`.
- [ ] **8.6** Владелец: приложение в Microsoft Entra (redirect `…/Integrations/Microsoft/Callback`, права Mail.Send, Mail.Read, Calendars.ReadWrite, User.Read, offline_access), секрет в `secrets.json` (`Authentication:Microsoft:*`).
- [ ] **8.7** Проверка с реальным аккаунтом и в браузере: подключить → агент → запуск → история шагов.
- [x] **8.8** Триггер `outlook.event_added` (`OutlookEventPoller`, курсор `outlookEventCursor`).
- **Позже:** вложения в письмах, ответ на письмо, Graph webhook вместо опроса (нужен публичный URL).

---

## 6. Безопасность (обязательно для всех фаз)

- Токены и секреты — только в `EncryptedPayload` или `secrets.json` / user-secrets. Не в `appsettings.json`, не в логах, не в `StepResult.Output`, не в GraphJson агента.
- Токен Telegram находится в URL запроса — не логировать URL целиком.
- Каждое действие проверяет: подключение существует, не удалено, `OwnerUserId == run.OwnerUserId`, `Kind` совпадает.
- Dry-run (`IsDryRun`) не отправляет сообщения и не пишет во внешние сервисы.
- Входящие webhook: проверка подписи до разбора тела, лимит размера (как `AutomationWebhookIngestService.MaxBodyBytes`).
- OAuth `state` привязан к пользователю и одноразовый.
- Миграции БД — только с явного разрешения владельца (см. D2).

---

## 7. Порядок и оценка

| Фаза | Зависит от | Объём |
|---|---|---|
| 0 | — | средний |
| 1 | 0 | средний |
| 2 Telegram | 1 | малый–средний |
| 3 Google + Sheets | 1 | средний (OAuth) |
| 4 Gmail | 3 | малый |
| 5 Drive | 3 | малый–средний |
| 6 WhatsApp | 1 | средний (webhook + туннель) |
| 7 | все | малый |

Рекомендуемый порядок: **0 → 1 → 2 → 3 → 4 → 5 → 6 → 7**. Каждая фаза — отдельный коммит после сборки и тестов.

---

### Фаза 9 — GitHub, добавлена 2026-11-07

- [x] **9.1** `Kind = github`, `GitHubOAuthService` (OAuth App, scopes `repo` / `public_repo`, `workflow`, `read:user`), подключение по токену, экран в `/Integrations`, callback `/Integrations/GitHub/Callback`, проверка `Test`.
- [x] **9.2** `GitHubApiClient` (REST, заголовки версии API, ошибки и лимиты, `SafePath`, `Repo`), 20 действий `github.*` (репозитории, задачи, pull request, ветки, коммиты, файлы, релизы, workflow и `github.api_request`).
- [x] **9.3** 5 триггеров опросом раз в минуту: `github.issue_opened`, `github.pr_opened`, `github.push`, `github.release_published`, `github.workflow_failed` (курсоры в payload подключения `githubCursors`).
- [x] **9.4** Формы билдера, переменные, значки, строки en / ru / hy, тесты (`GitHubIntegrationTests`, 29).
- [ ] **9.5** Владелец: создать GitHub OAuth App (Callback URL `…/Integrations/GitHub/Callback`), `Authentication:GitHub:ClientId` / `ClientSecret` в `secrets.json`.
- [ ] **9.6** Проверка с реальным аккаунтом и в браузере; шаблоны агентов для GitHub; раздел GitHub в `/Documentation`.
- **Позже:** webhook GitHub вместо опроса (нужен публичный адрес), постраничный обход списков, GitHub App вместо OAuth App, передача токена GitHub на ПК для push (`GIT_ASKPASS`, только с отдельным согласием).
