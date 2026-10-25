# Стандарты разработки Partnerly

Выведены из существующего кода на 2026-10-25. Это описание факта, а не идеальный гайд. Новые изменения должны следовать этим соглашениям, пока владелец не решит иначе.

## Именование

- Пространства: `Partnerly.Controllers`, `Partnerly.Models`, `Partnerly.Models.ViewModels`, `Partnerly.Models.DataTable`, `Partnerly.Models.Authorization`, `Partnerly.Infrastructure.*`, `Partnerly.Events`, `Partnerly.Hubs`, `Partnerly.Helpers`, `Partnerly.Descriptors.*`.
- Сущности: единственное число, PascalCase (`User`, `SupportTicket`).
- Интерфейсы: `I` + имя (`IUserService`, `IUserRepository`).
- Контроллеры: `{Сущность}Controller`, множественное число ресурса (`UsersController`).
- ViewModels: `{Сущность}ViewModel`, формы часто `{Сущность}FormViewModel`.
- Константы прав: `модуль.действие` в нижнем регистре с точкой (`users.edit`).
- Политика: `Permission:` + код (`Permission:users.edit`).
- Опечатки в существующих именах сохранять при стыковке (`IsOnlayn`, `SupportTicked*`, `HasPermision`, `ReffCode`, `TaskTickedID`), не «исправлять заодно».

Файлы контроллеров и сервисов — один основной тип на файл. `_BaseController` с подчёркиванием — исключение.

## Слои

1. Контроллер: HTTP, antiforgery, выбор View, маппинг ViewModel ↔ сущность, вызов сервиса.
2. Сервис: правила, логи, события, `ServiceResult`.
3. Репозиторий: EF, `QueryEntity`.
4. Не ходить из контроллера в `AppDbContext`, если для модуля уже есть репозиторий (AccessControl/PermissionService сейчас работают с контекстом напрямую — существующее исключение, не размножать без причины).

Новый модуль: интерфейс + реализация + регистрация Scoped в `Program.cs` + (при списке) DataTable.

## Контроллеры

- Админский CRUD: наследовать `_BaseController`.
- Список: `Index` отдаёт `Views/Shared/DataTable/_DataTablePartial.cshtml` с `DataTableOptions`.
- Форма Create/Edit: общее View `Form.cshtml`, POST с `[ValidateAntiForgeryToken]`.
- Права: `[Authorize(Policy = "Permission:...")]` на **GET и POST** одинаковых операций. Сейчас покрытие неполное — новые действия делать полными.
- Удаление: confirmation + `DeleteConfirmed`.
- Не использовать закомментированный `[ClaimAuthorize]` и `Role.Type`.

## ViewModels

- Отдельный класс в `Models/ViewModels`.
- Для таблиц — статический `CreateTableOptions(string tableID, IDataTableRegistry registry)`.
- Для аудита на форме — `IAuditableViewModel` и `PrepareViewModelAsync`.
- Create/Edit иногда совмещены в одном Form ViewModel с `CreateModel` / `EditModel`; POST снимает лишние ключи `ModelState.Remove`.

## Валидация

- Data Annotations на сущностях и ViewModels (`[Required]`, `[Display]`, кастомные `*Attribute` из `Descriptors/Attributes`).
- Справочники значений: наследники `StringListAttribute` (статусы и т.п.).
- Серверная проверка required при SaveChanges.
- Клиент: `_ValidationScriptsPartial`, jquery-validation в `wwwroot/lib`.

## Логирование и ошибки

- Доменные действия: `ILogService.CreateLogAsync` с `LogActionsAttribute` / `LogTypeAttribute`.
- Инфраструктура: `ILogger<T>` (Account, AI, OpenAI).
- Сообщения пользователю: `ErrorMessages`, `Messages`, `FieldsDisplayNames` + локализатор `IStringLocalizer<SharedResource>`.
- Не глотать исключения без лога. Не писать секреты в лог.

## Razor / CSS / JS

- Layout по умолчанию `_ContentNavbarLayout`.
- Переиспользовать `_Toast`, `_DeleteConfirmation`, `_AuditInfo`, DataTable partials.
- Стили проекта: `wwwroot/assets/css/site.css` и узкие css страниц; vendor не править без нужды.
- JS таблиц: модули в `wwwroot/js/datatable/modules/`.
- Локализация текстов UI через resx, не хардкодить третий язык вразброс.
- Новый Tag Helper — только если нет существующего (`RequiredLabelTagHelper`).

## Авторизация и безопасность

- Секреты не в Git.
- Cookie + свои User/Role.
- Проверка права — `IPermissionService` / политика, не новое «если RoleName == Admin» (кроме уже существующих Super/Admin в middleware и `_BaseController`).
- Файлы загрузок — через `IFileStorageService` / `IAttachmentService`, не произвольные пути вне `wwwroot/uploads`.
- Антифоргери на POST форм.

## Сборка и проверка

```text
dotnet build "Partnerly.sln"
```

Тестов нет. Для UI: сценарий пользователя на затронутых страницах.  
Не отмечать задачу выполненной без фактической проверки.

## Git и зависимости

- Коммит/push по просьбе.
- NuGet — не добавлять, если закрывается существующим пакетом (ClosedXML, ImageSharp, Handlebars, BCrypt уже есть).
- Целевая версия — net6.0, не поднимать TFM без решения владельца.
