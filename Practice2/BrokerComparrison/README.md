# Broker Comparison Practice

Консольный проект для сравнения `RabbitMQ` и `Redis` как брокеров сообщений.

## Быстрый запуск

```powershell
./Start-Brokers.ps1
./Run-Benchmarks.ps1
./Stop-Brokers.ps1
```

После прогона результаты сохраняются в папку `results`:

- `results.json`
- `results.csv`
- `summary.md`

## Что проверяет проект

- базовое сравнение на одинаковой нагрузке;
- влияние размера сообщения;
- влияние интенсивности потока;
- наличие деградации по backlog, потерям и ошибкам.

## Настройка

Основные сценарии лежат в `appsettings.json`, секция `Benchmark -> Scenarios`.

Если нужно менять путь для вывода результатов:

```powershell
dotnet run -- --output=custom-results
```
