# Журнал работ Partnerly

Новые записи — сверху. Не отмечать успех без проверки.

---

## 2026-11-03 — OpenNewCase: иконка Message как у Subject

- **Суть:** владелец: в Subject иконка нормальная, в Message — нет.
- **Сделано:** padding-left textarea больше не перекрывается глобальным правилом; иконка Message на той же вертикали, что Subject (центр первой строки 40px).
- **Проверка:** CSS-only. Сборка не требовалась. Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — OpenNewCase: без текста Support, иконки полей по центру 40px

- **Суть:** владелец: в попапе support ticket иконки полей выглядят плохо; убрать текст Support.
- **Сделано:** kicker Support убран, заголовок один. Иконки 15px, `top: 20px` (центр поля), padding-left 32px — не перекрывают текст после глобальных 40px-полей.
- **Проверка:** CSS/Razor. Сборка не требовалась. Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — System Settings: иконки = высота поля, поля уже

- **Суть:** владелец: на экране System Settings иконки возле полей уменьшить до размера поля; уменьшить width полей.
- **Сделано:** стили 43×56 из `page-mysettings.css` больше не протекают на System Settings. Иконки 40×40 как поле; `form-floating` max-width 320px.
- **Проверка:** CSS-only. Сборка не требовалась. Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — Иконки у полей меньше и по центру; поля уже

- **Суть:** владелец: после уменьшения height полей уменьшить иконки рядом; lookup-иконка по центру, не внизу; уменьшить ещё и width.
- **Сделано:** lookup 18px, `top: 20px` (центр 40px-поля, не 50% обёртки с validation). Password/`input-group-text` иконки 15px. System Preferences: поля 40px, иконки без квадрата 22px. `max-w-400` 320px, `max-w-600` 460px.
- **Проверка:** CSS-only. Сборка не требовалась. Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — Поля системы 40px; удалены неиспользуемые страницы Automation

- **Суть:** владелец: поля в системе сделать того же размера, что в попапе My Settings; удалить страницы, которые больше не используются.
- **Сделано:** в `partnerly-layout.css` поля/select/form-floating — 40px, шрифт 0.8125rem, как в попапе (textarea выше). Удалены неиспользуемые views: `Automation/List`, `History`, `Templates`, `Settings`, `Connections`, `RunDetails` и `_AutomationNav` (GET этих action по-прежнему редиректят на Index).
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — My Settings: без растянутых зазоров, мелкие notification, иконки без квадратов

- **Суть:** владелец: огромные промежутки между полями убрать; карточки notifications мельче; на mobile иконки нормально и удобно; квадратные фоны у иконок секций/полей убрать.
- **Сделано:** ряд полей больше не растягивает промежутки (`align-content: flex-start`, gap 6px). Notification-карточки `height: auto`, padding 6px. Иконки секций/полей/уведомлений без квадратного фона (в т.ч. dark). Mobile: иконки по центру сверху, 44px круг, равный шаг.
- **Проверка:** CSS/Razor class. Сборка не требовалась. Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — My Settings: компактные поля, без лишних border, mobile icon-меню сверху

- **Суть:** владелец: убрать лишние border, мелкие тексты/поля/карточки уведомлений, меньше отступов между полями; на mobile меню сверху и только иконки.
- **Сделано:** внутренние рамки body/nav/секций/notification-карточек сняты. Поля 40px, gap ~0.4rem, мелкий шрифт. Notification-карточки компактные без квадратных иконок. Password-hint и описания секций скрыты. До 767px меню сверху, только иконки (`aria-label` сохранён).
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — My Settings: фиксированный размер попапа, меню слева в одну колонку

- **Суть:** владелец: попап не должен менять размеры; пункты левого меню — на одном уровне слева.
- **Сделано:** карточка 720×640 (с потолком по viewport). Левое меню всегда колонка, пункты `flex-start`, иконки одной ширины 18px. На узком экране сайдбар остаётся слева (148px), не уходит в горизонтальные вкладки.
- **Проверка:** CSS-only. Сборка не требовалась. Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — My Settings: компактный header, меню и поля

- **Суть:** владелец: убрать первое слово Settings и полный текст «Manage your profile…» из header; в левом меню маленькие иконки без квадратов и один текст на пункт; справа одно поле на строку; скролл только в секции полей.
- **Сделано:** в `_MySettingsPopup` остался один заголовок Settings. Пункты Profile / Change password / Notifications — иконка + одна подпись, без подзаголовков и шевронов. Поля и уведомления — `col-12`. Карточка `overflow: hidden`; скролл только у `.settings-fields` / `.notification-settings`.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s), 111 Warning(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — My Settings: glass-попап, сохранение при закрытии

- **Суть:** владелец: My Settings сделать попапом; sticky buttons убрать; при закрытии попапа сохранять как кнопка Save.
- **Сделано:** оверлей `#partnerlyMySettings`. Навбар Settings открывает попап на текущей странице. Sticky/Back/Undo/Save нет. Close, Escape и клик по фону шлют тот же POST, что Save; при ошибке попап остаётся. Прямой `/Dashboard/Settings` — тот же попап.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — OpenNewCase без User; My Profile без вкладок и My Team

- **Суть:** владелец: не показывать поле User в попапе обращения; на My Profile убрать tab control, полностью удалить My Team, уменьшить кнопку Copy referral link.
- **Сделано:** CreatorName убран из `_OpenNewCasePopup` (UserId по-прежнему с сервера). My Profile — одна страница About/Activity, вкладки и контент My Team удалены, рефералы больше не грузятся. Кнопка рефссылки компактная (28px).
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — OpenNewCase: glass-попап на всех устройствах

- **Суть:** владелец: страница OpenNewCase — стеклянный попап как другие, с адаптивкой для всех устройств.
- **Сделано:** оверлей `#partnerlySupportCase` (kicker/title/форма/Cancel+Send). Футер Support открывает попап на текущей странице через AJAX; прямой URL тоже попап. POST AJAX возвращает JSON. CSS: phone sheet, tablet, desktop, dark, safe-area.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — Коммит и push: glass UI, Builder-страница, Agents controls

- **Суть:** владелец: сделать commit и push накопленной работы после glass/Builder/Agents.
- **Сделано:** коммит `7788ec1` на `Cursor-Agent` — `partnerly-glass.css`, Builder как страница, context-меню Agents, индикатор `IsActive`, Delete-попап. Push на `origin/Cursor-Agent`.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated, ранее в сессии). Браузер недоступен.

---

## 2026-11-03 — Agents: свечение индикатора по IsActive; Delete — glass-попап

- **Суть:** владелец: точка слева от имени агента светится если активен, не светится если на паузе; Delete из context-меню открывает попап удаления автоматизации, не `window.confirm`.
- **Сделано:** индикатор рисуется по `IsActive` (`.is-on` пульсирует, `.is-off` серый без glow). Pause/Start обновляют точку сразу. Delete открывает `#partnerlyAgentsDelete` (glass) и шлёт `POST /Automation/DeleteConfirmed/{id}`; строка уходит из меню и с хаба.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Тесты не гонял — движок не менялся. Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — Agents: right-click rename / pause / start / delete

- **Суть:** владелец: в submenu Agents по правому клику — Rename (применяется при клике в другое место), Приостановить если active, Start если пауза, Delete; все должно работать.
- **Сделано:** контекстное glass-меню. Rename — инлайн, save на blur/Enter, Escape отменяет. Pause/Start пишут `IsActive` (не toggle вслепую); при паузе снимаются schedule-wakeup, при старте published снова вооружается. Delete — confirm + `DeleteConfirmed`. Права `automation.edit` / `delete`.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s). `dotnet test Partnerly.Automation.Tests` — 91 Passed. Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — Редактор процесса — страница, не popup; кнопки мелкие glass

- **Суть:** владелец: редактор процесса не попап; кнопки в нём маленькие и стеклянные.
- **Сделано:** `GET /Automation/Builder/{id}` снова полная страница. Хаб Edit, меню Agents, create и `?edit=` ведут туда. Settings/Connections/Run остаются overlay поверх страницы. Тулбар и палитра — компактные glass-кнопки.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — Glass: DataTable, DeletePartial, системные попапы

- **Суть:** владелец: после коммита все DataTable-попапы, кнопки тулбара, badge и кнопки в колонках, DeletePartial и системные попапы — в стеклянном стиле как create-automation.
- **Сделано:** слой `partnerly-glass.css` после DataTable CSS. Покрыты Bootstrap `.modal`, note/attachments/send-group/email-preview/DataTable-note, фильтр/колонки/экспорт/bulk-delete, lookup, `_DeleteConfirmation`. Кнопки тулбара и колонок, badge — pill glass. App-shell (меню, карточки страниц) не трогал.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Второй коммит не делался.

---

## 2026-11-03 — Коммит Automation + меню Agents

- **Суть:** владелец: сделать коммит накопленной работы.
- **Сделано:** `b406307` на `Cursor-Agent` — Automation PHASE 3–7, хаб, Agents KPI, меню Agents/футер, тесты, миграция `AddAutomationTables`. Push не делался.
- **Проверка:** коммит создан. Секреты в коммит не входили.

---

## 2026-11-03 — Dropdown профиля как меню; язык чуть компактнее

- **Суть:** владелец: в dropdown фото профиля размер пунктов как в левом меню; в dropdown языка пункты немного меньше.
- **Сделано:** пункты профиля — `min-height: 34px`, шрифт `0.81rem`, иконки `16px`, padding как у `.partnerly-menu-link`. Язык: меньше padding, флаги `22×16`, код `8px`.
- **Проверка:** CSS, сборку не запускал. Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — Agents: плюс при закрытой секции; поиск в навбаре правее

- **Суть:** владелец: при закрытой секции Agents оставить «+»; если само меню свёрнуто — как сейчас (пункт Agents). Поиск в навбаре чуть правее.
- **Сделано:** «+» виден, пока сайдбар развёрнут, даже если секция Agents свёрнута. Пункт-иконка Agents только при `partnerly-sidebar-collapsed`. Поиск: `margin-left: 20px`.
- **Проверка:** CSS, сборку не запускал. Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — Меню Agents: live-список после create, закрытая секция как пункт

