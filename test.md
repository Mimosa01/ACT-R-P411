# Этап 4 — Продукции (Production Rules)
### Задание для самостоятельной работы

---

## Контекст: зачем это нужно

На этапах 1–3 вы построили память и буферы. Но кто решает, что делать дальше? Кто читает Goal buffer и инициирует запрос к памяти? Кто смотрит в Retrieval buffer и извлекает ответ?

В ACT-R этим занимается **процедурная система** — набор правил вида **IF → THEN**, которые называются **продукциями**. Это единственный механизм поведения агента. Нет продукций — нет действий.

---

## Теоретическая база

### Что такое продукция

Продукция — это правило из двух частей:

```
IF   <условие на состояние буферов>
THEN <действие над буферами>
```

Пример на псевдокоде ACT-R:

```
IF   goal buffer содержит chunk типа "find-product"
     И retrieval buffer пуст
THEN запросить DeclarativeMemory: найти факт умножения
     со слотами из goal buffer
```

Другая продукция:

```
IF   goal buffer содержит chunk типа "find-product"
     И retrieval buffer НЕ пуст
THEN вывести ответ из retrieval buffer
     очистить goal buffer
```

### Цикл работы процедурной системы

Каждый **такт** (cognitive cycle) процедурная система делает три шага:

```
1. Match   — найти все продукции, условие которых истинно сейчас
2. Select  — выбрать одну из них (conflict resolution)
3. Fire    — выполнить действие выбранной продукции
```

Один такт — одна продукция. Это жёсткое ограничение теории: агент не может одновременно делать два когнитивных действия.

### Conflict Set и Conflict Resolution

Если на шаге Match подошло несколько продукций — это называется **conflict set**. Нужно выбрать одну. На этом этапе стратегия простая: выбираем случайную из подошедших (или первую). На этапе 5 заменим это на выбор по **utility**.

### Что продукция может делать (действия)

В оригинальном ACT-R действия продукции строго ограничены:
- Изменить слот в буфере
- Очистить буфер
- Отправить запрос модулю (например, RequestRetrieval)
- Установить новый chunk в буфер

Продукция **не может** читать память напрямую, вызывать внешние функции, делать два действия подряд в одном такте. Только операции над буферами.

---

## Что нужно реализовать

### Файл 1: `Core/Production.cs` — новый файл

Продукция — это объект с именем, условием и действием.

**Поля и свойства:**
- `Name` типа `string` — имя продукции (для логов). Только геттер.
- `Condition` типа `Func<Buffers, bool>` — функция, принимает текущее состояние буферов, возвращает `true` если продукция подходит
- `Action` типа `Action<Buffers>` — функция, выполняет изменения над буферами

**Конструктор:** принимает все три поля.

**Метод `Matches(Buffers buffers)`:** вызывает `Condition(buffers)`, возвращает `bool`. Оборачивать в try-catch не нужно.

**Метод `Fire(Buffers buffers)`:** вызывает `Action(buffers)`. Перед вызовом выводит в консоль:
```
[Production] Срабатывает: "название продукции"
```

Сигнатуры:
```csharp
public Production(string name, Func<Buffers, bool> condition, Action<Buffers> action)
public bool Matches(Buffers buffers)
public void Fire(Buffers buffers)
```

---

### Файл 2: `Modules/ProceduralModule.cs` — новый файл

Процедурный модуль хранит список продукций и реализует цикл Match–Select–Fire.

**Поля:**
- приватный список `List<Production> _productions`
- ссылка на `Buffers _buffers`

**Конструктор:** принимает `Buffers buffers`.

**Метод `AddProduction(Production p)`:** добавляет продукцию в список.

**Метод `SelectAndFire()`:** один когнитивный такт. Алгоритм:

1. Найти все продукции где `p.Matches(_buffers) == true` — это conflict set
2. Если conflict set пуст — вывести `[Procedural] Нет подходящих продукций.` и вернуть `false`
3. Выбрать одну продукцию из conflict set. На этом этапе — первую (или случайную, на ваш выбор)
4. Вывести в лог все подошедшие продукции и какая выбрана
5. Вызвать `Fire()` на выбранной продукции
6. Вернуть `true`

Возвращаемый тип `bool` — пригодится в цикле агента на этапе 6.

Сигнатура:
```csharp
public bool SelectAndFire()
```

---

### Файл 3: `Productions/ArithmeticProductions.cs` — новый файл

Здесь определяются конкретные продукции для задачи умножения. Это : не как прямой алгоритм, а как последовательность срабатывающих правил.

На этапе 5 добавим **utility** — и агент научится выбирать между несколькими подходящими продукциями не случайно, а на основе накопленного опыта. статический класс с фабричным методом.

```csharp
public static class ArithmeticProductions
{
    public static IEnumerable<Production> Create(DeclarativeModule dm, GoalModule gm)
```

