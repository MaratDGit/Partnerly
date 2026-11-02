# Продолжение работы

Обновлено: 2026-11-02 (вечер).

## Текущее состояние

- Ветка: `Cursor-Agent` (tracking `origin/Cursor-Agent`).
- Приложение: ASP.NET Core MVC, .NET 6, монолит `Partnerly/Partnerly.csproj`.
- Документация агента на месте.
- UserPermissions / политики — `7e8173b` (на origin). Адатив четырёх страниц — `3f8568b` (на origin). User Groups send + попап — `e27dcad` (на origin).
- Дерево рефералов — коммит `f1e14ea`. Веб-инспектор — `bdc34e4`. Поиск в навбаре, Statistics+Dashboards, фото ролей — `57bf563`.
- Расширение System Settings — закоммичено на `Cursor-Agent`, **push ещё нет**. Миграция на локальной БД **применена**.
- Гостевой лендинг/чат — `bd0a764`. Account-попап — `51205de`. Privacy/Docs/dark-текст — `fd84fb8`. Кэш SystemSettings, цикл логотипа, flip auth и сборка попапа из точек — `3f1b869`. Попап fade, линии точек, письмо имени — `ca1f444` на `Cursor-Agent`. **Push ещё нет.**
- Вкладка History на Edit User Role и общий `<partnerly-tabs>` — в рабочем дереве, **не закоммичены**.
- Перестановка колонок DataTable (`reorder.js`) и панель настроек колонок — в рабочем дереве, **не закоммичены**.
- ID по праву `datatable.view_ids` и колонка Note — в рабочем дереве, **не закоммичены**.
- Смена языка без reload, включая гостевую `/` без перезапуска лендинга — в рабочем дереве, **не закоммичено**.
- Массовое удаление, уведомления `$N:`, CTA лендинга и DataTable UI — коммит `73db933` на `origin/Cursor-Agent`.
- Login/Register: сначала «Продолжить через email» — в рабочем дереве, **не закоммичено**.
- Меню: цвет иконки без квадрата, стекло на hover/выбор — в рабочем дереве, **не закоммичено**.
- Dropdown профиля: стекло, анимация, копирование реферальной ссылки — в рабочем дереве, **не закоммичено**.
- Statistics: верхние вкладки в стиле меню — в рабочем дереве, **не закоммичено**.
- Свёрнутое меню: подпись мелким шрифтом под иконкой — в рабочем дереве, **не закоммичено**.
- Скролл меню скрыт до hover; плавные вкладки Statistics — в рабочем дереве, **не закоммичено**.
- DataTable Select all по центру колонки Selected — в рабочем дереве, **не закоммичено**.
- DataTable: таблица на ширину контейнера, Selected первая 60px — в рабочем дереве, **не закоммичено**.
- Apple Sign In на Login/Register — в коммите `a84fd0d` (на origin). Миграция `20261102092927_AddAppleSignIn` **применена** на локальной БД 2026-11-02. Ключи Apple пустые.
- System Settings / My Settings: полная карточка, колонки, клик по switch — в `a84fd0d`.
- OpenNewCase / ViewCase (тикет для исполнителя + Save заметки) — в `a84fd0d`.
- Automation PHASE 1–2: `docs/ai/Automation/` (спека, анализ, ARCHITECTURE.md).
- Automation PHASE 3–7 + закрытие v1 + UX (glass create, delay amount/unit, no sticky): в рабочем дереве, **не закоммичено**. Миграция `20261102134353_AddAutomationTables` **применена** на локальной БД 2026-11-02.

## Последняя задача

Dropdown профиля: пункты как в левом меню (высота/шрифт/иконки). Dropdown языка чуть компактнее. Браузер не проверялся. Коммит не делался.

## Уже сделано недавно

- Поиск в навбаре; Statistics+Dashboards; фото ролей.
- Исполнитель тикета видит ViewCase.
- Веб-инспектор: live Chrome + extension, iframe HTML.
- Дерево рефералов, адаптив, User Groups send, профиль/email/loader.
- Лендинг: точки, логотип-ветка, чат-поиск без AI, иконки входа.

## Важные нюансы

- Секреты: user-secrets + локальный `secrets.json`. В Git Google OAuth пустой. SMTP/Google/OpenAI **не** в SystemSettings.
- Миграция `20261030074752_ExpandSystemSettingsAndUserLockout` создана и **применена** на локальной БД 2026-10-30.
- Cookie `LoginPath` = `/Account/Login` → 302 на `/?auth=login`.
- Реферал: `/Account/Register?reff=` открывает попап регистрации.
- Сброс пароля по письму (`ConfirmForgotPassword`), GoogleRegister и AccessDenied — ещё отдельные страницы.
- AI на гостевой поиск не включать, пока владелец не попросит.
- Не путать канонические файлы: журнал = `docs/ai/WORK_LOG.md`.

## Незавершённое и препятствия

- Автотесты есть только у Automation (`Partnerly.Automation.Tests`, 90). Остальной монолит без тестов (KI-011 частично).
- `execute_workflow` ставит child и не ждёт его окончания (fire-and-forget).
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер в этой сессии недоступен — проверить dark: формы (Settings, System Preferences, тикет), Documentation, Privacy-попап, таблицы.
- Браузер: попап на `/` — Login, Sign up, Forgot, ошибки валидации, Google, язык/тема при открытом диалоге.
- Privacy/Docs, зум точек и dark-текст закоммичены; push — по просьбе владельца.

## Рекомендуемый следующий шаг

1. Коммит Automation (PHASE 3–7 + v1 + UX + хаб + Agents KPI + меню Agents/футер) — по просьбе владельца.
2. Глазами: левое меню Agents, футер Documentation/Support, Statistics вкладка Agents, hover карточек без сдвига.
3. PHASE 8 отчёт — только по просьбе.
4. Другие стенды / прод — миграцию `AddAutomationTables` не применять без отдельного ок.

Перед кодом снова прочитать `AGENTS.md`, этот файл и `docs/ai/Automation/ARCHITECTURE.md`.
