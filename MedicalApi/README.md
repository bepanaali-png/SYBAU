# MedicalApi

Бэкенд частной клиники: пациенты, справки, каталог видов справок, записи на приём, статистика.
Данные хранятся в памяти: после перезапуска сервера всё возвращается к начальному состоянию.

## Запуск

Нужен .NET 8 SDK (`dotnet --version` должен показать 8.x).

```
dotnet run
```

Откройте адрес из консоли (`Now listening on: ...`) и добавьте `/swagger/index.html`.
По умолчанию это http://localhost:5080/swagger/index.html

После запуска уже есть тестовые данные (5 пациентов, справки, приёмы).
Отключить: `"SeedTestData": false` в `appsettings.json`.

## Эндпоинты

| Группа | Запросы |
| --- | --- |
| Patients | `GET/POST /api/patients`, `GET/PUT/DELETE /api/patients/{id}`, `GET /api/patients/{id}/spravki`, `GET /api/patients/{id}/appointments` |
| Spravki | `GET/POST /api/spravki`, `GET/PUT/DELETE /api/spravki/{id}` |
| SpravkaTypes | `GET/POST /api/SpravkaTypes`, `GET/PUT/DELETE /api/SpravkaTypes/{id}` |
| Appointments (свой модуль) | `GET/POST /api/appointments`, `GET/PUT/DELETE /api/appointments/{id}` |
| Stats (своё улучшение) | `GET /api/stats` |

## Решения, которые приняты

**Справки**
- В теле POST нужны только `patientId` и `spravkaTypeId`. Название и цена берутся из каталога, присланные `name` и `price` игнорируются.
- Цена и название копируются в справку при выписке: если цену в каталоге потом поменяют, старые справки не меняются.
- Сервер сам ставит `issuedAt` и `validUntil` (из срока действия вида справки).
- Справка для детей не выписывается взрослому, и наоборот (`audience` вида справки: `all`, `adults`, `children`).
- Поиск и сортировка: `GET /api/spravki?patientId=3&spravkaTypeId=2&sortBy=price&desc=true` (`sortBy`: `price`, `date`, `name`).

**Пациенты**
- Дубликат (то же ФИО без учёта регистра и та же дата рождения) не создаётся: 400.
- Поиск по части имени: `GET /api/patients?name=али`.
- Удаление пациента, у которого есть справки или приёмы: 409 Conflict. Удалить вместе со всем: `DELETE /api/patients/{id}?cascade=true`.

**Каталог видов справок**
- Вид, по которому уже выписаны справки, удалить нельзя: 409.
- Названия видов уникальны.

**Записи на приём**
- Приём только в будущем, не дальше чем на год вперёд, пн-сб с 08:00 до 20:00.
- Врач не может принимать двоих одновременно, пациент не может быть у двух врачей одновременно: 409 Conflict. Отменённые приёмы время не занимают.
- Новая запись всегда `scheduled`. Отмена и перенос через `PUT` (статус `cancelled`, новое `startTime`). Состоявшийся приём (`completed`) менять нельзя.
- Фильтры: `patientId`, `doctor`, `date`, `status`.

**Статистика**
- `GET /api/stats` показывает число пациентов, справок, общую выручку, разбивку по видам справок и число будущих приёмов.
- За период: `GET /api/stats?dateFrom=2026-11-01&dateTo=2026-11-30`.

## Коды ответов

| Код | Когда |
| --- | --- |
| 200 | успешно (GET, PUT) |
| 201 | создано (POST) |
| 204 | удалено (DELETE), тело пустое |
| 400 | неверные данные, в теле текст для человека |
| 404 | записи с таким id нет |
| 409 | данные верные, но действие невозможно (занято время, есть связанные записи) |

## Баги (задание 8)

1. **Новая справка открывается как чужая.** Причина: `Id = Count + 1`. После удаления записи из середины `Count + 1` совпадает с уже существующим Id. Исправлено счётчиком `NextId++`, который не уменьшается. Тот же счётчик стоит у пациентов, видов справок и приёмов.
2. **Swagger показывал не те коды.** Добавлены атрибуты `[ProducesResponseType]` с настоящими кодами (201, 204, 400, 404, 409) на каждый метод.

## Структура

```
Controllers/   PatientsController, SpravkiController, SpravkaTypesController, AppointmentsController, StatsController
Models/        Patient, Spravka, SpravkaType, Appointment, ClinicStats
SeedData.cs    тестовые данные
Program.cs     настройка сервера и Swagger
```
