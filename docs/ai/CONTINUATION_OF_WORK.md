# Продолжение работы

Обновлено: 2026-11-03 (ночь, Asia/Yerevan).

## Текущее состояние

- Ветка: `Cursor-Agent` (tracking `origin/Cursor-Agent`), **ahead of origin**.
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- Коммит `b406307` — Automation PHASE 3–7 + хаб + Agents KPI + меню Agents/футер + тесты + миграция `20261102134353_AddAutomationTables`.
- Следующий коммит (этот) — glass попапы/DataTable, Builder как страница, Agents context menu, индикатор `IsActive`, Delete glass-попап. SHA допишу после коммита.
- Миграция Automation **применена** на локальной БД 2026-11-02. Другие стенды / прод — не трогать без отдельного ок.

## Последняя задача

Владелец: commit и push. В коммите: glass UI, `/Automation/Builder/{id}`, Agents rename/pause/start, индикатор `IsActive`, Delete-попап.

## Уже сделано недавно

- Коммит Automation + меню Agents (`b406307`).
- Glass слой для попапов и DataTable chrome.
- Редактор процесса — страница `/Automation/Builder/{id}`, не popup.
- Меню Agents: секция, плюс, live-список, индикатор `IsActive`, давность.
- Футер Documentation/Support с иконками; Statistics chart-иконка.
- Navbar: поиск правее; dropdown профиля как меню; язык компактнее.
- Statistics Agents — живые KPI автоматизации.

## Важные нюансы

- Секреты: user-secrets + локальный `secrets.json`. В Git Google OAuth пустой. SMTP/Google/OpenAI **не** в SystemSettings.
- Cookie `LoginPath` = `/Account/Login` → 302 на `/?auth=login`.
- Реферал: `/Account/Register?reff=` открывает попап регистрации.
- AI на гостевой поиск не включать, пока владелец не попросит.
- Не путать канонические файлы: журнал = `docs/ai/WORK_LOG.md`.
- Общий glass app-shell владелец раньше откатывал — не возвращать. Текущий слой только попапы + DataTable chrome (`TR-2026-11-03-01`).

## Незавершённое и препятствия

- Автотесты есть только у Automation (`Partnerly.Automation.Tests`, 91). Остальной монолит без тестов (KI-011 частично).
- `execute_workflow` ставит child и не ждёт его окончания (fire-and-forget).
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер в этой сессии недоступен — проверить glass: DataTable фильтр/колонки/экспорт/удаление, DeletePartial, note/attachments, lookup, dark theme.
- Privacy/Docs, Automation, меню — в `b406307`; glass/Builder/Agents — в следующем коммите этой сессии. Push по просьбе владельца (сейчас).

## Рекомендуемый следующий шаг

1. Глазами: индикатор Agents, Delete glass-попап, right-click, Builder-страница, glass попапы.
2. PHASE 8 отчёт — только по просьбе.
3. Другие стенды / прод — миграцию `AddAutomationTables` не применять без отдельного ок.

Перед кодом снова прочитать `AGENTS.md`, этот файл и `docs/ai/Automation/ARCHITECTURE.md`.
