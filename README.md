# Oracle → ClickHouse Migrator

Консольный инструмент, который:
1. Читает структуру Oracle-таблиц (ALL_TAB_COLUMNS, PK).
2. Создаёт зеркальные ReplicatedMergeTree + Distributed в ClickHouse-кластере.
3. Потоково переносит данные (bulk-insert, курсоры, без загрузки в RAM).

## Быстрый старт
```bash
git clone https://github.com/your/repo oracle-to-ch-migrator
cd oracle-to-ch-migrator

# 1. правим examples/config.yaml (строки подключения)
# 2. создаём таблицы
dotnet run --project src/Migrator.Cli -- create all -c examples/config.yaml

# 3. переносим данные
dotnet run --project src/Migrator.Cli -- migrate all -c examples/config.yaml
