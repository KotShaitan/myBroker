# Отчет по финальному этапу (кратко)

## 1) Что реализовано по критериям и где

1. HTTP API брокера (ASP.NET Core Web API)
- Файл: `Program.cs`
- Как применено: поднят Web API, подключены контроллеры, DI, Swagger.
- Файл: `Controllers/BrokerController.cs`
- Как применено: реализованы endpoints `CreateTopic`, `EditTopic`, `DeleteTopic`, `Subscribe`, `Publish`, `Consume`, `Ack`, `Dlq`.

2. Управление топиками и очередями
- Файл: `Services/TopicService.cs`
- Как применено: создание топика с N очередями, поиск по id/имени, редактирование количества очередей, удаление топика, проверка уникальности имени.
- Файлы: `Models/Topic.cs`, `Models/Queue.cs`
- Как применено: доменная модель топика и очередей сообщений.

3. Pub/Sub (подписки consumer-ов)
- Файл: `Services/SubscribeService.cs`
- Как применено: подписка consumer на topic по имени, проверка подписки, загрузка/сохранение подписок.
- Файл: `Models/Subscription.cs`
- Как применено: модель подписки `TopicID + ConsumerID`.

4. Publish/Consume доставка сообщений
- Файл: `Services/DeliveryService.cs`
- Как применено:
  - `PublishAsync`: round-robin распределение сообщений по очередям топика.
  - `Consume`: выдача сообщений только подписанным consumer-ам.
  - хранение offset-ов чтения отдельно для каждого consumer и каждой очереди.
- Файл: `Models/Message.cs`
- Как применено: модель сообщения (id, topic, publisher, payload, time, attempts).

5. Персистентность в JSON
- Файл: `Services/FileManager.cs`
- Как применено: сохранение/загрузка `topics.json` и `subscriptions.json` через `System.Text.Json`.
- Файлы: `Data/topics.json`, `Data/subscriptions.json`
- Как применено: файловое хранилище состояния брокера.

6. Инициализация состояния при старте
- Файл: `Program.cs`
- Как применено: вызовы `InitAsync()` для загрузки топиков и подписок до начала обработки запросов.

7. Документация API (Swagger)
- Файл: `Program.cs`
- Как применено: `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI` в dev-окружении.

8. Контейнеризация и демо-среда
- Файлы: `Dockerfile`, `docker-compose.yml`
- Как применено: контейнеризация брокера и запуск через docker compose.
- Файл: `docker-compose.demo.yml`
- Как применено: поднимается демо-среда с 3 тестовыми микросервисами.
- Файл: `STAGE4_DEMO.md`
- Как применено: инструкция запуска и проверки демо.

9. Тестовая среда (3 примитивных микросервиса с логами)
- Файлы: `DemoServices/ProducerService/Program.cs`, `DemoServices/ConsumerServiceA/Program.cs`, `DemoServices/ConsumerServiceB/Program.cs`
- Как применено:
  - Producer создает топик и публикует сообщения.
  - Consumer A/B подписываются и потребляют сообщения.
  - В stdout пишутся подробные логи событий.

10. SDK для внешних проектов
- Файлы: `MyBroker.Sdk/BrokerApiClient.cs`, `MyBroker.Sdk/IBrokerApiClient.cs`, `MyBroker.Sdk/Contracts/*`
- Как применено: вынесен клиент для работы с API брокера (topic/subscribe/publish/consume/ack/dlq) из других .NET-проектов.

## 2) Дополнительный функционал

1. ACK
- Файлы: `Services/DeliveryService.cs`, `Controllers/BrokerController.cs`, `Models/Requests.cs`
- Как применено: после `Consume` consumer подтверждает обработку через `Ack`; при успешном ACK смещается offset.

2. Dead Letter Queue (DLQ)
- Файлы: `Services/DeliveryService.cs`, `Controllers/BrokerController.cs`
- Как применено: при неуспешном ACK/таймауте увеличивается число попыток; после 3 попыток сообщение уходит в DLQ, доступно через endpoint `Dlq`.
