// A use of each member the deprecation conformance tests name, one per line. It is only compiled,
// for the warnings the build records. See tools/build/deprecation_signals.py.
// clang-format off
#include <krpc_cnano.h>
#include "services/deprecated_service.h"
#include "services/test_service.h"

void uses(krpc_connection_t connection,                                                 // setup
          char ** text,                                                                 // setup
          krpc_schema_ProcedureCall * call,                                             // setup
          krpc_TestService_DeprecatedClass_t deprecated_class,                          // setup
          krpc_TestService_DeprecatedStruct_t * deprecated_struct,                      // setup
          krpc_TestService_PartlyDeprecatedStruct_t * partly_deprecated_struct,         // setup
          krpc_TestService_TestStruct_t * test_struct,                                  // setup
          krpc_DeprecatedService_ServiceStruct_t * service_struct) {                    // setup
  krpc_TestService_DeprecatedProcedure(connection, text, 1);                            // use: TestService.DeprecatedProcedure
  krpc_TestService_DeprecatedProperty(connection, text);                                // use: TestService.DeprecatedProperty
  (void)sizeof(krpc_TestService_DeprecatedClass_t);                                     // use: TestService.DeprecatedClass
  krpc_TestService_DeprecatedClass_DeprecatedMethod(connection, text, deprecated_class);  // use: TestService.DeprecatedClass.DeprecatedMethod
  krpc_TestService_DeprecatedClass_InheritedMethod(connection, text, deprecated_class);   // use: TestService.DeprecatedClass.InheritedMethod
  krpc_TestService_DeprecatedClass_InheritedStaticMethod(connection, text);             // use: TestService.DeprecatedClass.InheritedStaticMethod
  krpc_TestService_DeprecatedClass_InheritedProperty(connection, text, deprecated_class);  // use: TestService.DeprecatedClass.InheritedProperty
  (void)sizeof(krpc_TestService_DeprecatedEnum_t);                                      // use: TestService.DeprecatedEnum
  (void)KRPC_TESTSERVICE_DEPRECATEDENUM_VALUEA;                                         // use: TestService.DeprecatedEnum.ValueA
  (void)KRPC_TESTSERVICE_PARTLYDEPRECATEDENUM_VALUEA;                                   // use: TestService.PartlyDeprecatedEnum.ValueA
  (void)KRPC_TESTSERVICE_PARTLYDEPRECATEDENUM_VALUEB;                                   // use: TestService.PartlyDeprecatedEnum.ValueB
  (void)sizeof(krpc_TestService_DeprecatedStruct_t);                                    // use: TestService.DeprecatedStruct
  (void)deprecated_struct->value;                                                       // use: TestService.DeprecatedStruct.Value
  (void)partly_deprecated_struct->value;                                                // use: TestService.PartlyDeprecatedStruct.Value
  (void)partly_deprecated_struct->old_value;                                            // use: TestService.PartlyDeprecatedStruct.OldValue
  krpc_TestService_DeprecatedProcedure_call(call, 1);                                   // use: TestService.DeprecatedProcedure:call
  krpc_TestService_DeprecatedProperty_call(call);                                       // use: TestService.DeprecatedProperty:call
  krpc_TestService_DeprecatedClass_DeprecatedMethod_call(call, deprecated_class);       // use: TestService.DeprecatedClass.DeprecatedMethod:call
  krpc_DeprecatedService_Procedure(connection, text);                                   // use: DeprecatedService.Procedure
  krpc_DeprecatedService_Property(connection, text);                                    // use: DeprecatedService.Property
  (void)sizeof(krpc_DeprecatedService_ServiceClass_t);                                  // use: DeprecatedService.ServiceClass
  krpc_DeprecatedService_ServiceClass_StaticMethod(connection, text);                   // use: DeprecatedService.ServiceClass.StaticMethod
  krpc_DeprecatedService_ReasonedClass_StaticMethod(connection, text);                  // use: DeprecatedService.ReasonedClass.StaticMethod
  (void)sizeof(krpc_DeprecatedService_ServiceEnum_t);                                   // use: DeprecatedService.ServiceEnum
  (void)KRPC_DEPRECATEDSERVICE_SERVICEENUM_VALUE;                                       // use: DeprecatedService.ServiceEnum.Value
  (void)sizeof(krpc_DeprecatedService_ServiceStruct_t);                                 // use: DeprecatedService.ServiceStruct
  (void)service_struct->field;                                                          // use: DeprecatedService.ServiceStruct.Field
  krpc_TestService_FloatToString(connection, text, 1);                                  // use: TestService.FloatToString
  krpc_TestService_FloatToString_call(call, 1);                                         // use: TestService.FloatToString:call
  krpc_TestService_StringProperty(connection, text);                                    // use: TestService.StringProperty
  (void)sizeof(krpc_TestService_TestClass_t);                                           // use: TestService.TestClass
  (void)sizeof(krpc_TestService_TestEnum_t);                                            // use: TestService.TestEnum
  (void)KRPC_TESTSERVICE_TESTENUM_VALUEA;                                               // use: TestService.TestEnum.ValueA
  (void)sizeof(krpc_TestService_TestStruct_t);                                          // use: TestService.TestStruct
  (void)test_struct->int_field;                                                         // use: TestService.TestStruct.IntField
}
