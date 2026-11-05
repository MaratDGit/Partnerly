# Продолжение работы

Обновлено: 2026-11-03 (день, Asia/Yerevan).

## Текущее состояние

- Ветка: `Claude-Agent` (tracking `origin/Claude-Agent`), создана 2026-11-03 от `Cursor-Agent` (`34c5046`). `Cursor-Agent` больше не трогаем без просьбы.
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- Коммит `b406307` — Automation PHASE 3–7 + хаб + Agents KPI + меню Agents/футер + тесты + миграция `20261102134353_AddAutomationTables`.
- Коммит `7788ec1` — glass попапы/DataTable, Builder как страница, Agents context menu, индикатор `IsActive`, Delete glass-попап.
- Коммит `5ef721a` — OpenNewCase/My Settings попапы, My Profile без вкладок, поля 40px, unused Automation views удалены.
- Коммит `7909aa8` — гид автоматизации, 10 шаблонов, studio-билдер, светлее overlay попапов. Push на `origin/Cursor-Agent`.
- Миграция Automation **применена** на локальной БД 2026-11-02. Другие стенды / прод — не трогать без отдельного ок.

## Последняя задача

Сверка md-документов с кодом (2026-11-03): AGENTS, REVIEW_PROJECT, Architecture, Automation/ARCHITECTURE, KNOWN_ISSUES, DEVELOPMENT_STANDARDS. Тесты Automation 94/94. Вместе с переносом паролей — коммит `69823c3`, push `origin/Claude-Agent`. Встроенный браузер в этой сессии доступен.

## Уже сделано недавно

- Коммит Automation + меню Agents (`b406307`).
- Glass слой для попапов и DataTable chrome.
- Редактор процесса — страница `/Automation/Builder/{id}`, не popup.
- Меню Agents: секция, плюс, live-список, индикатор `IsActive`, давность.
- Футер Documentation/Support с иконками; Statistics chart-иконка.
- Navbar: поиск правее; dropdown профиля как меню; язык компактнее.
- Statistics Agents — живые KPI автоматизации.
- `/Documentation` — гид по триггерам и действиям агента (en/ru/hy); та же подсказка в правой панели билдера.
- Готовые шаблоны: 10 штук (малые/средние/большие), en/ru/hy, на хабе Agents и в попапе создания.
- Билдер: zoom −/+ / fit / колесо; полноэкранный холст после сайдбара, футер сайта виден; dropdown Trigger/Action, свойства overlay; right click / long-press карточки — Properties + Delete; glass-карточки, pan пустого места, фон light/dark.
- Overlay системных попапов светлее: `--partnerly-glass-overlay` 0.26 light / 0.36 dark, blur 6px (TR-2026-11-03-09).

## Важные нюансы

- Секреты: user-secrets + локальный `secrets.json`. В Git Google OAuth пустой. SMTP/Google/OpenAI **не** в SystemSettings.
- Cookie `LoginPath` = `/Account/Login` → 302 на `/?auth=login`.
- Реферал: `/Account/Register?reff=` открывает попап регистрации.
- AI на гостевой поиск не включать, пока владелец не попросит.
- Не путать канонические файлы: журнал = `docs/ai/WORK_LOG.md`.
- Общий glass app-shell владелец раньше откатывал — не возвращать. Текущий слой только попапы + DataTable chrome (`TR-2026-11-03-01`).

## Незавершённое и препятствия

- KI-009: пароли SMTP и seed перенесены в `secrets.json` (TR-2026-11-03-10), коммит `69823c3`. Осталось владельцу: сменить оба пароля — старые значения в истории Git. После рестарта приложения проверить отправку письма.
- Автотесты есть только у Automation (`Partnerly.Automation.Tests`, 94, зелёные 2026-11-03). Если приложение запущено — собирать с `-p:OutDir` во временную папку. Остальной монолит без тестов (KI-011 частично).
- `execute_workflow` ставит child и не ждёт его окончания (fire-and-forget).
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер в этой сессии недоступен — проверить glass: DataTable фильтр/колонки/экспорт/удаление, DeletePartial, note/attachments, lookup, dark theme.
- Privacy/Docs, Automation, меню — `b406307`; glass/Builder/Agents — `7788ec1`; попапы Settings/Support и компактные поля — `5ef721a`; docs/шаблоны/studio-билдер — `7909aa8`. Push `Cursor-Agent` по просьбе владельца.
- `/Documentation` содержит гид по триггерам и действиям; черновик DocsDraftNote больше не показывается. Проверки в браузере не было.

