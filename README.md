# DI Container for Unity
# MiniContainer - Документация

MiniContainer - это легковесный контейнер инверсии зависимостей (Dependency Injection) для Unity.
Он предоставляет простой и эффективный способ организации зависимостей в вашем проекте, улучшая
модульность, тестируемость и поддерживаемость кода.

## Оглавление

- [Установка](#установка)
- [Основные концепции](#основные-концепции)
- [Начало работы](#начало-работы)
- [Регистрация зависимостей](#регистрация-зависимостей)
    - [Регистрация классов](#регистрация-классов)
    - [Регистрация экземпляров](#регистрация-экземпляров)
    - [Регистрация компонентов Unity](#регистрация-компонентов-unity)
    - [Ленивые зависимости](#ленивые-зависимости)
    - [Фабрики](#фабрики)
    - [Расширенные примеры регистрации](#расширенные-примеры-регистрации)
    - [Время жизни](#время-жизни)
- [Получение зависимостей](#получение-зависимостей)
    - [Через Resolve](#через-resolve)
    - [Через атрибуты](#через-атрибуты)
- [Работа со сценами](#работа-со-сценами)
- [Области видимости (Scopes)](#области-видимости-scopes)
- [Жизненный цикл](#жизненный-цикл)
- [Автоматическая регистрация с IRegistrable](#автоматическая-регистрация-с-iregistrable)
- [Расширенные возможности](#расширенные-возможности)
- [Примеры](#примеры)

## Установка

* [Добавить](https://docs.unity3d.com/Manual/upm-ui-giturl.html) <b>Mini DI Container</b> package в Unity Package Manager - https://github.com/Mini-IT/Mini-container.git <br />

Unity Package Manager не поддерживает автоматические обновления для пакетов на основе git. Вы можете использовать [UPM Git Extension](https://github.com/mob-sakai/UpmGitExtension).

## Основные концепции

MiniContainer построен на следующих концепциях:

- **Контейнер (Container)** - центральный объект, который хранит и управляет зависимостями.
- **Регистрация (Registration)** - процесс добавления зависимостей в контейнер.
- **Разрешение (Resolution)** - процесс получения зависимостей из контейнера.
- **Время жизни (Lifetime)** - определяет, как долго экземпляр зависимости будет существовать.
- **Область видимости (Scope)** - логический контейнер для группировки зависимостей.

## Начало работы

Основой работы MiniContainer является класс `CompositionRoot`, который инициализирует и управляет контейнером:

```csharp
using MiniContainer;
using UnityEngine;

// Добавьте этот компонент на GameObject в вашей стартовой сцене
public class GameBootstrap : RootContainer 
{
    public override void Register(IBaseDIService builder)
    {
        // Регистрация сервисов
        builder.RegisterSingleton<IGameService, GameService>();
        builder.RegisterScoped<IPlayerService, PlayerService>();
        builder.RegisterTransient<IEnemyFactory, EnemyFactory>();
    }
    
    public override void Resolve()
    {
        // Код, выполняемый после разрешения всех зависимостей
        base.Resolve();
        
        // Инициализация системы после разрешения всех зависимостей
        var gameService = Container.Resolve<IGameService>();
        gameService.Initialize();
    }
}
```

Затем добавьте объект `CompositionRoot` на сцену и перетащите ваш `GameBootstrap` в поле `Root Containers` в инспекторе.

## Регистрация зависимостей

### Регистрация классов

```csharp
// Базовая регистрация классов
builder.Register<IService, ServiceImplementation>();

// Явное указание времени жизни
builder.Register<IService, ServiceImplementation>(ServiceLifeTime.Singleton);
builder.RegisterSingleton<IService, ServiceImplementation>();
builder.RegisterScoped<IService, ServiceImplementation>();
builder.RegisterTransient<IService, ServiceImplementation>();

// Регистрация без интерфейса
builder.Register<ServiceImplementation>();
builder.RegisterSingleton<ServiceImplementation>();

// Регистрация с несколькими интерфейсами
builder.Register<ServiceImplementation>().As<IService, IDisposable>();

// Регистрация с фабричным методом
builder.Register<IService>(() => new ServiceImplementation("param1"));
builder.RegisterSingleton<IService>(() => new ServiceImplementation("param1"));
```

### Регистрация экземпляров

```csharp
// Регистрация существующего экземпляра
var service = new ServiceImplementation();
builder.RegisterInstance<IService>(service);

// Регистрация экземпляра с множественными интерфейсами
builder.RegisterInstance(service).As<IService, IDisposable>();

// Регистрация экземпляра как самого себя
builder.RegisterInstanceAsSelf(service);
```

### Регистрация компонентов Unity

```csharp
// Регистрация компонента для создания на новом GameObject
builder.RegisterComponentOnNewGameObject<PlayerController>();

// С указанием родительского Transform и имени GameObject
builder.RegisterComponentOnNewGameObject<PlayerController>(parentTransform, "Player");

// Регистрация компонента из префаба
builder.RegisterComponentInNewPrefab<EnemyController>(enemyPrefab);
```

### Ленивые зависимости

```csharp
// Регистрация ленивых зависимостей
builder.RegisterLazy<IService>(container);
builder.RegisterLazy<IService, ServiceImplementation>(container);
```

### Фабрики

```csharp
// Регистрация фабрики для создания объектов
builder.RegisterFactory<IEnemyService>(container);

// После регистрации, получение фабрики из контейнера
var enemyFactory = container.Resolve<IFactory<IEnemyService>>();

// Получение сервиса через фабрику с дженериком
IEnemyService enemyService = enemyFactory.GetService<EnemyService>();

// Или используя конкретный тип
EnemyService concreteEnemyService = enemyFactory.GetService<EnemyService>();

// Получение сервиса через фабрику используя Type
Type enemyServiceType = typeof(EnemyService);
var enemyServiceByType = enemyFactory.GetService(enemyServiceType);

// Регистрация объектов реализующие IWeapon и получение их через фабрику
builder.RegisterTransient<Sword>();
builder.RegisterTransient<Bow>();
builder.RegisterTransient<Axe>();

var weaponFactory = container.Resolve<IFactory<IWeapon>>();
var sword = weaponFactory.GetService<Sword>(); // Получаем конкретную реализацию Sword
var bow = weaponFactory.GetService<Bow>();     // Получаем конкретную реализацию Bow
var axe = weaponFactory.GetService<Axe>();     // Получаем конкретную реализацию Axe
```

### Расширенные примеры регистрации

```csharp
// Регистрация с автоматическим определением всех реализуемых интерфейсов
builder.Register<ServiceImplementation>().AsImplementedInterfaces();

// Регистрация как сам тип и все его интерфейсы
builder.Register<ServiceImplementation>().AsSelf().AsImplementedInterfaces();

// Регистрация с параметризованным конструктором
builder.Register<ConfigurableService>(() => new ConfigurableService("config1", 42));

// Регистрация с несколькими типами интерфейсов
builder.Register<Service>().As(typeof(IService), typeof(IDisposable), typeof(IInitializable));

// Цепочка регистраций для одного типа с разными интерфейсами
builder
    .Register<MyService>()
    .As<IService>()
    .As<IDisposable>()
    .AsSelf();

// Регистрация с указанием родительского GameObject для Unity-компонентов
var parentTransform = GameObject.Find("ServiceContainer").transform;
builder.RegisterComponentOnNewGameObject<UIManager>(parentTransform, "UI Manager");

// Регистрация с заменой существующей реализации
builder.Register<ILogger, ConsoleLogger>(); // Базовая реализация
builder.Register<ILogger, FileLogger>();    // Заменяет предыдущую

// Регистрация по условию
if (Application.isEditor)
{
    builder.Register<ILogger, EditorLogger>();
}
else
{
    builder.Register<ILogger, ReleaseLogger>();
}

// Регистрация префаба для последующего инстанцирования
builder.RegisterComponentInNewPrefab<Enemy>(enemyPrefab, transformParent)
    .As<IEnemy>()
    .As<IDamageable>();

// Регистрация GameObject с указанием имени
builder.RegisterComponentOnNewGameObject<Canvas>(null, "Main UI Canvas")
    .As<IUIRoot>();
```

### Время жизни

MiniContainer поддерживает три типа времени жизни:

- **Singleton** - создается один экземпляр на весь контейнер.
- **Scoped** - создается один экземпляр на область видимости.
- **Transient** - создается новый экземпляр при каждом запросе.

## Получение зависимостей

### Через Resolve

```csharp
// Получение сервиса из контейнера
var service = container.Resolve<IService>();

// Разрешение всех зависимостей существующего объекта
container.ResolveObject(existingObject);
```

### Через атрибуты

```csharp
public class PlayerController : MonoBehaviour
{
    // Зависимости будут внедрены автоматически
    [Resolve] private IPlayerService _playerService;
    [Resolve] private IGameManager _gameManager;
    
    private void Start()
    {
        // Использование внедренных зависимостей
        _playerService.Initialize();
    }
}
```

## Работа со сценами

MiniContainer автоматически обрабатывает события загрузки и выгрузки сцен:

```csharp
// Реализуйте этот интерфейс для получения уведомлений о загрузке сцены
public class SceneHandler : IContainerSceneLoadedListener
{
    public void OnSceneLoaded(int scene)
    {
        Debug.Log($"Сцена {scene} загружена");
    }
}

// Реализуйте этот интерфейс для получения уведомлений о выгрузке сцены
public class SceneTracker : IContainerSceneUnloadedListener
{
    public void OnSceneUnloaded(int scene)
    {
        Debug.Log($"Сцена {scene} выгружена");
    }
}
```

## Области видимости (Scopes)

```csharp
// Создание новой области видимости
int scopeId = container.CreateScope();

// Установка текущей области видимости
container.SetCurrentScope(scopeId);

// Получение текущего ID области видимости
int currentScope = container.GetCurrentScope();

// Освобождение области видимости
container.ReleaseScope(scopeId);
```

Вы также можете использовать IScopeManager для работы с областями видимости:

```csharp
public class LevelManager : IContainerSceneLoadedListener
{
    private readonly IScopeManager _scopeManager;
    private readonly Dictionary<int, int> _levelScopes = new Dictionary<int, int>();
    
    public LevelManager(IScopeManager scopeManager)
    {
        _scopeManager = scopeManager;
    }
    
    public void OnSceneLoaded(int scene)
    {
        // Создание области видимости для новой сцены
        var scopeId = _scopeManager.CreateScope();
        _levelScopes[scene] = scopeId;
        
        // Переключение на новую область видимости
        _scopeManager.SetCurrentScope(scopeId);
    }
}
```

## Жизненный цикл

MiniContainer предоставляет два основных интерфейса для управления жизненным циклом:

### IContainerInitializable

Используется для инициализации объекта после его создания и внедрения всех зависимостей:

```csharp
public class ConfigService : IContainerInitializable
{
    public void Initialize()
    {
        // Код, выполняемый однократно после создания и внедрения зависимостей
        Debug.Log("Config service initialized");
    }
}
```

### IContainerLifeCycle

Центральный интерфейс для подписки на события жизненного цикла контейнера. Клиент сам отвечает за подписку и отписку от событий:

```csharp
public class GameManager
{
    private readonly IContainerLifeCycle _containerLifeCycle;
    
    public GameManager(IContainerLifeCycle containerLifeCycle)
    {
        _containerLifeCycle = containerLifeCycle;
    }

    public void Init()
    {
        // Подписка на события
        _containerLifeCycle.OnContainerUpdate += HandleUpdate;
        _containerLifeCycle.OnContainerSceneLoaded += HandleSceneLoaded;
        _containerLifeCycle.OnContainerSceneUnloaded += HandleSceneUnloaded;
        _containerLifeCycle.OnContainerApplicationFocus += HandleApplicationFocus;
        _containerLifeCycle.OnContainerApplicationPause += HandleApplicationPause;
    }
    
    private void HandleUpdate()
    {
        // Выполняется каждый кадр
    }
    
    private void HandleSceneLoaded(int sceneIndex)
    {
        Debug.Log($"Сцена {sceneIndex} загружена");
    }
    
    private void HandleSceneUnloaded(int sceneIndex)
    {
        Debug.Log($"Сцена {sceneIndex} выгружена");
    }
    
    private void HandleApplicationFocus(bool hasFocus)
    {
        Debug.Log($"Приложение {(hasFocus ? "получило" : "потеряло")} фокус");
    }
    
    private void HandleApplicationPause(bool isPaused)
    {
        Debug.Log($"Приложение {(isPaused ? "на паузе" : "возобновлено")}");
    }
    
    public void Dispose()
    {
        // Важно! Не забывайте отписываться от событий
        _containerLifeCycle.OnContainerUpdate -= HandleUpdate;
        _containerLifeCycle.OnContainerSceneLoaded -= HandleSceneLoaded;
        _containerLifeCycle.OnContainerSceneUnloaded -= HandleSceneUnloaded;
        _containerLifeCycle.OnContainerApplicationFocus -= HandleApplicationFocus;
        _containerLifeCycle.OnContainerApplicationPause -= HandleApplicationPause;
    }
}
```

## Автоматическая регистрация с IRegistrable

MiniContainer предоставляет механизм автоматической регистрации компонентов через интерфейс `IRegistrable`. Этот подход позволяет легко регистрировать MonoBehaviour-компоненты в контейнере без необходимости вручную прописывать их регистрацию.

### Как использовать IRegistrable

1. Реализуйте интерфейс `IRegistrable` в вашем MonoBehaviour-компоненте:

```csharp
using MiniContainer;
using UnityEngine;

public class PlayerController : MonoBehaviour, IRegistrable
{
    // Ваш код...
}
```

2. Добавьте GameObject с этим компонентом в список `Auto Register GameObjects` в инспекторе вашего Container-объекта:

```csharp
public class GameBootstrap : RootContainer 
{
    // В инспекторе добавьте GameObject с компонентом, реализующим IRegistrable,
    // в список Auto Register GameObjects
}
```
> **Примечание:** Поле `Auto Register GameObjects` доступно в инспекторе для любого класса, унаследованного от `Container`, включая `RootContainer` и `SubContainer`. Достаточно перетащить GameObject с компонентом, реализующим интерфейс `IRegistrable`, в это поле.

### Как это работает

Когда Container инициализируется, он автоматически:

1. Находит все MonoBehaviour-компоненты, реализующие интерфейс `IRegistrable`, на указанных GameObject'ах
2. Регистрирует эти компоненты в контейнере через вызов `builder.RegisterInstanceAsSelf(registrable)`
3. Сохраняет список зарегистрированных компонентов для последующего освобождения ресурсов при уничтожении контейнера

### Пример использования

```csharp
// Ваш MonoBehaviour-компонент, реализующий IRegistrable
public class UIManager : MonoBehaviour, IRegistrable
{
    public void ShowMainMenu()
    {
        // Реализация...
    }
}

// В другом месте вашего кода, после инициализации контейнера:
public class GameInitializer
{
    private readonly UIManager _uiManager;
    
    // UIManager будет автоматически внедрен через конструктор
    public GameInitializer(UIManager uiManager)
    {
        _uiManager = uiManager;
        _uiManager.ShowMainMenu();
    }
}
```

### Преимущества использования IRegistrable

- **Автоматическая регистрация** - не требуется вручную прописывать регистрацию каждого компонента
- **Явное указание зависимостей** - компоненты, реализующие IRegistrable, ясно указывают на то, что они должны быть зарегистрированы в контейнере
- **Автоматическое освобождение ресурсов** - компоненты, зарегистрированные через IRegistrable, автоматически освобождаются при уничтожении контейнера

IRegistrable особенно полезен для компонентов, которые уже присутствуют на сцене и не создаются динамически через контейнер (например, UI-элементы, предустановленные менеджеры и т.д.).

Также вы можете использовать поле `Auto Resolve GameObjects` в инспекторе для автоматического разрешения зависимостей у компонентов, которые не реализуют IRegistrable, но требуют внедрения зависимостей через атрибут [Resolve].

## Расширенные возможности

### Игнорирование типов

```csharp
// Игнорирование определенных типов при автоматическом внедрении
builder.IgnoreType<ILogger>();
```

### Настройка контейнера

```csharp
// Создание контейнера
var container = diService.GenerateContainer();
```

### Освобождение ресурсов

```csharp
// Освобождение всех ресурсов
container.ReleaseAll();

// Освобождение ресурсов определенного типа
container.Release<IPlayerService>();
container.Release(typeof(PlayerService));
```

## Примеры

### Пример сервиса игрока

```csharp
public interface IPlayerService
{
    void Initialize();
    void MovePlayer(Vector3 direction);
}

public class PlayerService : IPlayerService
{
    private readonly IInputService _inputService;
    private readonly IConfigService _configService;
    
    // Конструктор с внедрением зависимостей
    public PlayerService(IInputService inputService, IConfigService configService)
    {
        _inputService = inputService;
        _configService = configService;
    }
    
    public void Initialize()
    {
        Debug.Log("Player service initialized");
    }
    
    public void MovePlayer(Vector3 direction)
    {
        var speed = _configService.GetPlayerSpeed();
        // Логика перемещения игрока
    }
}
```

### Пример загрузчика уровня

```csharp
public class LevelLoader : MonoBehaviour, IContainerSceneLoadedListener
{
    [Resolve] private ILevelService _levelService;
    [Resolve] private IPlayerService _playerService;
    
    public void OnSceneLoaded(int scene)
    {
        if (_levelService.IsGameplayScene(scene))
        {
            _playerService.Initialize();
            Debug.Log($"Level {scene} initialized");
        }
    }
}
```

### Пример RootContainer

```csharp
// Основной контейнер зависимостей для игры
public class GameContainer : RootContainer
{
    public override void Register(IBaseDIService builder)
    {
        // Системные сервисы
        builder.RegisterSingleton<IInputService, InputService>();
        builder.RegisterSingleton<ILogService, LogService>();
        builder.RegisterSingleton<IConfigService, ConfigService>();
        
        // Сервисы игры
        builder.RegisterSingleton<IGameStateService, GameStateService>();
        builder.RegisterScoped<ILevelService, LevelService>();
        
        // Сервисы игрока
        builder.RegisterScoped<IPlayerService, PlayerService>();
        builder.RegisterTransient<IWeaponService, WeaponService>();
        
        // Фабрики
        builder.RegisterFactory<IEnemyService>(Container);
    }
    
    public override void Resolve()
    {
        base.Resolve();
        
        // Инициализация после разрешения всех зависимостей
        Debug.Log("Все зависимости разрешены и готовы к использованию");
    }
}
```

### Пример SubContainer

```csharp
// Подконтейнер, привязанный к конкретному уровню
public class LevelContainer : SubContainer
{
    public override void Register(IBaseDIService builder)
    {
        // Специфичные для уровня сервисы
        builder.RegisterScoped<IEnemyManager, EnemyManager>();
        builder.RegisterScoped<IPickupManager, PickupManager>();
        
        // Создание компонентов на сцене
        builder.RegisterComponentOnNewGameObject<EnemySpawner>(transform);
    }
    
    public override void Resolve()
    {
        base.Resolve();
        
        // Код, выполняемый после разрешения всех зависимостей в подконтейнере
        var enemyManager = Container.Resolve<IEnemyManager>();
        enemyManager.SpawnInitialEnemies();
    }
} 
