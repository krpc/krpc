#ifndef HEADER_KRPC_SERVICES_TEST_SERVICE
#define HEADER_KRPC_SERVICES_TEST_SERVICE

#include "krpc/service.hpp"
#include "krpc/encoder.hpp"
#include "krpc/decoder.hpp"

namespace krpc {

  namespace services{

    class TestService: public krpc::Service {
    public:

      class TestClass;

      TestService(Client* client);
      std::string float_to_string(float x);
      std::string double_to_string(double x);
      std::string int32_to_string(google::protobuf::int32 x);
      std::string int64_to_string(google::protobuf::int64 x);
      std::string bool_to_string(bool x);
      std::string bytes_to_hex_string(const std::string& x);
      google::protobuf::int32 string_to_int32(const std::string& x);
      std::string add_multiple_values(float x, google::protobuf::int32 y, google::protobuf::int64 z);

      std::string optional_arguments(std::string x, std::string y = "foo", std::string z = "bar", std::string anotherParameter = "baz");

      google::protobuf::int32 blocking_procedure(google::protobuf::int32 n, google::protobuf::int32 sum = 0);

      std::string string_property();
      void set_string_property(const std::string& x);
      std::string string_property_private_set();
      void set_string_property_private_get(const std::string& x);

      TestClass create_test_object(const std::string& x);
      TestClass echo_test_object(const TestClass& x);

      TestClass object_property();
      void set_object_property(const TestClass& x);

      void throw_argument_exception();
      void throw_invalid_operation_exception();

      class TestClass: public krpc::Object<TestClass> {
      public:
        TestClass(Client* client = NULL, google::protobuf::uint64 id = 0):
          Object(client, "TestService::TestClass", id) {}

        std::string object_to_string(const TestClass& object);
        std::string float_to_string(float x);
        std::string get_value();

        google::protobuf::int32 int_property();
        void set_int_property(google::protobuf::int32 value);

        TestClass object_property();
        void set_object_property(const TestClass& value);
      };

    };

    inline TestService::TestService(Client* client):
      Service(client) {}

    inline std::string TestService::float_to_string(float x) {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      Decoder::decode(result, client->invoke("TestService", "FloatToString", args));
      return result;
    }

    inline std::string TestService::double_to_string(double x) {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      Decoder::decode(result, client->invoke("TestService", "DoubleToString", args));
      return result;
    }

    inline std::string TestService::int32_to_string(google::protobuf::int32 x) {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      Decoder::decode(result, client->invoke("TestService", "Int32ToString", args));
      return result;
    }

    inline std::string TestService::int64_to_string(google::protobuf::int64 x) {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      Decoder::decode(result, client->invoke("TestService", "Int64ToString", args));
      return result;
    }

    inline std::string TestService::bool_to_string(bool x) {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      Decoder::decode(result, client->invoke("TestService", "BoolToString", args));
      return result;
    }

    inline google::protobuf::int32 TestService::string_to_int32(const std::string& x) {
      google::protobuf::int32 result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      Decoder::decode(result, client->invoke("TestService", "StringToInt32", args));
      return result;
    }

    inline std::string TestService::bytes_to_hex_string(const std::string& x) {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      Decoder::decode(result, client->invoke("TestService", "BytesToHexString", args));
      return result;
    }

    inline std::string TestService::add_multiple_values(float x, google::protobuf::int32 y, google::protobuf::int64 z) {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      args.push_back(Encoder::encode(y));
      args.push_back(Encoder::encode(z));
      Decoder::decode(result, client->invoke("TestService", "AddMultipleValues", args));
      return result;
    }

    inline std::string TestService::optional_arguments(std::string x, std::string y, std::string z, std::string anotherParameter) {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      args.push_back(Encoder::encode(y));
      args.push_back(Encoder::encode(z));
      args.push_back(Encoder::encode(anotherParameter));
      Decoder::decode(result, client->invoke("TestService", "OptionalArguments", args));
      return result;
    }