- **Суть:** владелец: после добавления автоматизации submenu не обновлялся; если Agents закрыт и иконка секции не видна — не показывать «+», а пункт Agents как обычный menu item.
- **Сделано:** `PartnerlyAgentsMenu.add/rename/remove` обновляет имена сразу после create/save/delete. Закрытая секция и свёрнутый сайдбар прячут заголовок с «+» и показывают пункт Agents с иконкой бота; клик раскрывает секцию (в узком сайдбаре ведёт на хаб).
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-03 — Меню: иконка Agents, без сдвига ширины, стрелки секций у текста

- **Суть:** владелец: слева от Agents иконка; при наведении меню не менять ширину; стрелки открыть/закрыть сразу после текста и только при наведении на меню.
- **Сделано:** у Agents `bx-bot`. Скроллбар больше не сжимает контент: `scrollbar-gutter: stable`, hover не трогает padding. Стрелки секций стоят сразу после подписи (`flex-start`), видны при hover/focus меню.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-02 — Меню Agents: секция-текст, плюс, индикатор процесса, давность

- **Суть:** владелец: Agents не пункт меню, а текст секции с открытием/закрытием; справа «+» на страницу Automation; у имён слева индикатор «в процессе», справа давность последнего действия (4d / 11m).
- **Сделано:** заголовок как у Main/Settings, шеврон открывает список. «+» → `/Automation?create=1` (на хабе сразу попап создания). Имена: зелёная точка если run queued/running/waiting, иначе серая; справа compact ago по последнему run (иначе дата определения). Свёрнутый сайдбар прячет имена, оставляет «+».
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-02 — Меню: Statistics chart-иконка, Agents как пункт после Settings

- **Суть:** владелец: иконка Statistics по смыслу имени; переименовать Automation → Agents; убрать dropdown-секцию Agents; сам пункт Agents — dropdown, имена автоматизаций — submenu без иконок; блок после Settings.
- **Сделано:** Statistics — `bx-bar-chart-alt-2`. Секция-заголовок Agents снята. Пункт Agents (иконка бота) стоит после Settings, открывает хаб; стрелка раскрывает имена без иконок. Свёрнутый сайдбар прячет submenu.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-02 — Меню Agents, Documentation/Support в футере

- **Суть:** владелец: убрать из левого меню секции Documentation и Support; в футере чуть увеличить эти ссылки и добавить иконки; в меню добавить секцию Agents со списком имён автоматизаций и небольшим отступом сверху.
- **Сделано:** секция Miscellaneous (Documentation + Support) снята с `_VerticalMenu`. Вместо Automation — секция Agents: хаб `/Automation` и до 40 видимых имён (`?edit={id}`). Отступ `.partnerly-menu-agents` 20px. Футер: `bx-file` / `bx-support`, шрифт ссылок 13px (мобильный 12px). Ключ resx `Agents`. Tasks в Settings не трогал — это админский список тикетов.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Браузер недоступен. Коммит не делался.

---

## 2026-11-02 — Меню: пункты чуть меньше, Web Inspector и Referral Tree скрыты

- **Суть:** владелец: немного уменьшить пункты меню; скрыть Web Inspector и Referral Tree.
- **Сделано:** высота/шрифт/иконки пунктов уменьшены в `partnerly-layout.css`. Referral Tree и секция Tools (Web Inspector) остались в разметке с `hidden`. Страницы по URL не трогал.
- **Проверка:** CSS/разметка, сборку не запускал. Браузер недоступен. Коммит не делался.

---

## 2026-11-02 — Statistics / Agents: живые KPI автоматизации; карточки без сдвига

- **Суть:** владелец: на Dashboards секция Agents — реальные данные Automation; карточки на сайте не сдвигаются при hover.
- **Сделано:** вкладка Agents больше не на случайных «играх». KPI: число автоматизаций, успешные запуски, очередь (queued/running/waiting), success rate по succeeded/(succeeded+failed). График и refresh (`GetStats` / `GetGamesByPeriod`) считают запуски из store с тем же фильтром владельца/manage. Hover `translateY` снят с dashboard/KPI/profile/referral карточек; в layout — страховка `transform: none`.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Автотесты не гонял — движок не менялся. Браузер недоступен. Коммит не делался.

---

## 2026-11-02 — Automation hub: таблица + попапы, меню только панель

- **Суть:** владелец: в левом меню оставить только панель автоматизации; на Index убрать тексты/табы, показать все автоматизации и кнопку создания; таблица в стиле Grok без DataTable; внизу готовые шаблоны; у строки History / Settings / Edit / Delete открывают popup; редактор процесса и подключения в том же стиле.
- **Сделано:** меню Automation — один пункт на Index. Index: glass-таблица (имя, статус, дата, иконки действий) + Create + сетка шаблонов. History / Settings / Delete / Builder / Connections / Run details — стеклянные overlay (`BuilderPanel` и др.), не отдельные страницы. Старые GET List/History/Templates/Settings/Connections/Builder/Delete/RunDetails редиректят на Index. Create POST по AJAX сразу открывает билдер. DataTable для этого хаба не используется.
- **Проверка:** `dotnet build Partnerly.sln` — 0 Error(s) (isolated OutputPath). Автотесты не гонял — логика движка не менялась. Браузер недоступен. Коммит не делался.

---

## 2026-11-02 — Automation: все экраны в стиле glass-create

- **Суть:** владелец: все экраны автоматизации в стиле попапа создания (стекло, kicker, lead).
- **Сделано:** общая `_AutomationNav` + `.automation-page` / `.automation-glass` на Dashboard, List, History, Templates, Connections, Settings, Builder, RunDetails. History больше не голый DataTable. Кнопки/поля как у create-popup. Site-wide glass не трогал.
- **Проверка:** `dotnet build Partnerly.sln` (isolated OutputPath). Автотесты не гонял — логика движка не менялась. Браузер недоступен. Коммит не делался.

---

## 2026-11-02 — Automation UX: glass create, labeled delay, no sticky

- **Суть:** владелец: завершить процесс; delay удобный (не JSON/`00:00:05`); новая автоматизация в стеклянном popup; после Save сразу билдер; sticky-кнопки убрать с экранов автоматизации.
- **Сделано:** GET Create больше не создаёт Untitled. POST Create (имя, описание, опциональный шаблон) → Builder. Popup на Dashboard / My Automations / Templates. Delay: amount+unit (сек/мин/час/день), legacy TimeSpan/int секунд сохранён; число без `:` — секунды, не дни. Delay-триггер армится при publish (one-shot). Свойства узлов — подписанные поля, без Config textarea. Builder toolbar Back/Save/Publish/Test/Run без `_StickyButtons`.
- **Решение:** TR-2026-11-02-11.
- **Проверка:** `dotnet test Partnerly.Automation.Tests` — Passed 90, Failed 0 (isolated OutputPath). Браузер в этой сессии недоступен — UI глазами не кликался. Коммит не делался. PHASE 8 отчёт не писался.

---

## 2026-11-02 — Automation: закрытие функциональных дыр v1

- **Суть:** владелец: отчёт не нужен, полностью завершить процесс автоматизации. Без коммита.
- **Сделано:** `execute_workflow` ставит child run того же владельца (чужой id → not found). `wait_until` заново проверяет условие, по `until` падает без retry. Retry backoff 1..60 с; SSRF/IDOR/timeout не ретраятся. Лимиты/TZ на Settings. Шаблоны из каталога кода (не seed БД). HTTP connection: create/delete, `http_request` клеит baseUrl. Меню History/Templates/Connections. Heartbeat на каждом шаге, run timeout.
- **Решение:** TR-2026-11-02-10.
- **Проверка:** `dotnet test Partnerly.Automation.Tests` — Passed 81, Failed 0 (isolated OutputPath, живой Partnerly.exe). Браузер не проверялся. Коммит не делался. PHASE 8 отчёт не писался.

---

## 2026-11-02 — Automation PHASE 7: IDOR/SSRF, retry/cancel/recover

- **Суть:** владелец: «idi dalshe», без коммита. PHASE 7 — тесты безопасности и исполнения: два пользователя, SSRF, retry до Fail, cancel queued/waiting, recover stale running, dry-run не зовёт email.
- **Сделано:** чужой Get/Update/Publish/Delete/Enqueue/Copy → `KeyNotFoundException`; manage видит чужое, удалённое скрыто и от manage. `PublicHttpGuard` — localhost, RFC1918, CGNAT, metadata, IPv6 ULA, `IsAllowedAsync` на литералах. Live `http_request` на `127.0.0.1` падает без исходящего запроса. Retry `create_record` без коннектора: 3 attempt → Failed. Cancel снимает pending/leased wakeup run; Advance на terminal no-op. Worker поднимает stale Running. Dry-run: `IEmailSender` spy Calls=0; live mail Calls=1. Denylist binder расширен. Лимиты dispatcher MaxConcurrent / MaxRunsPerHour. `execute_workflow` режет depth≥3; enqueue child по-прежнему stub.
- **Решение:** TR-2026-11-02-09.
- **Проверка:** `dotnet test Partnerly.Automation.Tests` — Passed 75, Failed 0 (сборка в отдельный OutputPath: `Partnerly.exe` был занят живым процессом). Браузер не проверялся. Коммит не делался.

---

## 2026-11-02 — Automation PHASE 6: UI, Drawflow, входящий webhook

- **Суть:** владелец: без коммита, идти дальше. PHASE 6 — дашборд, DataTable, билдер Drawflow, входящий webhook. Без AI.
- **Сделано:** меню Automation (Dashboard + My Automations). `AutomationController`: KPI, список, билдер, publish/run/copy/delete, история, connections, templates/settings-заглушки. Drawflow 0.0.59 в `wwwroot/vendor/drawflow`. Входящий `POST /Automation/hooks/{webhookPublicId}` с HMAC, 404 на неизвестный id. Секрет webhook хранится через `ISecretProtector`. Тексты en/ru/hy.
- **Решение:** TR-2026-11-02-08.
- **Проверка:** `dotnet test Partnerly.Automation.Tests` — Passed 39, Failed 0. Браузер в этой сессии недоступен — UI глазами не кликался. Коммит не делался.

---

## 2026-11-02 — Automation PHASE 5: коннекторы и интеграция Partnerly

