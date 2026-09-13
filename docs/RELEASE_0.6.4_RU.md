# EarthWorks 0.6.4 — authority, persistence, локализация и поддерживаемость

[English (primary)](RELEASE_0.6.4.md)

EarthWorks 0.6.4 — patch-релиз для Valheim 1.0.12 (Steam build 25253764, network version 40, Unity 6000.0.75f1), BepInExPack 5.4.2350 и Jotunn 2.30.0.

## Поведение для игрока

Существующий Route workflow сохранён: многоточечные прямые и кривые маршруты, точная и автоматическая высота, независимая ширина сторон, четыре продольных профиля, preview Current/Result/Difference, покрытия отдельных сегментов и постоянная проектная доска с шестью стадиями.

Запросы стадий доски теперь авторизуются по фактическому RPC-отправителю. Сервер/владелец проверяет дистанцию, лабораторное ограничение, владельца project piece и все активные wards над доской до выполнения этапа.

Английский и русский остаются встроенными в `EarthWorks.dll`. Переводчики могут переопределить строки файлами `Translations/EarthWorks/English/translations.json` и `Translations/EarthWorks/Russian/translations.json`; повреждённый внешний файл игнорируется без потери встроенного fallback.

## Совместимость и качество кода

- Повторно проверены `Hoverable.GetHoverOffset()`, ванильный `Sign.m_hoverOffset`, Harmony targets и все прямые/reflection контракты Valheim.
- Сохранены стабильные ID сериализуемых enum и чтение проектов v1-v4.
- Добавлена проверка отказа на повреждённых, обрезанных и слишком больших данных.
- Удалён недостижимый старый staged-editor path и его неиспользуемая локализация.
- Editor view, direct manipulation, camera grid/patches, создание проекта, persistence и настройки planner разделены по именованным модулям.
- Добавлены `EarthWorks.sln`, deterministic build, локальные overrides путей для обычного clone, portable CI и двуязычные документы по архитектуре/contribution.

## Проверено без запуска Valheim

- Release solution build: 0 warnings, 0 errors.
- Geometry regression executable: 24/24 PASS.
- Persistence/localization executable: 5/5 PASS.
- Localization audit: 227/227 EN/RU ключей, placeholders, literal references, dynamic state/stage и отсутствие смешанного языка.
- Valheim API audit: PASS для всех прямых, interface, Harmony и reflection контрактов.

Полный runtime acceptance в single-player, после reload/reconnect и в multiplayer ещё не выполнен. При подготовке релиза игра не запускалась.

## Лицензия

EarthWorks остаётся proprietary source-available software. Официальные неизменённые бинарники разрешены для личной некоммерческой игры. Fork разрешён для подготовки Pull Request. Использование кода, распространение изменённых бинарников и коммерческое использование требуют предварительного письменного разрешения Ostrix; см. `LICENSE.md`.
