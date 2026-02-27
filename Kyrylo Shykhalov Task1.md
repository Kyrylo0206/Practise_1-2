# Аналіз архітектури застосунків Modulith та Fitnet

## 1. Modulith - Аналіз

### 1.1 Діаграма високого рівня (Module-Level Architecture)

┌──────────────────────────────────────────────────────────────────┐
│                        Modulith Application                      │
│                                                                  │
│  ┌────────────────┐              ┌────────────────┐              │
│  │   WebApi       │              │ SharedKernel   │              │
│  │   (Presentation│◄─────────────│  (Common DDD   │              │
│  │    Layer)      │              │   Interfaces)  │              │
│  └────────▲───────┘              └────────────────┘              │
│           │                                                      │
│  ┌────────┴──────────────────────────────────────────────┐       │
│  │              Module Contracts (Explicit APIs)         │       │
│  │  Products.Contracts │ Orders.Contracts │ Users.Contracts      │
│  │                    Baskets.Contracts                  │       │
│  └────────▲──────────────────────────────────────────────┘       │
│           │                                                      │
│  ┌────────┴─────┬────────────┬────────────┬────────────┐         │
│  │   Products   │   Orders   │   Users    │  Baskets   │         │
│  │   Module     │   Module   │   Module   │  Module    │         │
│  └──────────────┴────────────┴────────────┴────────────┘         │
│           │                                                      │
│  ┌────────┴──────────────────────────────────────────────┐       │
│  │              Infrastructure Layer                     │       │
│  │  (Persistence, EF Core DbContexts per Module)         │       │
│  └───────────────────────────────────────────────────────┘       │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

**Ключові характеристики:**
- Кожен модуль має власний Contracts проєкт (чітке розмежування API)
- Спільне ядро (SharedKernel) для базових DDD концепцій
- Єдина точка входу через WebApi
- Кожен модуль має власний DbContext

### 1.2 Детальна діаграма модуля Orders

```
┌─────────────────────────────────────────────────────────────┐
│                    Orders Module                            │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              Endpoints (API Layer)                   │   │
│  │  - Get.cs, Delete.cs                                 │   │
│  │  - Get.Request.cs, Delete.Request.cs                 │   │
│  └──────────────────────▲───────────────────────────────┘   │
│                         │                                   │
│  ┌──────────────────────┴───────────────────────────────┐   │
│  │         UseCases (Application Layer)                 │   │
│  │  - AddOrderCommandHandler                            │   │
│  │  - AddOrderCommandValidator                          │   │
│  │  - DeleteOrderCommandHandler                         │   │
│  │  - DeleteOrderCommand                                │   │
│  └──────────────────────▲───────────────────────────────┘   │
│                         │                                   │
│  ┌──────────────────────┴───────────────────────────────┐   │
│  │              Domain Layer                            │   │
│  │  Aggregates:                                         │   │
│  │    - Order (Aggregate Root, IAggregateRoot)          │   │
│  │    - OrderItem (Entity)                              │   │
│  │  Specifications:                                     │   │
│  │    - OrderByIdSpec                                   │   │
│  │  ViewModels:                                         │   │
│  │    - OrderVm, OrderItemVm                            │   │
│  └──────────────────────▲───────────────────────────────┘   │
│                         │                                   │
│  ┌──────────────────────┴───────────────────────────────┐   │
│  │         Infrastructure Layer                         │   │
│  │  Data:                                               │   │
│  │    - OrderDbContext                                  │   │
│  │    - OrderRepository                                 │   │
│  │    - OrderConfiguration (EF Core mapping)            │   │
│  │    - Migrations                                      │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```
### 1.3 DDD та Clean Architecture в Modulith

#### Що присутнє:

**Bounded Contexts:**
- **Explicit**: Кожен модуль представляє окремий Bounded Context
  - Products (управління продуктами)
  - Orders (управління замовленнями)
  - Users (управління користувачами)
  - Baskets (управління кошиками)
- Контракти між модулями визначені через окремі `.Contracts` проєкти
- Комунікація між модулями через явні інтерфейси

