#include <gmock/gmock.h>
#include <gtest/gtest.h>

#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

#include <cstdio>
#include <cstdlib>

#include "server_test.hpp"
#include "services/test_service.h"

class test_client: public server_test {
};

TEST_F(test_client, test_version) {
  krpc_schema_Status status = krpc_schema_Status_init_default;
  ASSERT_EQ(KRPC_OK, krpc_KRPC_GetStatus(conn, &status));
  ASSERT_THAT(status.version, testing::MatchesRegex("[0-9]+\\.[0-9]+\\.[0-9]+"));
}

TEST_F(test_client, test_optional_return_value) {
  ASSERT_EQ(KRPC_OK, krpc_KRPC_GetStatus(conn, nullptr));
}

TEST_F(test_client, test_value_parameters) {
  auto string = new char[32];
  int32_t value = 0;
  ASSERT_EQ(KRPC_OK, krpc_TestService_FloatToString(conn, &string, 3.14159));
  ASSERT_STREQ("3.14159", string);
  ASSERT_EQ(KRPC_OK, krpc_TestService_DoubleToString(conn, &string, 3.14159));
  ASSERT_STREQ("3.14159", string);
  ASSERT_EQ(KRPC_OK, krpc_TestService_Int32ToString(conn, &string, 42));
  ASSERT_STREQ("42", string);
  ASSERT_EQ(KRPC_OK, krpc_TestService_Int64ToString(conn, &string, 123456789000L));
  ASSERT_STREQ("123456789000", string);
  ASSERT_EQ(KRPC_OK, krpc_TestService_BoolToString(conn, &string, true));
  ASSERT_STREQ("True", string);
  ASSERT_EQ(KRPC_OK, krpc_TestService_BoolToString(conn, &string, false));
  ASSERT_STREQ("False", string);
  ASSERT_EQ(KRPC_OK, krpc_TestService_StringToInt32(conn, &value, "12345"));
  ASSERT_EQ(12345, value);
  KRPC_BYTES(bytes, 4);
  bytes.data[0] = '\xde';
  bytes.data[1] = '\xad';
  bytes.data[2] = '\xbe';
  bytes.data[3] = '\xef';
  ASSERT_EQ(KRPC_OK, krpc_TestService_BytesToHexString(conn, &string, bytes));
  ASSERT_STREQ("deadbeef", string);
  delete[] string;
}

TEST_F(test_client, test_string_malloc_and_free) {
  char * string = nullptr;
  ASSERT_EQ(KRPC_OK, krpc_TestService_FloatToString(conn, &string, 3.14159));
  ASSERT_STREQ("3.14159", string);
  krpc_free(string);
}

TEST_F(test_client, test_multiple_value_parameters) {
  auto value = new char[32];
  ASSERT_EQ(KRPC_OK, krpc_TestService_AddMultipleValues(conn, &value, 0.14159, 1, 2));
  ASSERT_STREQ("3.14159", value);
  delete[] value;
}

TEST_F(test_client, test_properties) {
  auto string = new char[32];
  ASSERT_EQ(KRPC_OK, krpc_TestService_set_StringProperty(conn, "foo"));
  ASSERT_EQ(KRPC_OK, krpc_TestService_StringProperty(conn, &string));
  ASSERT_STREQ("foo", string);
  ASSERT_EQ(KRPC_OK, krpc_TestService_StringPropertyPrivateSet(conn, &string));
  ASSERT_STREQ("foo", string);
  ASSERT_EQ(KRPC_OK, krpc_TestService_set_StringPropertyPrivateGet(conn, "foo"));
  delete[] string;

  krpc_TestService_TestClass_t object;
  ASSERT_EQ(KRPC_OK, krpc_TestService_CreateTestObject(conn, &object, "bar"));
  ASSERT_EQ(KRPC_OK, krpc_TestService_set_ObjectProperty(conn, object));
  krpc_TestService_TestClass_t object2;
  ASSERT_EQ(KRPC_OK, krpc_TestService_ObjectProperty(conn, &object2));
  ASSERT_EQ(object2, object);
}