## Рекомендуемый следующий шаг

0. Интеграции реализованы (фазы 0–6) + хаб Agents переделан; шаблонов интеграций 14 (в т.ч. 6 тестовых для Sheets/Drive, 2026-11-04), тесты 136/136. Коммиты `9643c13`, `835e01d`; шаблоны Sheets/Drive — не закоммичены. У владельца Gmail API был выключен в Google Cloud (403) — включить Gmail/Sheets/Drive API. UI `/Integrations` и билдера проверен в браузере 2026-11-03. Осталось: живая проверка с реальным ботом / Google / Meta; шаблоны агентов и раздел `/Documentation` (7.1, 7.2); D5. Для превью новой сборки — `.claude/launch.json` (`partnerly-preview`, порт 7299, dll из временной папки сессии — путь надо обновить).

0h. 2026-11-05: независимые цепочки при ошибке, счётчики по процессам в History, список чатов Telegram в карточке (WORK_LOG, TR-2026-11-05-06). Тесты 177/177, **в браузере не проверено**, не закоммичено. Коммит `67dad89` (21 действие Google + выключение карточек) запушен.

0g. 2026-11-05: выключение карточек (WORK_LOG, TR-2026-11-05-05), тесты 176/176, **в браузере не проверено**, не закоммичено вместе с 21 действием Google (0f).

0f. 2026-11-05: Gmail / Sheets / Drive — добавлено 21 действие (WORK_LOG, TR-2026-11-05-04), тесты 170/170. **Не закоммичено, не проверено с реальным Google и в браузере.** Новый scope `gmail.modify` — Gmail-подключения нужно переподключить. Коммит `eec0c71` (Outlook, переводы палитры, окно «+») запушен в `origin/Claude-Agent`.

0e. 2026-11-05: интеграция Outlook (mail + calendar, Microsoft Graph) — код, тесты (152/152) и интерфейс готовы, **не проверено с реальным аккаунтом и в браузере**. Нужно от владельца: приложение в Microsoft Entra, redirect `…/Integrations/Microsoft/Callback`, `Authentication:Microsoft:ClientId/ClientSecret` в `secrets.json`. Детали: WORK_LOG, TR-2026-11-05-03, `INTEGRATIONS-PLAN.md` (фаза 8).

0d. 2026-11-05: избранное палитры хранится в БД (`AutomationFavorites`). **Миграция `20261105100000_AddAutomationFavorites` написана, но не применена** — до этого редактор работает на localStorage. Нужно: разрешение владельца на применение (`dotnet ef database update` / скрипт), затем проверить звёзды в двух браузерах. Миграция и снимок написаны вручную — сверить `dotnet ef migrations add` на пустое изменение. Также не проверены в браузере: окно «+» и входной порт слева.

0c. 2026-11-05 (позже): палитра с левым меню/иконками/избранным, история в карточке состояния, Undo/Redo — **не проверено в браузере**. История живёт только в памяти страницы (после перезагрузки начинается заново).

0b. 2026-11-05: редактор агента — «Готово», Copy/Paste, выпадающие поля из контекста, секции палитры (см. WORK_LOG). Сборка и тесты 142/142 ок, **в браузере не проверено**. Превью `partnerly-preview` (порт 7299) — `launch.json` указывает на сборку во временной папке сессии; нужен вход владельца. Проверить: Done при пустых/заполненных полях, Copy→Paste на пустом месте, поля create/update_record, чипы Gmail/Telegram в Add trigger/action.

0a. 2026-11-04: монитор запусков в попапе History (живой, с отсчётом до следующего запуска), правки webhook (повторная публикация, два webhook-узла, 429), даты в поясе настроек/агента — всё **не проверено в браузере** и не закоммичено. Тесты 142/142.

1. Глазами: Builder — футер, right click Delete на карточке, холст после меню; попапы системы — страница за ними читается. Хаб Agents и `/Documentation` тоже ещё не смотрели.
2. PHASE 8 отчёт — только по просьбе.
3. Другие стенды / прод — миграцию `AddAutomationTables` не применять без отдельного ок.

Перед кодом снова прочитать `AGENTS.md`, этот файл и `docs/ai/Automation/ARCHITECTURE.md`.
