# Антиплагиат (микросервисное  приложение)
Учебный проект по КПО: система проверки текстовых работ (.txt) на плагиат.
Архитектура - три микросервиса (.NET 8 + SQLite) и Docker Compose.

# Общая идея проекта

Проект моделирует поведение простой «антиплагиатной» системы.  
Студент отправляет текстовый файл → система сохраняет его → анализирует → возвращает отчёт о возможном плагиате и визуализацию текста.

Архитектура построена на **трёх независимых микросервисах**, взаимодействующих по HTTP: Client -> Gateway -> FileStoring -> FileAnalysis

Каждый сервис запускается в своём контейнере Docker.

---

# Архитектура микросервисов

## FileStoring (порт 5001)

**Ответственность:**

- Приём загружаемых файлов (`multipart/form-data`)
- Проверка расширения (.txt)
- Сохранение файлов в том (`/data`)
- Создание записи в SQLite:
  - `fileId`
  - `workId`
  - `studentId`
  - `uploadedAt`
- Возврат метаданных клиенту
- Предоставление содержимого файла по `fileId` (для FileAnalysis)

**Swagger:**  
http://localhost:5001/swagger

---

## FileAnalysis (порт 5002)

**Ответственность:**

- Получение текста работы по `fileId` из FileStoring
- Нормализация текста:
  - нижний регистр  
  - удаление пунктуации  
  - свёртка пробелов
- Вычисление SHA-256 хеша нормализованного текста
- Определение плагиата:
  - работа считается плагиатом, если **другой студент** отправил **работу того же задания** с **таким же хешем**
- Сохранение отчёта (`Report`) в SQLite:
  - `reportId`, `workId`, `studentId`, `fileId`
  - `contentHash`
  - `plagiarism = true/false`
  - `chartUrl` — ссылка на облако слов (QuickChart API)
  - `createdAt`
- Возврат отчётов по `workId` и `reportId`

**Swagger:**  
http://localhost:5002/swagger

---

## Gateway (порт 5000 → наружу 8080)

Gateway — единственная внешняя точка входа для клиента.  
Он управляет всей цепочкой:

### При загрузке работы (`POST /api/submit`):

1. Получает файл + имя студента + название задания
2. Отправляет файл в FileStoring → получает `fileId`, `workId`, `studentId`
3. Отправляет эти данные в FileAnalysis → получает `reportId`
4. Возвращает клиенту итоговый отчёт:

```json
{
  "reportId": "...",
  "workId": "...",
  "studentId": "...",
  "fileId": "...",
  "plagiarism": false,
  "contentHash": "...",
  "chartUrl": "...",
  "createdAt": "..."
}
```
**Дополнительные методы:**
- `GET /api/works/{workId}/reports`
 Список отчётов по заданию
- `GET /api/reports/{reportId}/wordcloud`
Возврат URL облака слов

**Swagger:**
http://localhost:8080/swagger



# Структура репозитория

```
AntiPlagiarismSolution/
│
├── Gateway/
│   ├── Program.cs
│   ├── Services/
│   ├── Dockerfile
│
├── FileStoring/
│   ├── Program.cs
│   ├── Services/, Repositories/, Domain/
│   ├── Dockerfile
│
├── FileAnalysis/
│   ├── Program.cs
│   ├── Services/
│   ├── Dockerfile
│
├── docker-compose.yml
├── .gitignore
└── README.md
```

# Используемые технологии
- C# / .NET 8
- ASP.NET Minimal API
- SQLite
- Docker + Docker Compose
- HttpClient (коммуникация микросервисов)
- Swagger / OpenAPI
- QuickChart API (генерация облака слов)

# Алгоритм проверки плагиата
FileAnalysis использует простой алгоритм обнаружения дубликатов.

**Шаг 1** — нормализация текста

Удаляются различия форматирования: все символы → нижний регистр

удаление:
  - `.,!?;:()[]{}…`
  - табов, CRLF, повторяющихся пробелов
приводит разные представления одного текста к идентичному виду

**Шаг 2** — хеширование

Используется SHA-256:
```ini contentHash = SHA256(normalizedText) ```

**Шаг 3** — определение плагиата

Работа считается плагиатом, если:
  - workId совпадает (то же задание),
  - hash совпадает,
  - studentId НЕ совпадает.

То есть если два разных студента отправили одинаковый текст, то одна из работ будет помечена как plagiarism = true.

# Запуск в Docker

## Конфигурация `docker-compose.yml`:

```yaml
services:
  filestoring:
    ports:
      - "5001:5001"
  fileanalysis:
    ports:
      - "5002:5002"
  gateway:
    ports:
      - "8080:5000"
```
## Сборка

```yaml
docker compose build
```
## Запуск

```yaml
docker compose up
```
## Остановка

```yaml
docker compose down
```

## Доступ к сервисам

| Сервис       | URL                                                            |
| ------------ | -------------------------------------------------------------- |
| Gateway      | [http://localhost:8080/swagger](http://localhost:8080/swagger) |
| FileStoring  | [http://localhost:5001/swagger](http://localhost:5001/swagger) |
| FileAnalysis | [http://localhost:5002/swagger](http://localhost:5002/swagger) |

**!Рекомендуется работать только с Gateway, т.к. FileStoring и FileAnalysis для отладки**


# Пример загрузки работы

Работу можно загружать как и через интерфейс swagger, так и через curl.

**Пример загрузки работы через curl:**

```bash
curl -X POST http://localhost:8080/api/submit \
  -F "file=@test.txt" \
  -F "studentName=Мария" \
  -F "assignmentName=ДЗ1"
```

# Обработка ошибок
Gateway перехватывает и нормализует ошибки:
  - недоступность микросервиса → `503 Service Unavailable`
  - неверный формат файла → `400 BadRequest`
  - внутренние ошибки файлового или аналитического сервиса не “вытекают” наружу


