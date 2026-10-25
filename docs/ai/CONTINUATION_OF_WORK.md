# Продолжение работы

Обновлено: 2026-10-25.

## Текущее состояние

- Ветка: `Cursor-Agent` (tracking `origin/Cursor-Agent`).
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- UserPermissions, снятие AccessControl, покрытие политиками — **незакоммичено**.

## Последняя задача

DataTable и StickyButtons: UI кнопок по `ViewData["Permissions"]`. Коды на CreateTableOptions и FormActions.

## Уже сделано недавно

- Ветка от `Belote-Dev`, очистка секретов, docs, чистка, i18n, UserPermissions, удаление AccessControl.

## Важные нюансы

- Секреты: user-secrets + локальный `secrets.json`. В Git Google OAuth пустой.
- Права: политики на CRUD; видимость — `IUserVisibilityService` (Super видит всех и скрыт от остальных; Admin не видит других Admin и Super; Employee не видит Admin и Super; User — себя).
- DataTable: id таблиц уникальны; тикеты — `supportTicketsTable`.
- БД: не запускать миграции без разрешения. Seeder **не удаляет** лишние RolePermissions на уже заполненной БД — после рестарта приложения только добавятся новые коды.
- Не путать канонические файлы: журнал = `docs/ai/WORK_LOG.md`.

## Незавершённое и препятствия

- Нет автотестов.
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер после политик не проверялся.
- Приложение могло быть запущено (сборка шла в `%TEMP%`).

## Рекомендуемый следующий шаг

1. Перезапустить приложение, чтобы seeder дописал новые коды Super/Admin/Employee/User.
2. Проверить в браузере четыре роли: меню, AccessDenied, DataTable и StickyButtons (Save/Create/Delete/StartCase).
3. По просьбе владельца — коммит.

Перед кодом снова прочитать `AGENTS.md` и этот файл.