- **Суть:** владелец: «nachinay» после паузы PHASE 5. Коннекторы, EventBridge, interceptor, email/notification, hub, права, PublicHttpGuard. Без UI/Drawflow (PHASE 6).
- **Сделано:** User / support_ticket / notification / user_group / email_template коннекторы (видимость по владельцу автоматизации, не `IUserVisibilityService`). EventBus → `event` trigger; SaveChanges → `entity.*` после commit, skip при `RecursionGuard.Current > 0`. Реальные `IEmailSender` / `INotificationService` / CUD через существующие сервисы. HTTP/webhook: named client `AutomationOutbound` 20 с, max 256 KB, без redirect. `AutomationHub` `/automationHub`. Права `automation.*` + seeder Employee/User без `manage`. Access в приложении — `CurrentUserAutomationAccessService`. Общий `Helpers.PublicHttpGuard` для инспектора и Automation.
- **Решение:** TR-2026-11-02-07.
- **Проверка:** `dotnet test Partnerly.Automation.Tests` — Passed 36, Failed 0. Браузер не нужен (нет UI). Коммит не делался. Прод/другие стенды не трогались.

---

## 2026-11-02 — Automation PHASE 4: EF, Cronos, worker

- **Суть:** владелец: идём дальше на следующий phase; ок на локальную миграцию. PHASE 4 — таблицы Automation*, планировщик, `IHostedService`. Без UI и без коннекторов.
- **Сделано:** Fluent-конфиги, `AutomationStore`, `ScheduleArmer`, `WakeupLeaser`, `AutomationWorker` (poll 1 с, lease 30 с). Cronos 0.8.4 для cron + TZ через `TimeZoneHelper`. `Program.cs`: `AddPartnerlyAutomation(useDatabase: true)`. HTTP/authoring только ставит `queued` + wakeup; worker делает Advance. Тесты по-прежнему на in-memory. Миграция `20261102134353_AddAutomationTables` создана и **применена на локальной БД** 2026-11-02. Прод не трогался.
- **Решение:** TR-2026-11-02-06.
- **Проверка:** `dotnet test Partnerly.Automation.Tests` — Passed 32, Failed 0. `dotnet ef database update` — Done. Браузер не нужен. Коммит не делался.

---

## 2026-11-02 — Automation PHASE 3: ядро на in-memory

- **Суть:** владелец: идти дальше по Universal Automation; при неясности перечитывать спеку и архитектуру. PHASE 3 — ядро без EF, без UI, без Hangfire/Cronos.
- **Сделано:** `Partnerly/Automation/` — абстракции, registry, evaluator, binder, validator, executor, in-memory store, handlers, authoring/run, HMAC, HTTP guard, access. В `Program.cs` — `AddPartnerlyAutomation()`. Проект `Partnerly.Automation.Tests` (xUnit, net6.0) в `Partnerly.sln`. Миграций, worker, коннекторов, меню и Drawflow нет.
- **Решение:** TR-2026-11-02-05.
- **Проверка:** `dotnet test Partnerly.Automation.Tests` — Passed 30, Failed 0. Браузер не нужен (нет UI). Коммит/push не делались.

---

## 2026-11-02 — Automation PHASE 2: архитектура

- **Суть:** владелец: начинать следующий этап после PHASE 1.
- **Сделано:** `docs/ai/Automation/ARCHITECTURE.md` — каталоги, таблицы/поля, GraphJson, коды trigger/action, worker, права, коннекторы, SignalR, UI-контракт. Код `Partnerly/Automation/` и миграции не создавались (это PHASE 3–4).
- **Решение:** TR-2026-11-02-04.
- **Сборка / браузер:** не нужны (документы).

---

## 2026-11-02 — Automation PHASE 1: спецификация и анализ

- **Суть:** владелец задал Universal Automation Platform; файл был только на локальном диске `F:\...\docs\ai\Automation\MASTER-SPECIFICATION.md`.
- **Сделано:** спецификация в репозитории `docs/ai/Automation/MASTER-SPECIFICATION.md`; анализ Partnerly без правок кода — `docs/ai/Automation/PHASE-1-ANALYSIS.md`. Код приложения не менялся. Реализация ядра не начиналась (спека: стоп до проверки).
- **Решения (предложение, не внедрялось):** не Hangfire; SQL wakeup + Cronos; граф nodes/edges; Drawflow; коннекторы User/Ticket/Notification/Group/EmailTemplate.
- **Сборка / браузер:** не требовались (только markdown).

---

## 2026-11-02 — ViewCase: Save в sticky для заметки

- **Суть:** на View Case в sticky появилась кнопка Save, как на Edit User / System Settings. Неактивна, пока заметка не изменена. После OK в модалке заметки Save включается и пишет Note в SupportTicket, остаётся на ViewCase.
- **Сделано:** `ViewCase.cshtml` — форма, hidden Note, ActionSave, `enableSaveOnChange`. POST `ViewCase` в контроллере обновляет только Note.
- **Проверка:** `dotnet build` проекта в temp — 0 ошибок. Браузер не кликал.

---

## 2026-11-02 — ViewCase как рабочий экран тикета для исполнителя

- **Суть:** View Case — рабочий экран задачи: номер, тема, цветной статус, шаги New → In progress → Closed, карточка обращения автора, заметка, исполнитель, даты, кто обновил, прочитано, вложения. Start/Close/Assign на виду. Карточка на всю область контента.
- **Сделано:** `Views/SupportTickets/ViewCase.cshtml`, `page-viewcase.css`. Контроллер наполняет email/фото/файлы/`UpdatedBy` из существующих полей SupportTicket и Attachments. Ключ `AssignedToYou` в en/ru/hy. Новых колонок БД нет.
- **Проверка:** `dotnet build` проекта в temp — 0 ошибок. Браузер не кликал.

---

## 2026-11-02 — OpenNewCase: компактнее, стекло Send, закрытие уведомления

- **Суть:** карточки Open New Case чуть меньше. Кнопка Send в стеклянном стиле. После отправки баннер успеха/лимита закрывается по X (раньше `data-bs-dismiss="alert"` не работал — не было класса `alert`). Адатив mobile / tablet / laptop / desktop.
- **Сделано:** `Views/SupportTickets/OpenNewCase.cshtml`, `page-newsupportticket.css`. Закрытие через `data-close-support-alert`.
- **Проверка:** правки Razor/CSS. Браузер не кликал.

---

## 2026-11-02 — My Settings на всю область контента, как System Settings

- **Суть:** страница My Settings занимает область контента как Roles/Users. Поля профиля и пароля в колонках вкладки. Переключатели уведомлений — две колонки, клик по карточке включает switch.
- **Сделано:** `Views/Dashboard/Settings.cshtml` — убран `container-xxl`, карточка `.card.settings-card` прямой потомок layout; заголовок в `card-header`. CSS `page-mysettings.css`: без max-width 760px, `--settings-gap`, full-height ≥1200px, mobile/tablet/laptop/desktop.
- **Проверка:** `dotnet build` проекта в temp — 0 ошибок. Браузер не кликал.

---

## 2026-11-02 — System Settings на всю область контента, переключатели в колонках

- **Суть:** страница System Preferences занимает область контента как Roles/Users (карточка — прямой потомок `.partnerly-layout-content`). Переключатели больше не на всю ширину: ширина как у колонок вкладки. Клик по всей карточке включает/выключает switch. Отступы равные через `--settings-gap`.
- **Сделано:** `Views/SystemPreferences/Index.cshtml` — убран `container-xxl`, заголовок в `card-header`, switch как `<label class="maintenance-setting">`. System/Mail: `col-12 col-md-6`; Access: `col-12 col-md-6 col-xl-4`. CSS: mobile / tablet / laptop / desktop.
- **Проверка:** `dotnet build -p:UseAppHost=false` в temp — 0 ошибок. Копирование в `bin` не удалось — dll занят Partnerly/VS. Браузер не кликал.

---

## 2026-11-02 — Apple кнопка видна, без ключа клик ничего не делает

- **Суть:** на Login/Register кнопка Apple снова видна. Пока ClientId пустой, клик не вызывает Challenge и не падает.
- **Сделано:** кнопка `type="button"` без submit, если OAuth не настроен; AddApple по-прежнему только при ClientId.
- **Проверка:** правки Razor/JS. Нужен перезапуск, если сайт уже запущен.

---

## 2026-11-02 — Apple без ClientId больше не валит старт

- **Суть:** пустой `Authentication:Apple:ClientId` больше не регистрирует Apple OAuth, поэтому нет `ArgumentException: The 'ClientId' option must be provided`. Кнопка Apple скрыта, пока ключ не задан.
- **Сделано:** `Program.cs` AddApple только если ClientId есть; `ExternalLoginOptions.IsAppleConfigured`; Login/Register не показывают Apple без ключа.
- **Проверка:** сборка в temp — 0 ошибок. Нужен перезапуск приложения (dll был занят Partnerly/VS).

---

## 2026-11-02 — Apple Sign In на Login/Register

- **Суть:** вход через Apple по той же схеме, что Google: кнопки на попапах Login/Register, callback, страница дорегистрации, переключатель в System Preferences. Ключи в Git пустые.
- **Сделано:** пакет `AspNet.Security.OAuth.Apple` 6.0.15; `Authentication:Apple` в `appsettings.json` (ClientId, ClientSecret, TeamId, KeyId, PrivateKey — пустые); `Program.cs` AddApple; `AccountController` AppleLogin/Callback/Register; `User.AppleId`; `SystemSettings.EnableAppleLogin`; миграция `20261102092927_AddAppleSignIn` **применена** на локальной БД 2026-11-02.
- **Проверка:** `dotnet build -p:UseAppHost=false` — 0 ошибок. `dotnet ef database update` — миграция применена (AppleId, EnableAppleLogin). Браузер не кликал. Живой вход через Apple без ключей не проверялся.

---

## 2026-11-02 — DataTable: таблица на всю ширину, Selected первая 60px