**Aggregates:**
- `Order` є Aggregate Root (реалізує `IAggregateRoot`)
- `OrderItem` є частиною агрегату Order
- Factory pattern для створення складних агрегатів
- Інкапсуляція бізнес-логіки всередині агрегатів

**Entities vs Value Objects:**
- Entities: `Order`, `OrderItem` (мають ідентифікатори)
- Базовий клас `EntityBase` з `SharedKernel`

**Repositories:**
- Використовуються Ardalis.Specification: `IRepository<T>` та `IReadRepository<T>` з SharedKernel
- Реалізації через `EfRepository<T>` в Infrastructure
- Підтримка Specification Pattern (`OrderByIdSpec`, `ProductByIdSpec` тощо)
- Повна CQRS реалізація: `ICommand<T>` / `IQuery<T>` з pipeline behaviors (Validation → Logging → Transaction)

**Clean Architecture Layers:**
```
Endpoints       → Presentation
UseCases        → Application
Domain          → Domain
Infrastructure  → Infrastructure (Data)
```

#### Що відсутнє:

**Domain Services:**
- Не виявлено явних Domain Services
- Вся логіка в агрегатах курсі в акадеи директора це рахувалось як не праивльно 

**Value Objects:**
- Присутні в Products модулі: `Money`, `Address` (наслідують базовий клас `ValueObject` з SharedKernel)
- Присутні в Users.Contracts: `FullName`
- Але не використовуються в Orders та Baskets — там залишаються примітиви

**Domain Events:**
- Присутні: `DomainEventBase` у SharedKernel
- Публікуються автоматично через `TxBehavior` після виконання команди (в рамках транзакції)
- Integration Events визначені в Contracts проєктах (`ProductUpdateQuantityEvent`, `BasketItemDeletedIntegrationEvent`)

**Business Rules:**
- Валідація реалізована через FluentValidation у `ValidationBehavior` (MediatR pipeline)
- Валідатори існують для кожної команди (наприклад, `AddProductCommandValidator`)
- Але бізнес-правила знаходяться на рівні Application, а не Domain — домен слабо захищений

#### Архітектурні ризики:

1. **Anemic Domain Model**: Агрегати мають мало бізнес-логіки
2. **Public Setters**: `Order.cs` має публічні сеттери (порушення інкапсуляції)
3. **Primitive Obsession**: Використання примітивних типів замість Value Objects
4. **Відсутність Repository Interfaces в Domain**: Порушення Dependency Inversion

#### Що заважатиме міграції до мікросервісів:

1. **Прямі залежності між модулями**: Можливі через спільну базу даних
2. **Відсутність асинхронної комунікації**: Синхронні виклики між модулями, краще асихроні 
3. **Спільні транзакції**: Можливі транзакції через кілька модулів
4. **SharedKernel**: Потенційно спільний код між майбутніми сервісами немає незалежності між системами

---

## 2. Fitnet - Аналіз

### 2.1 Діаграма високого рівня (Module-Level Architecture)

