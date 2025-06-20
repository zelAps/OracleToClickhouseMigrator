# Oracle → ClickHouse Migrator  

## 📦 Содержимое репозитория

| Каталог / файл                        | Назначение                                               |
|---------------------------------------|----------------------------------------------------------|
| `src/Migrator.Cli`                    | Консольное приложение (`dotnet run …`)                   |
| `src/Migrator.Core`                   | Бизнес-логика: чтение схем Oracle, генерация DDL и т.д.  |
| `src/Migrator.DataPump`               | Потоковая передача данных «Oracle → ClickHouse»          |
| `src/ZstdHttpClient`                  | Обёртка, отключающая zstd-сжатие у Http-драйвера CH      |
| `examples/`                           | Готовые YAML-конфиги                                     |
| `logs/migrator-YYYY-MM-DD.log`        | Рота­ци­онные логи Serilog                                 |
| `README.md`                           | Этот файл                                                |

---

## 🚀 Что умеет мигратор

- ✅ **Создание таблиц** в ClickHouse: локальных и распределённых  
- ✅ Автоматическое сопоставление типов Oracle → ClickHouse  
- ✅ Конфиг‑драйв: `source`, `target`, `owner`, `rename_fields`, `partition_expr`, `shard_key`, `where`  

- ✅ **Копирование данных** из Oracle курсором, запись батчами  
- ✅ Поддержка кластеров с `ReplicatedMergeTree` + `Distributed`  
- ✅ Подробный лог выполнения (Serilog)  
- ✅ CLI-интерфейс в стиле `create tables`, `migrate`, `--dry-run`, `--verbose`

---

## ⚙️ Минимальные требования

| Компонент                  | Версия / примечание                          |
|----------------------------|----------------------------------------------|
| .NET SDK                   | 8.0                                          |
| Oracle DB + клиент         | Oracle.ManagedDataAccess                     |
| ClickHouse cluster         | ≥ 22.3                                       |
| ZooKeeper / Keeper         | встроенный или внешний                       |

---

## 📝 Пример YAML-конфига (с переименованием)

```yaml
oracle:
  connection_string: >
    User Id=NDS2_MRR_USER;Password=NDS2_MRR_USER;
    Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)
      (HOST=host)(PORT=port))
      (CONNECT_DATA=(SERVICE_NAME=NDS2)))

click_house:
  connection_string: >
    Host=host;Port=port;Protocol=http;
    User=default;Password=password;Database=databaseName
  cluster: default
  zk_path_prefix: /clickhouse/tables
  replicas:
    - clickhouse-shard0-0
    - clickhouse-shard0-1
    - clickhouse-shard1-0
    - clickhouse-shard1-1

tables:
  - source: DECLARATION_ACTIVE
    target: DECLARATION_ACTIVE_NEW
    owner:  NDS2_MRR_USER
    rename_fields:
      DECLARATIONID: ID
      INN_DECLARANT: INN_TAXPAYER
      REGION_CODE:   REGION_ID
      DECL_DATE:     DECLARATION_DT
      STATUS_KNP_CODE: STATUS_CODE
````

---

## 🔧 Сборка и запуск

```bash
# Сборка
dotnet build

# Создание таблиц (локальных и распределённых)
dotnet run --no-launch-profile \
  --project src/Migrator.Cli -- \
  create tables DECLARATION_ACTIVE DECLARATION_ACTIVE_NEW \
  -c examples/conf-rename-demo.yaml

# Копирование данных
dotnet run --no-launch-profile \
  --project src/Migrator.Cli -- \
  migrate tables DECLARATION_ACTIVE DECLARATION_ACTIVE_NEW \
  -c examples/conf-rename-demo.yaml -v
```

---

## ✅ Проверка результата

```sql
-- Кол-во строк по всем шардам:
SELECT hostName(), count()
FROM clusterAllReplicas('default', 'migration_test', 'DECLARATION_ACTIVE_local')
GROUP BY host;

-- Очередь репликации должна быть пуста:
SELECT hostName(), queue_size
FROM system.replicas
WHERE database = 'migration_test'
  AND table = 'DECLARATION_ACTIVE_local';
```

---

## 🎛 CLI-опции

| Команда              | Описание                               |
| -------------------- | -------------------------------------- |
| `create tables A B`  | Создать таблицы A и B                  |
| `create all`         | Создать все таблицы из YAML            |
| `migrate tables A B` | Перенос данных только для A и B        |
| `migrate all`        | Перенос данных всех таблиц             |
| `--dry-run`          | Показывает DDL без выполнения          |
| `-v / --verbose`     | Подробный лог                          |
| `-c`                 | Путь к YAML (по умолчанию `conf.yaml`) |

Также можно переопределить параметры окружением:

```bash
ORA_CS="…" CH_CS="…" dotnet run …
```

---

## 🔥 Частые ошибки

| Ошибка                                         | Причина и решение                         |
| ---------------------------------------------- | ----------------------------------------- |
| ORA-12541                                      | Неверный `HOST` или listener недоступен   |
| Table default.… does not exist                 | Нет базы `Database=…` в строке ClickHouse |
| log\_pointer doesn’t exist / KEEPER\_EXCEPTION | Неправильный ZK path — пересоздать DDL   |

---

## 🧪 Разработка и тесты

```bash
# Запуск unit-тестов
dotnet test

# Проверка code style
dotnet format
```