    inline google::protobuf::int32 TestService::blocking_procedure(google::protobuf::int32 n, google::protobuf::int32 sum) {
      google::protobuf::int32 result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(n));
      args.push_back(Encoder::encode(sum));
      Decoder::decode(result, client->invoke("TestService", "BlockingProcedure", args));
      return result;
    }

    inline std::string TestService::string_property() {
      std::string result;
      Decoder::decode(result, client->invoke("TestService", "get_StringProperty"));
      return result;
    }

    inline void TestService::set_string_property(const std::string& x) {
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      client->invoke("TestService", "set_StringProperty", args);
    }

    inline std::string TestService::string_property_private_set() {
      std::string result;
      Decoder::decode(result, client->invoke("TestService", "get_StringPropertyPrivateSet"));
      return result;
    }

    inline void TestService::set_string_property_private_get(const std::string& x) {
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      client->invoke("TestService", "set_StringPropertyPrivateGet", args);
    }

    inline TestService::TestClass TestService::create_test_object(const std::string& x) {
      TestService::TestClass result(client);
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      Decoder::decode(result, client->invoke("TestService", "CreateTestObject", args));
      return result;
    }

    inline TestService::TestClass TestService::echo_test_object(const TestService::TestClass& x) {
      TestService::TestClass result(client);
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      Decoder::decode(result, client->invoke("TestService", "EchoTestObject", args));
      return result;
    }

    inline TestService::TestClass TestService::object_property() {
      TestService::TestClass result(client);
      Decoder::decode(result, client->invoke("TestService", "get_ObjectProperty"));
      return result;
    }

    inline void TestService::set_object_property(const TestService::TestClass& x) {
      std::vector<std::string> args;
      args.push_back(Encoder::encode(x));
      client->invoke("TestService", "set_ObjectProperty", args);
    }

    inline void TestService::throw_argument_exception() {
      client->invoke("TestService", "ThrowArgumentException");
    }

    inline void TestService::throw_invalid_operation_exception() {
      client->invoke("TestService", "ThrowInvalidOperationException");
    }

    inline std::string TestService::TestClass::object_to_string(const TestClass& object) {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(*this));
      args.push_back(Encoder::encode(object));
      Decoder::decode(result, client->invoke("TestService", "TestClass_ObjectToString", args));
      return result;
    }

    inline std::string TestService::TestClass::float_to_string(float x) {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(*this));
      args.push_back(Encoder::encode(x));
      Decoder::decode(result, client->invoke("TestService", "TestClass_FloatToString", args));
      return result;
    }

    inline std::string TestService::TestClass::get_value() {
      std::string result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(*this));
      Decoder::decode(result, client->invoke("TestService", "TestClass_GetValue", args));
      return result;
    }

    inline google::protobuf::int32 TestService::TestClass::int_property() {
      google::protobuf::int32 result;
      std::vector<std::string> args;
      args.push_back(Encoder::encode(*this));
      Decoder::decode(result, client->invoke("TestService", "TestClass_get_IntProperty", args));
      return result;
    }

    inline void TestService::TestClass::set_int_property(google::protobuf::int32 value) {
      std::vector<std::string> args;
      args.push_back(Encoder::encode(*this));
      args.push_back(Encoder::encode(value));
      client->invoke("TestService", "TestClass_set_IntProperty", args);
    }

    inline TestService::TestClass TestService::TestClass::object_property() {
      TestService::TestClass result(client);
      std::vector<std::string> args;
      args.push_back(Encoder::encode(*this));
      Decoder::decode(result, client->invoke("TestService", "TestClass_get_ObjectProperty", args));
      return result;
    }

    inline void TestService::TestClass::set_object_property(const TestService::TestClass& value) {
      std::vector<std::string> args;
      args.push_back(Encoder::encode(*this));
      args.push_back(Encoder::encode(value));
      client->invoke("TestService", "TestClass_set_ObjectProperty", args);
    }

  }
}

#endif