TEST_F(test_client, test_class_as_return_value) {
  krpc_TestService_TestClass_t object;
  ASSERT_EQ(KRPC_OK, krpc_TestService_CreateTestObject(conn, &object, "jeb"));
  auto string = new char[32];
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_GetValue(conn, &string, object));
  ASSERT_STREQ("value=jeb", string);
  delete[] string;
}

TEST_F(test_client, test_class_none_value) {
  krpc_TestService_TestClass_t none = KRPC_NULL;
  krpc_TestService_TestClass_t result;
  ASSERT_EQ(KRPC_OK, krpc_TestService_EchoTestObject(conn, &result, none));
  ASSERT_EQ(none, result);
  krpc_TestService_TestClass_t object;
  ASSERT_EQ(KRPC_OK, krpc_TestService_CreateTestObject(conn, &object, "bob"));
  auto string = new char[32];
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_ObjectToString(conn, &string, object, none));
  ASSERT_STREQ("bobnull", string);
  delete[] string;
  ASSERT_EQ(KRPC_OK, krpc_TestService_set_ObjectProperty(conn, none));
  ASSERT_EQ(KRPC_OK, krpc_TestService_ObjectProperty(conn, &object));
  ASSERT_EQ(none, object);
}

TEST_F(test_client, test_class_methods) {
  krpc_TestService_TestClass_t object;
  ASSERT_EQ(KRPC_OK, krpc_TestService_CreateTestObject(conn, &object, "bob"));
  auto string = new char[32];
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_GetValue(conn, &string, object));
  ASSERT_STREQ("value=bob", string);
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_FloatToString(conn, &string, object, 3.14159));
  ASSERT_STREQ("bob3.14159", string);
  krpc_TestService_TestClass_t object2;
  ASSERT_EQ(KRPC_OK, krpc_TestService_CreateTestObject(conn, &object2, "bill"));
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_ObjectToString(conn, &string, object, object2));
  ASSERT_STREQ("bobbill", string);
  delete[] string;
}

TEST_F(test_client, test_class_static_methods) {
  auto string = new char[32];
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_StaticMethod(conn, &string, "", ""));
  ASSERT_STREQ("jeb", string);
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_StaticMethod(conn, &string, "bob", "bill"));
  ASSERT_STREQ("jebbobbill", string);
  delete[] string;
}

TEST_F(test_client, test_class_properties) {
  krpc_TestService_TestClass_t object;
  int32_t value;
  ASSERT_EQ(KRPC_OK, krpc_TestService_CreateTestObject(conn, &object, "jeb"));
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_set_IntProperty(conn, object, 0));
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_IntProperty(conn, &value, object));
  ASSERT_EQ(0, value);
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_set_IntProperty(conn, object, 42));
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_IntProperty(conn, &value, object));
  ASSERT_EQ(42, value);
  krpc_TestService_TestClass_t object2;
  ASSERT_EQ(KRPC_OK, krpc_TestService_CreateTestObject(conn, &object2, "kermin"));
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_set_ObjectProperty(conn, object, object2));
  krpc_TestService_TestClass_t object3;
  ASSERT_EQ(KRPC_OK, krpc_TestService_TestClass_ObjectProperty(conn, &object3, object));
  ASSERT_EQ(object2, object3);
}

TEST_F(test_client, test_blocking_procedure) {
  int32_t value;
  ASSERT_EQ(KRPC_OK, krpc_TestService_BlockingProcedure(conn, &value, 0, 0));
  ASSERT_EQ(value, 0);
  ASSERT_EQ(KRPC_OK, krpc_TestService_BlockingProcedure(conn, &value, 1, 0));
  ASSERT_EQ(value, 1);
  ASSERT_EQ(KRPC_OK, krpc_TestService_BlockingProcedure(conn, &value, 2, 0));
  ASSERT_EQ(value, 1+2);
  int expected = 0;
  for (int i = 1; i <= 42; i++)
    expected += i;
  ASSERT_EQ(KRPC_OK, krpc_TestService_BlockingProcedure(conn, &value, 42, 0));
  ASSERT_EQ(value, expected);
}

