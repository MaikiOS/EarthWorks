# Проверка EarthWorks 0.6.5

[English (primary)](TESTING.md)

Paint-grid, seams/углы Heightmap, совместная работа с ATMC и доказательства по каждому инструменту описаны в [плане terrain-совместимости и тестирования](docs/TERRAIN_COMPATIBILITY_AND_TEST_PLAN_RU.md).

## Среда

- Только профиль `TerrainRamp-1.0-Test`.
- Персонаж `Test`, мир `TerrainRamp_Lab`.
- В `Player.log` ожидаются `EarthWorks 0.6.5 loaded` и строка TestBootstrap.
- Профиль `Default` не использовать.

## Локализация

1. Запустить `scripts\Audit-Localization.ps1`: все пять проверок должны быть PASS, количество ключей — 227.
2. В игре выбрать English и пройти editor, preview, validation error и доску: русского текста быть не должно.
3. Переключить язык на Russian без перезапуска мира и повторить те же экраны: должны измениться заголовки, controls, Inspector, ошибки, сообщения камеры и этапы доски.
4. Убедиться, что вместо текста нигде не показаны `$earthworks_*`, `state_drawing` или `controls_drawing`.

## Основной маршрут

1. Открыть лабораторную панель `F8`, проверить управление курсором, прогресс, stamina и сброс площадки.
2. Выбрать EarthWorks Route в категории Misc мотыги.
3. Создать маршрут минимум из четырёх точек: прямой участок, плавная кривая и Corner.
4. В `F7` проверить Plan/Isometric, drag, insert/delete, exact/auto height, левую/правую ширину и четыре профиля.
5. Сравнить Current, Result и Difference; красные ошибки должны блокировать создание.
6. Назначить bare/paved surface всему маршруту и отдельному сегменту.
7. Создать доску и выполнить все шесть стадий; instant execution допустим только для `Test` в `TerrainRamp_Lab`.
8. Сверить фактический terrain и paint с preview.

## Persistence и multiplayer

1. Перезагрузить мир: доска, стадия и результат должны сохраниться.
2. Перезапустить сервер и подключиться повторно.
3. Подключить второй клиент с тем же модом: маршрут, доска, стадии и изменения terrain должны совпадать.
4. Проверить одновременное взаимодействие, ward/protected area, угрозу, урон и потерю доступа.
5. В `TerrainRamp_SurvivalQA` убедиться, что лабораторные обходы недоступны.

## Сбор доказательств

Сохранить свежий `Player.log`, версии и SHA-256 DLL, скриншоты English/Russian экранов, preview до выполнения и terrain после reload/reconnect. Любой exception, смешанный язык, расхождение preview или потеря состояния означает NO-GO.
