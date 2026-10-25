# Журнал работ Partnerly

Новые записи — сверху. Не отмечать успех без проверки.

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
