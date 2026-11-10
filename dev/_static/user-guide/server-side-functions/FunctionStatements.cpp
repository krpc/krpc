#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <vector>

int main() {
  auto conn = krpc::connect();

  typedef krpc::services::KRPC::Expression Expr;
  typedef krpc::services::KRPC::Type KType;
  auto e = Expr::bind(conn);
  auto t = KType::bind(conn);

  // total = 0; for (auto x : {1, 2, 3}) total += x;
  auto total = e.variable("total", t.int_());
  auto x = e.variable("x", t.int_());
  auto values = e.create_list(std::vector<Expr>(
      {e.constant_int(1), e.constant_int(2), e.constant_int(3)}));
  auto program = e.block_with_variables(
      std::vector<Expr>({total, x}),
      std::vector<Expr>(
          {e.assign(total, e.constant_int(0)),
           e.for_each(x, values, e.assign(total, e.add(total, x))), total}));

  std::cout << krpc::run<int32_t>(program) << std::endl;
}
