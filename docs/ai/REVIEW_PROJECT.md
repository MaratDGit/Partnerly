# Обзор проекта Partnerly

Дата сверки с кодом: 2026-10-25.  
Статус: подтверждено по исходникам текущего дерева `Cursor-Agent`, если не указано иное.

## Назначение

Веб-приложение для внутренней работы с пользователями и администрирования: регистрация и вход (пароль и Google), роли и права, группы пользователей, уведомления (в т.ч. SignalR), обращения в поддержку, вложения, шаблоны писем, системные настройки, журнал действий. В модели также заложены платежи и транзакции, но экранов под них нет.

Продуктовое имя в `AppSettings:AppName` — «Belote» (конфигурация). Код и решение называются Partnerly.

## Технологии и версии (из `Partnerly.csproj`)

| Технология | Версия / факт |
|------------|----------------|
| Целевой фреймворк | `net6.0` |
| ASP.NET Core MVC | контроллеры + Razor Views |
| Entity Framework Core + SQL Server | 6.0.0 |
| Аутентификация | Cookie + `Microsoft.AspNetCore.Authentication.Google` 6.0.36 |
| Пароли | BCrypt.Net-Next 4.0.3 |
| Excel / CSV | ClosedXML 0.105.1, EPPlus 8.2.0, свой экспорт DataTable |
| Письма | SMTP через `IEmailSender`, шаблоны Handlebars.Net 2.1.6 |
| Изображения | SixLabors.ImageSharp 3.1.11 и связанные пакеты |
| DataTables.net | пакет 2.3.4 (в UI используется собственный JS-модуль в `wwwroot/js/datatable`) |
| SignalR | `NotificationHub` |
| OpenAI | `IOpenAIService` / `AIController` |

`Nullable` и implicit usings включены.

## Структура решения

```text
Partnerly.sln
└── Partnerly/Partnerly.csproj     единственный проект
```

Основные каталоги проекта:

| Каталог | Назначение |
|---------|------------|
| `Controllers/` | MVC-контроллеры |
| `Models/` | сущности EF, ViewModels, DataTable |
| `Models/Authorization/` | Permission, RolePermission, UserPermission, audit |
| `Infrastructure/Interfaces/` | контракты сервисов и репозиториев |
| `Infrastructure/Services/` | бизнес-логика, middleware, hosted services, seeder |
| `Infrastructure/Repositories/` | доступ к данным |
| `Infrastructure/Authorization/` | политики прав |
| `Events/` | внутрипроцессная шина событий |
| `Hubs/` | SignalR |
| `Views/` | Razor |
| `wwwroot/` | статика, загрузки |
| `Resources/` | `.resx` локализации |
| `Migrations/` | миграции EF |
| `Helpers/`, `Descriptors/` | tag helper, атрибуты-справочники, сообщения |

Тестовых проектов нет. Каталога `docs/ai/` до этой настройки не существовало. Уже был черновик корневого `AGENTS.md` (не в Git). `.github/copilot-instructions.md` содержит только Azure-правила Copilot и к архитектуре Partnerly не относится.

## Основные модули

| Модуль | Контроллер / вход | Назначение |
|--------|-------------------|------------|
| Вход / регистрация | `AccountController` | cookie, Google, подтверждение email, сброс пароля |
| Лендинг / ошибки | `HomeController` | редирект на Dashboard, maintenance, error |
| Дашборд | `DashboardController` | стартовый экран после входа, дерево рефералов (`/Dashboard/ReferralTree`) |
| Пользователи | `UsersController` | список DataTable, CRUD, inline |
| Роли | `RolesController` | роли, права роли |
| Права | `RolesController` | DataTable role/user permissions на Edit / UserRoleEdit |
| Группы | `UserGroupsController` | группы и участники |
| Уведомления | `NotificationController` + hub | список и realtime |
| Тикеты | `SupportTicketsController` | обращения |
| Вложения | `AttachmentsController` + `IFileStorageService` | файлы сущностей |
| Письма | `EmailTemplatesController` | HTML-шаблоны |
| Настройки | `SystemPreferencesController` | `SystemSettings` |
| Журнал | `LogRecordsController` | логи |
| Язык | `LanguageController` | культура |
| AI | `AIController` | прокси к OpenAI |
| Веб-инспектор | `WebInspectorController` | загрузка страницы, HTML и видимые данные (`/WebInspector`) |

## Важные зависимости и интеграции

- **SQL Server** — строка `ConnectionStrings:DbContext` (значение не документируется).
- **SMTP** — секция `EmailSettings` в конфигурации.
- **Google OAuth** — `Authentication:Google:*` (в Git — пустые плейсхолдеры; локально user-secrets / Development).
- **OpenAI** — `OpenAI:ApiKey` из user-secrets или `secrets.json`; модель в `appsettings.json`.
- **SignalR** — `/notificationHub`, идентификатор пользователя = `ClaimTypes.NameIdentifier`.
- **Локальные файлы** — `wwwroot/uploads/attachments`, `wwwroot/uploads/profiles`.

## Запуск (локально)

Профиль `Partnerly` в `launchSettings.json`: Development, URL на localhost и LAN. Конкретные адреса в документацию не копируются как обязательные.

## Чего в репозитории нет (факт)

- Отдельного API-проекта и SPA.
- Автотестов.
- Контроллеров Payments / Transactions.
- Каталога `.github/workflows` в репозитории нет. `.github/copilot-instructions.md` — только заметки Azure для Copilot.
