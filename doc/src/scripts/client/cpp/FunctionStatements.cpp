#include <iostream>
#include <vector>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>

int main() {
  auto conn = krpc::connect();

  typedef krpc::services::KRPC::Expression Expr;
  typedef krpc::services::KRPC::Type KType;

  // total = 0; for (auto x : {1, 2, 3}) total += x;
  auto total = Expr::variable(conn, "total", KType::int_(conn));
  auto x = Expr::variable(conn, "x", KType::int_(conn));
  auto values = Expr::create_list(conn, std::vector<Expr>({
    Expr::constant_int(conn, 1),
    Expr::constant_int(conn, 2),
    Expr::constant_int(conn, 3)}));
  auto program = Expr::block_with_variables(
    conn,
    std::vector<Expr>({total, x}),
    std::vector<Expr>({
      Expr::assign(conn, total, Expr::constant_int(conn, 0)),
      Expr::for_each(conn, x, values,
        Expr::assign(conn, total, Expr::add(conn, total, x))),
      total}));

  std::cout << krpc::run_function<int32_t>(program) << std::endl;
}
