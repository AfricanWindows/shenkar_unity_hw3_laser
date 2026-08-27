# Задание 3 — Laser Weapon

Unity 6000.5.4f1 · 2D · новый Input System · всё в `Assembly-CSharp`

Управление: `1` — fireball · `2` — axe · `3` — laser · `LeftCtrl` — выстрел.

---

## 1. Файлы

| Требуется по заданию | Файл в проекте |
|---|---|
| `LaserWeapon.cs` | [Assets/Scripts/Player/Weapons/LaserWeapon.cs](Assets/Scripts/Player/Weapons/LaserWeapon.cs) |
| `LaserBuilder.cs` | [Assets/Scripts/Weapons/Laser/LaserBuilder.cs](Assets/Scripts/Weapons/Laser/LaserBuilder.cs) |
| `LaserDirector.cs` | [Assets/Scripts/Weapons/Laser/LaserDirector.cs](Assets/Scripts/Weapons/Laser/LaserDirector.cs) |
| `LaserFactory.cs` | [Assets/Scripts/Weapons/Laser/LaserFactory.cs](Assets/Scripts/Weapons/Laser/LaserFactory.cs) |
| `LaserPoolManager.cs` | [Assets/Scripts/Weapons/Laser/LaserPoolManager.cs](Assets/Scripts/Weapons/Laser/LaserPoolManager.cs) |
| `LaserPowerUp.cs` | [Assets/Scripts/Player/PowerUps/LaserPowerUp.cs](Assets/Scripts/Player/PowerUps/LaserPowerUp.cs) |
| `BaseProjectile.cs` | [Assets/Scripts/Weapons/Projectiles/BaseProjectile.cs](Assets/Scripts/Weapons/Projectiles/BaseProjectile.cs) |
| `BaseWeapon.cs` | [Assets/Scripts/Weapons/BaseWeapon.cs](Assets/Scripts/Weapons/BaseWeapon.cs) |
| правка `WeaponsHandler.cs` | [Assets/Scripts/Player/WeaponsHandler.cs](Assets/Scripts/Player/WeaponsHandler.cs) |
| правка `PlayerPowerUp.cs` | **не потребовалась** — см. §6 |

Дополнительно (generic-слой, ради которого это SOLID, а не «просто работает»):

```
Assets/Scripts/Core/Pooling/          IFactory.cs   IPoolable.cs   IObjectPool.cs   GenericObjectPool.cs
Assets/Scripts/Weapons/Projectiles/   ProjectileStats.cs   ProjectileConfigSO.cs   BaseProjectile.cs
                                      IProjectileBuilder.cs   ProjectileDirector.cs   ProjectileFactory.cs
Assets/Scripts/Weapons/               WeaponType.cs   BaseWeapon.cs
Assets/Scripts/Weapons/Laser/         LaserProjectile.cs   LaserBuilder.cs   LaserDirector.cs
                                      LaserFactory.cs   LaserPoolManager.cs
Assets/Scripts/Player/Pickable/       LaserPickable.cs
```

---

## 2. Цепочка целиком

```
ProjectileConfigSO  (asset: speed / lifetime / damage / scale / pierce / animator)
        ↓  «какие значения»
LaserBuilder        : IProjectileBuilder<LaserProjectile>   — КАК собрать, по шагам
        ↓
LaserDirector       : ProjectileDirector<LaserProjectile>   — В КАКОМ ПОРЯДКЕ (рецепт)
        ↓
LaserFactory        : ProjectileFactory<LaserProjectile>    — Factory Method: отдать готовый продукт
        ↓
GenericObjectPool<LaserProjectile> : IObjectPool<T>         — переиспользование
        ↓
LaserWeapon         : BaseWeapon : IUseableWeapon           — когда можно стрелять
        ↓
WeaponsHandler                                              — ввод (цифра 3 + LeftCtrl)
        ↑
LaserPickable → LaserPowerUp → LaserWeapon.Equip()          — разблокировка навсегда
```

