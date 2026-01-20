# Data Model

This document describes the data structures and database schema used in CSMOO.

## Database: LiteDB

CSMOO uses LiteDB, a serverless, embedded NoSQL database. All data is stored in a single file (`gamedata.db` by default).

### Collections

The database contains the following collections:

1. `gameobjects` - GameObject instances
2. `objectclasses` - ObjectClass definitions
3. `players` - Player accounts (subset of GameObjects)
4. `verbs` - Verb definitions
5. `functions` - Function definitions

## Core Data Structures

### GameObject

The fundamental runtime object type. All game entities are GameObjects.

**Properties**:
- `Id` (string, BsonId): Unique identifier (GUID)
- `Name` (string): Human-readable name
- `Aliases` (List<string>): Alternative names
- `DbRef` (int): Numeric reference (#1, #2, etc.)
- `ClassId` (string): The object's class (via `GetType().Name`)
- `Properties` (BsonDocument): Dynamic properties
- `PropAccessors` (Dictionary<string, List<Keyword>>): Property access modifiers
- `Location` (GameObject?): Parent object/room (via Properties["location"])
- `Contents` (List<string>): Child object IDs (via Properties["contents"])
- `Owner` (GameObject?): Object owner
- `CreatedAt` (DateTime): Creation timestamp
- `ModifiedAt` (DateTime): Last modification timestamp

**Subtypes**:
- `Room`: Location objects
- `Player`: User avatars (also stored in `players` collection)
- `Item`: Items and objects
- `Container`: Objects that contain other objects
- `Exit`: Connections between rooms

**Storage**: Stored in `gameobjects` collection (Player also in `players` collection)

### ObjectClass

Class definitions (templates) that GameObjects inherit from.

**Properties**:
- `Id` (string, BsonId): Unique identifier
- `Name` (string): Class name (e.g., "Room", "Player", "Sword")
- `ParentClassId` (string?): Parent class for inheritance
- `Properties` (BsonDocument): Default properties for instances
- `PropAccessors` (Dictionary<string, List<Keyword>>): Property access modifiers
- `Methods` (BsonDocument): Class methods (deprecated)
- `Description` (string): Class description
- `IsAbstract` (bool): Whether class can be instantiated
- `CreatedAt` (DateTime): Creation timestamp
- `ModifiedAt` (DateTime): Last modification timestamp

**Storage**: Stored in `objectclasses` collection

**Inheritance**: Classes form a hierarchy. Child classes inherit properties from parent classes.

### Verb

Represents a command/action that can be executed on an object.

**Properties**:
- `Id` (string, BsonId): Unique identifier
- `ObjectId` (string): Object this verb belongs to
- `Name` (string): Verb name (e.g., "look", "get")
- `Aliases` (string): Space-separated aliases (e.g., "l examine")
- `Pattern` (string): Command pattern with variables (e.g., "* at *")
- `Code` (string): C# code to execute
- `Permissions` (string): Access level ("public", "owner", "wizard")
- `Description` (string): Verb description
- `CreatedBy` (string): Creator identifier
- `CreatedAt` (DateTime): Creation timestamp
- `ModifiedAt` (DateTime): Last modification timestamp

**Storage**: Stored in `verbs` collection

**Inheritance**: Objects inherit verbs from their class hierarchy.

### GameFunction

Represents a reusable function that can be called from scripts.

**Properties**:
- `Id` (string, BsonId): Unique identifier
- `Name` (string): Function name
- `Code` (string): C# code for function body
- `Description` (string): Function description
- `Parameters` (List<FunctionParameter>): Function parameters
- `ReturnType` (string): Return type ("void", "string", "int", etc.)
- `Permissions` (string): Access level
- `CreatedBy` (string): Creator identifier
- `CreatedAt` (DateTime): Creation timestamp
- `ModifiedAt` (DateTime): Last modification timestamp

**Storage**: Stored in `functions` collection

**Note**: There's also a `Function` class (metadata) that's separate from `GameFunction` (full definition).

### FunctionParameter

Parameter definition for functions.

**Properties**:
- `Name` (string): Parameter name
- `Type` (string): Parameter type
- `DefaultValue` (object?): Optional default value

### Player

Extends GameObject. Represents a user account.

**Additional Properties** (beyond GameObject):
- `PasswordHash` (string): Hashed password
- `SessionGuid` (Guid?): Active session identifier

**Storage**: 
- Stored in both `gameobjects` and `players` collections
- `players` collection for quick player lookups by name

## Property System

### Property Storage

Properties are stored in `BsonDocument` (LiteDB's document type):

```csharp
public BsonDocument Properties { get; set; } = new BsonDocument();
```

**Property Access**:
- Direct: `Properties["name"]` (returns `BsonValue`)
- Via API: `ObjectManager.GetProperty(obj, "name")`
- Dynamic: `obj.name` (via DynamicObject)

### Property Types

Properties can store:
- Primitive types: `string`, `int`, `bool`, `double`
- Collections: `List<T>`, arrays
- Nested objects: `BsonDocument`
- Null values

### Property Inheritance

Properties are resolved in this order:
1. **Instance properties**: Specific to individual objects
2. **Class properties**: From object's class
3. **Parent class properties**: Walk up inheritance chain

### Property Access Modifiers

Properties can have access modifiers stored in `PropAccessors`:

- `Public`: Anyone can read/write
- `Private`: Only object itself
- `Internal`: Only object's owner
- `Protected`: Only objects of same class
- `ReadOnly`: Can read, cannot write
- `WriteOnly`: Can write, cannot read

## Database Indexes

LiteDB indexes are created for performance:

**gameobjects**:
- `Id` (primary key)
- `ClassId`
- `Properties.location`
- `Properties.owner`

**objectclasses**:
- `Id` (primary key)
- `Name`

**players**:
- `Id` (primary key)
- `Name`
- `SessionGuid`

**verbs**:
- `Id` (primary key)
- `ObjectId`
- `Name`

**functions**:
- `Id` (primary key)
- `Name`

## Serialization

### LiteDB Serialization

All data classes use LiteDB's serialization:
- Properties marked with `[BsonId]` become document IDs
- Properties marked with `[BsonIgnore]` are not serialized
- All public properties are serialized by default

### Object Subtype Conversion

When loading from database:
1. Objects deserialize as `GameObject` base type
2. `ObjectManager.ConvertToSubtype()` converts to correct subtype
3. Conversion based on `Properties["classid"]` or class name

## Relationships

### Object Hierarchy

```
ObjectClass (template)
    ↓ (inherits)
GameObject (instance)
    ↓ (has location)
Location (Room/Container)
    ↓ (contains)
Contents (List<GameObject>)
```

### Ownership

```
GameObject
    ↓ (owned by)
Owner (GameObject?, usually Player)
```

### Verb Relationships

```
GameObject
    ↓ (has)
Verbs (List<Verb>)
    ↓ (inherited from)
Class Verbs
```

## Data Consistency

### Referential Integrity

CSMOO does not enforce referential integrity at the database level:
- Object references are stored as string IDs
- Missing references result in `null` lookups
- Cleanup required when deleting objects

### Transaction Safety

- LiteDB supports transactions
- Database operations wrapped in transactions where needed
- Write operations are atomic

## Migration and Schema Changes

### Schema Flexibility

LiteDB's schema-less design allows:
- Adding new properties without migration
- Properties missing on old objects are handled gracefully
- Backward compatibility with old data

### Version Management

- No explicit schema versioning currently
- Properties can be versioned via metadata
- Migration scripts possible for future use

## Performance Considerations

### Caching

- Objects cached in memory (`ObjectManager._objectCache`)
- Classes cached (`ObjectManager._objectClassCache`)
- Cache refreshed on object updates

### Query Optimization

- Indexes on frequently queried fields
- LINQ queries optimized by LiteDB
- Batch operations for multiple objects

### Memory Usage

- All objects loaded into cache at startup
- Large worlds may require significant memory
- Consider lazy loading for future optimization

## Example Data

### Example GameObject

```json
{
  "_id": "abc123",
  "Properties": {
    "name": "Sword",
    "description": "A gleaming steel sword",
    "damage": 10,
    "weight": 5,
    "location": "room-id-456",
    "dbref": 42
  },
  "CreatedAt": "2024-01-01T00:00:00Z",
  "ModifiedAt": "2024-01-01T00:00:00Z"
}
```

### Example ObjectClass

```json
{
  "_id": "class-sword",
  "Name": "Sword",
  "ParentClassId": "class-item",
  "Properties": {
    "weaponType": "melee",
    "damage": 8,
    "defaultDescription": "A sword"
  },
  "Description": "A weapon class for swords",
  "CreatedAt": "2024-01-01T00:00:00Z"
}
```

### Example Verb

```json
{
  "_id": "verb-123",
  "ObjectId": "abc123",
  "Name": "swing",
  "Pattern": "",
  "Code": "Say(\"You swing the sword!\"); return \"The sword gleams.\";",
  "Permissions": "public",
  "CreatedAt": "2024-01-01T00:00:00Z"
}
```