```
┌──────────────────────────────────────────────────────────────┐
│                    Fitnet Application                        │
│                 (Single Project Structure)                   │
│                                                              │
│  ┌───────────────────────────────────────────────────────┐   │
│  │                  Program.cs                           │   │
│  │              (Application Entry Point)                │   │
│  └───────────────────────▲───────────────────────────────┘   │
│                          │                                   │
│  ┌───────────────────────┴───────────────────────────────┐   │
│  │              Module Registration                      │   │
│  │  ContractsModule │ OffersModule │ PassesModule │      │   │
│  │                ReportsModule                          │   │
│  └──────────────▲─────────────────────────────────▲──────┘   │
│                 │                                 │          │
│  ┌──────────────┴────────┐         ┌──────────────┴──────┐   │
│  │  Contracts Module     │         │  Offers Module      │   │
│  │  (Bounded Context)    │         │  (Bounded Context)  │   │
│  │                       │         │                     │   │
│  │  PrepareContract/     │         │  Prepare/           │   │
│  │  SignContract/        │         │  Data/              │   │
│  │  Data/                │         │                     │   │
│  └───────────────────────┘         └─────────────────────┘   │
│                                                              │
│  ┌──────────────┴────────┐         ┌──────────────┴──────┐   │
│  │  Passes Module        │         │  Reports Module     │   │
│  │  (Bounded Context)    │         │  (Bounded Context)  │   │
│  └───────────────────────┘         └─────────────────────┘   │
│                                                              │
│  ┌───────────────────────────────────────────────────────┐   │
│  │          Common (Shared Utilities)                    │   │
│  │  - BusinessRulesEngine                                │   │
│  │  - EventsPublisher (In-Memory Event Bus)              │   │
│  └───────────────────────────────────────────────────────┘   │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

**Ключові характеристики:**
- Монолітний проєкт з логічним розділенням через namespaces
- Кожен модуль = Bounded Context
- Вертикальне slice-ування функціональності
- In-memory Event Bus для міжмодульної комунікації
- Кожен модуль має власну схему БД

### 2.2 Детальна діаграма модуля Contracts

```
┌───────────────────────────────────────────────────────────────┐
│                     Contracts Module                          │
│                  (Bounded Context: Contracts)                 │
│                                                               │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │              API Layer (Endpoints)                      │  │
│  │  PrepareContractEndpoint                                │  │
│  │  SignContractEndpoint                                   │  │
│  │  ContractsEndpoints (registration)                      │  │
│  └──────────────────────▲──────────────────────────────────┘  │
│                         │                                     │
│  ┌──────────────────────┴──────────────────────────────────┐  │
│  │         Business Process Slices                         │  │
│  │                                                         │  │
│  │  ┌─────────────────────────────────────────────┐        │  │
│  │  │  PrepareContract/ (Vertical Slice)          │        │  │
│  │  │  - PrepareContractRequest                   │        │  │
│  │  │  - PrepareContractRequestValidator          │        │  │
│  │  │  - PrepareContractEndpoint                  │        │  │
│  │  │  BusinessRules/                             │        │  │
│  │  │    - ContractCanBePreparedOnlyForAdultRule  │        │  │
│  │  │    - PreviousContractHasToBeSignedRule      │        │  │
│  │  │    - CustomerMustBeSmallerThan...Rule       │        │  │
│  │  └─────────────────────────────────────────────┘        │  │
│  │                                                          │  │
│  │  ┌─────────────────────────────────────────────┐        │  │
│  │  │  SignContract/ (Vertical Slice)             │        │  │
│  │  │  - SignContractRequest                      │        │  │
│  │  │  - SignContractRequestValidator             │        │  │
│  │  │  - SignContractEndpoint                     │        │  │
│  │  │  BusinessRules/                             │        │  │
│  │  │    - ContractCanOnlyBeSignedWithin30Days... │        │  │
│  │  │  Events/                                    │        │  │
│  │  │    - ContractSignedEvent                    │        │  │
│  │  └─────────────────────────────────────────────┘        │  │
│  └──────────────────────▲──────────────────────────────────┘  │
│                         │                                     │
│  ┌──────────────────────┴──────────────────────────────────┐  │
│  │              Domain Layer (Data/)                       │  │
│  │                                                          │  │
│  │  Contract (Aggregate Root)                              │  │
│  │    - Properties: Id, CustomerId, PreparedAt, etc.       │  │
│  │    - Factory Method: Contract.Prepare(...)              │  │
│  │    - Domain Logic: Sign(...)                            │  │
│  │    - Business Rule Validation                           │  │
│  │                                                          │  │
│  └──────────────────────▲──────────────────────────────────┘  │
│                         │                                     │
│  ┌──────────────────────┴──────────────────────────────────┐  │
│  │         Infrastructure Layer (Data/Database/)           │  │
│  │                                                          │  │
│  │  - ContractsPersistence (DbContext)                     │  │
│  │  - ContractEntityConfiguration                          │  │
│  │  - Migrations/                                          │  │
│  │  - DatabaseModule (DI registration)                     │  │
│  │                                                          │  │
│  └─────────────────────────────────────────────────────────┘  │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