- **Суть:** колонки стартуют с ширин из кода/сохранения. Если в `.table-responsive` остаётся место, оно пропорционально делится между видимыми колоками, кроме Selected и Actions. Таблица занимает ширину контейнера. Если сумма больше контейнера — горизонтальный скролл. Selected, если видна, всегда первая, не тянется, max 60px, не переставляется.
- **Сделано:** `_DataTable.cshtml` (Selected в начало), `DataTableColumn.SelectedColumn` (`Reorderable = false`), `resize.js` (`applyLayout`), `reorder.js` / `columnSettings.js` (Selected закреплена первой), CSS Selected 60px.
- **Проверка:** `node --check` для resize/reorder/columnSettings. `dotnet build` скомпилировал код; копирование `Partnerly.exe` не удалось — файл занят процессом Partnerly (7060). Браузер не кликал.

---

## 2026-11-02 — DataTable: Select all по центру колонки Selected

- **Суть:** колонка Selected 60px и в шапке, и в строках. Чекбоксы на одной вертикали. Таблица больше не растягивает Selected за счёт `width: 100%` — ширина таблицы = сумма колонок, Selected не resizable.
- **Сделано:** `DataTableColumn.cs`, `partnerly-data-table.css`, `render.js`, `resize.js`.
- **Проверка:** правки CSS/JS. Браузер не кликал.

---

## 2026-11-02 — Скролл меню и плавное переключение

- **Суть:** скроллбар левого меню скрыт, пока курсор/фокус не на сайдбаре; сам ползунок — тонкий градиентный. Переключение секций меню и вкладок Statistics плавнее: стекло едет между вкладками, панель появляется с fade.
- **Сделано:** `partnerly-layout.css`, `dashboards.css`, `dashboards.js`. Появление скроллбара не сжимает пункты: ширина контента компенсируется за счёт padding.
- **Проверка:** `node --check` dashboards.js. Браузерных инструментов нет.

---

## 2026-11-02 — Свёрнутое меню: подпись под иконкой

- **Суть:** когда левое меню свёрнуто на десктопе, название пункта видно мелким шрифтом под иконкой (до двух строк). Бейджи и внешние иконки по-прежнему скрыты. На мобильном оверлее меню остаётся обычным списком.
- **Сделано:** `partnerly-layout.css`.
- **Проверка:** только CSS. Браузерных инструментов нет.

---

## 2026-11-02 — Statistics: верхние вкладки в стиле меню

- **Суть:** пункты верхней секции Statistics (Games / Users / Tasks / Roles / Groups / Notifications) — зеркальный hover/выбор как у сайдбара, без сплошной фиолетовой заливки.
- **Сделано:** `dashboards.css`. Счётчик остаётся бейджем, цвет текста/иконки — primary / accent-blue.
- **Проверка:** только CSS. Браузерных инструментов нет.

---

## 2026-11-02 — Dropdown профиля: стекло, анимация, копирование реферальной ссылки

- **Суть:** меню из фото профиля — то же стекло, что у пунктов сайдбара. Рядом с реферальным кодом иконка копирования: в буфер идёт тот же URL, что на My Profile (`/Account/Register?reff=`). Открытие — короткий fade+scale.
- **Сделано:** `_NavbarPartial.cshtml`, `navbar.css`. `data-bs-display="static"`, скрытие через opacity/visibility (как у языка). После копирования иконка на секунду становится галочкой. Фон панели плотнее (~94–97% непрозрачности, blur 22px), чтобы задний фон не читался сквозь меню.
- **Проверка:** CSS/разметка по файлам. Браузерных инструментов нет — клик по фото, копирование и dark не прогонялись.

---

## 2026-11-02 — Меню: без квадрата у иконки, стекло на hover/выбор

- **Суть:** выбранный пункт больше не красит иконку фиолетовым квадратом (из‑за него иконка казалась больше). Меняется только цвет иконки. Hover и active — одинаковый зеркальный стиль на всей строке (блик сверху, лёгкий tint, blur).
- **Сделано:** `partnerly-layout.css` (light/dark; свёрнутое меню больше не сбрасывает стекло на hover). Общий glass-слой сайта откатил владелец — не возвращать.
- **Проверка:** CSS в файле и отдаётся с `https://localhost:5278`. Браузерные инструменты в этой сессии недоступны — визуально hover/выбор light+dark не кликались.

---

## 2026-11-02 — Login/Register: сначала Continue with email

- **Суть:** в попапах входа и регистрации имя/email/пароль скрыты. Вместо них кнопка «Продолжить через email»; после клика поля открываются, кнопка скрывается. Google остаётся. При ошибке сервера поля сразу открыты.
- **Сделано:** `_LandingAuthLogin.cshtml`, `_LandingAuthRegister.cshtml`, `landing-auth.js`, `landing.css`, тексты en/ru/hy.
- **Сборка:** `node --check` landing-auth.js. Браузер в этой сессии недоступен.

---

## 2026-11-02 — Стеклянная CTA с бегущей линией, зеркальные кнопки auth

- **Суть:** «Начать бесплатно» — стекло, по контуру кружит разноцветная светящаяся линия. Submit в Login / Register / Forgot и Google — тот же зеркальный стиль, без сплошной заливки.
- **Сделано:** `Home/Index.cshtml` (orbit + glass слои), `landing.css`.
- **Сборка:** только CSS/разметка. Браузер в этой сессии недоступен.

---

## 2026-11-02 — CTA на лендинге вместо Login/Register

- **Суть:** на `/` убраны Login и Register. Одна кнопка открывает регистрацию: en Start for free / ru Начать бесплатно / hy Սկսել անվճար. «Попробовать» слабее, «Начать» без «бесплатно» не снимает барьер цены.
- **Сделано:** `Home/Index.cshtml`, `LandingHeroCta` в en/ru/hy, стиль `.landing-hero-cta`. Вход остаётся в попапе (переключатель с регистрации).
- **Сборка:** код собрался; копирование `Partnerly.exe` не удалось — файл занят запущенным процессом Partnerly. Браузер в этой сессии недоступен.

---

## 2026-11-02 — Панель колонок в viewport, тарифы выше sticky-кнопок

- **Суть:** настройки DataTable обрезались, если грид у нижнего края экрана. На форме попап тарифов оказывался под sticky-кнопками (z-index 80 vs 100).
- **Сделано:** `columnSettings.js` переносит панель на `body`, ставит `position: fixed` и открывает вверх, если снизу мало места. `partnerly-plans.css` — z-index 1300, sticky-кнопки при открытых тарифах ниже оверлея.
- **Сборка:** `node --check` columnSettings.js. Браузер в этой сессии недоступен.

---

## 2026-11-02 — Pagination внизу карточки на Edit Role / User Role / Group

- **Суть:** на Edit Role, Edit User Role и Edit Group грид не заполнял карточку, pagination стоял сразу под строками. Теперь вложенный DataTable тянется до низа карточки (≥1200px), как на обычных списках.
- **Сделано:** `partnerly-layout.css` — flex-цепочка card-body → form → tabs/table; `partnerly-tabs.css` — активный tab-pane заполняет высоту, padding вокруг грида снят.
- **Сборка:** только CSS. Браузер в этой сессии недоступен.

---

## 2026-11-02 — Системные уведомления: код вместо текста

- **Суть:** регистрация и статусы тикета больше не пишут готовый текст в `Notification.Message`. В БД код `$N:Key|args`; текст собирается на языке того, кто смотрит (cookie культуры). Массовая рассылка из групп и ручное Create остаются обычным текстом.
- **Сделано:** `NotificationMessage`; события UserRegistered / TicketCreated / TicketStatusChanged пишут код; навбар JS, DataTable, export, Edit, dashboard, поиск — `Format`. Старые записи без префикса `$N:` показываются как раньше.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-11-01 — Корзина слева, видимый progress bar

- **Суть:** иконка массового удаления перенесена в левую часть тулбара. Полоса прогресса в попапе теперь заполняется (цвет был `var(--dt-primary)`, а попап на `body` теряет токены таблицы).
- **Сделано:** `_DataTable.cshtml` — кнопка в `toolbar-left`; CSS/JS — fill через `scaleX` и цвет `#696cff`.
- **Сборка:** правки UI, `dotnet build` не запускался. `node --check` bulkDelete.js. Браузер в этой сессии недоступен.

---

## 2026-11-01 — Массовое удаление в DataTable

- **Суть:** во всех таблицах есть колонка Selected (выкл. по умолчанию; у group members — включена). Справа в тулбаре иконка удаления по праву `datatable.bulk_delete` (Super и Administrator). Видна, когда колонка Selected включена в гриде и есть выбранные строки. Попап: выбранные / удалённые / оставшиеся, Start и Stop с подтверждением. После прогона — запись в системный журнал через `ILogService`.
- **Сделано:** `DataTableColumn.HiddenSelected` / `VisibleSelected`; `PermissionCodes.DataTableBulkDelete` в `All`; `DataTablesController` DeleteRecord + LogBulkDelete; JS `bulkDelete.js`; тексты en/ru/hy. History-таблица ролей (`userAccessAuditTable`) Selected есть, массовое удаление нет.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок, 112 предупреждений. `node --check` — ок. Браузер в этой сессии недоступен. Новое право появится после старта (`PermissionSeeder`).

---

## 2026-11-01 — Тарифы, документация, меню на Docs

- **Суть:** «Blot Academy» внизу меню заменён на кнопку тарифов со стеклянным попапом (Free 1 агент / Studio до 20 / Usage по ресурсам, текущий план и usage по дням). License в футере снят. Документация про ИИ-агентов. `/Documentation` наследует `_BaseController`, поэтому права меню заполняются.
- **Сделано:** `DocumentationController`; редирект `Home/Documentation`; `_PlansModal` + CSS/JS; тексты en/ru/hy. Оплата и реальные действия агентов ещё не подключены — usage 0/1 на Free.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-11-01 — Короткая форма регистрации

- **Суть:** с лендинга убраны телефон и подтверждение пароля. Поле реферала только если в URL есть `reff`.
- **Сделано:** `_LandingAuthRegister` — имя, email, пароль, terms; реферал readonly+hidden при `FromReferrerLink`. `RegisterViewModel` без `ConfirmPassword` (и поле снято с Users Create, та же модель). Register POST не проверяет пустой телефон.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-11-01 — Гостевой чат: вопросы-кнопки и слайдер агента

