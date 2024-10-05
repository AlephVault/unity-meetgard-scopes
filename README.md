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

# TODO explain methods
# TODO explain Model subclasses

### Installing the objects in the protocol

# TODO explain here