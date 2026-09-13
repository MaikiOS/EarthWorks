# EarthWorks 0.6.5

EarthWorks plans persistent Valheim terrain roads with multi-point curves, exact elevation and independent width control, Current/Result/Difference previews, and a six-stage networked project board.

Compatibility target: Valheim 1.0.12, Steam build 25253764, network version 40, Unity 6000.0.75f1, BepInExPack 5.4.2350, and Jotunn 2.30.0.

This patch removes the obsolete half-cell paint offset, preserves special paint-mask alpha, adds paint-grid seam/corner regression coverage, and limits grass refresh to the sampled road corridor. In-game seam/corner, save/reload, and ATMC coexistence acceptance remains pending.

Install `EarthWorks.dll` and `EarthWorks.Geometry.dll` together under `BepInEx/plugins`. EarthWorks must be present on the server and every participating client.

Source, documentation, roadmap, contribution guide, and permission contact: https://github.com/MaikiOS/EarthWorks

## Русский

EarthWorks создаёт постоянные проекты дорог Valheim: многоточечные кривые, точная высота, независимая ширина сторон, preview Current/Result/Difference и сетевая доска с шестью стадиями.

Целевая совместимость: Valheim 1.0.12, Steam build 25253764, network version 40, Unity 6000.0.75f1, BepInExPack 5.4.2350 и Jotunn 2.30.0.

В этом patch удалён устаревший paint offset на полклетки, сохраняется alpha специальных mask-данных, добавлены paint-grid тесты seams/углов, а обновление травы ограничено коридором дороги. Игровая проверка seams/углов, save/reload и совместимости с ATMC ещё впереди.

Установи `EarthWorks.dll` и `EarthWorks.Geometry.dll` вместе в `BepInEx/plugins`. Мод требуется на сервере и у всех участвующих клиентов.
