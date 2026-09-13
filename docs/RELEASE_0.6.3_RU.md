# EarthWorks 0.6.3 — Valheim 1.0.12 и локализация

[English (primary)](RELEASE_0.6.3.md)

EarthWorks 0.6.3 — patch-релиз совместимости и локализации для Valheim 1.0.12 (Steam build 25253764, network version 40, Unity 6000.0.75f1) и Jotunn 2.30.0.

## Исправлено и проверено

- `RoadProjectBoard` реализует контракт Valheim 1.0 `Hoverable.GetHoverOffset()` и возвращает значение клонированного ванильного `Sign.m_hoverOffset`.
- Прямые вызовы, Harmony targets и reflection-контракты terrain прошли аудит текущей assembly.
- Инструмент маршрута использует штатную категорию `Misc`, пока custom build categories Jotunn 2.30.0 недоступны.
- Исправлено неверное имя localization key для состояния `Drawing`.
- Весь editor, камера, planner, доска и результаты terrain-операций используют парные английский/русский словари.
- Аудит локализации проходит 252 ключа и совпадение форматных параметров.
- Проходят все 24 geometry-проверки; Release-сборка завершается без ошибок и предупреждений.

В тестовом профиле сохранена наша сборка Jotunn 2.30.0 с исправлением регистрации terrain-операций. GPL-код TerrainTools не копировался; выбранные идеи roadmap должны реализовываться независимо.

## Runtime-статус

Valheim не запускался. Выполнение проекта, persistence, reconnect, второй клиент и survival acceptance остаются следующим контролируемым тестом.

## SHA-256

```text
EarthWorks.dll          366231DBD3651F60D805017CA70BC8222CACA939548D90A785C9B7124C0D165A
EarthWorks.Geometry.dll 02C8B477388C3168E62B48A3F975CB9ADC409CA8C231F226505C52969EC3A1FB
EarthWorks-0.6.3.zip    5282AB51C129B9B5DD1AB3B9A4C37354E97CCF9102D6CC416352E43324A4706B
```
