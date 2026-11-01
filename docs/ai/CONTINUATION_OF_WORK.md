# Продолжение работы

Обновлено: 2026-11-01.

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
- Массовое удаление DataTable (`datatable.bulk_delete`, попап Start/Stop, лог `BD`) — в рабочем дереве, **не закоммичено**.

## Последняя задача

Массовое удаление в DataTable. Корзина в **левой** части тулбара. Progress bar заполняется через `scaleX` (не CSS-переменная таблицы). History, tabs, reorder, settings, ID/Note, language, копирайт агентов, тарифы ещё не закоммичены.

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

- Нет автотестов.
- Модули Payment/Transaction без UI.
- KI-003 seed user на Admin, не Super — открыто.
- Браузер в этой сессии недоступен — проверить dark: формы (Settings, System Preferences, тикет), Documentation, Privacy-попап, таблицы.
- Браузер: попап на `/` — Login, Sign up, Forgot, ошибки валидации, Google, язык/тема при открытом диалоге.
- Privacy/Docs, зум точек и dark-текст закоммичены; push — по просьбе владельца.

## Рекомендуемый следующий шаг

1. В браузере под Super/Admin: в настройках колонок включить Selected, отметить строки, справа появляется корзина. Попап: Start/Stop с подтверждением, счётчики, после удаления запись в Logs с действием Bulk delete.
2. Group members: Selected уже видна, корзина без включения колонки. History на Edit User Role — Selected есть, корзины нет.
3. Коммит History + tabs + column reorder/settings + ID/Note + language + bulk delete и push — по просьбе владельца.

Перед кодом снова прочитать `AGENTS.md` и этот файл.