Пул знает только `IFactory<T>` · фабрика ничего не знает про пул · снаряд не знает про конкретный пул (возврат через callback) · оружие знает только `IObjectPool<LaserProjectile>`.

Новое оружие = новый config-asset + наследник `BaseProjectile` + наследник фабрики. **Ни один существующий класс не правится** (Open/Closed).

---

## 3. Power-Up сделан как цветок

Задание: «ניתן לממש את זה בדומה ל־FireFlowerPowerUp.cs» — реализовать по аналогии с цветком. Сделано ровно 1:1:

| Fire Flower (уже был в проекте) | Laser (добавлено) |
|---|---|
| `Prefab_Flower` — SpriteRenderer + Collider2D `IsTrigger` | `Prefab_LaserPowerUp` — SpriteRenderer + Collider2D `IsTrigger` |
| `FireFlowerController : BasePickable` | `LaserPickable : BasePickable` |
| `CreatePowerUp() → new FireFlowerPowerUp()` | `CreatePowerUp() → new LaserPowerUp()` |
| `FireFlowerPowerUp : IPowerUp` → `fireballWeapon.Equip()` | `LaserPowerUp : IPowerUp` → `laserWeapon.Equip()` |
| подобрал → стреляет **навсегда** | подобрал → стреляет **навсегда** |

`Equip()` выставляет флаг и **никто и никогда его не снимает** — таймера нет, эффект постоянный, как у цветка.
Подбор обрабатывает `BasePickable.OnTriggerEnter2D` — тот же самый класс, что у цветка, звезды, ключа и топоров. Дублирующего `BasePowerUp` не создавалось (см. §6).

---

## 4. Паттерны — где что реализовано

### Builder — `LaserBuilder`
Собирает снаряд по шагам: `SetSpeed → SetLifetime → SetDamage → SetSize → SetPiercing → SetAnimation → Build()`.
Знает **как** собрать, но **не знает какие значения** — числа приходят снаружи.

> Отличие от кода с урока: там был `SetSpeed()` без параметра и захардкоженный `400` внутри билдера — билдер становился вторым местом, где живёт баланс. Здесь префаб и контейнер приходят **через конструктор** (не `Resources.Load`, не синглтон), а числа — из `ProjectileConfigSO`. `Reset()` обнуляет накопленное, поэтому один билдер собирает сколько угодно лазеров.

### Director — `LaserDirector`
Владеет **порядком** шагов (рецептом). Подменив билдер, тем же процессом получим другое представление. Отдельный `LaserDirector` — точка расширения: если у лазера появится «зарядка» или «длина луча», переопределяется `Construct()`, а generic-директор не трогается.

### Factory Method — `ProjectileFactory<T>` → `LaserFactory`

| Термин лекции | Класс |
|---|---|
| Product | `BaseProjectile` |
| Concrete Product | `LaserProjectile` |
| Creator | `ProjectileFactory<T>` |
| Concrete Creator | `LaserFactory` |
| Factory Method | `Create()` |

Остальная игра просит «дай лазер» и не знает ни про билдер, ни про порядок шагов, ни про `Instantiate`.

### Object Pool — `GenericObjectPool<T>` + `LaserPoolManager`
`GenericObjectPool<T>` — **обычный C#-класс, не MonoBehaviour**: пул это учёт, ему не нужен transform.

Структуры данных:
- `Queue<T> _inactive` — `Enqueue`/`Dequeue` за **O(1)**. В коде с урока был `List` + `foreach` с линейным поиском неактивного объекта — **O(n) на каждый выстрел**: чем больше пул, тем медленнее стрельба.
- `HashSet<T> _active` — `Remove`/`Contains` за **O(1)**. Так ловится **повторный `Release`**: без этой проверки один и тот же лазер попал бы в очередь дважды и был бы выдан двум выстрелам сразу.

