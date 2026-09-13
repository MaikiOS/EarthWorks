# EarthWorks 0.6.2 — совместимость с Valheim 1.0

EarthWorks 0.6.2 — patch-релиз для Valheim 1.0.7, Steam build 25185596, network version 39 и Unity 6000.0.75f1.

## Что исправлено

Valheim 1.0 добавил в интерфейс `Hoverable` метод `float GetHoverOffset()`. Из-за отсутствия метода в `RoadProjectBoard` версия 0.6.1 не могла построить vtable класса и многократно писала `TypeLoadException` в `Player.log`.

В 0.6.2 проектная доска реализует новый метод и сохраняет значение `m_hoverOffset` клонированного ванильного `Sign`. Дополнительный API-аудит выявил ещё два изменения Valheim 1.0:

- `Heightmap.Poke(int delayed, bool paintOnly)` вместо старого вызова с одним `bool`;
- `TerrainComp.Save(bool)` вместо reflection-вызова без аргументов.

Все места обновлены под текущие сигнатуры.

## Основные возможности

- многоточечные дороги с прямыми, X-Spline, B-Spline, Bezier и Corner-сегментами;
- единый редактор положения, высоты и независимой ширины сторон;
- четыре продольных профиля и endpoint terrain-plane fitting;
- земляное и мощёное покрытие, включая настройки отдельных сегментов;
- Current/Result/Difference preview на реальной terrain-сетке;
- постоянная сетевая проектная доска и шесть стадий выполнения;
- Plan и Isometric камеры редактора;
- русская и английская локализация.

## Проверено перед выпуском

- полная Release-сборка: 0 ошибок, 0 предупреждений;
- geometry tests: 24/24 PASS;
- API audit: PASS для интерфейсов, Harmony targets, прямых и reflection-контрактов;
- зависимости сборки: BepInEx 5.4.23.5 из BepInExPack 5.4.2350 и Jotunn 2.29.2.0;
- состав Thunderstore ZIP и SHA-256 обеих DLL проверены после распаковки;
- независимый read-only review: PASSED.

## Важный статус

Valheim не запускался при подготовке этого релиза. Полный игровой acceptance — выполнение всех стадий, reload/restart, второй клиент и survival-проверка — остаётся отдельным следующим шагом.

## SHA-256

```text
EarthWorks.dll          CBFBA2A4C679E28DCE1B8D846445FAF1FD2B863A9E7429EC11F2E693D7F98F21
EarthWorks.Geometry.dll B396C0100C004ADA764AF7F7D1825E469DB8FC3A4BFEFA3393BDBD04CCCE7668
EarthWorks-0.6.2.zip    9A779215514AEE22562B6663EE3EB8F6218D23FC0A71BF3D2FACD63E49D30E21
```

Полный план развития: [ROADMAP_RU.md](../ROADMAP_RU.md).
