import { connect } from "@krpc-org/krpc";

const conn = await connect();
const { Expression, Type } = conn.krpc;

// let total = 0;
// for (const x of [1, 2, 3])
//   switch (x) {
//     case 1: case 2: total = total + 10; break;
//     default: total = total + 1; break;
//   }
const total = await Expression.variable("total", await Type.int());
const x = await Expression.variable("x", await Type.int());
const values = await Expression.createList([
  await Expression.constantInt(1),
  await Expression.constantInt(2),
  await Expression.constantInt(3),
]);
const body = await Expression.match(
  x,
  [[await Expression.constantInt(1), await Expression.constantInt(2)], []],
  [
    await Expression.assign(
      total,
      await Expression.add(total, await Expression.constantInt(10)),
    ),
    await Expression.assign(
      total,
      await Expression.add(total, await Expression.constantInt(1)),
    ),
  ],
);
const program = await Expression.blockWithVariables(
  [total, x],
  [
    await Expression.assign(total, await Expression.constantInt(0)),
    await Expression.forEach(x, values, body),
    total,
  ],
);

console.log(await conn.run(program));