`Prewarm()` платит цену `Instantiate` во время загрузки, а не на первом выстреле.
`Get()` при пустой очереди создаёт через фабрику (если `allowGrowth` и не превышен `maxSize`), иначе `null` + `Debug.LogWarning`.
При создании объекта пул отдаёт ему callback: `item.SetReleaseCallback(() => Release(item))` — снаряд узнаёт **как** вернуться домой, но не **где** дом (Dependency Inversion).

> В Unity есть встроенный `UnityEngine.Pool.ObjectPool<T>`, но по заданию написан свой — это отмечено комментарием в самом классе.

`LaserPoolManager` — **composition root**, а не логика пула: собирает `builder → director → factory → pool` в `Awake` и дальше только делегирует. Логи `[Laser] Taken/Returned` он вешает на события `ItemTaken`/`ItemReleased` пула — сам пул остаётся laser-агностичным.
Контейнер для спящих лазеров он создаёт **в корне сцены** (объект `LaserPool`), а не на себе: иначе лазеры были бы детьми Марио и ездили бы за ним.

### Template Method — три применения
1. **`BaseProjectile.Fire()`** — скелет выстрела: `SetPositionAndRotation → OnBeforeFire → ApplyMovement(GetDirection()) → OnAfterFire`. Порядок фиксирован, наследник не может его переставить. `LaserProjectile` переопределяет только `GetDirection() => Vector2.up`.
2. **`BaseWeapon.Attack()`** — скелет спуска: `разблокировано? → кулдаун прошёл? → FireInternal() → запомнить время`. Проверка разблокировки живёт в базовом классе, поэтому наследник **не может её забыть**.
3. **`BasePickable.OnTriggerEnter2D()`** — скелет подбора (был в проекте с hw2): `CompareTag → CreatePowerUp() → CollectPowerUp → SetActive(false)`. `LaserPickable` — три строки.

---

## 5. SOLID

- **S** — пул хранит, фабрика создаёт, билдер настраивает, директор задаёт порядок, снаряд летает, оружие решает *когда* стрелять, handler читает ввод. `LaserPoolManager` — только сборка зависимостей.
- **O** — новое оружие = новый config + наследники. `WeaponsHandler` сам находит всё, что реализует `IWeapon`, — лазер подхватился **без единой правки логики** handler'а.
- **L** — `LaserProjectile` подставляется всюду, где ждут `BaseProjectile`, и не ломает контракт: `Fire()` работает, `Despawn()` работает, пул не замечает разницы.
- **I** — мелкие интерфейсы: `IPoolable`, `IFactory<T>`, `IObjectPool<T>`, `IWeapon`, `IUseableWeapon`, `IDamageable`. Оружию не видны prewarm-счётчики и политика роста.
- **D** — `LaserWeapon` зависит от `IObjectPool<LaserProjectile>`; пул — от `IFactory<T>`; снаряд возвращается через `Action`-callback и не знает ни одного класса пула.

---

## 6. Performance & Optimization

**Скрипты**
- Ни одного `GameObject.Find` / `FindObjectOfType` / `GetComponent` в `Update`/`FixedUpdate`. `Rigidbody2D` кешируется в `Awake` (`BaseProjectile`), пул и fire point — в `OnAwake` (`LaserWeapon`).
- `OnTriggerEnter2D` использует `TryGetComponent(out IDamageable)` — без аллокации, когда компонента нет (а это самый частый случай).
- Тег сравнивается через `CompareTag`, а не `other.tag == "Player"` (второе аллоцирует строку на каждом контакте).
- **Ни одного пустого `Start()`/`Update()`** в новых скриптах.
- **У снаряда нет `Update` вообще.** Время жизни — `Invoke(nameof(ExpireByLifetime), lifetime)` в `OnSpawned()` и `CancelInvoke()` в `OnDespawned()`. Скорость задаётся **один раз** при выстреле, дальше объект несёт физика.
- Никаких `new` в горячем пути — на этом построен пул.
- Структуры данных: `Queue` (пул), `HashSet` (активные), `enum WeaponType` вместо строк.
- `ProjectileStats` — `struct`, копируется без аллокации; immutable, поэтому у летящего снаряда нельзя «на ходу» переписать урон.

