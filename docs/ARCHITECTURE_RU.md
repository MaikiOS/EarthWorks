# Архитектура EarthWorks

[English (primary)](ARCHITECTURE.md)

EarthWorks разделяет математику маршрута, взаимодействие редактора, планирование, сохранение и изменение мира. Это позволяет проверять каждую правку на той границе, которую она действительно затрагивает.

## Поток данных

`EarthWorksPlugin` регистрирует настройки и prefabs. `RoadDraftSession` хранит приватный черновик. `RoadTerrainPlanner` преобразует его в единый неизменяемый `RoadBuildPlan`. Preview и выполнение используют один и тот же план. `RoadProjectRecord` сохраняет постоянную часть в ZDO доски. Доска проверяет инициатора RPC, после чего `RoadTerrainApplier` изменяет мир через штатные `Heightmap` и `TerrainComp` Valheim.

## Карта модулей

| Область | Файлы | Где вносить изменения |
| --- | --- | --- |
| Запуск и config | `EarthWorksPlugin.cs` | Регистрация плагина, настроек, инструмента или prefab доски |
| Интерфейс редактора | `EarthWorksEditorView.cs` | Панели, надписи, кнопки и раскладка |
| Жизненный цикл черновика | `RoadDraftSession.cs` | Этапы, завершение маршрута, review и создание проекта |
| Работа редактора | `RoadDraftSession.Editor.cs` | Выбор, drag, handles, точные значения и shortcuts |
| Камера и сетка | `RoadEditorCamera.cs`, `RoadEditorGrid.cs`, `RoadEditorPatches.cs` | Plan/Isometric и перехват ввода |
| Чистая геометрия | `src/EarthWorks.Geometry` | Кривые, профили, offsets и solvers |
| Планирование terrain | `RoadTerrainPlanner.cs`, `RoadBuildSettings.cs` | Проверки, sampling, cut/fill и footprint |
| Сохранение | `RoadProjectRecord.cs` | Формат проекта и совместимость миров |
| Доска и authority | `RoadProjectAuthority.cs`, `RoadProjectBoard.cs`, `RoadProjectFactory.cs` | Размещение, RPC-права, стадии и разметка |
| Изменение мира | `RoadTerrainApplier.cs` | Ownership, транзакция, rollback и paint |
| Локализация | `Translations/EarthWorks` и `EarthWorksLocalization.cs` | Новый или переведённый пользовательский текст |

## Нельзя нарушать

- Числовые значения сохраняемых enum постоянны. Новые значения добавляются в конец и закрепляются тестом.
- Изменение формата требует новой версии и чтения всех поддерживаемых старых версий.
- Клиент не выдаёт себе terrain range, ward-доступ или переход стадии.
- Preview и выполнение используют один рассчитанный план.
- Обычная игра не может вызвать мгновенное лабораторное выполнение.
- Patch `Player.Update` выполняется до ванильного ввода только для уже захваченного EarthWorks действия; область перехвата должна оставаться минимальной.

## Границы проверки

Portable CI выполняет geometry- и localization-проверки. Полная сборка, persistence-тесты и API-аудит требуют легально установленных локальных игровых DLL. Runtime подтверждается только сценарием `TESTING.md`; успешная сборка не является runtime-доказательством.