Метод возвращает список из **двух продукций**:

---

**Продукция 1: `"start-retrieval"`**

*Условие:*
- Goal buffer НЕ пуст
- Тип chunk'а в Goal buffer — `"find-product"`
- Retrieval buffer ПУСТ (запрос ещё не делался)

*Действие:*
- Прочитать из Goal buffer слоты `multiplicand` и `multiplier`
- Вызвать `dm.RequestRetrieval(...)` с этими значениями

*Смысл:* агент видит цель и инициирует поиск в памяти.

---

**Продукция 2: `"retrieve-answer"`**

*Условие:*
- Goal buffer НЕ пуст
- Тип chunk'а в Goal buffer — `"find-product"`
- Retrieval buffer НЕ пуст (память что-то нашла)

*Действие:*
- Прочитать из Retrieval buffer слот `product`
- Вывести ответ в консоль: `[Answer] 3 * 4 = 12`
- Вызвать `gm.ClearGoal()` — цель достигнута

*Смысл:* агент видит результат в буфере и завершает задачу.

---

### Файл 4: `Program.cs` — обновить

Теперь `Program.cs` собирает всю систему и запускает **ручной цикл** из нескольких тактов.

Последовательность:

```csharp
// 1. Создать память, загрузить факты
// 2. Создать Buffers
// 3. Создать GoalModule, DeclarativeModule, ProceduralModule
// 4. Создать продукции через ArithmeticProductions.Create(...)
//    и добавить их в ProceduralModule через AddProduction
// 5. Установить цель: find-product, multiplicand=3, multiplier=4
// 6. Запустить цикл вручную:
//    Такт 1 → SelectAndFire()  // должна сработать "start-retrieval"
//    Такт 2 → SelectAndFire()  // должна сработать "retrieve-answer"
//    Такт 3 → SelectAndFire()  // должно быть "нет подходящих"
```

Между тактами выводите разделитель:
```
--- Такт 1 ---
--- Такт 2 ---
```

---

## Структура после этапа 4

```
ACTR/
├── Core/
│   ├── Chunk.cs               ✓
│   ├── Buffer.cs              ✓
│   ├── Buffers.cs             ✓
│   ├── DeclarativeMemory.cs   ✓
│   └── Production.cs          ← новый
├── Modules/
│   ├── GoalModule.cs          ✓
│   ├── DeclarativeModule.cs   ✓
│   └── ProceduralModule.cs    ← новый
├── Productions/
│   └── ArithmeticProductions.cs ← новый
└── Program.cs                 обновить
```

---

## Ожидаемый вывод в консоли

```
--- Такт 1 ---
[Procedural] Conflict set: ["start-retrieval"]
[Procedural] Выбрана: "start-retrieval"
[Production] Срабатывает: "start-retrieval"
[Buffer: retrieval] очищен
[DM] Retrieval success (activation: -0.693): [mult-3-4 ...]
[Buffer: retrieval] ← [mult-3-4 ...]

--- Такт 2 ---
[Procedural] Conflict set: ["retrieve-answer"]
[Procedural] Выбрана: "retrieve-answer"
[Production] Срабатывает: "retrieve-answer"
[Answer] 3 * 4 = 12
[GoalModule] Цель очищена.
[Buffer: goal] очищен

--- Такт 3 ---
[Procedural] Нет подходящих продукций.
```

---

## Критерии выполнения

| # | Критерий |
|---|----------|
| 1 | `Production` хранит условие и действие как делегаты, не как наследование |
| 2 | `SelectAndFire` возвращает `false` при пустом conflict set |
| 3 | Такт 1 срабатывает `"start-retrieval"`, такт 2 — `"retrieve-answer"` |
| 4 | Продукции определены в `ArithmeticProductions`, не в `Program.cs` |
| 5 | Действие продукции читает слоты из буфера, не из внешних переменных |

---

## Частые ошибки

**Обе продукции срабатывают в такт 1** — условие `"retrieve-answer"` не проверяет `Retrieval.IsEmpty`. Без этой проверки она подходит сразу, и агент пытается прочитать пустой буфер.

**Продукция напрямую вызывает `memory.Retrieve`** — нарушение архитектуры. Продукция работает только через модули (`dm.RequestRetrieval`), которые пишут в буферы.

**`SelectAndFire` вызывает `Fire` у всех из conflict set** — должна сработать ровно одна. Один такт — одна продукция.

**Условие читает тип chunk'а через приведение типов** — не нужно. Используйте `chunk.ChunkType` и `chunk.GetSlot(...)`. Chunk уже умеет это делать.

---

## Связь с теорией

После этого этапа у вас есть работающий когнитивный агент. Он ставит цель, ищет знание в памяти и выдаёт ответ — всё через продукции и буферы. Именно так ACT-R описывает человеческое решение задач