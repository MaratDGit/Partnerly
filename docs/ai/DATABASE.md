# База данных Partnerly

Сверка: сущности, `AppDbContext`, Fluent API, `AppDbContextModelSnapshot`, миграции.  
Прямого подключения к SQL Server при составлении документа **не было**.  
Секреты и строки подключения сюда не записываются.

## Провайдер и контекст

- EF Core 6, `UseSqlServer`.
- Контекст: `Partnerly.Models.AppDbContext`.
- Старт приложения: `DbInitializer.Initialize` → `context.Database.EnsureCreated()`, затем seed ролей/админа/настроек/шаблонов писем, затем `PermissionSeeder.SeedAsync`.

`EnsureCreated()` создаёт пустую БД по текущей модели и **не применяет** историю миграций. Если БД уже есть, он её не обновляет. Совместное использование `EnsureCreated` и папки `Migrations` — источник рассинхрона (см. `KNOWN_ISSUES.md`).

## Правила агента

Без явного разрешения владельца запрещено: подключаться к прод-БД, менять данные, запускать миграции, менять схему, удалять объекты, выполнять необратимый SQL.

Новые изменения схемы — только после разрешения, через миграции EF, с обновлением этого файла.

## Соглашения сущностей

Большинство таблиц:

- PK: `Guid Id`.
- Мягкое удаление: `IsDeleted`.
- Аудит: `CreatedBy`, `CreatedDate`, `UpdatedBy`, `UpdatedDate`, часто `Note`.
- Интерфейс аудита: `IAuditableEntity` (без `IsDeleted`).

Исключения: `SystemSettings.Id` — `int`; `UserGroupMember` — составной ключ `(UserId, GroupId)`; таблицы прав — свои PK Guid и уникальные индексы.

Заполнение аудита — в `AppDbContext.SaveChangesAsync` для Added/Modified, если `SkipValidations == false`. Там же проверка required-полей через `ValidationHelper`.

## Таблицы (имена из snapshot)

### Users

Пользователь системы. FK: `RoleId` → Roles, `ReferrerId` → Users (`OnDelete: Restrict`).  
Поля (подтверждено сущностью): GoogleId, Email, PasswordHash, Phone, FirstName, LastName, PhotoUrl, Balance, LastRoleUpdateTime, MyReferralCode, LastActivity, IsOnlayn, IsBlocked, EmailConfirmed, AllowSendNotifications, AllowSendEmails, системные колонки.  
Навигации: Role, Referrer, Referrals, Transactions, Payments, UserPermissions.  
Экран дерева рефералов (`/Dashboard/ReferralTree`) только читает `ReferrerId` / `Referrals`, схему не меняет.

### Roles

Имя роли, `IsActive`, аудит, `IsDeleted`. Коллекции Users, RolePermissions.  
Поле `Type` удалено миграцией `20261024211314_deleteRoleType`.

### Permissions

Справочник прав. Уникальный индекс по `Code`. Обязательные `Code` (max 150), `Module` (max 100), также Name, Description, IsActive, IsDeleted.

### RolePermissions

Связь роль–право. Уникальный индекс `(RoleId, PermissionId)`.  
FK Role — Cascade, FK Permission — Restrict.

### UserPermissions

Индивидуальное право. Уникальный индекс `(UserId, PermissionId)`.  
`IsGranted` (bool): явное разрешение или запрет. Перекрывает роль в `PermissionService`.  
FK User — Cascade, FK Permission — Restrict.

### AccessControlAuditLogs

Журнал изменений прав. Индексы `(TargetUserId, CreatedDateUtc)`, `(TargetRoleId, CreatedDateUtc)`.  
Не реализует `IAuditableEntity`.

### UserGroups / UserGroupMembers

Группы и состав. PK членов — `(UserId, GroupId)`. Оба FK — Cascade.  
У `User` нет обратной коллекции членов (`WithMany()` без навигации).

### EmailConfirmationTokens

Токены подтверждения email / сброса пароля (уточнять сценарии по сервису, не по имени таблицы).

### EmailTemplates

Имя, тема, HTML-тело (Handlebars-плейсхолдеры в seed).

### Logs

Журнал действий приложения (`Action`, `Type`, `LogMessage`).

### Notifications

Уведомления пользователя. Связь с тикетами — по полям сущности (проверять `Notification.cs` при изменениях).

### SupportTickets

Обращения. `TicketID` (строковый номер), Subject, Message, Status, AssignedTo, IsRead.  
FK `CreatedBy` → Users.Creator Restrict; `AssignedTo` → Users.Assigned Restrict.  
Навигации `Creator` и `Assigned` помечены как non-nullable в модели — фактически Assigned может быть пустым в данных (**предположение:** риск при Include без учёта null).

### Attachments

Полиморфные вложения: индекс `(EntityType, EntityId, IsDeleted)`. Файлы на диске, не в BLOB (сервис хранения).

### SystemSettings

Обычно одна запись. Id int. Maintenance, сроки токенов, интервал онлайн-статуса, `TaskTickedID`, `ScrollingText`.

### Payments

Сумма, метод, статус (`PaymentStatus` attribute). `IsDeleted` в модели — `bool?`, в отличие от большинства сущностей (`bool`). Контроллера нет.

### Transactions

Очки / описание по пользователю. Контроллера нет.

## Связи (кратко)

```text
Role 1—* User *—1 User(Referrer)
User 1—* UserPermission *—1 Permission
Role 1—* RolePermission *—1 Permission
User 1—* Payment
User 1—* Transaction
UserGroup 1—* UserGroupMember *—1 User
User 1—* SupportTicket (Creator / Assigned)
```

Fluent API **не** описывает явно все FK Payments/Transactions/Notifications — часть может быть по конвенции EF. При сомнении сверять snapshot.

## Seed при пустой БД (`DbInitializer`)

Если нет Super-роли — создаётся роль `Constants.SuperRoleName` со своим Guid.  
Если пользователей 0 — создаётся суперпользователь с `RoleId = adminRoleId`, роли Admin/Employee/User, лог, SystemSettings, два email-шаблона.

**Факт:** Guid супер-роли и `adminRoleId` **разные**. Суперпользователь привязывается к роли Admin, не к Super-роли.

Пароль и email seed заданы в `Constants` — в этот документ не копируются.

## Миграции

Каталог: `Partnerly/Migrations`.  
Начальная: `20250923122644_InitialClean`.  
Права: `20261021145957_AddRolePermissionsSystem`.  
Удаление Role.Type: `20261024211314_deleteRoleType`.  
Есть `20250923122849_testMigration`.

Полный список файлов — в каталоге; не все имена отражают фактический SQL без чтения файла.

## Целостность

- Уникальность кода права и пар роль/право, пользователь/право.
- Restrict на реферера и на Permission при удалении права, на создателя/исполнителя тикета.
- Cascade на членов группы и на RolePermission/UserPermission при удалении роли/пользователя.
- Required-поля проверяются и атрибутами, и `SaveChangesAsync`.

## Подтверждено vs предположение

| Тема | Статус |
|------|--------|
| Имена таблиц в snapshot | подтверждено |
| Fluent-связи, перечисленные в `OnModelCreating` | подтверждено |
| Фактическое состояние конкретной клиентской БД | **не проверялось** |
| Применялись ли все миграции на машине разработчика | **неизвестно** (`EnsureCreated` может расходиться с миграциями) |
| Индексы, не заданные в Fluent, но созданные конвенцией | смотреть snapshot; не выдумывать |
