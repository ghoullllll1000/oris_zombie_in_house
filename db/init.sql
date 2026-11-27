CREATE TABLE IF NOT EXISTS characters (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    health INT NOT NULL,
    feature TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS monsters (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(100) NOT NULL,
    behavior TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS weapons (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    category VARCHAR(100) NOT NULL,
    effect TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS items (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    usage VARCHAR(100) NOT NULL,
    effect TEXT NOT NULL
);

INSERT INTO characters (name, health, feature) VALUES
    ('Саша', 3, 'Увлекается боевыми искусствами: у него всегда есть с собой нож (можно использовать его в любой момент, как если бы у Саши была такая карточка оружия).'),
    ('Настя', 3, 'Работает медсестрой: использованная ею аптечка даёт не одну жизнь, а сразу две.'),
    ('Макс', 3, 'Самый младший игрок, зато самый быстрый: к каждому своему ходу при передвижении может (при желании) добавлять +1.'),
    ('Надя', 3, 'Работает в Полиции: в начале игры у неё с собой есть пистолет (можно использовать его в любой момент, как если бы у Нади была такая карточка оружия).'),
    ('Борис', 5, 'Невероятно здоровый и выносливый: в начале игры у него на две жизни больше, чем у остальных.');

INSERT INTO monsters (name, type, behavior) VALUES
    ('Зомби', 'Обычный', 'Нападает на игрока и кусает, если вертушка показывает — зубы.'),
    ('Дьявольские псы', 'Обычный', 'Нападает на игрока и кусает, если вертушка показывает — зубы.'),
    ('Пауки-мутанты', 'Обычный', 'Нападает на игрока и кусает, если вертушка показывает — зубы.'),
    ('Болотный ужас', 'Босс', 'Нападает на игрока и кусает, если вертушка показывает — зубы. Это босс. Его можно убить только с помощью гранатомёта.');

INSERT INTO weapons (name, category, effect) VALUES
    ('Нож', 'Холодное оружие', 'Может убить монстра, если вертушка показывает перекрещенные шпаги.'),
    ('Арбалет', 'Холодное оружие', 'Может убить монстра, если вертушка показывает перекрещенные шпаги.'),
    ('Топор', 'Холодное оружие', 'Может убить монстра, если вертушка показывает перекрещенные шпаги.'),
    ('Пистолет', 'Огнестрельное оружие', 'Может убить одного монстра, если вертушка показывает прицел.'),
    ('Автомат', 'Огнестрельное оружие', 'Может убить одного монстра, если вертушка показывает прицел.'),
    ('Дробовик', 'Огнестрельное оружие', 'Может убить одного монстра, если вертушка показывает прицел.'),
    ('Граната', 'Взрывчатка', 'Может сразу взорвать любого монстра, кроме босса — Болотного ужаса.'),
    ('Гранатомёт', 'Специальное оружие', 'Единственный способ избавиться от босса — Болотного ужаса. Нельзя использовать на других врагов.');

INSERT INTO items (name, usage, effect) VALUES
    ('Аптечка', 'Здоровье', 'Восполняет одну жизнь (а при применении Настей — сразу две). Можно увеличить жизни сверх начального количества.'),
    ('Доски', 'Защита проёмов', 'С их помощью можно забить окно или дверь, положив карточку досок на клетку с изображением двери или окна.'),
    ('Канистра с бензином', 'Машина', 'Нужна, чтобы заправить и завести красную машину для победы.'),
    ('Ключ', 'Машина', 'Нужен, чтобы завести красную машину для победы.');

CREATE TABLE IF NOT EXISTS orm_test_entities (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    age INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS orm_test_characters (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS orm_test_children (
    id SERIAL PRIMARY KEY,
    character_id INTEGER NOT NULL REFERENCES orm_test_characters(id),
    name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS orm_test_profiles (
    id SERIAL PRIMARY KEY,
    character_id INTEGER UNIQUE NOT NULL REFERENCES orm_test_characters(id),
    biography TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS orm_test_weapons (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS orm_test_character_weapons (
    character_id INTEGER NOT NULL REFERENCES orm_test_characters(id),
    weapon_id INTEGER NOT NULL REFERENCES orm_test_weapons(id),
    PRIMARY KEY (character_id, weapon_id)
);