- **Суть:** «Сбои» заменены на «В очереди». Placeholder поиска — простые фразы (это просто / каждый может / как начать / что это). В чате кнопки-вопросы; ответы локальные: текст или маленький слайдер с примером агента и шагами работы. OpenAI на гостевой поиск не включался.
- **Сделано:** resx en/ru/hy; `Home/Index` JSON промптов; `landing-search.js` + CSS чипов/слайдера; language.js копирует `data-chat-prompts`.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок, 112 предупреждений. `node --check` — ок. Браузер в этой сессии недоступен.

---

## 2026-11-01 — Тексты и карточки: с игр на ИИ-агентов

- **Суть:** публичные тексты и KPI-карточки больше не про игры. Контекст — создание ИИ-агентов и их запуск на задачах.
- **Сделано:** en/ru/hy — лендинг (hero, typewriter, ответы чата) и вкладка Statistics (Agents, создано агентов, выполнено, сбои, успешность, графики). Иконки `bx-game`/`bx-trophy` → `bx-bot` / `bx-check-circle` / `bx-pie-chart-alt-2`. Ключи ресурсов и расчёт цифр не менялись.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок, 112 прежних предупреждений. Браузер в этой сессии недоступен.

---

## 2026-11-01 — Язык на гостевой `/` без перезапуска лендинга

- **Суть:** у неавторизованного на главной смена языка больше не перезапускает точки, письмо имени и typewriter.
- **Сделано:** на `body.landing-body` после AJAX не вызываются `document.write` и `form.submit()`. Парсится HTML, обновляются переключатель, hero, поиск, модалки; `#landingDots` и `.landing-brand` остаются. `landing-search/auth/info` умеют `refresh()` без повторной подписки. На лендинге нет fallback-навигации и нет затемнения canvas.
- **Сборка:** `node --check` для четырёх JS — ок. Браузер в этой сессии недоступен.

---

## 2026-11-01 — Смена языка без reload браузера

- **Суть:** клик по флагу меняет культуру без полной навигации. Тексты страницы всё равно берутся с сервера (Razor/resx), поэтому HTML текущей страницы подменяется после установки cookie.
- **Сделано:** `LanguageController` отвечает JSON на AJAX; `partnerly-language.js` POST + GET текущий URL (`cache: no-store`) и `document.write` нового HTML. Fallback — обычный submit формы.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. `node --check` для `partnerly-language.js`. Браузер в этой сессии недоступен.

---

## 2026-11-01 — Анимация открытия language dropdown

- **Суть:** панель языка вырастает от флажка (fade + scale), флаги появляются по очереди.
- **Сделано:** `data-bs-display="static"` (без Popper transform); меню всегда `display:block`, скрытие через opacity/visibility, чтобы CSS-transition реально играла. Bootstrap `display:none` раньше срывал анимацию.
- **Сборка:** только CSS/разметка. Браузер в этой сессии недоступен.

---

## 2026-11-01 — Переключатель языка: компактные флаги

- **Суть:** клик по флагу открывает компактную панель: три флага в ряд, под ними мелкие коды ENG / RUS / HYE, без полных названий. Появление с fade+scale.
- **Сделано:** общий `_LanguageSwitcher.cshtml` для навбара и лендинга; стили в `navbar.css`; текущий язык подсвечен. Анимация на внутреннем tray, чтобы не спорить с Popper.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок, 109 прежних предупреждений. Браузер в этой сессии недоступен.

---

## 2026-10-31 — Save после изменения заметки на форме

- **Суть:** OK в попапе заметки теперь включает кнопку Save документа.
- **Сделано:** после записи в hidden `Note` шлём `input`/`change`, чтобы `enableSaveOnChange` увидел правку. Модалка вынесена в `body`, поэтому события textarea до формы не доходили.
- **Сборка:** код только в `_StickyButtons.cshtml`. Браузер в этой сессии недоступен.

---

## 2026-10-31 — Попап Note из DataTable: без Cancel, крестик в шапке

- **Суть:** из таблицы в попапе заметки нет Cancel; крестик закрытия больше не обрезается углом карточки.
- **Сделано:** footer только OK; `.btn-close` в flex-потоке шапки (`position: static`), без отрицательного margin Bootstrap.
- **Сборка:** `node --check` для `render.js`. Браузер в этой сессии недоступен.

---

## 2026-10-31 — DataTable: ID по праву и колонка Note

- **Суть:** ID в таблице и в списке колонок только при `datatable.view_ids` (Super / Administrator / Employee по seed). Note — в списке колонок таблиц с полем Note, выкл. по умолчанию; в таблице иконка, заполненная — цвет кнопки заметки формы, клик открывает попап как на форме.
- **Сделано:** право и seed; `HiddenNote()` на users/roles/groups/members/logs/notifications/email/tickets/attachments; фильтр колонок в `_DataTable.cshtml`; тип `Note` в JS + CSS. Нет Note на rolePermissions / userPermissions / userAccessAudit (поля нет).
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок, 112 прежних предупреждений. `node --check` для columns/render/inline/actions.js. Браузер в этой сессии недоступен.

---

## 2026-10-31 — DataTable: панель настроек колонок

- **Суть:** справа от поиска — кнопка-шестерёнка; попап со всеми колонками таблицы, вкл/выкл и drag-порядок.
- **Сделано:** `columnSettings.js`, кнопка в правом углу тулбара, попап в стиле таблицы, видимость в localStorage, Reset. Actions не в списке.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-10-31 — DataTable: колонка до Actions и иконка перестановки

- **Суть:** можно поставить колонку сразу перед Actions; на hover вместо руки — grip.
- **Сделано:** drop-зона перед `__actions` (в т.ч. широкая последняя колонка); `td.actions` с `data-field`; курсор `grab` заменён на `bx-grid-vertical`.
- **Сборка:** `node --check` для `reorder.js` / `render.js`. Браузер в этой сессии недоступен.

---

## 2026-10-31 — Перестановка колонок DataTable

- **Суть:** в таблицах можно менять места колонок перетаскиванием заголовка; колонка Actions закреплена справа.
- **Сделано:** модуль `reorder.js` (pointer, ghost, FLIP), флаг `ColumnReorderEnabled`, порядок в localStorage при `RememberState`. Sort и resize не ломаются: drag начинается после порога, не с ручки ширины.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок, 109 прежних предупреждений. Браузер в этой сессии недоступен.

---

## 2026-10-31 — Общий tab control

- **Суть:** вкладки Edit User Role выглядят как на Edit Group; стили вынесены в общий control сайта.
- **Сделано:** `<partnerly-tabs>` / `<partnerly-tab>`, `partnerly-tabs.css`. Подключены User Role Edit и User Groups. Инлайн-CSS групп убран.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-10-30 — История на Edit User Role

- **Суть:** страница роли пользователя переименована в Edit User Role; вкладка History показывает `AccessControlAuditLogs` по этому пользователю.
- **Сделано:** вкладки Permissions / History, DataTable `userAccessAuditTable`, смена роли пишет `UserRoleChanged`. Права как у страницы (`users.manage_roles`).
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Имя сайта пишется при первой загрузке

- **Суть:** при первой загрузке `/` название появляется как рукопись, не fade/blur буквами. Дальше по-прежнему перерисовывается только логотип.
- **Сделано:** `landing-brand.js` открывает каждую букву clip-path слева направо с разной длительностью; тонкий курсор идёт за строкой.
- **Сборка:** `node --check` для `landing-brand.js`. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Попап без морфа точек; линии между точками

- **Суть:** Login/Signup больше не собирается из точек. Попап плавно проявляется, тексты лендинга гаснут. Точки тянутся друг к другу короткими линиями.
- **Сделано:** убран `gatherToAuth`. Overlay/карточка — opacity. `is-auth-open` / `is-info-open` скрывает бренд и hero. `landing-dots.js` ищет соседей и растит линию с двух концов.
- **Сборка:** `node --check` для `landing-dots.js` и `landing-auth.js` — ок. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Откат морфа из одной точки

- **Суть:** владелец попросил вернуть предыдущий вариант открытия Login/Signup.
- **Сделано:** `landing-dots.js`, `landing-auth.js`, `landing.css` возвращены к сборке всех точек в силуэт карточки (как в `3f1b869`).
- **Сборка:** восстановление из git, синтаксис не менялся.

---

## 2026-10-30 — Плавнее сборка попапа; имя без цикла

- **Суть:** переход точек в Login/Signup мягче; в шапке заново рисуется только логотип-ветка, имя остаётся.
- **Сделано:** точки с задержкой по дистанции, ease-in-out, пауза в облаке, карточка проявляется ~0.8 с. `landing-brand.js` пишет имя один раз.
- **Сборка:** только JS/CSS.

---

## 2026-10-30 — Login/Signup появляется из точек лендинга

- **Суть:** клик Login или Sign up на `/` собирает светящиеся точки в силуэт попапа, затем стеклянная карточка проявляется поверх.
- **Сделано:** `landing-dots.js` `gatherToAuth` — точки растут и летят в rect карточки, потом возвращаются. `landing-auth.js` открывает overlay прозрачным (`is-forming`), затем `is-formed`. Flip между панелями не тронут. URL `?auth=` открывается сразу.
- **Сборка:** только JS/CSS. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Переворот auth-попапа

- **Суть:** Login / Register / Forgot при смене панели переворачиваются на обратную сторону, как карточка.
- **Сделано:** `landing-auth.js` крутит `rotateY` до ребра, меняет панель, доворачивает. Следующий переход — в другую сторону. Первый открытие без переворота. `prefers-reduced-motion` — сразу смена.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Имя сайта рисуется циклом на лендинге

- **Суть:** в шапке гостевой `/` ветка и имя приложения появляются как рисунок, затем стираются и рисуются снова.
- **Сделано:** `landing-brand.js` — штрихи логотипа, буквы по очереди, пауза ~5.6 с, стирание, повтор. `prefers-reduced-motion` оставляет статику.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Кэш SystemSettings

- **Суть:** не читать строку настроек из БД на каждый HTTP-запрос.
- **Сделано:** `IMemoryCache` в `GetCurrentAsync` (10 мин, сброс при сохранении). Middleware maintenance и токены писем берут `GetCurrentAsync`. Форма редактирования по-прежнему `GetSetupByIDAsync` из БД.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер не требовался.

---

## 2026-10-30 — Контраст текста в тёмной теме

