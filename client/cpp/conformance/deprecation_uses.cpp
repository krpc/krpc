// A use of each member the deprecation conformance tests name, one per line. It is only compiled,
// for the warnings the build records. See tools/build/deprecation_signals.py.
// clang-format off
#include <krpc.hpp>
#include "services/deprecated_service.hpp"
#include "services/test_service.hpp"

using krpc::Client;
using krpc::services::DeprecatedService;                                    // setup
using krpc::services::TestService;

using PartlyDeprecatedEnum = TestService::PartlyDeprecatedEnum;             // setup

// Naming a deprecated type warns as well, so the type of these members is a parameter
template <typename C>
void inherited_static_method(Client& client) {
  C::inherited_static_method(client);                                       // use: TestService.DeprecatedClass.InheritedStaticMethod
}

template <typename C>
void reasoned_static_method(Client& client) {
  C::static_method(client);                                                 // use: DeprecatedService.ReasonedClass.StaticMethod
}

template <typename C>
void service_static_method(Client& client) {
  C::static_method(client);                                                 // use: DeprecatedService.ServiceClass.StaticMethod
}

template <typename E>
void deprecated_enum_value() {
  (void)E::value_a;                                                         // use: TestService.DeprecatedEnum.ValueA
}

template <typename E>
void service_enum_value() {
  (void)E::value;                                                           // use: DeprecatedService.ServiceEnum.Value
}

void uses(Client& client,                                                   // setup
          TestService& test_service,                                        // setup
          DeprecatedService& deprecated_service,                            // setup
          TestService::DeprecatedStruct& deprecated_struct,                 // setup
          TestService::PartlyDeprecatedStruct& partly_deprecated_struct,    // setup
          TestService::TestStruct& test_struct,                             // setup
          DeprecatedService::ServiceStruct& service_struct,                 // setup
          TestService::DeprecatedClass& deprecated_class) {                 // setup
  test_service.deprecated_procedure(1);                                     // use: TestService.DeprecatedProcedure
  test_service.deprecated_property();                                       // use: TestService.DeprecatedProperty
  (void)sizeof(TestService::DeprecatedClass);                               // use: TestService.DeprecatedClass
  deprecated_class.deprecated_method();                                     // use: TestService.DeprecatedClass.DeprecatedMethod
  deprecated_class.inherited_method();                                      // use: TestService.DeprecatedClass.InheritedMethod
  inherited_static_method<TestService::DeprecatedClass>(client);            // setup
  deprecated_class.inherited_property();                                    // use: TestService.DeprecatedClass.InheritedProperty
  (void)sizeof(TestService::DeprecatedEnum);                                // use: TestService.DeprecatedEnum
  deprecated_enum_value<TestService::DeprecatedEnum>();                     // setup
  (void)PartlyDeprecatedEnum::value_a;                                      // use: TestService.PartlyDeprecatedEnum.ValueA
  (void)PartlyDeprecatedEnum::value_b;                                      // use: TestService.PartlyDeprecatedEnum.ValueB
  (void)sizeof(TestService::DeprecatedStruct);                              // use: TestService.DeprecatedStruct
  (void)deprecated_struct.value;                                            // use: TestService.DeprecatedStruct.Value
  (void)partly_deprecated_struct.value;                                     // use: TestService.PartlyDeprecatedStruct.Value
  (void)partly_deprecated_struct.old_value;                                 // use: TestService.PartlyDeprecatedStruct.OldValue
  (void)sizeof(TestService::DeprecatedException);                           // use: TestService.DeprecatedException
  test_service.deprecated_procedure_stream(1);                              // use: TestService.DeprecatedProcedure:stream
  test_service.deprecated_procedure_call(1);                                // use: TestService.DeprecatedProcedure:call
  test_service.deprecated_property_stream();                                // use: TestService.DeprecatedProperty:stream
  test_service.deprecated_property_call();                                  // use: TestService.DeprecatedProperty:call
  deprecated_class.deprecated_method_stream();                              // use: TestService.DeprecatedClass.DeprecatedMethod:stream
  deprecated_class.deprecated_method_call();                                // use: TestService.DeprecatedClass.DeprecatedMethod:call
  (void)sizeof(DeprecatedService);                                          // use: DeprecatedService
  deprecated_service.procedure();                                           // use: DeprecatedService.Procedure
  deprecated_service.property();                                            // use: DeprecatedService.Property
  (void)sizeof(DeprecatedService::ServiceClass);                            // use: DeprecatedService.ServiceClass
  service_static_method<DeprecatedService::ServiceClass>(client);           // setup
  reasoned_static_method<DeprecatedService::ReasonedClass>(client);         // setup
  (void)sizeof(DeprecatedService::ServiceEnum);                             // use: DeprecatedService.ServiceEnum
  service_enum_value<DeprecatedService::ServiceEnum>();                     // setup
  (void)sizeof(DeprecatedService::ServiceStruct);                           // use: DeprecatedService.ServiceStruct
  (void)service_struct.field;                                               // use: DeprecatedService.ServiceStruct.Field
  (void)sizeof(DeprecatedService::ServiceException);                        // use: DeprecatedService.ServiceException
  (void)sizeof(TestService);                                                // use: TestService
  test_service.float_to_string(1);                                          // use: TestService.FloatToString
  test_service.float_to_string_stream(1);                                   // use: TestService.FloatToString:stream
  test_service.float_to_string_call(1);                                     // use: TestService.FloatToString:call
  test_service.string_property();                                           // use: TestService.StringProperty
  (void)sizeof(TestService::TestClass);                                     // use: TestService.TestClass
  (void)sizeof(TestService::TestEnum);                                      // use: TestService.TestEnum
  (void)TestService::TestEnum::value_a;                                     // use: TestService.TestEnum.ValueA
  (void)sizeof(TestService::TestStruct);                                    // use: TestService.TestStruct
  (void)test_struct.int_field;                                              // use: TestService.TestStruct.IntField
  (void)sizeof(TestService::CustomException);                               // use: TestService.CustomException
}