**Физика** (настройки префаба `Laser`, см. §7.3)
- Примитивный **`BoxCollider2D`**, `Is Trigger ☑`. Никаких polygon/mesh-коллайдеров.
- `Rigidbody2D`: **`Gravity Scale = 0`**, движение через Rigidbody (`linearVelocity`), а не `transform.Translate`.
- **`Collision Detection = Continuous`** — лазер быстрый, при Discrete он проскакивал бы сквозь врагов между кадрами физики.
- **`Interpolate = Interpolate`** — плавная картинка.

**Сцена и рендер**
- Все лазеры — под одним контейнером `LaserPool` в корне сцены, который **не двигается**: иерархия плоская, лишних пересчётов трансформов нет.
- Спрайт лазера — один материал, попадает в динамический батчинг.

**ScriptableObject** — значения лежат один раз в ассете, а не дублируются полями на каждом префабе.

---

## 7. Настройка в редакторе

Всё, что нельзя сделать кодом. По шагам.

### 7.1 Спрайты

Положить рисунки в `Assets/Sprites/` с именами по конвенции проекта (`Sprite_Fireball`, `Sprite_FireFlower`, `Sprite_Axe`…):

| Что нарисовано | Имя файла |
|---|---|
| луч лазера (снаряд) | `Sprite_Laser.png` |
| пистолет (предмет на уровне, который подбирают) | `Sprite_LaserGun.png` |

Выделить оба в Project и в Inspector'е выставить:
`Texture Type = Sprite (2D and UI)` · `Sprite Mode = Single` · `Alpha Is Transparency ☑` · `Generate Mip Maps ☐` ·
`Pixels Per Unit` — подобрать так, чтобы размер на сцене был нормальным (у существующих спрайтов проекта единица = один тайл; у луча лучше сделать его узким и высоким).

### 7.2 Config-ассет с числами лазера

`Assets/Settings/` → ПКМ → **Create ▸ Weapons ▸ Projectile Config** → назвать **`LaserConfig`**.

| Поле | Значение | Комментарий |
|---|---|---|
| Speed | `12` | units/sec |
| Lifetime | `2` | сек до авто-возврата в пул |
| Damage | `1` | врагам хватает 1 |
| Scale | `1` | |
| Pierces Enemies | по вкусу | ☐ = исчезает при попадании, ☑ = пролетает сквозь врагов |
| Animator Controller | пусто | если анимации лазера нет |

Это единственное место, где живут числа. В коде их нет.

### 7.3 Префаб снаряда — `Laser.prefab`

Пустой GameObject в сцене, имя **`Laser`**, добавить:

- **Sprite Renderer** → `Sprite_Laser`
- **Rigidbody2D**: `Gravity Scale = 0` · `Collision Detection = Continuous` · `Interpolate = Interpolate` · `Freeze Rotation Z ☑`
- **BoxCollider2D**: `Is Trigger ☑`, размер под спрайт
- **`LaserProjectile`** (скрипт):
  - `Ignore Tag = Player`
  - `Sprite Rotation Z = 0` — если луч нарисован вертикально; поставить `90`, если нарисован лёжа
  - `Blocking Layers` — оставить пустым (тогда лазер останавливает любой solid не-триггерный коллайдер: пол, потолок, платформы; монеты и пикапы — триггеры — он пролетает)

Перетащить в `Assets/Prefabs/` (рядом с `Fireball.prefab` и `Axe.prefab`), объект со сцены удалить.

### 7.4 Оружие на Марио

Открыть `Assets/Resources/Tiles/Prefab_Mario.prefab`.

1. Добавить дочерний объект **`LaserWeapon`**, позиция ~`(0.4, 0.3, 0)` — оттуда вылетает луч.
   **Он должен быть последним в списке детей** — тогда `WeaponsHandler` пронумерует его третьим и он будет отвечать на клавишу `3` (сейчас дети: `WeaponsHandler`, `FireballWeapon`, `AxeWeapon`).