TEST_F(test_client, test_enums) {
  krpc_TestService_TestEnum_t value;
  ASSERT_EQ(KRPC_OK, krpc_TestService_EnumReturn(conn, &value));
  ASSERT_EQ(value, KRPC_TESTSERVICE_TESTENUM_VALUEB);
  ASSERT_EQ(KRPC_OK, krpc_TestService_EnumEcho(conn, &value, KRPC_TESTSERVICE_TESTENUM_VALUEA));
  ASSERT_EQ(value, KRPC_TESTSERVICE_TESTENUM_VALUEA);
  ASSERT_EQ(KRPC_OK, krpc_TestService_EnumEcho(conn, &value, KRPC_TESTSERVICE_TESTENUM_VALUEB));
  ASSERT_EQ(value, KRPC_TESTSERVICE_TESTENUM_VALUEB);
  ASSERT_EQ(KRPC_OK, krpc_TestService_EnumEcho(conn, &value, KRPC_TESTSERVICE_TESTENUM_VALUEC));
  ASSERT_EQ(value, KRPC_TESTSERVICE_TESTENUM_VALUEC);
}

TEST_F(test_client, test_test_service_enum_members) {
  ASSERT_EQ(0, KRPC_TESTSERVICE_TESTENUM_VALUEA);
  ASSERT_EQ(1, KRPC_TESTSERVICE_TESTENUM_VALUEB);
  ASSERT_EQ(2, KRPC_TESTSERVICE_TESTENUM_VALUEC);
}

