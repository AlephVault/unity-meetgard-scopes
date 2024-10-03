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