2. На этот объект повесить **два** компонента:

   **`LaserPoolManager`**
   | Поле | Значение |
   |---|---|
   | Laser Prefab | `Laser.prefab` |
   | Laser Config | `LaserConfig` |
   | Container | **пусто** — пул сам создаст `LaserPool` в корне сцены |
   | Prewarm Count | `10` |
   | Max Size | `30` |
   | Allow Growth | ☑ |

   **`LaserWeapon`**
   | Поле | Значение |
   |---|---|
   | Laser Pool | перетащить **этот же объект** (`LaserPoolManager` рядом) |
   | Fire Point | пусто — стреляет из своей позиции |
   | Cooldown | `0.25` |
   | Unlocked From Start | **☐ снять** — иначе можно стрелять без подбора |

> Почему пул лежит на самом Марио, а не отдельным объектом на сцене: префаб не может хранить ссылку на объект сцены. Марио создаётся `Tools ▸ Level Creator` при построении уровня, и ссылка на сценический `LaserPool` слетала бы при каждом перестроении. Внутри префаба всё держится само. Спящие лазеры при этом детьми Марио **не становятся** — `LaserPoolManager` кладёт их в отдельный неподвижный `LaserPool` в корне сцены.

### 7.5 Предмет для подбора — `Prefab_LaserPowerUp`

Как `Prefab_Flower`, только с пистолетом:

- пустой GameObject, имя **`Prefab_LaserPowerUp`**
- **Sprite Renderer** → `Sprite_LaserGun`
- **BoxCollider2D**: `Is Trigger ☑`
- **`LaserPickable`** (скрипт), `Player Tag = Player`

Сохранить в **`Assets/Resources/Tiles/Prefab_LaserPowerUp.prefab`** — имя должно совпасть точно, оно уже прописано в `TilePalette` под кодом **16**.

Поставить на уровень одним из двух способов:
- перетащить префаб в Hierarchy и поставить куда нужно;
- либо вписать код `16` в нужную клетку `Assets/Resources/Level01-Mario.txt` и перестроить уровень через `Tools ▸ Level Creator`.

### 7.6 Layers и Layer Collision Matrix *(опционально)*

Лазер работает и без этого — он отличает триггеры от solid-коллайдеров. Но в лекции про Performance это отдельный пункт, и сейчас в проекте пользовательских слоёв нет вообще.

1. `Edit ▸ Project Settings ▸ Tags and Layers` → добавить **`Projectile`**, **`Enemy`**, **`Ground`**.
2. Назначить: `Laser.prefab` → `Projectile`; `Prefab_Goomba` / `Prefab_Bowser` → `Enemy`; `Prefab_Floor` / `Prefab_MovingTile` / `Prefab_BlinkTile` → `Ground`.
3. `Edit ▸ Project Settings ▸ Physics 2D ▸ Layer Collision Matrix`: у `Projectile` оставить галочки **только** напротив `Enemy` и `Ground`, остальное снять.
4. В `Laser.prefab` заполнить `Blocking Layers = Ground`.

---

## 8. Проверка (Definition of Done)

| Проверка | Ожидаемо |
|---|---|
| Старт сцены | `[Laser] Pool prewarmed with 10 lasers`, в Hierarchy появился `LaserPool` с 10 неактивными объектами |
| `3`, затем `LeftCtrl` **до** подбора | `[Laser] Locked - pick up the LaserPowerUp first`, выстрела нет |
| Подобрал пистолет | `[LaserPowerUp] Picked up - laser unlocked`, предмет исчез |
| `LeftCtrl` после подбора | `[Laser] Taken from pool (inactive left: 9)` → `[Laser] Fired from (...)`, луч летит **строго вверх** |
| Попадание во врага | `[Laser] Hit ...` → `[Laser] Returned to pool (inactive now: 10)`, урон через `IDamageable` |
| Промах | через `Lifetime` сек: `[Laser] Lifetime expired` → `[Laser] Returned to pool (...)` |
| Смерть и респавн / перезаход на уровень | лазер остаётся разблокированным, пока жив объект Марио; новый Марио начинает заново, как и с цветком |
| Очередь выстрелов | под `LaserPool` число объектов **не растёт** после прогрева — ни одного `Instantiate` после старта |
| Fireball и Axe | работают ровно как раньше (`1` и `2`) |