- **Суть:** в dark mode подписи форм, Documentation и часть других текстов оставались угольно-серыми от светлой палитры Sneat.
- **Сделано:** подняты `--partnerly-text` / `--text-dark` / `--muted` и Bootstrap `--bs-body-color` / `--bs-heading-color` / card tokens. Перекрыты `.form-label`, headings, `.card-body`, `.text-muted` / `.text-body` / `.text-heading`, legal-copy. Ярче floating labels в Settings, System Preferences, auth и лендинге.
- **Сборка:** только CSS. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Privacy попап и черновик Documentation

- **Суть:** политика не открывается новой вкладкой, а попапом на лендинге; Documentation — внутренняя черновая страница вместо пустой внешней ссылки.
- **Сделано:** гость `/Home/Privacy` → `/?dialog=privacy`, из регистрации `data-info` поверх формы. Залогиненный видит Privacy в карточке приложения. Меню/футер → `Home/Documentation` (черновик, en/ru/hy). Колесо точек не зумит, пока открыт info-попап.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер недоступен.

---

## 2026-10-30 — Колесо мыши масштабирует точки лендинга

- **Суть:** на гостевой `/` без попапа и без чата колесо мыши увеличивает/уменьшает светящиеся точки, с нижним и верхним пределом.
- **Сделано:** `landing-dots.js` — масштаб 0.4…2.5, по умолчанию 1. В `is-chat` и `is-auth-open` жест не перехватывается.
- **Сборка:** только JS. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Коммит account-попапа

- **Суть:** зафиксировать попап входа, удаление старых account-страниц и глаз на сбросе пароля.
- **Сделано:** коммит `51205de` на `Cursor-Agent`. Push не делался.

---

## 2026-10-30 — Глаз на сбросе пароля не показывал пароль

- **Суть:** на `ConfirmForgotPassword` клик по иконке глаза не переключал поле в видимый текст.
- **Сделано:** на span глаза добавлен `password-toggle-icon` (как в Settings). `site.js` также ловит `.form-password-toggle .input-group-text`; иконка выше поля по z-index, чтобы клик не перехватывал input.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Удалены неиспользуемые account-страницы и ключи ресурсов

- **Суть:** убрать views, которые после попапа больше не отдаются, и ключи SharedResource, которые код не читает.
- **Сделано:** удалены `Views/Account/Login`, `Register`, `ForgotPassword`, `RegistrationSuccessful`, `ForgotPasswordSuccessful`, `ConfirmEmailSuccess`. Оставлены AccessDenied, GoogleRegister, ConfirmForgotPassword. Из en/ru/hy сняты 38 неиспользуемых ключей (старый бренд login/register, старый лендинг, старый Web Inspector, `UseBuiltInUserRole` и др.). В SharedResource осталось 540 ключей, неиспользуемых по коду — 0.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок.

---

## 2026-10-30 — Account как попап на главной

- **Суть:** Login, Register, Forgot password и экраны успеха открываются стеклянным попапом на гостевой `/`, в стиле лендинга, без отдельных auth-страниц.
- **Сделано:** GET `/Account/Login|Register|ForgotPassword` и успешный ConfirmEmail / смена пароля ведут на `/?auth=...`. Ошибки POST возвращают `Home/Index` с ModelState. Login/Sign up и иконки чата открывают попап (`data-auth`). Google/Apple по-прежнему навигация. Сброс пароля по письму, GoogleRegister и AccessDenied — отдельные страницы.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Иконки поиска, скролл чата, тексты входа

- **Суть:** одна иконка lookup справа в стиле прежней левой; send в чате тот же стиль; аккуратный скролл; бот зовёт войти или зарегистрироваться.
- **Сделано:** снята левая `bx-search`. Submit/send без круглой заливки. Тонкий scrollbar. В ответе ссылки Login/Sign up + Google/Apple. Новые `LandingChatReply*`.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок.

---

## 2026-10-30 — Гостевой поиск превращается в чат

- **Суть:** Enter/кнопка поиска на `/` — лендинг становится чатом, поиск внизу, тексты скрываются, бот отвечает заглушкой; под ответом Google и Apple.
- **Сделано:** клиентский чат без OpenAI. GET `GoogleLogin`; `AppleLogin` пока ведёт на Login. Кнопка search/send у поля.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер недоступен.

---

## 2026-10-30 — Лёгкий фон у выбранного пункта меню

- **Суть:** выбранный пункт снова с небольшим оттенком, без левой полоски.
- **Сделано:** ровный фон `primary-hover` (dark: rgba 0.16), без inset-линии и без градиента слева.
- **Сборка:** только CSS.

---

## 2026-10-30 — Свёрнутый сайдбар: без имени, без полоски у иконки

- **Суть:** в закрытом меню авторизованного пользователя не показывать имя сайта; убрать светящуюся полоску/фон слева у иконки пункта.
- **Сделано:** `.partnerly-brand-name` скрыт при `partnerly-sidebar-collapsed` (на мобилке drawer имя остаётся). Снят градиент и inset-линия у active `.partnerly-menu-link`; в свёрнутом десктопе фон/shadow у пункта прозрачные, активна только иконка.
- **Сборка:** только CSS.

---

## 2026-10-30 — Favicon ветки и локализованные title account

- **Суть:** иконка вкладки — новый знак (ветка на индиго-квадрате); title account-страниц на en/ru/hy и с именем приложения.
- **Сделано:** `favicon.svg` / заменены `favicon.png` и `favicon.ico`. Partial `_PageTitle`: `{страница} - {appName}`. Account titles через Localizer (`Login`, `Signup`, `ForgotPassword`, `Resetpassword`, `Completeregistration`, `EmailSent`, `EmailVerified`, `AccountCreated`, `AccessDenied`).
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер недоступен.

---

## 2026-10-30 — Имя жирнее, без стрелки у аватара, заголовок над поиском

- **Суть:** имя сайта чуть жирнее; убрать стрелку у dropdown аватара в навбаре; над поиском на гостевой главной — текст.
- **Сделано:** Cormorant 700 + лёгкий text-stroke на имени (лендинг, сайдбар, account, maintenance). Снят `bx-chevron-down` у user dropdown, скрыт caret Bootstrap. Над search: `LandingHeroTitle` / `LandingHeroSub` (en/ru/hy).
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер недоступен.

---

## 2026-10-30 — SVG-логотип сайта

- **Суть:** векторный логотип в палитре сайта (индиго/фиолет), в коде, без PNG-картинки в UI.
- **Сделано:** знак без квадрата — ветка с узлами (ствол, развилка, боковой побег). Файл `wwwroot/assets/img/site-logo.svg`, partial `_SiteLogo.cshtml`. Стоит у имени на лендинге, в сайдбаре, на account-страницах и Maintenance. Favicon: SVG + PNG fallback.
- **Сборка:** `dotnet build Partnerly.sln` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-10-30 — Точки рождаются и гаснут

- **Суть:** светящиеся точки на лендинге не только мерцают, но появляются в случайных местах и исчезают.
- **Сделано:** у каждой точки свой срок жизни, fade in/out; новые спавнятся в random координатах, старые снимаются. При reduced-motion поле статичное.
- **Сборка:** только JS.

---

## 2026-10-30 — Тема по клику без dropdown

- **Суть:** иконка темы сразу переключает light/dark, без меню.
- **Сделано:** dropdown снят на лендинге и в авторизованном навбаре. `partnerly-theme.js` по клику пишет противоположный режим в localStorage.
- **Сборка:** Razor/JS.

---

## 2026-10-30 — Флаг без стрелки, Login/Sign up по центру

- **Суть:** в закрытом language dropdown только флаг (лендинг и авторизованный навбар); точки мельче и гуще; Login убрать из шапки, поставить в центр рядом с Sign up.
- **Сделано:** снят `bx-chevron-down` у языка в `_LandingLayout` и `_NavbarPartial`. Dots ~1–2.5px, до ~420 шт. Под поиском текстовые Login и Signup, light/dark.
- **Сборка:** Razor/CSS/JS.

---

## 2026-10-30 — Лендинг: точки, Login-текст, поиск, имя в меню

- **Суть:** крупнее точки; Login только текст под light/dark; поиск по центру с печатью; имя жирнее везде, в сайдбаре без логотипа, в свёрнутом меню мелкое имя.
- **Сделано:** `landing-dots.js` радиус ~3–6px. Login без кнопки. Центральный search с typewriter (`LandingType1–3`), submit → Login. Cormorant 700 на лендинге, account и `partnerly-brand-name`. Logo в `_VerticalMenu` снят; collapsed показывает имя ~0.68rem.
- **Сборка:** Razor/CSS/JS/resx.

---

## 2026-10-30 — Имя без эффектов, точки вместо нейросети

- **Суть:** у имени на лендинге снять анимации; с account-страниц убрать favicon-логотип; нейросеть заменить мелкими мерцающими точками близких цветов.
- **Сделано:** имя — Cormorant Garamond, без блика/пульса. Logo img снят с Login/Register/Forgot/Google/success. `landing-neural.js` удалён; `landing-dots.js` рисует мелкие точки indigo/violet.
- **Сборка:** Razor/CSS/JS.

---

## 2026-10-30 — Лендинг: отступы, эффект имени, рост сетей

- **Суть:** имя и кнопки ближе к центру; другой эффект названия; нейросеть гуще и медленнее, без фона мозга, из каждой цепочки рождается новая сеть.
- **Сделано:** горизонтальные поля навбара `clamp(48px, 9vw, 120px)`. Имя — цвет + пульс letter-spacing и блик, не радужный градиент. Canvas: 160 стартовых узлов, до 360; из ребра раз в ~1.35 с вырастает кластер из 9 узлов; морф ~52 с; эллипс мозга убран.
- **Сборка:** только CSS/JS/Razor.

---

## 2026-10-30 — Гостевой лендинг без меню

