# Продолжение работы

Обновлено: 2026-10-26.

## Текущее состояние

- Ветка: `Cursor-Agent` (tracking `origin/Cursor-Agent`).
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- UserPermissions / политики — `7e8173b` (на origin). Адатив четырёх страниц — `3f8568b` (на origin). User Groups send + попап — `e27dcad` (на origin).
- Дерево рефералов — коммит `f1e14ea`. Веб-инспектор и сопутствующий UI — в следующем коммите этой сессии.

## Последняя задача

Инспектор парсит HTML iframe: все фреймы вкладки сливаются в полный HTML/текст/карточки. Нужен Reload unpacked-расширения (v1.1.0, право `webNavigation`).

## Уже сделано недавно

- Ветка от `Belote-Dev`, очистка секретов, docs, чистка, i18n, UserPermissions, удаление AccessControl.
- Адатив Dashboard / Profile / Settings / Support, в том числе 1280×800.
- User Groups: Send notification / Send email выбранным в гриде.

## Важные нюансы

- Секреты: user-secrets + локальный `secrets.json`. В Git Google OAuth пустой.
- Права: политики на CRUD; видимость — `IUserVisibilityService` (Super видит всех и скрыт от остальных; Admin не видит других Admin и Super; Employee не видит Admin и Super; User — себя).
- Дерево рефералов: корень = сам / CanViewUser / своя downline; потомки не фильтруются CanViewUser, Super скрыт если смотрящий не Super.
- DataTable: id таблиц уникальны; тикеты — `supportTicketsTable`.
- БД: не запускать миграции без разрешения. Seeder **не удаляет** лишние RolePermissions на уже заполненной БД.
- Не путать канонические файлы: журнал = `docs/ai/WORK_LOG.md`.

## Незавершённое и препятствия

- Нет автотестов.
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер: дерево рефералов в этой сессии не открывалось.
- Приложение могло быть запущено (сборка шла в `%TEMP%`).

## Рекомендуемый следующий шаг

1. chrome://extensions → Reload у Partnerly Web Inspector (v1.1.0). Открыть сайт с iframe: HTML и карточка Iframes должны включать фреймы.
2. Следующая фича по просьбе владельца. Push — только если попросит.

Перед кодом снова прочитать `AGENTS.md` и этот файл.