TEST_F(test_client, test_collections) {
  {
    krpc_list_int32_t list = KRPC_NULL_LIST;
    krpc_list_int32_t result = KRPC_NULL_LIST;
    ASSERT_EQ(KRPC_OK, krpc_TestService_IncrementList(conn, &result, &list));
    ASSERT_EQ(result.size, 0);
    // ASSERT_EQ(result.items, NULL);
    KRPC_FREE_LIST(result);
  }
  {
    krpc_list_int32_t list = KRPC_NULL_LIST;
    list.size = 3;
    list.items = new int32_t[3];
    list.items[0] = 0;
    list.items[1] = 1;
    list.items[2] = 2;

    krpc_list_int32_t result = KRPC_NULL_LIST;
    ASSERT_EQ(KRPC_OK, krpc_TestService_IncrementList(conn, &result, &list));
    ASSERT_EQ(result.size, 3);
    for (size_t i = 0; i < result.size; i++)
      ASSERT_EQ(result.items[i], list.items[i]+1);
    delete[] list.items;
    KRPC_FREE_LIST(result);
  }
  {
    krpc_dictionary_string_int32_t dictionary = KRPC_NULL_DICTIONARY;
    krpc_dictionary_string_int32_t result = KRPC_NULL_DICTIONARY;
    ASSERT_EQ(KRPC_OK, krpc_TestService_IncrementDictionary(conn, &result, &dictionary));
    ASSERT_EQ(result.size, 0);
    // ASSERT_EQ(result.items, NULL);
    KRPC_FREE_DICTIONARY(result);
  }
  {
    krpc_dictionary_string_int32_t dictionary = KRPC_NULL_DICTIONARY;
    dictionary.size = 3;
    dictionary.entries = new krpc_dictionary_entry_string_int32_t[3];
    dictionary.entries[0].key = (char*)"a";
    dictionary.entries[0].value = 0;
    dictionary.entries[1].key = (char*)"b";
    dictionary.entries[1].value = 1;
    dictionary.entries[2].key = (char*)"c";
    dictionary.entries[2].value = 2;
    krpc_dictionary_string_int32_t result = KRPC_NULL_DICTIONARY;
    ASSERT_EQ(KRPC_OK, krpc_TestService_IncrementDictionary(conn, &result, &dictionary));
    ASSERT_EQ(result.size, 3);
    for (size_t i = 0; i < result.size; i++) {
      auto& entry = result.entries[i];
      if (!strcmp("a", entry.key))
        ASSERT_EQ(entry.value, 1);
      else if (!strcmp("b", entry.key))
        ASSERT_EQ(entry.value, 2);
      else if (!strcmp("c", entry.key))
        ASSERT_EQ(entry.value, 3);
      else
        FAIL();
    }
    delete[] dictionary.entries;
    for (size_t i = 0; i < result.size; i++)
      krpc_free(result.entries[i].key);
    KRPC_FREE_DICTIONARY(result);
  }
  {
    krpc_set_int32_t set = KRPC_NULL_SET;
    krpc_set_int32_t result = KRPC_NULL_SET;
    ASSERT_EQ(KRPC_OK, krpc_TestService_IncrementSet(conn, &result, &set));
    ASSERT_EQ(result.size, 0);
    // ASSERT_EQ(result.items, NULL);
    KRPC_FREE_SET(result);
  }
  {
    krpc_set_int32_t set = KRPC_NULL_SET;
    set.size = 3;
    set.items = new int32_t[3];
    set.items[0] = 0;
    set.items[1] = 1;
    set.items[2] = 2;
    krpc_set_int32_t result = KRPC_NULL_SET;
    ASSERT_EQ(KRPC_OK, krpc_TestService_IncrementSet(conn, &result, &set));
    ASSERT_EQ(result.size, 3);
    uint8_t found = 0;
    for (size_t i = 0; i < result.size; i++) {
      auto& item = result.items[i];
      switch (item) {
      case 1:
        found |= 0x1;
        break;
      case 2:
        found |= 0x2;
        break;
      case 3:
        found |= 0x4;
        break;
      default:
        FAIL();
      }
    }
    ASSERT_EQ(found, 0x1 | 0x2 | 0x4);
    delete[] set.items;
    KRPC_FREE_SET(result);
  }
  {
    krpc_tuple_int32_int64_t tuple = { 1, 2 };
    krpc_tuple_int32_int64_t result = { 0, 0 };
    ASSERT_EQ(KRPC_OK, krpc_TestService_IncrementTuple(conn, &result, &tuple));
    ASSERT_EQ(result.e0, 2);
    ASSERT_EQ(result.e1, 3);
  }
}

TEST_F(test_client, test_nested_collections) {
  {
    krpc_dictionary_string_list_int32_t dictionary = KRPC_NULL_DICTIONARY;
    krpc_dictionary_string_list_int32_t result = KRPC_NULL_DICTIONARY;
    ASSERT_EQ(KRPC_OK, krpc_TestService_IncrementNestedCollection(conn, &result, &dictionary));
    ASSERT_EQ(result.size, 0);
    // ASSERT_EQ(NULL, result.entries);
    KRPC_FREE_DICTIONARY(result);
  }
  {
    krpc_dictionary_string_list_int32_t dictionary = KRPC_NULL_DICTIONARY;
    dictionary.size = 3;
    dictionary.entries = new krpc_dictionary_entry_string_list_int32_t[3];
    dictionary.entries[0].key = (char*)"a";
    dictionary.entries[0].value = KRPC_NULL_LIST;
    dictionary.entries[0].value.size = 2;
    dictionary.entries[0].value.items = new int32_t[2];
    dictionary.entries[0].value.items[0] = 0;
    dictionary.entries[0].value.items[1] = 1;
    dictionary.entries[1].key = (char*)"b";
    dictionary.entries[1].value = KRPC_NULL_LIST;
    dictionary.entries[2].key = (char*)"c";
    dictionary.entries[2].value = KRPC_NULL_LIST;
    dictionary.entries[2].value.size = 1;
    dictionary.entries[2].value.items = new int32_t[1];
    dictionary.entries[2].value.items[0] = 2;

    krpc_dictionary_string_list_int32_t result = KRPC_NULL_DICTIONARY;
    ASSERT_EQ(KRPC_OK, krpc_TestService_IncrementNestedCollection(conn, &result, &dictionary));

    std::map<std::string, std::vector<int32_t>> actual;
    ASSERT_EQ(result.size, 3);
    for (size_t i = 0; i < result.size; i++) {
      std::string key(result.entries[i].key);
      actual[key] = std::vector<int32_t>();
      for (size_t j = 0; j < result.entries[i].value.size; j++)
        actual[key].push_back(result.entries[i].value.items[j]);
    }
    std::map<std::string, std::vector<int32_t>> expected;
    expected["a"] = std::vector<int>();
    expected["a"].push_back(1);
    expected["a"].push_back(2);
    expected["b"] = std::vector<int>();
    expected["c"] = std::vector<int>();
    expected["c"].push_back(3);
    ASSERT_EQ(expected, actual);

    delete[] dictionary.entries[0].value.items;
    delete[] dictionary.entries[2].value.items;
    delete[] dictionary.entries;
    for (size_t i = 0; i < result.size; i++) {
      krpc_free(result.entries[i].key);
      KRPC_FREE_LIST(result.entries[i].value);
    }
    KRPC_FREE_DICTIONARY(result);
  }
}

