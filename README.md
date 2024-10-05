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

Extra scopes can be loaded anywhere between the server being launched and the server being stopped. They can also be unloaded (and all their connections are moved to a special `Limbo` scope) freely. All the remaining loaded dynamic/extra scopes will be unloaded when the server stops.

## How to interact with the scopes and objects

# TODO explain this.