### 2.3 DDD та Clean Architecture в Fitnet

#### Що присутнє:

**Bounded Contexts:**
- **Explicit & Well-Defined**:
  - **Contracts**: Підготовка та підписання контрактів
  - **Offers**: Створення пропозицій для клієнтів
  - **Passes**: Керування абонементами
  - **Reports**: Звітність
- Кожен контекст має власну схему БД
- Міжконтекстна комунікація через Domain Events

**Aggregates:**
- `Contract` є чітким Aggregate Root
- Factory methods для створення (`Contract.Prepare`)
- Інкапсуляція бізнес-логіки (приватні сеттери)
- Валідація інваріантів під час створення та модифікації

**Business Rules:**
- **Явний Business Rules Engine** (`Common/BusinessRulesEngine`)
- Кожне правило - окремий клас (Single Responsibility)
- Правила валідуються перед зміною стану
- Приклади:
  - `ContractCanBePreparedOnlyForAdultRule`
  - `ContractCanOnlyBeSignedWithin30DaysFromPreparation`
  - `PreviousContractHasToBeSignedRule`

**Domain Events:**
- Event-based communication (`ContractSignedEvent`)
- In-memory Event Bus (`Common/EventsPublisher`)
- Асинхронна обробка подій (`PassExpiredEventHandler`)

**Clean Architecture:**
```
Endpoints              → Presentation (API)
Business Process       → Application (Use Cases)
  (PrepareContract/)
Data/Contract.cs       → Domain (Entities, Aggregates)
Data/Database/         → Infrastructure (Persistence)
```

**Vertical Slice Architecture:**
- Кожен бізнес-процес у власній папці
- Усі пов'язані артефакти разом (Request, Validator, Endpoint, BusinessRules)
- Легко знайти та модифікувати функціональність

#### Переваги архітектури:

1. **Rich Domain Model**: Агрегати містять справжню бізнес-логіку
2. **Explicit Business Rules**: Правила чітко визначені та тестовані
3. **Event-Driven Communication**: Loose coupling між модулями
4. **Vertical Slices**: Висока когезія, низьке зчеплення
5. **Single Project**: Простота для команди, що починає
6. **Schema per Module**: Логічна ізоляція даних

#### Потенційні покращення:

**Value Objects:**
- `CustomerId` можна було б зробити Value Object
- `Duration` міг би бути доменною концепцією з валідацією

**Repository Pattern:**
- Відсутні явні Repository interfaces
- Прямий доступ до DbContext з endpoints

**Bounded Context Isolation:**
- Модулі в одному проєкті - можливі неконтрольовані залежності
- Відсутній механізм контролю залежностей на рівні compile-time

#### Що допомагає майбутній міграції до мікросервісів:

1. **Event-Driven Communication**: Готова до розподіленої системи
2. **Schema per Module**: Кожен модуль має власну БД
3. **Bounded Contexts**: Чіткі межі для виділення сервісів
4. **Vertical Slices**: Легко ідентифікувати що переносити
5. **Loose Coupling**: Мінімальні залежності між модулями

#### Що заважатиме міграції до мікросервісів:

1. **In-Memory Event Bus**: 
   - Втрата повідомлень при рестарті
   - Відсутність гарантій доставки
   - Потрібна заміна на RabbitMQ/Kafka

2. **Транзакції**:
   - Можливі ACID транзакції через кілька модулів
   - Потрібно впроваджувати Saga pattern

3. **Прямі залежності**:
   - Теоретично можливі через єдиний проєкт
   - Потрібні Architecture Tests для контролю

4. **Shared Common**:
   - Спільний код (BusinessRulesEngine, EventsPublisher)
   - Потрібно буде дублювати або виносити в shared library

---

## 3. Порівняльний аналіз

### 3.1 Таблиця порівняння