- **Суть:** для неавторизованных домашняя страница — минималистичный современный лендинг: без меню и блоков, в навбаре язык/тема как у авторизованных плюс Login, слева имя сайта, в центре нейросеть → мозг.
- **Сделано:** `_LandingLayout` очищен (нет логотипа, ссылки Videos, футера, гамбургера). Имя из System Settings с градиентным эффектом. Те же dropdown языка и темы (`navbar.css` + `partnerly-theme.js`). Canvas-анимация `landing-neural.js`: две сети с пульсами и вспышками столкновений сливаются в силуэт мозга. Features/видео/карточки убраны. Privacy переведён на тот же layout.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildLanding` — 0 ошибок. Браузер недоступен.

---

## 2026-10-30 — Коммит System Settings

- **Суть:** владелец попросил закоммитить отложенный пакет System Settings.
- **Сделано:** коммит на `Cursor-Agent` (настройки, миграция, lookup ролей/auto-assign, timezone DataTable). Push не делался.
- **Сборка:** в этом шаге не запускалась.

---

## 2026-10-30 — Lookup роли новичка в System Settings

- **Суть:** Default role for new users — тот же lookup ролей, что auto-assign tickets to role, а не dropdown.
- **Сделано:** `/Roles/GetLookupList?fieldName=RoleName`; скрытый Id + имя; крестик очищает (тогда UserService берёт роль `User`). `RoleOptions` убран. Поведение назначения роли при регистрации не менялось.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildRoleLookup` — 0 ошибок. Браузер недоступен.

---

## 2026-10-30 — DataTable даты по SystemSettings.TimeZoneId

- **Суть:** колонки Date/DateTime в DataTable должны показывать и фильтровать календарный день в часовом поясе из System Settings, а не в поясе браузера.
- **Сделано:** `formatDate` / `formatDateTime` / `formatDatePattern` читают `window.partnerlyTimeZone`. Relative старше 7 дней — та же зона (`dd.MM.yyyy`). Intl при ошибке падает на UTC, не на local. Фильтры Date/DateRange на сервере переводят выбранный день из пояса настроек в UTC (`TimeZoneHelper.StartOfCalendarDayUtc`). JSON списка по-прежнему UTC, без двойного сдвига. Экспорт уже конвертировал.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildTz` — 0 ошибок, 108 предупреждений (уже были). Браузер недоступен.

---

## 2026-10-30 — Relative date в DataTable без лишней цифры

- **Суть:** после обновления ячейки даты показывалось «1 a minute ago».
- **Сделано:** для одной минуты/часа `formatRelativeDate` не ставит число вперёд — строка уже полная (`a minute ago`, `минуту назад`, `մեկ րոպե առաջ`). Для 2+ число остаётся.
- **Сборка:** только JS.

---

## 2026-10-30 — Lookup вместо dropdown у автоназначения тикетов

- **Суть:** поля auto-assign user/role в System Settings должны использовать системный lookup, как на тикетах и ролях.
- **Сделано:** `/Users/GetLookupList` и `/Roles/GetLookupList?fieldName=RoleName`; скрытый Id + имя; крестик очищает выбор (бывший пункт None). Dropdown ролей для роли новичка не трогал.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildLookup` — 0 ошибок. Браузер недоступен.

---

## 2026-10-30 — System Settings: операционные опции вместо hardcode

- **Суть:** вынести в настройки регистрацию/доступ, почту и уведомления, файлы, автоназначение тикетов, внешний вид, lockout, AI, часовой пояс; перевязать все прежние зашитые значения.
- **Сделано:** новые колонки `SystemSettings` + `User.AccessFailedCount`/`LockoutEnd`. Вкладки System / Access / Mail / Files / Tickets. Вызовы переведены: роль новичка, Google Challenge, cookie, размер фото, EmailSender, NotificationService, письма админу, вложения, auto-assign тикетов, app name, текст обслуживания, язык, AI-кнопка, timezone в списках/логах/экспорте. Миграция `20261030074752_ExpandSystemSettingsAndUserLockout` создана; применена на локальной БД 2026-10-30 (колонки Users/SystemSettings + дефолты строки Id=1). Коммит не делался.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildSettings` — 0 ошибок, 111 предупреждений (уже были). Браузер недоступен.

---

## 2026-10-29 — Фото роли в одну линию, tooltip не под меню

- **Суть:** подсказка с именем уходила под левое меню; фото стояли под названием роли.
- **Сделано:** аватарки в строке с именем роли; tooltip справа от фото, z-index выше сайдбара.
- **Сборка:** только Razor/CSS.

---

## 2026-10-29 — Statistics + Dashboards на одной странице

- **Суть:** слить Dashboards со Statistics; первой вкладкой Games; в Roles у Users by role — фото пользователей (макс. 5) с именем/email по наведению; рядом Permissions by role.
- **Сделано:** `/Dashboard` — вкладки Games / Users / Tasks / Roles / Groups / Notifications. Пункт меню Dashboards убран; `/Dashboard/Dashboards` редирект на Index. `UsersByRole` отдаёт до 5 превью с фото; карточка `PermissionsByRole` — число прав роли от каталога. i18n `Games`, `PermissionsByRole`. View `Dashboards.cshtml` не удалялся (редирект в контроллере).
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildStatsMerge` — 0 ошибок. Браузер недоступен.

---

## 2026-10-29 — Поиск уже и виден на планшете

- **Суть:** поле ещё уже; на планшетах показывать, на телефоне нет.
- **Сделано:** десктоп `max-width: 300px`; планшет (768–1199) `240px`; скрыто только `<768px`.
- **Сборка:** только CSS.

---

## 2026-10-29 — Поиск в навбаре скрыт на мобильной ширине

- **Суть:** поиск в навбаре не показывать в мобильной версии.
- **Сделано:** `@media (max-width: 1199.98px)` — `.partnerly-site-search { display: none }`, тот же порог, что у кнопки меню. На ≥1200px поле остаётся.
- **Сборка:** только CSS. Браузер в этой сессии недоступен.

---

## 2026-10-29 — Поиск в навбаре вместо заголовка страницы

- **Суть:** вместо title в навбаре — поиск по разделам меню и данным сайта.
- **Сделано:** поле в `_NavbarPartial` (placeholder, без глобального loader). GET `/Search/Query` (`[Authorize]`, без отдельного permission). `ISiteSearchService` отдаёт секции по эффективным правам и записи Users / Roles / Groups / Tasks / Notifications / EmailTemplates / Logs / Attachments; списки режутся `VisibleUserIds()` (тикеты ещё по `AssignedTo`, вложения по `CreatedBy`). Минимум 2 символа, до 5 строк в группе. JS: debounce, AbortController, стрелки/Enter. i18n en/ru/hy.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildSiteSearch` — 0 ошибок, 110 предупреждений (уже были). Браузер в этой сессии недоступен.

---

## 2026-10-29 — Дашборды: без Overview, счётчики на вкладках

- **Суть:** убрать вкладку Overview; в маленьких кнопках секций сверху показывать total записей.
- **Сделано:** вкладка и панель Overview удалены. Users/Tasks/Roles/Groups/Notifications — бейдж с total. JS открывает первую доступную секцию вместо overview. CSS бейджа light/dark, на узком экране прячется только подпись, число остаётся. Шапка секции (`dashboards-panel-head`) снята; ссылка ViewList стоит в карточке Total каждой вкладки.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildDashTabs` — 0 ошибок. Правка ссылки ViewList — Razor/CSS, отдельная сборка ниже если запускалась.

---

## 2026-10-29 — Statistics: заголовок и высота KPI

- **Суть:** на странице Statistics заголовок всегда был Dashboard; нижние KPI-карточки разной высоты.
- **Сделано:** `Dashboard/Index` — `ViewData["Title"]` = Statistics. Навбар снова показывает Title. Высота KPI в одном ряду ровная, без растягивания всех рядов. Карточки Games by day и Wins/Losses в одном ряду одной высоты (`dashboard-charts-row`).
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildStatsTitle` — 0 ошибок. Правка длины карточек — только CSS. Браузер недоступен.

---

## 2026-10-29 — Дашборды: ViewList в карточке Total

- **Суть:** убрать `dashboards-panel-head`; открытие списка — из карточки Total.
- **Сделано:** шапка с заголовком секции снята у Users/Tasks/Roles/Groups/Notifications. Ссылка `@Localizer["ViewList"]` в карточке Total.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildDashTotalLink` — 0 ошибок. Браузер недоступен.

---

## 2026-10-29 — Закрытие заметки OK и подгон колонок DataTable

- **Суть:** OK в попапе заметки не закрывал модалку; в тулбаре таблицы нужна иконка справа от фильтра, чтобы подогнать ширину колонок под полный текст заголовка и иконки сортировки/фильтра.
- **Сделано:** `_StickyButtons.cshtml` — OK пишет во все hidden `Note` (не только `#hiddenNote`), `getOrCreateInstance().hide()`, `data-bs-dismiss`, модалка на `document.body`. DataTable: кнопка `data-table-fit-columns` сразу справа от фильтра; `resize.js` считает ширину заголовка (uppercase + letter-spacing) + запас на sort/filter/resizer и задаёт ширину таблицы, чтобы колонки не сжимались. Колонка `__actions` при подгоне меряется по кнопкам в строках, не по тексту заголовка. i18n `DataTableFitColumns` en/ru/hy.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildNoteFit` — 0 ошибок, 110 предупреждений (уже были). JS-правка колонки кнопок отдельно не собиралась. Браузер в этой сессии недоступен.

---

## 2026-10-29 — Назначенный видит тикет из уведомления

- **Суть:** View на уведомлении вёл на ViewCase, и исполнитель получал «нет доступа», если автора тикета не видно по ролям.
- **Сделано:** `CanViewSupportTicketAsync` — автор из видимых или `AssignedTo` = текущий пользователь. ViewCase / Start / Close / Edit / Delete / inline / вложения / Data / Export / lookup. В `QueryEntityExt` добавлен `AssignedTo`.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildTicketAssignee` — 0 ошибок. Браузер не проверялся.

---

## 2026-10-28 — Дашборды: убраны журнал и шаблоны, ровные KPI

