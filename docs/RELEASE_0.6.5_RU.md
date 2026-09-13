# EarthWorks 0.6.5 — штатная paint-grid и обновление коридора

[English (primary)](RELEASE_0.6.5.md)

EarthWorks 0.6.5 — patch-релиз для Valheim 1.0.12 (Steam build 25253764, network version 40, Unity 6000.0.75f1), BepInExPack 5.4.2350 и Jotunn 2.30.0.

## Изменения

- Paint использует точные штатные terrain-grid координаты, уже сохранённые planner; устаревший offset на полклетки удалён.
- Dirt и paving сохраняют текущий alpha paint-mask, используемый специальным terrain, включая lava.
- Общий индекс paint проверяется с обеих сторон zone seam, во всех углах mask 65×65 и на внешних координатах.
- Обновление травы идёт по sampled-позициям и локальной ширине дороги вместо всего bounding-circle маршрута.
- Двуязычные roadmap и contribution guide теперь содержат конкретные задачи для игроков, владельцев серверов, переводчиков, UI/C# contributors и других авторов модов.

Точный preview paint-core/bilinear feather ещё не реализован. Игровые проверки двух/четырёх зон, alpha в Ashlands, save/reload, второго клиента и совместимости с ATMC также впереди.

## Проверено без запуска Valheim

- Release solution build: 0 warnings, 0 errors.
- Geometry и paint-grid executable: 26/26 PASS.
- Persistence/localization executable: 5/5 PASS.
- Localization audit: 227/227 EN/RU ключей, placeholders, literal references, dynamic state/stage и отсутствие смешанного языка.
- Valheim API audit: PASS для direct, interface, Harmony, reflection, native paint-coordinate, alpha-preservation и corridor-refresh контрактов.
- Структура ZIP проверена: только две DLL, icon, manifest, README, changelog и license.

## Проверенные артефакты и установка

```text
EarthWorks.dll           7797C10D98E124E45883A86FD9322E0476CEEA15E502CEC3FCB69EBFAE3BAFD5
EarthWorks.Geometry.dll  74BF59B52BC25486AA7659A6988C18C4A9044BC728947B159A46155A295C3D6A
EarthWorks-0.6.5.zip      EE754DC0CB1B98991D0781448626D059FD38FDAB5B1D70DDAB2D1906B5DCDDAC
```

При закрытом Valheim обе DLL 0.6.5 установлены штатным guarded-скриптом в `TerrainRamp-1.0-Test\BepInEx\plugins\Ostrix-EarthWorks`. Установленные версии — 0.6.5.0, hashes совпадают с package. До и после установки проверены hashes всех 86 файлов профиля вне EarthWorks — ни один не изменился.

## Лицензия

EarthWorks остаётся proprietary source-available software. Официальные неизменённые бинарники разрешены для личной некоммерческой игры. Fork разрешён для подготовки Pull Request. Использование кода, распространение изменённых бинарников и коммерческое использование требуют предварительного письменного разрешения Ostrix; см. `LICENSE.md`.