| Критерій | Modulith | Fitnet |
|----------|----------|---------|
| **Структура проєкту** | Багато проєктів (модуль = проєкт) | Один проєкт (модуль = namespace) |
| **Bounded Contexts** | Explicit (через проєкти) | Explicit (через namespaces + schemas) |
| **Aggregates** | Присутні (з недоліками) | Присутні (правильно реалізовані) |
| **Value Objects** | Присутні (Money, Address, FullName) | Відсутні (тільки примітиви) |
| **Domain Events** | Присутні (DomainEventBase + TxBehavior) | Присутні (In-Memory EventBus) |
| **Business Rules** | FluentValidation в Application шарі | Явний Business Rules Engine в Domain |
| **Repository Pattern** | Повний (Ardalis IRepository/IReadRepository) | Тільки Passes (після рефакторингу) |
| **Clean Architecture** | Повна (окремі шари в кожному модулі) | Спрощена (все в одному проєкті) |
| **Domain Model** | Середній (є агрегати, але логіка в handlers) | Rich (бізнес-логіка в entities) |
| **Vertical Slices** | Відсутні (класичні шари) | Присутні |
| **Module Communication** | MediatR + Contracts проєкти | Event-Driven (In-Memory) |
| **Database Isolation** | DbContext per module | Schema per module |
| **Migration Readiness** |  Середня | Висока |
| **Compile-Time Safety** | Вища (проєкти) | Нижча (namespaces) |
Побудовано разом з чатом дана порівняльна табличка 
### 3.2 Сильні сторони кожного підходу

**Modulith:**
- Чіткі межі модулів на рівні compile-time
- Неможливо створити неконтрольовані залежності між модулями
- Explicit Contracts між модулями
- Легко екстрагувати модуль в окремий проєкт/сервіс

**Fitnet:**
- Rich Domain Model з явними Business Rules
- Event-Driven Architecture 
- Vertical Slice Architecture 
- Краще дотримання DDD принципів

### 3.3 Слабкі сторони

**Modulith:**
- Бізнес-логіка переважно в Application шарі (handlers), а не в Domain entities
- Baskets домен залежить від Products.Contracts — порушення ізоляції домену
- AddOrderCommand знаходиться в Orders.Contracts — будь-хто може створити замовлення напряму
- Users модуль незавершений (немає endpoints, немає CQRS)

**Fitnet:**
- In-Memory Event Bus проблема для архітектури 
- Можливість створити залежності між модулями
- Відсутність Repository abstractions

---

## 4. Висновки та рекомендації

### 4.1 Загальні висновки

**Modulith** демонструє **класичний підхід** до побудови модульного моноліту з акцентом на:
- Структурну організацію через проєкти
- Compile-time безпеку
- Явні контракти між модулями

Однак має недоліки:
- Бізнес-логіка сконцентрована в handlers, а не в доменних entities
- Cross-module coupling через домен (Baskets ↔ Products)
- Users модуль незавершений

**Fitnet** демонструє **прогресивний підхід** з акцентом на:
- Domain-Driven Design best practices
- Event-Driven Architecture
- Vertical Slice Architecture
- Простоту та швидкість розробки

Однак має компроміси:
- Відсутність compile-time контролю залежностей
- In-memory Event Bus (не для production critical systems)

### 4.2 Рекомендації для Modulith

#### Критичні покращення:

1. **Збагатити Domain Model:**
   ```csharp
   // Замість public setters:
   public string? Code { get; set; }
   
   // Використовувати:
   private string _code;
   public string Code => _code;
   
   public void UpdateCode(string newCode)
   {
       ValidateCode(newCode);
       _code = newCode;
   }
   ```

2. **Впровадити Value Objects:**
   ```csharp
   public record OrderCode(string Value)
   {
       public OrderCode(string value) 
       {
           if (string.IsNullOrWhiteSpace(value))
               throw new ArgumentException("Order code cannot be empty");
           Value = value;
       }
   }
   ```

