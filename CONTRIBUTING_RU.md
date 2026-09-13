# Участие в разработке EarthWorks

[English (primary)](CONTRIBUTING.md)

Pull Request приветствуются для точечных исправлений, обновлений совместимости Valheim, тестов, документации, локализации и согласованных пунктов roadmap. EarthWorks является source-available, а не open-source проектом; перед использованием или распространением кода прочитай [LICENSE.md](LICENSE.md).

## Перед изменением

1. Прочитай [карту архитектуры](docs/ARCHITECTURE_RU.md), [контракт продукта](PROJECT_CONTRACT_RU.md) и [протокол тестирования](TESTING_RU.md).
2. Перед крупной функцией создай Issue и согласуй поведение и границы.
3. Один Pull Request должен содержать одно изменение поведения или один механический рефакторинг.

## Локальная настройка

Valheim, BepInEx и Jotunn DLL нельзя распространять через репозиторий. Скопируй `Directory.Build.props.user.example` в `Directory.Build.props.user` и укажи свои пути либо задай `EARTHWORKS_PROFILE_ROOT` и `VALHEIM_MANAGED_DIR`.

Требуемые версии указаны в начале `README.md`.

```powershell
dotnet build .\EarthWorks.sln -c Release
dotnet run --project .\tests\EarthWorks.GeometryTests\EarthWorks.GeometryTests.csproj -c Release
dotnet run --project .\tests\EarthWorks.Tests\EarthWorks.Tests.csproj -c Release
.\scripts\Audit-Localization.ps1
.\scripts\Audit-ValheimApi.ps1
```

Автоматические тесты не должны запускать Valheim или устанавливать файлы в профиль. Runtime-проверка выполняется отдельно и только явно.

## Имена и совместимость

- Используй `Road...` для текущего домена дорожного проекта и `EarthWorks...` только для общих сервисов плагина.
- Имя файла должно отражать его главный тип или ответственность.
- Unity/Jotunn код не должен попадать в `EarthWorks.Geometry`.
- Нельзя менять номера сохраняемых enum. Новые значения добавляются в конец и закрепляются persistence-тестом.
- Версия `RoadProjectRecord` повышается только при изменении бинарной структуры с сохранением чтения поддерживаемых старых версий.
- Пользовательский текст добавляется в оба JSON внутри `Translations/EarthWorks`; русские строки в C# запрещены.
- Комментарии объясняют инварианты, причины совместимости и неочевидные ограничения, а не пересказывают код.

## Доказательства в Pull Request

Опиши первопричину, затронутый call path, влияние на совместимость и точные выполненные проверки. Сборка доказывает только компиляцию. Поведение в игре, multiplayer, save/reload и внешний вид остаются непроверенными до выполнения `TESTING_RU.md`.

Отправляя Pull Request, ты принимаешь условия участия из [LICENSE.md](LICENSE.md). Fork GitHub разрешён для подготовки Pull Request, но лицензия не разрешает использовать код EarthWorks в другом проекте без письменного согласия Ostrix.
