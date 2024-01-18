# The plugin will no longer be updated

# jellyfin.kinopoisk.ru

Fetches metadata from [kinopoisk.ru](https://www.kinopoisk.ru). This site is popular in the Russian-speaking community and contains almost no English-language information, so further description will be in Russian.

Плагин для Jellyfin для загрузки метаданных фильмов, сериалов с сайта [kinopoisk.ru](https://www.kinopoisk.ru) используя сторонние API:
- [kinopoiskapiunofficial.tech](https://kinopoiskapiunofficial.tech)
- [kinopoisk.dev](https://kinopoisk.dev)

Если что-то не работает - смело создавай новый issue. Я не пользуюсь плагином 24/7 - могу и не знать о сломавшейся функциональности.

Спасибо [svyaznoy362](https://github.com/svyaznoy362) за тестирования версий.

## Установка

1. Администрирование - Панель - Расширенное - Плагины - вкладка Репозитории - добавить адрес [https://raw.githubusercontent.com/luzmane/jellyfin.kinopoisk.ru/manifest/manifest.json](https://raw.githubusercontent.com/luzmane/jellyfin.kinopoisk.ru/manifest/manifest.json).
2. Запустить задачу обновление плагинов.
3. После этого на вкладке Каталог найти "КиноПоиск" (раздел Метаданные) и установить.
4. Перезагрузить Jellyfin

## Настройка

Параметры плагина искать в: Администрирование - Панель - Расширенное - Плагины - вкладка "Мои плагины" - КиноПоиск - "три точки" - Параметры

Если плагин не работает или работает плохо - попробуйте зарегистрировать (и указать в параметрах) свой собственный ApiToken для соответствующего сайта. По-умолчанию прописан общий, который быстро заканчивается.

## Использование

Плагин умеет работать с двумя сайтами ([kinopoiskapiunofficial.tech](https://kinopoiskapiunofficial.tech), [kinopoisk.dev](https://kinopoisk.dev)) в настройках можно выбрать откуда получать информацию. По умолчанию запросы идут на [kinopoisk.dev](https://kinopoisk.dev), работая с общим API токеном (спасибо [mdwitr](https://github.com/mdwitr0)). Его хватает на 200 запросов в день - Token быстро заканчивается. Поэтому лучше зарегестрировать свой собственный (и указать в параметрах). Для [kinopoiskapiunofficial.tech](https://kinopoiskapiunofficial.tech) также есть общий токен. Ограничение для него 500 запросов в день - тоже не бесконечный. Поэтому лучше зарегестрировать свой собственный (и указать в параметрах).

Плагин умеет подхватывать ID КиноПоиска в имени файла по шаблону "<текст>kp<ID КиноПоиска><текст без цифр><текст>" или "<текст>kp-<ID КиноПоиска><текст без цифр><текст>" и использовать его для поиска в базе. Также умеет искать по названию фильма (если сможет название распознать из имени файла).

### Загружаемые данные
Поддерживаются:

- Фильмы
- Сериалы
- Актёры

На данный момент грузятся:

- Жанры
- Название
- Оригинальное название (на английском)
- Рейтинги (оценки фильма и рейтинг MPAA)
- Слоган
- Дата выхода фильма
- Описание
- Постеры и задники
- Актёры
- Названия эпизодов
- Дата выхода эпизодов
- Студии
- Трейлеры
- Факты о фильме/сериале/персоне (встраивается в описание)

## Требования

* Плагин тестировался на версии 10.8.11
* Собирался c .Net 6
