# Отчёт: первоначальный анализ Partnerly

- **Тип:** проект (первичная настройка памяти агента)
- **Период:** 2026-10-25
- **Ветка Git:** Cursor-Agent
- **Источники:** исходный код, `AGENTS.md` (черновик), git status/log этой ветки
- **Дата составления:** 2026-10-25
- **Сборка и тесты:** не запускались

## Краткое резюме

Partnerly — монолит ASP.NET Core MVC на .NET 6 с собственными пользователями/ролями, cookie и Google OAuth, политиками `Permission:`, серверным DataTable, SignalR-уведомлениями и EF Core + SQL Server. Создана система документации `docs/ai/` и правила Cursor. Бизнес-логика, схема БД и зависимости проекта не менялись. Живая база не открывалась.

## Созданные и обновлённые файлы

| Файл | Действие |
|------|----------|
| `AGENTS.md` | обновлён существующий черновик |
| `.cursor/rules/partnerly-workflow.mdc` | создан |
| `.cursor/rules/partnerly-security.mdc` | создан |
| `docs/ai/REVIEW_PROJECT.md` | создан |
| `docs/ai/Architecture.md` | создан |
| `docs/ai/DATABASE.md` | создан |
| `docs/ai/DEVELOPMENT_STANDARDS.md` | создан |
| `docs/ai/TECHNICAL_SOLUTIONS.md` | создан |
| `docs/ai/KNOWN_ISSUES.md` | создан |
| `docs/ai/WORK_LOG.md` | создан |
| `docs/ai/CONTINUATION_OF_WORK.md` | создан |
| `docs/ai/REPORT_TEMPLATE.md` | создан |
| `docs/reports/2026-10-25-pervonachalnyj-analiz.md` | этот отчёт |

Существующий `.github/copilot-instructions.md` сохранён (Azure Copilot, не архитектура Partnerly). Других проектных markdown, кроме лицензий библиотек, не найдено.

## Что выяснено об архитектуре

- Один проект в `Partnerly.sln`. Слои внутри: контроллер → сервис → репозиторий → `AppDbContext`.
- Аутентификация: cookie + Google, не ASP.NET Identity.
- Авторизация: `[Authorize]` на `_BaseController`; точечные политики `Permission:{code}`; индивидуальные `UserPermission` перекрывают роль.
- Списки: собственный DataTable (ViewModel options + `DataTableQueryService` + Razor/JS).
- События: in-memory `EventBus`. Realtime: `NotificationHub`.
- Старт: `EnsureCreated()` + seed + `PermissionSeeder`.
- Локализация en/ru/hy. Layout `_ContentNavbarLayout`.
- Платежи и транзакции есть в модели, UI нет.
- Тестовых проектов нет.

## Открытые вопросы (не проверены runtime)

- Состояние конкретной SQL-базы относительно миграций.
- Срабатывают ли middleware, зарегистрированные после `MapControllerRoute`.
- Фактический эффект столкновения id DataTable тикетов и email-шаблонов в браузере.
- Нужна ли ротация ключей после отклонённого GitHub push.
- Коммит документации — не выполнялся.

## Готовность к работе с AI-агентом

**Да**, при условии что следующая сессия читает `AGENTS.md` и `docs/ai/CONTINUATION_OF_WORK.md`. Документация в Git ещё не закоммичена.

## Следующие шаги

По решению владельца: коммит docs; либо точечные KI (seeder Super, DataTable id, покрытие политик); либо новая продуктовая задача.