Фильтр в консоли Unity: **`[Laser`**.

---

## 9. Отступления от спеки и почему

Проект — продолжение hw2, и часть сущностей из спеки в нём **уже была**. Правило «не дублируй» победило правило «создай файл с таким именем».

| Спека просит | Что сделано | Почему |
|---|---|---|
| `BaseWeapon`, `BaseProjectile`, `IWeapon` | Новая иерархия в **namespace** `Game.Weapons` / `Game.Projectiles` / `Game.Core` | Все три имени уже заняты в global namespace: `Player/Liskov/BaseWeapon.cs` (демо Лискова, от неё наследуется `LightningWeapon`, объект `TestBaseWeapon` есть в сцене), `Player/Projectiles/BaseProjectile.cs` (им пользуются fireball и axe), `Interfaces/IWeapon.cs`. Namespace даёт **имена файлов ровно как в листе сдачи** и при этом ничего не ломает. |
| `BasePowerUp : MonoBehaviour` с `OnTriggerEnter2D` | Переиспользован существующий **`BasePickable`** | Это буквально тот же класс: `OnTriggerEnter2D` + `CompareTag` + шаблонный метод. Второй такой же был бы дублем. Итог: `LaserPickable : BasePickable` + `LaserPowerUp : IPowerUp` — ровно так же, как устроен цветок (см. §3). |
| `IWeaponUnlockService.Unlock(WeaponType.Laser)` | Переиспользован **`IUseableWeapon.Equip()`** | Разблокировка через `Equip()` уже работает для fireball. Отдельный сервис был бы вторым механизмом для той же задачи. |
| правка `PlayerPowerUp.cs` | **не потребовалась** | `PlayerPowerUp.CollectPowerUp(IPowerUp)` уже полностью generic — он вызывает `ApplyPowerUp` у чего угодно. Это и есть OCP: если бы пришлось его править, дизайн был бы хуже. |
| правка `WeaponsHandler.cs` | минимальная: выстрел вынесен в `FireSelected()` с проверкой границ | Handler уже собирал всё, что реализует `IWeapon`, поэтому лазер подхватился сам. Сообщение «Locked» живёт в `BaseWeapon.Attack()` — там, где ему и место по Template Method, и handler не обязан знать, что лазер существует. |
| `IPoolable { OnSpawned, OnDespawned }` | добавлен третий член `SetReleaseCallback(Action)` | Спека предлагала `SetReleaseCallback` на `BaseProjectile` и `Release((T)x)` с приведением типа внутри пула. Через интерфейс приведение не нужно вообще — пул остаётся строго типизированным. |
| — | добавлен `SetPiercing(bool)` в билдер | Задание требует выбор «пролетает сквозь врагов / исчезает при попадании». Значение приходит из `ProjectileConfigSO`, а не хардкодится. |
| — | добавлен `ignoreTag` в `BaseProjectile` | Марио — **не-триггерный** коллайдер и **не** `IDamageable`. Без этого лазер «упирался» бы в самого стрелка в тот же кадр, в котором появился. |
| — | `TilePalette`: код **16** → `Prefab_LaserPowerUp` | Чтобы предмет можно было расставлять через `Tools ▸ Level Creator`, как все остальные тайлы. |

---

## 10. Компиляция

Проект собирается без ошибок и без warnings — и runtime-сборка (`Assembly-CSharp`), и редакторная (`Assembly-CSharp-Editor`).
