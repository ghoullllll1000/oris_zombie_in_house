# Семестровая работа по ОРИС (1-й семестр). Wiki по игре "Зомби в доме"
## Подготовка окружения
Перед сборкой и запуском Docker-контейнера с базой данных postgres и основным проектом рекомендуется создать файл *.env* в корневой директории (рядом с docker-compose.yaml)
Ниже приведён шаблон для данного файла:
```
ZOMBIE_PORT=<port>
ZOMBIE_HOST=<host> # если запускаете в docker, укажите host="+"

ZOMBIE_DB_USER=<postgres_user>
ZOMBIE_DB_PASSWD=<password>
ZOMBIE_DB_NAME=<db_name>
ZOMBIE_DB_PORT=<db_port>
ZOMBIE_DB_HOST=<db_host>

ZOMBIE_DB_CONNECTION_STRING="Host=${ZOMBIE_DB_HOST};Port=${ZOMBIE_DB_PORT};Database=${ZOMBIE_DB_NAME};Username=${ZOMBIE_DB_USER};Password=${ZOMBIE_DB_PASSWD}"
```

## Сборка и запуск проекта
В корневой директории данного проекта выполните следующую команду:
```shell
docker compose up -d --build
```

Откройте сайт по ссылке: http://localhost:9999