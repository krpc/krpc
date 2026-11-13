import { connect } from "@krpc-org/krpc";

const conn = await connect();
const MapFilterType = conn.spaceCenter.MapFilterType;

// Show only probes and ships in map mode
await conn.spaceCenter.setMapFilter(MapFilterType.probes.union(MapFilterType.ships));

// Check whether map mode shows debris
const filter = await conn.spaceCenter.getMapFilter();
console.log(filter.contains(MapFilterType.debris));

// Show debris as well
await conn.spaceCenter.setMapFilter(filter.union(MapFilterType.debris));
