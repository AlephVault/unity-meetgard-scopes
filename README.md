# Unity Meetgard.Scopes

This package contains an object synchronization and world partitioning mechanism based on Meetgard.

# Install

This package is not available in any UPM server. You must install it in your project like this:

1. In Unity, with your project open, open the Package Manager.
2. Either refer this GitHub project: https://github.com/AlephVault/unity-meetgard-scopes.git or clone it locally and refer it from disk.
3. Also, the following packages are dependencies you need to install accordingly (in the same way and also ensuring all the recursive dependencies are satisfied):

     - https://github.com/AlephVault/unity-support-generic.git
     - https://github.com/AlephVault/unity-meetgard.git

# Usage

This package depends heavily on Meetgard. Read that [package's docs](https://github.com/AlephVault/unity-meetgard.git) before coming here.

Scopes are a mean to create "rooms" (or stuff that works like... rooms) in a game or application. The intention of a room is that certain things can be chosen to be notified / broadcast to a room instead of the entire server. Some things about the rooms are:

1. Rooms are templated. They are each an independent game object created statically (when the server starts) or dynamically (on demand).
2. The users define the templates in design time and, for both each statically-instantiated room or dynamically-instantiated room, a template object (a prefab, essentially, but with certain behaviours) must be set up __both in the client and the server__.
3. Connections can be added to scopes and removed from scopes. They can belong only to one scope at a time. Clients are notified when they're taken from a scope and/or put into another scope.
4. There are special scopes named "Limbo" and "Maintenance". The server does not send any object's notification there, but the client is totally able to tell whether they're in Limbo, Maintenance or any other scope.

There's also a notion of "objects" here. They're not standard Unity objects, but objects that account for some information being conveyed from servers to clients. There are some key points about these objects:

1. Objects are templated. Templating objects involves, always, dynamic loads, and they're typically instantiated after the scopes.
2. Typically, objects belong to scopes. Once there, changes made to them (under specific criteria) will be notified to the clients.
3. Also, data about these objects are fully notified to the clients in a scope when the object enters the scope. Clients in the scope are also notified when the objects leave the scope.
4. Also, when a client enters a scope, it gets notified about _all the objects_ in the scope.
5. Objects can be, at certain time, out of every possible scope. The object exists and there's no error here, but it will never be notified to the clients. Under certain conditions, this might be desirable.

So, pretty much, this is what happens:

1. A priori, clients connect to the server, which was already loaded and has some scopes loaded as well.
2. Then, clients can be moved to one scope or another. Clients cannot be on "no scope". When that happens, they go to "Limbo".
3. While in a scope, clients get notified of all the objects (**not** all the connections) in that scope.

This package defines no particular way to interact with those objects, but only the notion of the objects being notified to the clients.

The sections here will describe the whole life-cycle and the involved objects.

## Involved protocols

The in-scene `NetworkServer` object must add a `ScopesProtocolServerSide` protocol. Also, the in-scene `NetworkClient` object must add the corresponding `ScopesProtocolClientSide` at the same in-object position.

Those classes are located at `AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server` and `AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Client` respectively.

This document will presume that those components will be properly added to those respective objects from now on.

## Creating the Scopes

As said in the previous sections, Scopes are a fancy way to describe what we usually know as "rooms" in most games. A single client connection will only belong to one of those rooms.

### Creating the scope assets

The first thing to do is to define the scope _prefabs_. The first one is the _server side_:

1. Create, in scene, an object as you please. Add, on it, whatever behaviour is conceived as necessary.
2. This one will be the "server side" of a scope. Add the `ScopeServerSide` component to it (at `AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server`).
3. Drag it anywhere you want in the project view to store it as a prefab asset (e.g. into `Assets/Objects`).
4. Delete the in-scene instance, keeping the prefab only.

The next one is the _client side_:

1. Create, in scene, an object as you please. Add, on it, whatever behaviour is conceived as necessary.
2. This one will be the "client side" of a scope. Add the `ScopeClientSide` component to it (at `AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Client`).
3. Drag it anywhere you want in the project view to store it as a prefab asset (e.g. into `Assets/Objects`).
4. Delete the in-scene instance, keeping the prefab only.

Now, the template is ready (perhaps more behaviours are needed? that's a per-game question, but so far it's ready as a bare scope at least).

### Installing the scopes in the protocol

Now that both client and server parts of a scope are defined, add them in the respective protocol sides (client and server) _in the same list and position_.

In the client object, ensure the `NetworkClient` (and other dependencies) is added and then these components in order:

1. The `ZeroProtocolClientSide` component. Configure it properly (e.g. "Don't Destroy" to True).
2. The `ScopesProtocolClientSide` component.

In the server object, ensure the `NetworkServer` (and other dependencies) is added and then these components in order:

1. The `ZeroProtocolServerSide` component. Configure it properly (e.g. "Don't Destroy" to True).
2. The `ScopesProtocolServerSide` component.

Once these elements are set, it's time to configure the _scopes_. In the previous steps, it was (or must have been) guaranteed that each scope is defined in pairs: client and server sides. So now it's time to use them:

1. In the client object, in its `ScopesProtocolClientSide`, add your scopes' client side objects to the `Default Scope Prefabs` array.
2. In the server object, in its `ScopesProtocolServerSide`, do the same with the scopes' server side objects.
3. **Ensure the corresponding objects are added in the same order on each side** in both client and server protocols. Since each scope is created by pairs of client/server object, ensure both parts of a same scope are added respectively but at the same index on each client/server protocol.

There, the _default scope prefabs_ are set (a later section will describe how to use the _extra scope prefabs_). An important note about these default scopes is their life-cycle:

1. When the server starts, the default scopes are instantiated one by one and registered.
2. When the server stops, those instantiated scopes are released.
3. Scopes (default or extra) have callbacks that tell the users when they're being loaded or being unloaded. Custom logic can/should be added, in extra behaviours attached to the scopes to attend those events and react properly.

### Configuring the scopes

The `ScopeClientSide` component does not need an extra configuration, but it's up to the user to add new behaviours and actual game logic.

Almost the same can be said about the `ScopeServerSide` component, save for one property: These objects have an optional `Prefab Key` property, which must be unique and will only relate to `extra scopes` (which will be detailed later).

It must be remembered that scopes by themselves _do not synchronize custom data from the server_, but any additional Meetgard-based logic can be used to synchronize data. This means that this typically occurs:

1. When entering a scope in the server, a matching client-side scope will be loaded in the client (any former scope will be deleted).
2. Then, all the in-server objects will be recognized and loaded in the client side. Objects will be detailed later but, essentially, the same logic applies: clients have matching client-side objects that correspond to the objects in the server-side, but with less logic and more visuals instead.
3. Additionally from that, _nothing else is synchronized from the server_. The scope might have its own detail and objects matching from the server into the clients, and the clients must have all the relevant information to represent the scope.
   1. If, by chance, there's something else to synchronize, there are particular events that can be implemented to detect a connection entering a server-side scope and then doing whatever is needed (e.g. manually sending a custom message that synchronizes more data).

## Creating the objects

### Creating the objects assets

This part is tricky. Object assets must also be properly defined with a client-side part and a server-side part.

While scopes have their load/unload life-cycle, the objects have a different life-cycle:

1. Objects can be _spawned_ at any time after the server is started. Already in-scope clients will be aware of it immediately.
2. Objects can be _refreshed_ at any time after they're spawned. Already in-scope clients will be aware of it immediately.
3. Objects can be _de-spawned_ at any time after they're spawned. Already in-scope clients will be aware of it immediately.
4. When a client becomes in-scope, it will detect all the in-scope spawned objects with their respectively most-recent refreshed data.

In order to create an object asset, the process is similar, yet requires _more work_:

1. Create the in-scene object.
2. Add the server-side Unity logic it needs via custom behaviours.
3. Create a subclass of `AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server.ObjectServerSide` relevant to the object (unless a suitable class was defined for other object(s)).
   1. There are many methods, that will be explained in this section, that must be implemented in the new subclass.
4. Drag it anywhere you want in the project view to store it as a prefab asset (e.g. into `Assets/Objects`).
5. Delete the in-scene instance, keeping the prefab only.

And similar for the client:

1. Create the in-scene object.
2. Add the client-side Unity logic it needs via custom behaviours.
3. Create a subclass of `AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Client.ObjectClientSide` relevant to the object (unless a suitable class was defined for other object(s)).
   1. There are many methods, that will be explained in this section, that must be implemented in the new subclass.
4. Drag it anywhere you want in the project view to store it as a prefab asset (e.g. into `Assets/Objects`).
5. Delete the in-scene instance, keeping the prefab only.

Users do not need to explicitly keep track of spawned objects, as they don't need so call explicit things on (default) scopes.

This will be explained in later sections.

#### Subclassing `ObjectServerSide` and `ObjectClientSide`

Defining a subclass of `ObjectServerSide` involves defining these methods:

```csharp
using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server;

class MyObjectServerSide : ObjectServerSide 
{
    public List<Tuple<HashSet<ulong>, ISerializable>> FullData(HashSet<ulong> connections) 
    {
        // The return value is a list of groupings of the connections
        // and the data packet that will be sent respectively to each
        // group.
        //
        // This happens when the object sends some public data to the
        // users in general, but some particular data to a reduced
        // group of objects.
        //
        // By default, one might have no special concesions and just
        // return the same data to all the connections, so this is a
        // useful and default implementation:
        return new List<Tuple<HashSet<ulong>, ISerializable>>() 
        {
            new Tuple<HashSet<ulong>, ISerializable>(connections, new SomeISerializableClass() 
            {
                SomeField = SomeData,
                ...
            });
        }
    }
    
    public List<Tuple<HashSet<ulong>, ISerializable>> RefreshData(HashSet<ulong> connections, string context) 
    {
        // The return value is similar here, but the data is intended
        // to NOT be complete. The idea is the following:
        //
        // 1. The context is an arbitrary string and should tell an
        //    idea of which data is to be sent.
        // 2. The data to send will not be complete. Mandatory, will
        //    ALWAYS BE OF THE SAME TYPE, but with many fields in null.
        //
        // An example implementation sending the same conditional updates
        // to all the connections comes like this:
        SomeISerializableClass what = null;
        switch(context) 
        {
            case "foo":
                what = new SomeISerializableClass() { Foo = SomeFooValue };
                break;
            case "bar":
                what = new SomeISerializableClass() { Bar = SomeBarValue };
                break;
            default:
                what = new SomeISerializableClass() 
                {
                    /* At your criteria - perhaps nothing here, or perhaps a default "full" initialization */
                };
                break;
        }
        return new List<Tuple<HashSet<ulong>, ISerializable>>() 
        {
            new Tuple<HashSet<ulong>, ISerializable>(connections, what);
        }
    }
    
    public ISerializable FullData(ulong connection) 
    {
        // Use this method like the previous ones but to notify the
        // full contents to a single connection. This one will most
        // likely be a constant return w.r.t the argument, like this:
        return new SomeISerializableClass() 
        {
            SomeField = SomeData,
            ...
        };
    }
    
    public ISerializable RefreshData(ulong connection, string context) 
    {
        // Use this method like the previous ones but to notify some
        // perhaps context-depending partial contents, like this:
        switch(context) 
        {
            case "foo":
                return new SomeISerializableClass() { Foo = SomeFooValue };
            case "bar":
                return new SomeISerializableClass() { Bar = SomeBarValue };
            default:
                return new SomeISerializableClass() 
                {
                    /* At your criteria - perhaps nothing here, or perhaps a default "full" initialization */
                };
        }
    }
}
```

While, for the client, the overrides will look like this:

```csharp
using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server;
using System.IO;
using AlephVault.Unity.Binary;

// Let's get or create a MyType object.

// Let's make a Write-Serializer.
class MyObjectClientSide : ObjectServerSide 
{
    protected void ReadSpawnData(byte[] data) 
    {
        // Given an array of bytes, the idea here is to read
        // CONSISTENTLY the full data sent by the object. For
        // simplicity, this process is pretty much constant
        // but a `Serializer` must be used according to the
        // mechanics in `unity-binary` for that purpose. Also,
        // the class to use should match the server's or at
        // least have the same mechanism of serialization.
        SomeISerializableClass myObj = new SomeISerializableClass();
        Buffer stream = new Buffer(data);
        Serializer serializer = new Serializer(new Reader(stream));
        serializer.Serialize(myObj);
        // Then, do something affecting myObj.
    }
    
    protected ISerializable ReadRefreshData(byte[] data)
    {
        // Here, the refresh is NOT context-aware. The data
        // might come with some null fields or some custom
        // attributes telling what's being refreshed and
        // what's not.
        //
        // The implementation must parse it straightly, like
        // this implementation.
        SomeISerializableClass myObj = new SomeISerializableClass();
        Buffer stream = new Buffer(data);
        Serializer serializer = new Serializer(new Reader(stream));
        serializer.Serialize(myObj);
        return myObj;
        // In the end, the returned object will be forwarded
        // to the .OnRefreshed(ISerializable myObj).
    }
}
```

So this is the first part: __the client-side and server-side must both define these respective methods__.

However, as it can easily be spotted, there's a lot of boilerplate to consider here, especially related to using the `unity-binary` features. Fortunately, there's a better and easier convenience alternative.

#### Alternate convenient subclasses: `ModelServerSide` and `ModelClientSide`

There are two convenience classes that can be used to have a better _templated_ management of the spawn/refresh code.

1. `AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Client.ModelClientSide`, a convenience better than `ObjectClientSide`.
2. `AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server.ModelServerSide`, a convenience better than `ObjectServerSide`.

The purpose is _the exact same_ of the previous classes, but with convenience implementations that might be slightly more restrictive to some extent. For example, before everything, it must be understood that now each subclass works, respectively, _with a fixed ISerializable class_.

While there's still room for some black _magick_ in the ObjectServerSide / ObjectClientSide pair, those tricks are still hard to implement and are not necessarily that worth the effort. Typically, under very serious needs, it's better to use these convenience classes.

Using these classes _still involves implementing abstract methods_. For the server side class(es) it looks like this:

```csharp
using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server;
using AlephVault.Unity.Binary;

class MyModelServerSide : ModelServerSide<SomeISerializableClass> 
{
    protected SomeISerializableClass GetRefreshData(ulong connection, string context) 
    {
        // This is the same logic here: Gven a connection and a context,
        // return the partial object to be updated.
        switch(context) 
        {
            case "foo":
                return new SomeISerializableClass() { Foo = SomeFooValue };
            case "bar":
                return new SomeISerializableClass() { Bar = SomeBarValue };
            default:
                return new SomeISerializableClass()
                {
                    /* At your criteria - perhaps nothing here, or perhaps a default "full" initialization */
                };
        }
    }
    
    protected abstract SpawnType GetFullData(ulong connection) 
    {
        // This is the same logic: For a connection here, return the
        // data. Although it'd be typically the same object for each
        // input connection.
        return new SomeISerializableClass() 
        {
            // ... a complete initialization here ...
        }
    }
    
    // The other methods are already implemented. For example: If always
    // returning the _same_ object instance, then it'll be grouped for
    // all the connections to have the same. Grouping is automatically
    // done by object instance / identity.
}
```

And for the client classes looks like this:

```csharp
using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Client;
using AlephVault.Unity.Binary;

class MyModelServerSide : ModelClientSide<SomeISerializableClass> 
{
    protected void UpdateFrom(RefreshType refreshData)
    {
        // Implement the logic to fully update the object
        // from partial data, properly detecting the missing
        // or null fields.
    }
    
    protected void InflateFrom(SpawnType fullData)
    {
        // Implement the logic to fully update the object
        // from full data.
    }
}
```

With this, the objects are ready to be synchronized when they're used.

**Hint**: The prefabs can have a `Prefab Key`. When settings this, prefabs where such field is set can also be referenced by their `Prefab Key` (the idea is to set a useful name there).

### Installing the objects in the protocol

Once both server-side object prefabs and client-side objects prefabs are there, it's time to install them. This have to be done _for each pair of objects_:

1. Having the client-side prefab of the object, add it to the client object: in the `ScopesProtocolClientSide` component, in the `Object Prefabs` property, at a new index.
2. Having the server-side prefab of the object, add it to the server object: in the `ScopesProtocolServerSide` component, in the `Object Prefabs` property, at a new index.

Also, _remember both indices_ (in this example: just 0 and 1) for they'll be useful when creating. __The indices must always match__ between client and server for each single object.

## Bonus: Creating extra scopes

The server and client sides can define `extra scopes` (by adding matching scope objects at the same indices). As long as matching objects are defined on each bound at the same index, they can be used to be loaded as dynamic scopes.

Registered extra scopes' prefabs **must** have a value set in their `Prefab Key` (in the server-side) property before being registered as extra scopes. Also, **the values must be unique** among all the registered extra scopes prefabs. This key will be used to instantiate them.

Extra scopes can be loaded anywhere between the server being launched and the server being stopped. They can also be unloaded (and all their connections are moved to a special `Limbo` scope) freely. All the remaining loaded dynamic/extra scopes will be unloaded when the server stops.

## How to interact with the scopes and objects

### Accessing the scopes

The `ScopesProtocolServerSide` class has ways to access the scopes, which involves:

- Accessing the static scopes.
- Loading a dynamic scope.
- Accessing the dynamic scopes.
- Unloading a dynamic scope.

Let's say that `ScopesProtocolServerSide protocol` is an assigned variable of that protocol in the server side for the purpose of these examples.

To get one of the static scopes out of a given valid `index`, simply access: `ScopeServerSide scope = protocol.LoadedScopes[index];`. This is a read-only dictionary (accessing an invalid index will raise the same `KeyNotFoundException` and so).

To get one of the dynamic scopes out of a given valid `index`, the process is the same. There's no difference on where the scopes are stored, being them dynamic or static. However, each scope has some properties:

```csharp
ScopeServerSide scope = protocol.LoadedScopes[index];

// scope.PrefabKey contains a key that only makes sense for extra scopes PREFABS.
string prefabKey = scope.PrefabKey;

// scope.PrefabId contains the id of a prefab used to instantiate this scope.
// This value is meaningful for the loaded extra scopes only: it will be the
// index 0..N-1 of the extra scope prefab registered in the protocol.
// For default scopes, this value is AlephVault.Unity.Meetgard.Scopes.Types.Constants.Scope.DefaultPrefab.
// That's the canonical way to tell whether the scope is static or dynamic.
uint prefabId = scope.PrefabId;

// The id of the scope. It will be unique. If there are M registered default
// scopes in the protocol, when prefabId == ...DefaultPrefab, thus making the
// scope a default one, this value will be 0..M-1, matching the index of the
// default prefab used to instantiate it.
uint id = scope.Id;

// Tells whether the scope is a default one.
bool isDefault = scope.IsDefaultScope;

// Tells whether the scope is an extra one (complements the .IsDefaultScope).
bool isExtra = scope.IsExtraScope;

// Gets the protocol that instantiated this scope. It will match the `protocol`
// instance in this example.
ScopesProtocolServerSide protocol_ = scope.Protocol;

// Tells whether the scope is ready. A scope is ready when it's initialized.
// The initialized scope can be manipulated.
bool ready = scope.Ready;
```

In order to load and unload extra scopes, and considering that `Prefab Key` is a mandatory property in the extra prefabs, these methods can be used to load/unload them:

```csharp
// Loads a scope.
ScopeServerSide scope = await protocol.LoadExtraScope("SomePrefabKey", (scope_) => { /* Set some data on scope_ */ });

// Unloads an extra scope.
await UnloadExtraScope(scope); // Unloads and destroys the object.
await UnloadExtraScope(scope, true); // Unloads and destroys the object.
await UnloadExtraScope(scope, false); // Unloads but does not destroy the object, if for some reason is needed. The users must manually invoke Destroy(scope.gameObject) when they think it's time to.
```

Given a valid `"SomePrefabKey"` which must be valid among the `Prefab Key` of all the registered extra scope prefabs, `LoadExtraScope` picks the corresponding prefab and instantiates it. The callback to initialize the scope (second argument) is totally optional but recommended, since it allows to customize the scope before it's being added and loaded into the list of loaded scopes.

In contrast, `UnloadExtraScope` takes the duty of removing a scope from the list of loaded scopes (it essentially unloads it) and then, perhaps (and by default), destroys the entire scope game object.

#### Server-side scopes

Also, server-side scopes have some useful event that can be attended. These are mainly intended to be invoked from other behaviours in the scope object:

```csharp
public event Func<Task> OnLoad = null;
```

The `OnLoad` event is triggered when the scope is loaded (either as default or extra scope). The scope is now `Ready` and typically users would want to initialize it / create inner objects or related stuff.

```csharp
public event Func<Task> OnUnload = null;
```

The `OnUnload` is the opposite of the `OnLoad`. The idea is that by this point all the connections were just kicked (i.e. sent to Limbo) but the scope is not yet removed (but being removed). Users must ensure the logic is small and, in the meantime, the game server will not move any connection inside this scope.

```csharp
public event Func<ulong, Task> OnJoining = null;
public event Func<ulong, Task> OnLeaving = null;
public event Func<ulong, Task> OnGoodBye = null;
```

These three events are triggered while the scope is already loaded. For the three callback, the only argument is the id of the connection. The callbacks mean:

1. A new connection just joined the scope. It's registered as belonging to its new scope.
2. A connection just left the scope. By this point, _it's unsafe to try to change the scope again or the object's parent_. Just use this method to modify the object's data or something like that.
3. A connection just terminated. Make some proper cleanups here, if any.

Finally, there are object-related events:

```csharp
public event Func<ObjectServerSide, Task> OnSpawned = null;
```

The `OnSpawned` event tells that an object has just spawned into this scope. Perhaps the object already existed from other scopes, limbo, or plain initialization but the point is that it just entered this scope.

```csharp
public event Func<ObjectServerSide, Task> OnDespawned = null;
```

The `OnDespawned` event tells that the object has just de-spawned from this scope. It does necessarily mean it was destroyed, however.

Finally, scopes also have a set of methods that can be used:

```csharp
public Task AddObject(ObjectServerSide target);
public Task RemoveObject(ObjectServerSide target);
public Task RefreshExistingObject(ObjectServerSide target, string context);
public Task RefreshExistingObjectsTo(ulong connection, string context);
public IEnumerable<ObjectServerSide> Objects();
public IEnumerable<ulong> Connections(ISet<ulong> except = null);
```

`AddObject` manually changes an object which belonged to no scope at all to now belong to this scope (this also involves making the object a _child_ of the scope's transform). It is an error if the object is destroyed, not initialized, or belongs to another real scope.
`RemoveObject` manually changes an object which belonged to a scope to now have no scope (i.e. it moves it to Limbo scope and, in terms of hierarchy, moves it to the root).

However, `AddObject` and `RemoveObject` are not needed to be called explicitly: Changing the object's hierarchy (e.g. taking it from one scope and moving it to the other by calling `transform.SetParent(anotherTransform)`) will automatically trigger this behaviour if either the source is a scope or the target it.

`RefreshExistingObject` is a special method that tells, to all the connections in the same scope, that certain object (which **must** belong to the scope) is being refreshed in certain _context_. Each object knows how to generate refresh data based on a _context_ (it was detailed in earlier sections).

Finally, `RefreshExistingObjectsTo` is another special method that tells, to a single connection in the same scope, all the updates that correspond to all the objects present here in some particular context. This method is quite particular and there might be cases when it's not useful to invoke them (especially if not all the objects understand the same arbitrary context strings).

So far, these 4 methods are entirely optional and a game can be developed without invoking them.

There are two more methods here: `Objects()` is an enumerable over the current in-scope objects, and `Connections(except = null)` is an enumerable over the current in-scope connections (perhaps: except some specified connections).

__Please note__: Adding and removing objects will only work when the object belongs to the same protocol of the scope. Otherwise, weird errors will occur.

#### Client-side scopes

The client-side of each scope does not have a way to be managed (it does not make sense) but, instead, it provides a set of events to tell what's going on:

```csharp
public event Action OnLoad;
public event Action OnUnload;
```

It's first useful to have an explanation: A connection can only belong to _one and only one scope_. When a connection is moved to another scope in the server, a synchronization message is sent to the endpoint client to change to that scope.

The client, then, proceeds to unload whatever scope is loaded (if any) and then proceeds to load whatever scope is required (if any).

With this in mind, when a scope client side is loaded, it's first created and registered and _then_ the `OnLoad` event is triggered. By this point, the `OnLoad`ed scope is empty, and later it will receive all the child objects. However, by this point it receives no particular configuration: It's up to the server-side's `OnJoined`, in that case, to send custom messages to the client connection so they get how to properly refresh the just-loaded scope.

In contrast, `OnUnload` is invoked when the scope was told to unload. There's no particularly needed explanation or caveat here.

### Managing the protocols

#### Protocol life-cycle events

##### On server-side

There are some `ScopesProtocolServerSide` callbacks that are useful to be attended, regarding the life-cycle.

```csharp
public event Action<System.Exception> OnLoadError = null;
```

This `OnLoadError` is triggered when there was an error loading one of the default scopes. After this event is handled, the server will close.

```csharp
public event Action OnLoadComplete = null;
```

This `OnLoadComplete` is triggered when the set of default scopes was successfully loaded. Ensure no exception is triggered here, or the server will stop.

```csharp
public event Action<uint, ScopeServerSide, System.Exception> OnUnloadError = null;
```

This `OnUnloadError` event is triggered for one specific scope raising an error when being unloaded, passing id, scope instance and the exception.

```csharp
public event Action OnUnloadComplete = null;
```

This `OnUnloadComplete` event is triggered when the default scopes are completely loaded. Ensure no exception is triggered here.

#### Protocol connection events

##### On server-side

```csharp
public event Func<ulong, Task> OnWelcome = null;
```

This `OnWelcome` event is triggered when a new connection is established, by passing the connection id.

```csharp
public event Func<ulong, uint, Task> OnLeavingScope = null;
```

This `OnLeavingScope` event is triggered when a connection is leaving a certain scope (it can even be `Scope.Limbo` or `Scope.Maintenance`), given by its id.

```csharp
public event Func<ulong, uint, Task> OnJoiningScope = null;
```

This `OnJoiningScope` event is triggered when a connection is entering a certain scope (it can even be `Scope.Limbo` or `Scope.Maintenance`), given by its id.

```csharp
public event Func<ulong, uint, Task> OnGoodBye = null;
```

This `OnGoodbye` event is triggered when a connection is terminated, by passing the connection id.

##### On client-side

```csharp
public event Action OnWelcome;
```

This `OnWelcome` event is triggered when the connection is initialized in the scopes. It will, at first, not belong to any scope.

```csharp
public event Action<ScopeClientSide> OnMovedToScope;
```

This `OnMovedToScope` event is triggered when the server moved the current connection to a new scope. Then, the new scope was successfully loaded and this event is triggered with its instance.

```csharp
public event Action<ObjectClientSide> OnSpawned;
```

This `OnSpawned` event is triggered when an object was spawned from the server side in the same scope this connection is in. In this case, the client side for that object was also spawned in the client and this event is triggered with its instance.

```csharp
public event Action<ObjectClientSide, ISerializable> OnRefreshed;
```

This `OnRefreshed` event is triggered when an object was refreshed from the server side in the same scope this connection is in. In this case, this callback is invoked with the client-side object and the data used to refresh it.

```csharp
public event Action<ObjectClientSide> OnDespawned;
```

This `OnDespawned` event is triggered when an object was de-spawned from the server side in the same scope this connection is in. The formerly valid object (client side) is passed to this callback.

```csharp
public event Action<string> OnLocalError;
```

This `OnLocalError` event is triggered when a local error occurred in the client side. By this point, the client has closed and notified the server so the server can close its side. The string is arbitrary and just a way to inform a code of what happened.

- InvalidServerScope, ScopeMismatch and ScopeLoadError refers to some sort of mismanagement of the scope.
- SpawnError, RefreshError and DespawnError refers to some sort of mismanagement of the objects in the scope.
- Future developments by any user can use any string they want, however (those 6 strings are used by this library and are only purely informative).

#### Useful properties and methods in the protocol

##### On server-side

```csharp
// Sends a connection to a new scope.
public Task SendTo(ulong connectionId, uint newScopeId, bool force = false);

// Sends a connection to the Limbo scope.
public Task SendToLimbo(ulong connectionId);
```

`SendTo` sends a given connection (by its id) to a new scope, which can even be `Scope.Limbo` or `Scope.Maintenance`. The `force` flag is used to tell that the relevant events and changes should occur even if the current scope is the same as the target scope. An error will be triggered if the scope by the index does not exist.

`SendToLimbo` is just a convenience over `SendTo` which passes `Scope.Limbo` as argument.

##### On client-side

```csharp
public async Task<bool> RequireIsCurrentScopeAndHoldsObjects(uint scopeIndex);
```

This `RequireIsCurrentScopeAndHoldsObjects` just checks that the current scope is the one specified and, if it's not, then triggers a local error out of it and closes the connection.

```csharp
public ScopeClientSide CurrentScope { get; private set; }
```

This `CurrentScope` property tells the current scope the object is in. If `null`, it will be either in `Limbo` or `Maintenance` scopes.

```csharp
public uint CurrentScopeId { get; private set; }
```

This `CurrentScopeId` property tells the id of the current scope the object is in. It might also be `Scope.Limbo` or `Scope.Maintenance`.

### Dynamically creating objects

Objects are all of them created dynamically from the registered prefabs (_it is untested what happens when a scope being just-loaded already has instances of these registered objects prefabs in it_).

Similar to the way the `ScopesProtocolServerSide protocol` object loads extra scopes, objects can be loaded, but with some differences:

1. They're not a priori inserted into any parent scope.
2. Selecting their source prefab can be done by `Prefab Key` (only for the objects that have a key) or its prefab index (not so recommended).

```csharp
Action<ObjectServerSide> callback = (obj) => { /* An optional callback to initialize the just-created and not-yet-spawned object */ };

// Instantiate an object by the index of the prefab.
// The callback argument is optional.
ObjectServerSide obj = protocol.InstantiateHere(0, callback);

// Instantiate an object by the prefab key, if a registered prefab has that key.
// The callback argument is optional.
ObjectServerSide obj2 = protocol.InstantiateHere("somePrefabKey", callback);

// Instantiate an object by the prefab object itself, if that prefab objct is registered in the server protocol.
// The callback argument is optional.
ObjectServerSide obj2 = protocol.InstantiateHere(somePrefabReference, callback);
```

