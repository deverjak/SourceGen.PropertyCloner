# PropertyCloner Generator

A C# source generator that automatically generates clone methods for your classes.

## Features

- Generates extension methods for cloning objects
- Selective property cloning using attributes
- Supports inheritance (clones marked properties from base classes)
- Zero runtime reflection - all code is generated at compile time

## Installation

1. Add the PropertyCloner generator to your project:

```xml
<PackageReference Include="PropertyCloner.Generator" Version="1.0.0" />
```

## Usage

### 1. Mark your class with `[PropertyCloner]` attribute:

```csharp
[PropertyCloner]
public class Person
{
    [Clonable] public string Name { get; set; }
    [Clonable] public int Age { get; set; }
    public string InternalId { get; set; }  // Won't be cloned
}
```

### 2. Mark properties you want to clone with `[Clonable]` attribute

### 3. Use the generated Clone() extension method:

```csharp
var person = new Person { Name = "John", Age = 30, InternalId = "123" };
var clone = person.Clone();
```

## Inheritance Support

The generator supports cloning properties from base classes:

```csharp
public class BaseEntity
{
    [Clonable] public int Id { get; set; }
    [Clonable] public DateTime CreatedAt { get; set; }
}

[PropertyCloner]
public class User : BaseEntity
{
    [Clonable] public string Username { get; set; }
    public string Password { get; set; }  // Won't be cloned
}
```

When cloning a `User` object, properties marked as `[Clonable]` from both `User` and `BaseEntity` will be copied.

## Limitations

- Only supports simple property cloning (no deep cloning of complex objects)
- Properties must have both getter and setter
- Collections are cloned by reference, not deep copied
- All properties must be accessible from the generated code