TEST_F(test_client, test_collections_of_objects) {
  krpc_list_object_t l1 = KRPC_NULL_LIST;
  krpc_list_object_t l2 = KRPC_NULL_LIST;
  ASSERT_EQ(KRPC_OK, krpc_TestService_AddToObjectList(conn, &l2, &l1, "jeb"));
  ASSERT_EQ(1, l2.size);
  {
    auto value = new char[32];
    krpc_object_t obj = l2.items[0];
    krpc_TestService_TestClass_GetValue(conn, &value, obj);
    ASSERT_EQ("value=jeb", std::string(value));
    delete[] value;
  }
  krpc_list_object_t l3 = KRPC_NULL_LIST;
  ASSERT_EQ(KRPC_OK, krpc_TestService_AddToObjectList(conn, &l3, &l2, "bob"));
  ASSERT_EQ(2, l3.size);
  {
    auto value = new char[32];
    krpc_object_t obj = l3.items[0];
    krpc_TestService_TestClass_GetValue(conn, &value, obj);
    ASSERT_EQ("value=jeb", std::string(value));
    delete[] value;
  }
  {
    auto value = new char[32];
    krpc_object_t obj = l3.items[1];
    krpc_TestService_TestClass_GetValue(conn, &value, obj);
    ASSERT_EQ("value=bob", std::string(value));
    delete[] value;
  }
  KRPC_FREE_LIST(l2);
  KRPC_FREE_LIST(l3);
}

TEST_F(test_client, test_invalid_operation_exception) {
  int32_t value;
  ASSERT_EQ(KRPC_ERROR_RPC_FAILED, krpc_TestService_ThrowInvalidOperationException(conn, &value));
}

TEST_F(test_client, test_argument_exception) {
  int32_t value;
  ASSERT_EQ(KRPC_ERROR_RPC_FAILED, krpc_TestService_ThrowArgumentException(conn, &value));
}

TEST_F(test_client, test_argument_null_exception) {
  int32_t value;
  ASSERT_EQ(KRPC_ERROR_RPC_FAILED, krpc_TestService_ThrowArgumentNullException(conn, &value, ""));
}

TEST_F(test_client, test_argument_out_of_range_exception) {
  int32_t value;
  ASSERT_EQ(KRPC_ERROR_RPC_FAILED,
            krpc_TestService_ThrowArgumentOutOfRangeException(conn, &value, 0));
}

TEST_F(test_client, test_custom_exception) {
  int32_t value;
  ASSERT_EQ(KRPC_ERROR_RPC_FAILED, krpc_TestService_ThrowCustomException(conn, &value));
}