3. **Додати Domain Events:**
   ```csharp
   public class Order : EntityBase, IAggregateRoot
   {
       private readonly List<IDomainEvent> _domainEvents = new();
       public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
       
       public void AddDomainEvent(IDomainEvent domainEvent)
       {
           _domainEvents.Add(domainEvent);
       }
   }
   ```

4. **Переписати Repository Interfaces у Domain:**
   ```csharp
   namespace Modulith.Modules.Orders.Domain;
   
   public interface IOrderRepository
   {
       Task<Order?> GetByIdAsync(Guid id);
       Task AddAsync(Order order);
   }
   
   namespace Modulith.Modules.Orders.Infrastructure;
   
   internal class OrderRepository : IOrderRepository
   {
   }
   ```

5. **Прибрати ViewModels з Domain:**
   - Використовувати mapping для проекцій

### 4.3 Рекомендації для Fitnet

#### Покращення для production:

1. **Замінити In-Memory Event Bus:**
   ```csharp
   public class OutboxMessage
   {
       public Guid Id { get; set; }
       public string Type { get; set; }
       public string Payload { get; set; }
       public DateTime OccurredOn { get; set; }
       public DateTime? ProcessedOn { get; set; }
   }
   ```

2. **Додати Architecture Tests:**
   ```csharp
   [Fact]
   public void Modules_Should_Not_Have_Circular_Dependencies()
   {
       var result = Types.InAssembly(typeof(Program).Assembly)
           .That().ResideInNamespace("Fitnet.Contracts")
           .ShouldNot().HaveDependencyOn("Fitnet.Offers")
       
       result.IsSuccessful.Should().BeTrue();
   } //приклад
   ```

3. **Впровадити Repository Pattern:**
   ```csharp
   public interface IContractRepository
   {
       Task<Contract?> GetByIdAsync(Guid id);
       Task<Contract?> GetByCustomerIdAsync(Guid customerId);
       Task AddAsync(Contract contract);
   }
   ```

4. **Додати Value Objects:**
   ```csharp
   public record CustomerId(Guid Value)
   {
       public static CustomerId New() => new(Guid.NewGuid());
   }
   
   public record ContractDuration
   {
       public TimeSpan Value { get; }
       
       private ContractDuration(TimeSpan value) => Value = value;
       
       public static ContractDuration Standard() => 
           new(TimeSpan.FromDays(365));
   }
   ```

5. **Підготувати до масштабування команди:**
   - Розглянути перехід на багато проєктів при зростанні
   - Впровадити ArchUnit або NetArchTest для контролю архітектури

### 4.5 Міграція до мікросервісів

**Fitnet готовіший до міграції, тому що:**
- Event-Driven Communication вже налаштована
- Bounded Contexts чітко визначені
- Кожен модуль має власну схему БД
- Vertical Slices легко екстрагувати

**Кроки для обох:**

1. **Замінити In-Memory Events на Message Broker:**
   - RabbitMQ/Kafka/Azure Service Bus
   - Впровадити Outbox/Inbox patterns

2. **Розділити бази даних:**
   - Modulith: вже має окремі DbContexts
   - Fitnet: вже має окремі schemas (легко розділити)

3. **Екстрагувати модуль в сервіс:**
   - Почати з найменш пов'язаного (наприклад, Reports)
   - Залишити API Contracts

4. **Впровадити Saga Pattern:**
   - Замінити ACID транзакції на eventual consistency
   - Compensating transactions для rollback

### 4.6 Фінальна оцінка

**Modulith:**
- DDD/Clean Architecture: **7/10** (хороша структура з Value Objects та CQRS, але бізнес-логіка не в домені)
- Migration Readiness: **7/10** (добра структура, але потрібна async comm)
- Team Scalability: **9/10** (відмінно для великих команд)

**Fitnet:**
- DDD/Clean Architecture: **9/10** (відмінна реалізація DDD)
- Migration Readiness: **8/10** (готовий, потрібен production-ready event bus)
- Team Scalability: **6/10** (підходить для невеликих команд)

Для оформлення завдання використоно чат джпт 
---