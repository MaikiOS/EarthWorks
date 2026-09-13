# Changelog

## 0.6.2

- Добавлен новый контракт Valheim 1.0 `Hoverable.GetHoverOffset()` для `RoadProjectBoard`.
- Сохранён hover offset исходной ванильной таблички при замене компонента `Sign` на проектную доску EarthWorks.
- Вызовы немедленного обновления Heightmap переведены на сигнатуру `Poke(int delayed, bool paintOnly)`.
- Reflection-вызовы сохранения terrain переведены на `TerrainComp.Save(bool)` с точным выбором overload.
- Проведён аудит используемых интерфейсов, Harmony targets, прямых и reflection-вызовов Valheim 1.0.7.
- Сборка выполнена против Valheim 1.0.7 build 25185596, BepInExPack 5.4.2350 и Jotunn 2.29.2.
- Версии `EarthWorks.dll` и `EarthWorks.Geometry.dll` обновлены до 0.6.2.0.
- Логика terrain, состояния проекта, редактора и этапов строительства намеренно не менялась.

## 0.6.1

- Геометрия маршрута, высота и ширина объединены в один редактор прямого управления.
- Добавлены X-Spline, B-Spline, Bezier и Corner.
- Добавлены независимая ширина сторон, четыре продольных профиля, endpoint fitting, точный preview и лабораторное выполнение проекта по этапам.