- **Суть:** с экрана дашбордов убраны email templates, logs, подсказка, «My numbers» и карточка Active; KPI выровнены, в карточках больше цифр.
- **Сделано:** вкладки Users / Tasks / Roles / Groups / Notifications; сетка 4 колонки одинаковой высоты; подписи с процентами; списки с бейджами; группы по размеру; уведомления по типу.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildDashboards2` — 0 ошибок. Браузер в этой сессии недоступен.

---

## 2026-10-28 — Модульные дашборды

- **Суть:** отдельная страница KPI по users, tasks, roles, groups, notifications; пункт меню под Statistics.
- **Сделано:** `Dashboard/Dashboards`; `IInsightsDashboardService` считает по `VisibleUserIds()`; секции по существующим `*.view`; личные KPI всегда; вкладки, карточки, CSS-полоски, light/dark; i18n en/ru/hy. Нового permission и Chart.js нет. Index с играми не менялся.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildDashboards` — 0 ошибок, 110 предупреждений (уже были). Браузер в этой сессии недоступен.

---

## 2026-10-28 — Инспектор парсит HTML внутри iframe

- **Суть:** live-снимок включает документы всех фреймов, не только top.
- **Сделано:** inject `observe.js` во все frameId; merge HTML/текст/карточки; same-origin fallback через `contentDocument`; карточка Iframes. Opaque/sandbox фреймы могут остаться закрытыми.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildInspectorFrames` — 0 ошибок. Нужен Reload unpacked extension (новое право `webNavigation`).

---

## 2026-10-28 — Инспектор: кнопка расширения, вкладки, карточки

- **Суть:** кнопка открывает chrome://extensions (автоустановка Chrome не даёт); текст и HTML — вкладки с форматированием; клик по KPI показывает только этот тип.
- **Сделано:** `OPEN_EXTENSIONS` в background; путь папки копируется; observe шлёт details; pretty HTML + подсветка; списки links/forms/… .
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildInspectorUi` — 0 ошибок.

---

## 2026-10-27 — Realtime инспектор через расширение Chrome

- **Суть:** каждое изменение в открытом окне сайта сразу отражается индикаторами в Partnerly.
- **Сделано:** MV3-расширение `tools/partnerly-web-inspector-extension`; страница очищена; split — окно сайта слева на мониторе, справа KPI/текст/HTML; вспышка индикаторов на mutation/click/input/навигацию. Always-on-top OS недоступен, окна ставятся рядом.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildInspectorLive2` — 0 ошибок.

---

## 2026-10-27 — Инспектор открывает сайт в окне Chrome

- **Суть:** живой сайт — отдельное окно Chrome; HTML/текст/данные остаются в Partnerly.
- **Сделано:** `window.open` по клику «Открыть» (до fetch, иначе popup-blocker); убран sandbox iframe; карточка «сайт в Chrome» + фокус/повтор; серверный снимок как раньше. i18n en/ru/hy.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildWebInspectorLive` — 0 ошибок.

---

## 2026-10-27 — Лоадер не снимался в веб-инспекторе

- **Суть:** по «Открыть» глобальный `#globalLoader` крутился и не исчезал, хотя страница уже пришла.
- **Сделано:** `spiner.js` не показывает лоадер на `data-no-loader` и на submit с `preventDefault`; прячет его, если документ уже `complete` и нет jQuery AJAX (ajaxStop/Error, load, pageshow, visibility). Инспектор: `data-no-loader` + `PartnerlyLoader.hide()` после fetch.
- **Сборка:** не запускалась (JS).

---

## 2026-10-27 — Веб-инспектор

- **Суть:** нужна страница, где открывается любой сайт как в браузере, и программа читает HTML и видимые данные для автоматизации.
- **Сделано:** секция меню Tools → Веб-инспектор; сервер качает страницу, превью + текст + ссылки/формы/таблицы + сырой HTML; копирование HTML/JSON; SSRF-блок localhost/private; право `web_inspector.use`. Iframe чужого origin не используется.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildWebInspector` — 0 ошибок.
- **Ограничение:** без JS-движка SPA могут открыться неполностью. Нужен рестарт приложения, чтобы seeder выдал право Super/Admin/Employee.

---

## 2026-10-27 — Превью email-шаблона

- **Суть:** попап Preview на форме Email Templates выглядел как сырой Bootstrap modal.
- **Сделано:** оформление как у note/lookup: иконка, крестик в рамке, карточка «письма» с темой и iframe; светлая и тёмная тема; модалка переносится в `body`, чтобы её не резал `overflow` layout. HTML письма остаётся на белом фоне.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildEmailPreview` — 0 ошибок.

---

## 2026-10-27 — Навбар не обновлял фото профиля

- **Суть:** после смены фото на My Profile картинка в БД новая, в навбаре — старая до повторного входа.
- **Сделано:** `AccountController.UploadPhoto` после сохранения перевыпускает cookie и claim `UserPhotoUrl`; навбар на текущей странице по-прежнему обновляется JS.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildPhotoClaim` — 0 ошибок.

---

## 2026-10-27 — Тёмная тема: label и placeholder в полях

- **Суть:** в тёмной теме имя поля и placeholder наезжали друг на друга; на светлой нет.
- **Сделано:** у `form-floating` в dark placeholder снова прозрачный (как в Bootstrap). Обычные инпуты по-прежнему показывают подсказку. Правка в `site.css`, плюс дерево рефералов.
- **Сборка:** не запускалась (CSS). Браузер в этой сессии не проверялся.

---

## 2026-10-26 — Экран дерева рефералов

- **Суть:** нужен красивый экран с деревом рефералов в меню под Statistics, связи между пользователями как у дерева.
- **Сделано:** пункт меню «Дерево рефералов»; `Dashboard/ReferralTree` и JSON `GetReferralTree`; `UserService.GetReferralTreeAsync` по `ReferrerId`; карточки + SVG-связи, pan/zoom, поиск, свернуть/развернуть; lookup пользователя при `users.view`. Super скрыт от остальных. Новых пакетов и миграций нет.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildReferralTree` — 0 ошибок (предупреждения прежние, не из этой фичи).
- **Браузер:** не проверялся (нет логина в этой сессии).

---

## 2026-10-26 — User Groups popup: выбранные и стили

- **Суть:** попап всегда писал «select at least one user» при отмеченных чекбоксах и выглядел без стилей.
- **Сделано:** выбранные читаются из `selectedRows` и из checked checkbox/`data-row-keys`; модалка переносится в `document.body` (иначе `overflow: hidden` карточки её режет); стили как у note-modal, светлая и тёмная тема.
- **Сборка:** не запускалась (разметка/CSS/JS). Браузер не проверялся.

---

## 2026-10-26 — User Groups: popup уведомление / письмо выбранным

- **Суть:** на форме группы кнопки Send notification / Send email ничего не делали. Нужен popup с текстом и отправка выбранным в гриде (чекбоксы).
- **Сделано:** модалка на Edit; `SendNotification` / `SendEmail` используют `INotificationService` и `IEmailSender`; только участники группы, видимые текущему пользователю; пропуск blocked и тех, кто выключил уведомления/почту. DataTable ctx вешается на элемент таблицы, KeyField UserId уже был.
- **Сборка:** `dotnet build Partnerly.sln` в `%TEMP%\PartnerlyBuildGroupSend` — 0 ошибок.
- **Браузер:** не проверялся.

---

## 2026-10-26 — Отчёт по изменениям Cursor-Agent

- **Суть:** владелец запросил полный отчёт работ на Partnerly.
- **Сделано:** `docs/reports/2026-10-26-izmeneniya-cursor-agent.md` по шаблону; сверка WORK_LOG / KI / TR / git. Новых правок приложения нет.
- **Сборка:** не нужна. Браузер не проверялся.

---

## 2026-10-26 — Nest Hub Max 1280×800: страницы обрезались

- **Суть:** на 1280×800 включается десктопный layout (≥1200). `.partnerly-layout-content` имел `overflow: hidden`, скролл только у прямого `.card > .card-body`. Dashboard / Profile / Settings / Support — `container-xxl`, не `.card`, низ экрана обрезался.
- **Сделано:** для страниц без `.card` включён `overflow-y: auto`; уменьшены padding, hero, графики, сетка команды и сайдбар Settings на 1200–1366 и при `max-height: 850px`.
- **Сборка:** не запускалась (CSS). Браузер не проверялся (нужен логин).

---

## 2026-10-26 — Повторный адаптив Support / Profile / Dashboard / Settings

- **Суть:** прошлый проход оставил `nowrap`, `max-width: 220px` у email, сетку команды с `minmax(260px)`, иконки-вкладки Settings без подписей, двойной padding `container-p-y`.
- **Сделано:** текст переносится; команда с 992px — карточки; вкладки Settings с подписями и горизонтальным скроллом; Support/Dashboard компактнее на телефоне; у контейнеров этих страниц сброшен лишний gutter на ≤1199.
- **Сборка:** не запускалась (CSS). Браузер не проверялся.

---

## 2026-10-25 — Адаптив Support, Profile, Dashboard, My Settings

- **Суть:** страницы плохо перестраивались при смене ширины и при сворачивании сайдбара (графики Dashboard не пересчитывали ширину).
- **Сделано:** сетка Bootstrap `col-12`/`col-lg`/`col-xl`; брейкпоинты 1199/991/768/575 в CSS страниц; Support обёрнут в `container-xxl`; событие `partnerly-layout-change` после toggle сайдбара, Dashboard перерисовывает графики.
- **Сборка:** не запускалась (разметка/CSS/JS). Браузер не проверялся.

---

## 2026-10-25 — Иконка Support левее остальных в свёрнутом меню

- **Суть:** пункт Support содержит бейдж `24/7`. При свёрнутом сайдбаре текст/бейдж прятались через `width: 0`, но padding бейджа и `gap` flex оставались — иконка смещалась влево.
- **Сделано:** в collapsed режиме текст, бейдж и external скрыты через `display: none`, у ссылки `gap: 0`. На мобилке отображение возвращено.
- **Сборка:** не нужна. Браузер не проверялся.

---

## 2026-10-25 — Свёрнутое меню вспыхивает при клике по иконке

- **Суть:** класс `partnerly-sidebar-collapsed` ставился только в `DOMContentLoaded`. После перехода по иконке страница сначала рисовала широкое меню, потом JS сворачивал его.
- **Сделано:** тот же класс из localStorage сразу после `<body>`; у скрытых заголовков секций `pointer-events: none`; клик по секции в collapsed desktop не переключает аккордеон.
- **Сборка:** не запускалась (разметка/CSS/JS). Браузер не проверялся.

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
