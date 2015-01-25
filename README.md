# Database
My attempt at creating a distributed document database. Currently this is being done as an independent study project for school, but I plan on continuing this project afterwards as well.

Currently working:
- All node types
- Controller node primary voting
- Basic database operations (only on a single node and not persistant)

The database is run by 3 separate node types, controller, storage, and query.

### Controller Node

The controller node is the node that controls all management operations of the database. It handles moving data chunks around and managing all node connections.

If there are multiple controllers, then one will be marked as the primary and handle all operations while the others will be in standby in case the primary goes down. In order to prevent weird things during a network partition, a controller node has to be connected to a majority of the nodes in its connection string in order to become a primary.

### Storage Node

The storage node is the node that handles all of the storage and querying of the database chunks that it owns. The physical database, where all the data is actually stored, is spread across all of the active storage nodes. 

### Query Node

The query node is the node that handles incoming user queries and routes them to the correct storage nodes, aggregating the results if necessary.
