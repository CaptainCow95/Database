# Database [![Build Status](https://travis-ci.org/CaptainCow95/Database.svg)](https://travis-ci.org/CaptainCow95/Database)
A distributed document-based database.

Currently working:
- All node types
- Controller node primary voting
- Basic database operations (not persistant, RAM only)
- Database will move chunks of itself around to maintain a balance on all storage nodes.

TODO:
- Fix sync issues with non-primary controller nodes
- Add replica of the chunks, so if a storage node goes down, the data can be recovered
- Some sort of operation log to assist in replication, probably similar in nature to MongoDB's oplog
- indexes for query operations

The database is run by 3 separate node types, controller, storage, and query.

### Controller Node

The controller node is the node that controls all management operations of the database. It handles moving data chunks around and managing all node connections.

If there are multiple controllers, then one will be marked as the primary and handle all operations while the others will be in standby in case the primary goes down. In order to prevent weird things during a network partition, a controller node has to be connected to a majority of the nodes in its connection string in order to become a primary.

### Storage Node

The storage node is the node that handles all of the storage and querying of the database chunks that it owns. The physical database, where all the data is actually stored, is spread across all of the active storage nodes. 

### Query Node

The query node is the node that handles incoming user queries and routes them to the correct storage nodes, aggregating the results if necessary.

## Usage

In order for the database to be used, it requires that at least one controller node, query node, and storage node be active. The easiest way is to simply startup and shutdown each node individually once, so that they generate a default config file. Once the programs are configured, start them all up and they should automatically connect to eachother and begin functioning. The settings for the nodes are as follows:

#### Controller Node Settings
- <b>ConnectionString:</b> A comma separated list of all the controller's nodes names and ports, in the format "name:port". The names used here are the DNS names or IP addresses that all nodes will use to try to connect to the controllers. <b>This must be the exact same string for every node that tries to join the network.</b>
- <b>Port:</b> The port this controller node will run on. The controller will automatically look-up it's name from the ConnectionString.
- <b>WebInterfacePort:</b> The port that the web interface will run on. Simply point a web browser at this port to view the page. At this point in time, there is no way to disable this, but it will only be accessible outside of localhost if the controller program is run as an administrator.
- <b>MaxChunkItemCount:</b> The maximum number of items allowed in a chunk before the database will attempt to split it into 2 separate chunks.
- <b>RedundantNodesPerLocation:</b> (Planned, not yet implemented) The number of replicas for each chunk of the database for each location defined by the storage nodes.
- <b>LogLevel:</b> Denotes the detail level of the logging. The options from least logging to most logging are as follows: Error, Warning, Info, Debug.

#### Storage Node Settings
- <b>ConnectionString:</b> A comma separated list of all the controller's nodes names and ports, in the format "name:port". The names used here are the DNS names or IP addresses that all nodes will use to try to connect to the controllers. <b>This must be the exact same string for every node that tries to join the network.</b>
- <b>NodeName:</b> The DNS name or IP address of this node, so that other nodes can connect to it.
- <b>Port:</b> The port this node will run on.
- <b>Location:</b> (Planned, not yet implemented) The location of the storage node, used by the controllers in combination with their RedundantNodesPerLocation setting.
- <b>CanBecomePrimary:</b> (Planned, not yet implemented) A true or false value that indicates whether this storage node can become a primary storage node, or if it must always remain a replica for all of the chunks that it contains.
- <b>Weight:</b> (Not yet implemented, trivial to do) The weight of the storage node compared to other nodes. This will determine how many chunks are stored on this node compared to others. For example, a node with a weight of 2 will contain roughly twice as many chunks as a node with a weight of 1 when the database is fully balanced.
- <b>LogLevel:</b> Denotes the detail level of the logging. The options from least logging to most logging are as follows: Error, Warning, Info, Debug.

#### Query Node Settings
- <b>ConnectionString:</b> A comma separated list of all the controller's nodes names and ports, in the format "name:port". The names used here are the DNS names or IP addresses that all nodes will use to try to connect to the controllers. <b>This must be the exact same string for every node that tries to join the network.</b>
- <b>NodeName:</b> The DNS name or IP address of this node, so that other nodes can connect to it.
- <b>Port:</b> The port this node will run on.
- <b>LogLevel:</b> Denotes the detail level of the logging. The options from least logging to most logging are as follows: Error, Warning, Info, Debug.
