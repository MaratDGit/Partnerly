# Журнал работ Partnerly

Новые записи — сверху. Не отмечать успех без проверки.

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
