#pragma once

#include <string.h>

#include <krpc_cnano/decoder.h>
#include <krpc_cnano/deprecated.h>
#include <krpc_cnano/encoder.h>
#include <krpc_cnano/error.h>
#include <krpc_cnano/memory.h>
#include <krpc_cnano/pb_decode.h>
#include <krpc_cnano/pb_encode.h>
#include <krpc_cnano/types.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * A server side expression.
 *
 * Numeric operands of differing types are converted to a common type.
 */
typedef krpc_object_t krpc_KRPC_Expression_t;
/**
 * The type of a value a procedure takes or returns.
 */
typedef krpc_object_t krpc_KRPC_Type_t;

#ifndef KRPC_TYPE_DICTIONARY_INT32_OBJECT
#define KRPC_TYPE_DICTIONARY_INT32_OBJECT

typedef struct krpc_dictionary_entry_int32_object_s krpc_dictionary_entry_int32_object_t;
struct krpc_dictionary_entry_int32_object_s {
  int32_t key;
  krpc_object_t value;
};

typedef struct krpc_dictionary_int32_object_s krpc_dictionary_int32_object_t;
struct krpc_dictionary_int32_object_s {
  size_t size;
  krpc_dictionary_entry_int32_object_t * entries;
};

static inline krpc_error_t krpc_encode_dictionary_int32_object(
  pb_ostream_t * stream, const krpc_dictionary_int32_object_t * value);
static inline krpc_error_t krpc_encode_size_dictionary_int32_object(
  size_t * size, const krpc_dictionary_int32_object_t * value);
static inline bool krpc_encode_callback_dictionary_int32_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
static inline krpc_error_t krpc_decode_dictionary_int32_object(
  pb_istream_t * stream, krpc_dictionary_int32_object_t * value);

#endif  // KRPC_TYPE_DICTIONARY_INT32_OBJECT

#ifndef KRPC_TYPE_DICTIONARY_STRING_OBJECT
#define KRPC_TYPE_DICTIONARY_STRING_OBJECT

typedef struct krpc_dictionary_entry_string_object_s krpc_dictionary_entry_string_object_t;
struct krpc_dictionary_entry_string_object_s {
  char * key;
  krpc_object_t value;
};

typedef struct krpc_dictionary_string_object_s krpc_dictionary_string_object_t;
struct krpc_dictionary_string_object_s {
  size_t size;
  krpc_dictionary_entry_string_object_t * entries;
};

static inline krpc_error_t krpc_encode_dictionary_string_object(
  pb_ostream_t * stream, const krpc_dictionary_string_object_t * value);
static inline krpc_error_t krpc_encode_size_dictionary_string_object(
  size_t * size, const krpc_dictionary_string_object_t * value);
static inline bool krpc_encode_callback_dictionary_string_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
static inline krpc_error_t krpc_decode_dictionary_string_object(
  pb_istream_t * stream, krpc_dictionary_string_object_t * value);

#endif  // KRPC_TYPE_DICTIONARY_STRING_OBJECT

#ifndef KRPC_TYPE_KRPC_CLIENTINFO
#define KRPC_TYPE_KRPC_CLIENTINFO

typedef struct krpc_KRPC_ClientInfo_s krpc_KRPC_ClientInfo_t;
struct krpc_KRPC_ClientInfo_s {
  krpc_bytes_t id;
  char * name;
  char * address;
};

static inline krpc_error_t krpc_encode_KRPC_ClientInfo(
  pb_ostream_t * stream, const krpc_KRPC_ClientInfo_t * value);
static inline krpc_error_t krpc_encode_size_KRPC_ClientInfo(
  size_t * size, const krpc_KRPC_ClientInfo_t * value);
static inline bool krpc_encode_callback_KRPC_ClientInfo(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
static inline krpc_error_t krpc_decode_KRPC_ClientInfo(
  pb_istream_t * stream, krpc_KRPC_ClientInfo_t * value);

#endif  // KRPC_TYPE_KRPC_CLIENTINFO

#ifndef KRPC_TYPE_LIST_KRPC_CLIENTINFO
#define KRPC_TYPE_LIST_KRPC_CLIENTINFO

typedef struct krpc_list_KRPC_ClientInfo_s krpc_list_KRPC_ClientInfo_t;
struct krpc_list_KRPC_ClientInfo_s {
  size_t size;
  krpc_KRPC_ClientInfo_t * items;
};

static inline krpc_error_t krpc_encode_list_KRPC_ClientInfo(
  pb_ostream_t * stream, const krpc_list_KRPC_ClientInfo_t * value);
static inline krpc_error_t krpc_encode_size_list_KRPC_ClientInfo(
  size_t * size, const krpc_list_KRPC_ClientInfo_t * value);
static inline bool krpc_encode_callback_list_KRPC_ClientInfo(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
static inline krpc_error_t krpc_decode_list_KRPC_ClientInfo(
  pb_istream_t * stream, krpc_list_KRPC_ClientInfo_t * value);

#endif  // KRPC_TYPE_LIST_KRPC_CLIENTINFO

#ifndef KRPC_TYPE_NULLABLE_BYTES
#define KRPC_TYPE_NULLABLE_BYTES

typedef struct krpc_nullable_bytes_s krpc_nullable_bytes_t;
struct krpc_nullable_bytes_s {
  krpc_bytes_t value;
  bool has_value;
};

static inline krpc_error_t krpc_encode_nullable_bytes(
  pb_ostream_t * stream, const krpc_nullable_bytes_t * value);
static inline krpc_error_t krpc_encode_size_nullable_bytes(
  size_t * size, const krpc_nullable_bytes_t * value);
static inline bool krpc_encode_callback_nullable_bytes(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
static inline krpc_error_t krpc_decode_nullable_bytes(
  pb_istream_t * stream, krpc_nullable_bytes_t * value);

#endif  // KRPC_TYPE_NULLABLE_BYTES

#ifndef KRPC_TYPE_LIST_NULLABLE_BYTES
#define KRPC_TYPE_LIST_NULLABLE_BYTES

typedef struct krpc_list_nullable_bytes_s krpc_list_nullable_bytes_t;
struct krpc_list_nullable_bytes_s {
  size_t size;
  krpc_nullable_bytes_t * items;
};

static inline krpc_error_t krpc_encode_list_nullable_bytes(
  pb_ostream_t * stream, const krpc_list_nullable_bytes_t * value);
static inline krpc_error_t krpc_encode_size_list_nullable_bytes(
  size_t * size, const krpc_list_nullable_bytes_t * value);
static inline bool krpc_encode_callback_list_nullable_bytes(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
static inline krpc_error_t krpc_decode_list_nullable_bytes(
  pb_istream_t * stream, krpc_list_nullable_bytes_t * value);

#endif  // KRPC_TYPE_LIST_NULLABLE_BYTES

#ifndef KRPC_TYPE_LIST_OBJECT
#define KRPC_TYPE_LIST_OBJECT

typedef struct krpc_list_object_s krpc_list_object_t;
struct krpc_list_object_s {
  size_t size;
  krpc_object_t * items;
};

static inline krpc_error_t krpc_encode_list_object(
  pb_ostream_t * stream, const krpc_list_object_t * value);
static inline krpc_error_t krpc_encode_size_list_object(
  size_t * size, const krpc_list_object_t * value);
static inline bool krpc_encode_callback_list_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
static inline krpc_error_t krpc_decode_list_object(
  pb_istream_t * stream, krpc_list_object_t * value);

#endif  // KRPC_TYPE_LIST_OBJECT

#ifndef KRPC_TYPE_SET_OBJECT
#define KRPC_TYPE_SET_OBJECT

typedef struct krpc_set_object_s krpc_set_object_t;
struct krpc_set_object_s {
  size_t size;
  krpc_object_t * items;
};

static inline krpc_error_t krpc_encode_set_object(
  pb_ostream_t * stream, const krpc_set_object_t * value);
static inline krpc_error_t krpc_encode_size_set_object(
  size_t * size, const krpc_set_object_t * value);
static inline bool krpc_encode_callback_set_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
static inline krpc_error_t krpc_decode_set_object(
  pb_istream_t * stream, krpc_set_object_t * value);

#endif  // KRPC_TYPE_SET_OBJECT

/**
 * The game scene. See KRPC::game_scene.
 */
typedef enum {
  /**
   * The game scene showing the Kerbal Space Center buildings.
   */
  KRPC_KRPC_GAMESCENE_SPACECENTER = 0,
  /**
   * The game scene showing a vessel in flight (or on the launchpad/runway).
   */
  KRPC_KRPC_GAMESCENE_FLIGHT = 1,
  /**
   * The tracking station.
   */
  KRPC_KRPC_GAMESCENE_TRACKINGSTATION = 2,
  /**
   * The Vehicle Assembly Building.
   */
  KRPC_KRPC_GAMESCENE_EDITORVAB = 3,
  /**
   * The Space Plane Hangar.
   */
  KRPC_KRPC_GAMESCENE_EDITORSPH = 4,
  /**
   * The mission builder.
   */
  KRPC_KRPC_GAMESCENE_MISSIONBUILDER = 5,
  /**
   * The astronaut complex. This is a pseudo-scene, shown when the
   * astronaut complex facility is open within the space center scene.
   */
  KRPC_KRPC_GAMESCENE_ASTRONAUTCOMPLEX = 6,
  /**
   * Mission control. This is a pseudo-scene, shown when the
   * mission control facility is open within the space center scene.
   */
  KRPC_KRPC_GAMESCENE_MISSIONCONTROL = 7,
  /**
   * Research and development. This is a pseudo-scene, shown when the
   * research and development facility is open within the space center scene.
   */
  KRPC_KRPC_GAMESCENE_RESEARCHANDDEVELOPMENT = 8,
  /**
   * The administration facility. This is a pseudo-scene, shown when the
   * administration facility is open within the space center scene.
   */
  KRPC_KRPC_GAMESCENE_ADMINISTRATION = 9
} krpc_KRPC_GameScene_t;

/**
 * The kind of a type, with the values of the communication protocol's type codes.
 */
typedef enum {
  /**
   * A double precision floating point number.
   */
  KRPC_KRPC_TYPECODE_DOUBLE = 1,
  /**
   * A single precision floating point number.
   */
  KRPC_KRPC_TYPECODE_FLOAT = 2,
  /**
   * A signed 32-bit integer.
   */
  KRPC_KRPC_TYPECODE_SINT32 = 3,
  /**
   * A signed 64-bit integer.
   */
  KRPC_KRPC_TYPECODE_SINT64 = 4,
  /**
   * An unsigned 32-bit integer.
   */
  KRPC_KRPC_TYPECODE_UINT32 = 5,
  /**
   * An unsigned 64-bit integer.
   */
  KRPC_KRPC_TYPECODE_UINT64 = 6,
  /**
   * A boolean.
   */
  KRPC_KRPC_TYPECODE_BOOL = 7,
  /**
   * A string.
   */
  KRPC_KRPC_TYPECODE_STRING = 8,
  /**
   * A sequence of bytes.
   */
  KRPC_KRPC_TYPECODE_BYTES = 9,
  /**
   * A class defined by a service.
   */
  KRPC_KRPC_TYPECODE_CLASS = 100,
  /**
   * An enumeration defined by a service.
   */
  KRPC_KRPC_TYPECODE_ENUMERATION = 101,
  /**
   * A structure defined by a service.
   */
  KRPC_KRPC_TYPECODE_STRUCT = 102,
  /**
   * A tuple, with the types of its values in KRPC::Type::types.
   */
  KRPC_KRPC_TYPECODE_TUPLE = 300,
  /**
   * A list, with the type of its values in KRPC::Type::types.
   */
  KRPC_KRPC_TYPECODE_LIST = 301,
  /**
   * A set, with the type of its values in KRPC::Type::types.
   */
  KRPC_KRPC_TYPECODE_SET = 302,
  /**
   * A dictionary, with the types of its keys and values in KRPC::Type::types.
   */
  KRPC_KRPC_TYPECODE_DICTIONARY = 303
} krpc_KRPC_TypeCode_t;

/**
 * Create an event from a server side function that evaluates to a boolean value.
 *
 * The function is evaluated on each update, and each call adds a new event. Any
 * exception from the function ends the event, including one for an object that is not
 * loaded. A procedure that pauses execution to resume on a later tick produces an
 * error on the event's stream.
 */
static inline krpc_error_t krpc_KRPC_AddEvent(krpc_connection_t connection, krpc_schema_Event * returnValue, krpc_KRPC_Expression_t function);

/**
 * Add a stream of the value a server side function computes on each update.
 *
 * Each call adds a new stream, with values of the type given by
 * KRPC::Expression::return_type. Any exception from the function ends the
 * stream, including one for an object that is not loaded. A procedure that pauses
 * execution to resume on a later tick produces an error on the stream.
 */
static inline krpc_error_t krpc_KRPC_AddFunctionStream(krpc_connection_t connection, krpc_schema_Stream * returnValue, krpc_KRPC_Expression_t function);

/**
 * Add a streaming request and return its identifier.
 *
 * The stream starts sending results from the next update.
 */
static inline krpc_error_t krpc_KRPC_AddStream(krpc_connection_t connection, krpc_schema_Stream * returnValue, const krpc_schema_ProcedureCall * call);

/**
 * Compile a server side function, so that running, streaming or adding it as an
 * event does not compile it.
 *
 * Compiling a large function takes time on the game's main thread. Compiling it
 * in advance keeps that time out of its first run.
 */
static inline krpc_error_t krpc_KRPC_CompileFunction(krpc_connection_t connection, krpc_KRPC_Expression_t function);

/**
 * Returns the identifier for the current client.
 */
static inline krpc_error_t krpc_KRPC_GetClientID(krpc_connection_t connection, krpc_bytes_t * returnValue);

/**
 * Returns the name of the current client.
 * This is an empty string if the client has no name.
 */
static inline krpc_error_t krpc_KRPC_GetClientName(krpc_connection_t connection, char * * returnValue);

/**
 * Returns information on all services, procedures, classes, properties etc. provided by the server.
 * Can be used by client libraries to automatically create functionality such as stubs.
 */
static inline krpc_error_t krpc_KRPC_GetServices(krpc_connection_t connection, krpc_schema_Services * returnValue);

/**
 * Returns some information about the server, such as the version.
 */
static inline krpc_error_t krpc_KRPC_GetStatus(krpc_connection_t connection, krpc_schema_Status * returnValue);

/**
 * Hold the game on its current physics tick until KRPC::release_tick is called
 * or the hold times out. Every call made in between is executed before the game
 * advances, so a read, compute and write all happen in the state of one tick.
 *
 * The game renders no frame, takes no input and runs no physics while the tick is
 * held. Keep a hold as short as possible, and release it even when the code between
 * the two calls fails.
 *
 * A tick is held once, so this waits for the next tick when the current one has
 * already been released. One client holds the tick at a time, and only a client
 * making a call can hold it: a stream or an event cannot.
 *
 * A hold ends by itself when it times out, when the client disconnects, and when the
 * client calls a procedure that takes more than one tick to finish, such as staging
 * or warping. Stream updates are sent once the tick is released, so waiting for a
 * stream or an event inside a hold waits out the timeout.
 */
static inline krpc_error_t krpc_KRPC_HoldTick(krpc_connection_t connection);

/**
 * Let the game move on from the tick being held and hold the one after it, so that a
 * program acts on consecutive physics ticks. Holds the soonest tick it can if no tick
 * is currently being held.
 *
 * A release and a hold made as two separate calls let the game advance between them.
 * This takes the next tick without that gap. Call it at the top of a loop that has to
 * act on every tick, work, and come back to it.
 *
 * The game advances one tick per call, so it runs at the rate of the loop for as long
 * as the loop runs. End the loop with KRPC::release_tick.
 *
 * Every limit of KRPC::hold_tick applies, the timeout included. A hold lost to
 * the timeout is not reported, and the next call takes whatever tick it lands on.
 */
static inline krpc_error_t krpc_KRPC_NextTick(krpc_connection_t connection);

/**
 * Let the game move on from the tick that KRPC::hold_tick held it on. Does
 * nothing when no tick is held, which is the case once a hold has timed out.
 */
static inline krpc_error_t krpc_KRPC_ReleaseTick(krpc_connection_t connection);

/**
 * Remove a streaming request.
 *
 * Does nothing for a stream that has ended. Throws an exception if the id has never
 * been issued.
 */
static inline krpc_error_t krpc_KRPC_RemoveStream(krpc_connection_t connection, uint64_t id);

/**
 * Run a server side function once, within a single physics tick, and return
 * the value it produces.
 *
 * The arguments are encoded at the types given by KRPC::Expression::parameter_types.
 * Throws an exception if a procedure the function calls pauses execution, and the
 * effects up to the pause remain.
 * @return The encoded value of the function, or no bytes for a function with no
 * value.
 * @param function The function to run.
 * @param arguments The encoded arguments, one per parameter.
 */
static inline krpc_error_t krpc_KRPC_RunFunction(krpc_connection_t connection, krpc_bytes_t * returnValue, bool * returnValueIsNull, krpc_KRPC_Expression_t function, const krpc_list_nullable_bytes_t * arguments);

/**
 * Set the update rate for a stream in Hz.
 *
 * Does nothing for a stream that has ended. Throws an exception if the id has never
 * been issued.
 */
static inline krpc_error_t krpc_KRPC_SetStreamRate(krpc_connection_t connection, uint64_t id, float rate);

/**
 * The RPC clients that are currently connected to the server.
 */
static inline krpc_error_t krpc_KRPC_Clients(krpc_connection_t connection, krpc_list_KRPC_ClientInfo_t * returnValue);

/**
 * The current game scene. Setting this switches the game to the given
 * scene, or opens/closes the corresponding facility for the pseudo-scenes.
 * Scene changes happen asynchronously: setting this property returns
 * immediately, and clients should poll it until it reports the requested
 * scene. Setting it to KRPC::GameScene::flight resumes the save's
 * active vessel, and fails if there is none. Moving between
 * KRPC::GameScene::editor_vab and KRPC::GameScene::editor_sph
 * keeps the vessel that is being constructed, as the game's own switch
 * editor button does. Leaving flight saves the game first, unless the game
 * cannot be saved, e.g. while under acceleration.
 */
static inline krpc_error_t krpc_KRPC_GameScene(krpc_connection_t connection, krpc_KRPC_GameScene_t * returnValue);

/**
 * The current game scene. Setting this switches the game to the given
 * scene, or opens/closes the corresponding facility for the pseudo-scenes.
 * Scene changes happen asynchronously: setting this property returns
 * immediately, and clients should poll it until it reports the requested
 * scene. Setting it to KRPC::GameScene::flight resumes the save's
 * active vessel, and fails if there is none. Moving between
 * KRPC::GameScene::editor_vab and KRPC::GameScene::editor_sph
 * keeps the vessel that is being constructed, as the game's own switch
 * editor button does. Leaving flight saves the game first, unless the game
 * cannot be saved, e.g. while under acceleration.
 */
static inline krpc_error_t krpc_KRPC_set_GameScene(krpc_connection_t connection, krpc_KRPC_GameScene_t value);

/**
 * The number of game states that have finished loading.
 *
 * Increases once a game load, quickload, revert or scene change has finished.
 */
static inline krpc_error_t krpc_KRPC_GameStateLoads(krpc_connection_t connection, uint32_t * returnValue);

/**
 * Whether the game is paused.
 */
static inline krpc_error_t krpc_KRPC_Paused(krpc_connection_t connection, bool * returnValue);

/**
 * Whether the game is paused.
 */
static inline krpc_error_t krpc_KRPC_set_Paused(krpc_connection_t connection, bool value);

/**
 * Numerical addition.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_Add(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * The result of applying an accumulator function over a collection.
 *
 * Throws an exception if the collection is empty.
 * @return The accumulated value.
 * @param arg The collection.
 * @param func The accumulator function.
 */
static inline krpc_error_t krpc_KRPC_Expression_Aggregate(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func);

/**
 * The result of applying an accumulator function over a collection, starting from a seed.
 *
 * The seed widens to the type of the accumulated value, the function's result.
 * @return The accumulated value.
 * @param arg The collection.
 * @param seed The seed value.
 * @param func The accumulator function.
 */
static inline krpc_error_t krpc_KRPC_Expression_AggregateWithSeed(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t seed, krpc_KRPC_Expression_t func);

/**
 * Whether a predicate function returns true for every value of a collection.
 * @return Whether all items satisfy the predicate.
 * @param arg The collection.
 * @param predicate The predicate function.
 */
static inline krpc_error_t krpc_KRPC_Expression_All(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t predicate);

/**
 * Logical or bitwise and, which evaluates both operands.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_And(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * Whether a predicate function returns true for any value of a collection.
 * @return Whether any item satisfies the predicate.
 * @param arg The collection.
 * @param predicate The predicate function.
 */
static inline krpc_error_t krpc_KRPC_Expression_Any(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t predicate);

/**
 * A statement adding a value to a list or a set.
 *
 * The value goes on the end of a list. A set holding the value is left unchanged.
 * @param collection The list or set to add to.
 * @param value The value to add.
 */
static inline krpc_error_t krpc_KRPC_Expression_Append(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t value);

/**
 * An assignment to a local variable, state variable or function parameter.
 *
 * The value's type must be assignable to the variable's type. A numeric value of a
 * different type is widened, and narrowing it requires a cast.
 * @param variable The variable to assign to.
 * @param value The value to assign.
 */
static inline krpc_error_t krpc_KRPC_Expression_Assign(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t variable, krpc_KRPC_Expression_t value);

/**
 * The average of the values of a collection.
 *
 * Throws an exception if the collection is empty and its values are not nullable.
 * @return The average of the elements in the collection.
 * @param arg The list or set.
 */
static inline krpc_error_t krpc_KRPC_Expression_Average(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * A block of statements, whose value is that of its last statement.
 * @param statements The statements.
 */
static inline krpc_error_t krpc_KRPC_Expression_Block(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * statements);

/**
 * A block of statements with local variables.
 *
 * The value of the block is that of its last statement. The variables are in scope
 * for the statements of the block, including within nested functions.
 * @param variables The local variables of the block.
 * @param statements The statements.
 */
static inline krpc_error_t krpc_KRPC_Expression_BlockWithVariables(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * variables, const krpc_list_object_t * statements);

/**
 * A break statement, which ends the enclosing loop.
 */
static inline krpc_error_t krpc_KRPC_Expression_Break(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue);

/**
 * A dictionary built from a collection, with a key and a value computed from each
 * element.
 *
 * A repeated key takes the last of its values.
 * @return The dictionary.
 * @param arg The list or set.
 * @param keyFunc The function computing an element's key.
 * @param valueFunc The function computing an element's value.
 */
static inline krpc_error_t krpc_KRPC_Expression_BuildDictionary(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t keyFunc, krpc_KRPC_Expression_t valueFunc);

/**
 * An RPC call.
 *
 * The instance the call is made on, and the values of its arguments, are fixed
 * when the expression is created. A call to a procedure with no return value is
 * evaluated for its effects, as a statement in a KRPC::Expression::block. A collection the
 * procedure returns is copied, as a client would receive it.
 * @param call The procedure call.
 */
static inline krpc_error_t krpc_KRPC_Expression_Call(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_schema_ProcedureCall * call);

/**
 * An RPC call, where some or all of the arguments are computed by expressions.
 *
 * Position 0 is the instance of a class method or property. A position with no
 * expression takes the argument encoded in the call, or the parameter's default. A
 * collection the procedure returns is copied, as a client would receive it.
 * @param call The RPC to call.
 * @param args Expressions computing the call's arguments, by position.
 */
static inline krpc_error_t krpc_KRPC_Expression_CallWithArguments(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_schema_ProcedureCall * call, const krpc_dictionary_int32_object_t * args);

/**
 * A value cast to the given type.
 *
 * A tuple or collection is cast element by element.
 * @param arg The value to cast.
 * @param type Type to cast the argument to.
 */
static inline krpc_error_t krpc_KRPC_Expression_Cast(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Type_t type);

/**
 * A statement removing every value from a list or a set, or every entry from a dictionary.
 * @param collection The list, set or dictionary.
 */
static inline krpc_error_t krpc_KRPC_Expression_Clear(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection);

/**
 * The values of one collection followed by those of another.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The first collection followed by the second.
 * @param arg1 The first collection.
 * @param arg2 The second collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_Concat(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2);

/**
 * The second argument if the condition is true, and the third argument otherwise.
 *
 * Throws an exception if the two values have no common type.
 * @param condition The boolean condition.
 * @param ifTrue The value when the condition is true.
 * @param ifFalse The value when the condition is false.
 */
static inline krpc_error_t krpc_KRPC_Expression_Conditional(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t ifTrue, krpc_KRPC_Expression_t ifFalse);

/**
 * Logical and, which evaluates the second operand only when the first is true.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConditionalAnd(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * Logical or, which evaluates the second operand only when the first is false.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConditionalOr(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * A constant value of boolean type.
 * @param value The value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantBool(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, bool value);

/**
 * A constant value of bytes type.
 * @param value The value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantBytes(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_bytes_t value);

/**
 * A constant value of double precision floating point type.
 * @param value The value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantDouble(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, double value);

/**
 * A constant value of an enumeration a service defines.
 * @param service The name of the service the enumeration is defined in.
 * @param name The name of the enumeration.
 * @param value The value of the member of the enumeration.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantEnum(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * service, const char * name, int32_t value);

/**
 * A constant value of single precision floating point type.
 * @param value The value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantFloat(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, float value);

/**
 * A constant value of integer type.
 * @param value The value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantInt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, int32_t value);

/**
 * A constant value of long integer type.
 * @param value The value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantLong(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, int64_t value);

/**
 * A null constant of the given type.
 *
 * The type is made nullable, as by KRPC::Type::nullable_type.
 * @param type The type of the value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantNull(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t type);

/**
 * A constant object, an instance of a class a service defines.
 *
 * The object is given by its object identifier.
 * @param value The object identifier of the object.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantObject(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, uint64_t value);

/**
 * A constant value of string type.
 * @param value The value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantString(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * value);

/**
 * A constant value of unsigned integer type.
 * @param value The value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantUInt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, uint32_t value);

/**
 * A constant value of unsigned long integer type.
 * @param value The value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConstantULong(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, uint64_t value);

/**
 * Whether a collection contains a value.
 * @return Whether the collection contains the value.
 * @param arg The collection.
 * @param value The value to look for.
 */
static inline krpc_error_t krpc_KRPC_Expression_Contains(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value);

/**
 * Whether a dictionary contains a key.
 * @return Whether the dictionary contains the key.
 * @param dictionary The dictionary.
 * @param key The key to look for.
 */
static inline krpc_error_t krpc_KRPC_Expression_ContainsKey(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t dictionary, krpc_KRPC_Expression_t key);

/**
 * A continue statement, which skips to the next iteration of the enclosing loop.
 */
static inline krpc_error_t krpc_KRPC_Expression_Continue(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue);

/**
 * The string representation of a value.
 *
 * A value other than a number, boolean, string or enumeration converts as .NET
 * formats it, which for a collection is the name of its type. Throws an exception if
 * the value is null.
 * @return The string representation of the value.
 * @param arg The value to convert.
 */
static inline krpc_error_t krpc_KRPC_Expression_ConvertToString(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * The number of values in a collection.
 * @return The number of elements in the collection.
 * @param arg The list, set or dictionary.
 */
static inline krpc_error_t krpc_KRPC_Expression_Count(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * A dictionary of the given keys and corresponding values, with numbers widened to
 * their common type. Throws an exception if there are no keys, the counts differ, the
 * keys are not of a valid key type, or a key is null.
 *
 * A repeated key takes the last of its values.
 * @return The dictionary.
 * @param keys The keys.
 * @param values The values.
 */
static inline krpc_error_t krpc_KRPC_Expression_CreateDictionary(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * keys, const krpc_list_object_t * values);

/**
 * An empty dictionary of the given key and value types. Throws an exception if the
 * key type is nullable or not a valid key type.
 * @return The empty dictionary.
 * @param keyType The type of the dictionary's keys.
 * @param valueType The type of the dictionary's values.
 */
static inline krpc_error_t krpc_KRPC_Expression_CreateEmptyDictionary(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t keyType, krpc_KRPC_Type_t valueType);

/**
 * An empty list of the given value type.
 * @return The empty list.
 * @param valueType The type of the values the list holds.
 */
static inline krpc_error_t krpc_KRPC_Expression_CreateEmptyList(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t valueType);

/**
 * An empty set of the given value type. Throws an exception if the value type is
 * nullable.
 * @return The empty set.
 * @param valueType The type of the values the set holds.
 */
static inline krpc_error_t krpc_KRPC_Expression_CreateEmptySet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t valueType);

/**
 * A list of the given values, with numbers widened to their common type. Throws an
 * exception if there are no values.
 * @return The list.
 * @param values The values.
 */
static inline krpc_error_t krpc_KRPC_Expression_CreateList(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * values);

/**
 * A set of the given values, with numbers widened to their common type. Throws an
 * exception if there are no values, or a value is null.
 *
 * The values are evaluated in an unspecified order.
 * @return The set.
 * @param values The values.
 */
static inline krpc_error_t krpc_KRPC_Expression_CreateSet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_set_object_t * values);

/**
 * A structure with the given field values. Throws an exception if the field values do
 * not match its fields.
 * @return The structure.
 * @param type The type of the structure.
 * @param fieldValues The values of the structure's fields,
 * in the order the structure declares them.
 */
static inline krpc_error_t krpc_KRPC_Expression_CreateStruct(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t type, const krpc_list_object_t * fieldValues);

/**
 * A tuple of the given elements. Throws an exception if there are no elements or more
 * than seven.
 * @return The tuple.
 * @param elements The elements.
 */
static inline krpc_error_t krpc_KRPC_Expression_CreateTuple(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * elements);

/**
 * The keys of a dictionary, as a list.
 *
 * The collection operations take the keys of a dictionary from here, and its values
 * from KRPC::Expression::dictionary_values.
 * @return The keys of the dictionary.
 * @param dictionary The dictionary.
 */
static inline krpc_error_t krpc_KRPC_Expression_DictionaryKeys(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t dictionary);

/**
 * The values of a dictionary, as a list.
 * @return The values of the dictionary.
 * @param dictionary The dictionary.
 */
static inline krpc_error_t krpc_KRPC_Expression_DictionaryValues(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t dictionary);

/**
 * The values of a collection with duplicates removed, keeping the first
 * occurrence of each.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The distinct values of the collection.
 * @param arg The collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_Distinct(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * Numerical division.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_Divide(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * The value at the given position in a collection.
 *
 * Throws an exception if the index is out of range.
 * @return The value at the given position.
 * @param arg The collection.
 * @param index The position of the value, counting from zero.
 */
static inline krpc_error_t krpc_KRPC_Expression_ElementAt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t index);

/**
 * Equality comparison.
 *
 * Collections, tuples, bytes and structures compare by their contents. Throws an
 * exception if the operands are of unrelated types.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_Equal(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * The values of the first collection that are not in the second, with
 * duplicates removed.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The difference between the two collections.
 * @param arg1 The first collection.
 * @param arg2 The second collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_Except(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2);

/**
 * Logical or bitwise exclusive or.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_ExclusiveOr(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * The first value of a collection.
 *
 * Throws an exception if the collection is empty.
 * @return The first value.
 * @param arg The collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_First(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * A loop that evaluates the body once for each value of a collection.
 *
 * The loop variable is a local variable declared by an enclosing
 * KRPC::Expression::block_with_variables, or a parameter of an enclosing KRPC::Expression::lambda.
 * A KRPC::Expression::break or KRPC::Expression::continue in the body applies to this loop.
 * @param variable The loop variable.
 * @param collection The collection to iterate over.
 * @param body The statement evaluated on each iteration.
 */
static inline krpc_error_t krpc_KRPC_Expression_ForEach(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t variable, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t body);

/**
 * An element of a tuple, list or dictionary.
 *
 * A tuple takes a constant integer index, a list a zero indexed integer and a
 * dictionary a key. Throws an exception if the index is out of range or the key is
 * missing, and for a tuple when the expression is created.
 * @return The element.
 * @param arg The tuple, list or dictionary.
 * @param index The index or key of the element.
 */
static inline krpc_error_t krpc_KRPC_Expression_Get(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t index);

/**
 * The value of a field of a structure. Throws an exception if the value is not a
 * structure, or has no field of the given name.
 * @return The value of the field.
 * @param arg The structure.
 * @param name The name of the field to access.
 */
static inline krpc_error_t krpc_KRPC_Expression_GetField(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, const char * name);

/**
 * Greater than numerical comparison.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_GreaterThan(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * Greater than or equal numerical comparison.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_GreaterThanOrEqual(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * The values of a collection grouped by the key a function produces for each of them.
 * @return A dictionary of each key to the values that produced it.
 * @param arg The collection.
 * @param key A function producing the key to group values by.
 */
static inline krpc_error_t krpc_KRPC_Expression_GroupBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key);

/**
 * An if statement, with no value.
 *
 * Use KRPC::Expression::conditional for an if-then-else that produces a value.
 * @param condition The boolean condition.
 * @param body The statement evaluated when the condition is true.
 */
static inline krpc_error_t krpc_KRPC_Expression_IfThen(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t body);

/**
 * An if-else statement, with no value.
 *
 * Use KRPC::Expression::conditional for an if-then-else that produces a value.
 * @param condition The boolean condition.
 * @param body The statement evaluated when the condition is true.
 * @param elseBody The statement evaluated when the condition is false.
 */
static inline krpc_error_t krpc_KRPC_Expression_IfThenElse(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t body, krpc_KRPC_Expression_t elseBody);

/**
 * The values in both of two collections, with duplicates removed.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The intersection of the two collections.
 * @param arg1 The first collection.
 * @param arg2 The second collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_Intersect(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2);

/**
 * A function call.
 * @return A function call.
 * @param function The function to call.
 * @param args The arguments to call the function with.
 */
static inline krpc_error_t krpc_KRPC_Expression_Invoke(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t function, const krpc_dictionary_string_object_t * args);

/**
 * Whether a value is null. Throws an exception if the type of the value cannot be null.
 * @param value The value.
 */
static inline krpc_error_t krpc_KRPC_Expression_IsNull(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t value);

/**
 * A function with the given parameters and body.
 *
 * The body is an expression or a block of statements, and a body with no value is
 * evaluated for its effects. A KRPC::Expression::return or KRPC::Expression::return_nothing
 * statement ends the evaluation.
 * @return A function.
 * @param parameters The parameters of the function.
 * @param body The body of the function.
 */
static inline krpc_error_t krpc_KRPC_Expression_Lambda(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * parameters, krpc_KRPC_Expression_t body);

/**
 * The last value of a collection.
 *
 * Throws an exception if the collection is empty.
 * @return The last value.
 * @param arg The collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_Last(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * Bitwise left shift.
 *
 * Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
 * for a 64-bit value.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_LeftShift(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * Less than numerical comparison.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_LessThan(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * Less than or equal numerical comparison.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_LessThanOrEqual(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * The largest value of a collection.
 *
 * Throws an exception if the collection is empty and its values are not nullable.
 * @return The largest value in the collection.
 * @param arg The list or set.
 */
static inline krpc_error_t krpc_KRPC_Expression_Max(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * The value of a collection with the largest key.
 *
 * Throws an exception if the collection is empty.
 * @return The value with the largest key.
 * @param arg The collection.
 * @param key A function producing the key to compare values by.
 */
static inline krpc_error_t krpc_KRPC_Expression_MaxBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key);

/**
 * The smallest value of a collection.
 *
 * Throws an exception if the collection is empty and its values are not nullable.
 * @return The smallest value in the collection.
 * @param arg The list or set.
 */
static inline krpc_error_t krpc_KRPC_Expression_Min(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * The value of a collection with the smallest key.
 *
 * Throws an exception if the collection is empty.
 * @return The value with the smallest key.
 * @param arg The collection.
 * @param key A function producing the key to compare values by.
 */
static inline krpc_error_t krpc_KRPC_Expression_MinBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key);

/**
 * Numerical modulo operator.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 * @return The remainder of arg0 divided by arg1
 */
static inline krpc_error_t krpc_KRPC_Expression_Modulo(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * Numerical multiplication.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_Multiply(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * Numerical negation, which negates a uint as a long.
 * @param arg The operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_Negate(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * Logical or bitwise negation.
 * @param arg The operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_Not(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * Inequality comparison.
 *
 * Collections, tuples, bytes and structures compare by their contents. Throws an
 * exception if the operands are of unrelated types.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_NotEqual(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * Logical or bitwise or, which evaluates both operands.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_Or(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * The values of a collection ordered by the key a function produces for each of them.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The ordered collection.
 * @param arg The collection to order.
 * @param key A function producing the key to order values by.
 */
static inline krpc_error_t krpc_KRPC_Expression_OrderBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key);

/**
 * A named parameter of a function.
 *
 * A value is bound to it by name with KRPC::Expression::invoke, or by its position in
 * the parameters of a KRPC::Expression::lambda.
 * @return A named parameter.
 * @param name The name of the parameter.
 * @param type The type of the parameter.
 */
static inline krpc_error_t krpc_KRPC_Expression_Parameter(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * name, krpc_KRPC_Type_t type);

/**
 * Numerical power operator.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 * @return arg0 raised to the power of arg1, at the common type of the operands
 */
static inline krpc_error_t krpc_KRPC_Expression_Power(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * A removal of a value from a list or a set, or of an entry from a dictionary.
 *
 * The first occurrence is removed from a list.
 * @return Whether the collection held the value.
 * @param collection The list, set or dictionary.
 * @param value The value to remove, or the key of the entry to
 * remove.
 */
static inline krpc_error_t krpc_KRPC_Expression_Remove(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t value);

/**
 * A statement removing the element at an index of a list.
 *
 * Throws an exception if the index is out of range.
 * @param list The list.
 * @param index The index of the element, counting from zero.
 */
static inline krpc_error_t krpc_KRPC_Expression_RemoveAt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t list, krpc_KRPC_Expression_t index);

/**
 * A return statement, which ends the enclosing function with a value.
 * @param value The value to return.
 */
static inline krpc_error_t krpc_KRPC_Expression_Return(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t value);

/**
 * A return statement, which ends the enclosing function with no value.
 */
static inline krpc_error_t krpc_KRPC_Expression_ReturnNothing(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue);

/**
 * The values of a collection in the opposite order.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The reversed collection.
 * @param arg The collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_Reverse(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * Bitwise right shift.
 *
 * Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
 * for a 64-bit value.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_RightShift(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * The result of a function for each value of a collection.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The collection of function results.
 * @param arg The list or set.
 * @param func The function, taking an element of the collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_Select(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func);

/**
 * The values of the collections a function returns for each value of a collection,
 * flattened into one.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The flattened collection of function results.
 * @param arg The list or set.
 * @param func The function, taking an element of the collection
 * and returning a collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_SelectMany(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func);

/**
 * A statement setting an element of a list, or an entry of a dictionary.
 *
 * A key the dictionary does not hold is added. Throws an exception if the index
 * is out of range for the list.
 * @param collection The list or dictionary.
 * @param index The index of the element, counting from zero, or the key of the entry.
 * @param value The value to set.
 */
static inline krpc_error_t krpc_KRPC_Expression_Set(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t index, krpc_KRPC_Expression_t value);

/**
 * A collection without its first count values.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The collection without its first count values.
 * @param arg The collection.
 * @param count The number of values to skip.
 */
static inline krpc_error_t krpc_KRPC_Expression_Skip(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t count);

/**
 * A variable that keeps its value between evaluations of a function.
 *
 * The initial value is evaluated once, when the variable is created, and gives the
 * variable its type. The value belongs to the variable, so every function that
 * contains it shares it.
 * @return A state variable.
 * @param name The name of the variable.
 * @param initialValue The initial value of the variable.
 */
static inline krpc_error_t krpc_KRPC_Expression_StateVariable(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * name, krpc_KRPC_Expression_t initialValue);

/**
 * The concatenation of strings.
 *
 * Use KRPC::Expression::convert_to_string to convert other values to strings.
 * @return The concatenated string.
 * @param args The strings to concatenate, in order.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringConcat(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * args);

/**
 * Whether one string occurs within another.
 * @return Whether the value occurs in the string.
 * @param arg The string to search.
 * @param value The string to search for.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringContains(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value);

/**
 * Whether a string ends with another.
 * @return Whether the string ends with the value.
 * @param arg The string to test.
 * @param value The string to test for.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringEndsWith(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value);

/**
 * The character at the given position in a string, as a string of length one.
 *
 * Throws an exception if the index is out of range.
 * @return The character at the given position.
 * @param arg The string.
 * @param index The position of the character, counting from zero.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringGet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t index);

/**
 * The position of the first occurrence of one string within another.
 * @return The position of the value, counting from zero, or -1 if it does not occur.
 * @param arg The string to search.
 * @param value The string to search for.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringIndexOf(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value);

/**
 * Strings joined together, separated by another string.
 * @return The joined string.
 * @param separator The string to put between the values.
 * @param values The strings to join.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringJoin(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t separator, krpc_KRPC_Expression_t values);

/**
 * The number of characters in a string.
 * @return The length of the string.
 * @param arg The string.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringLength(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * A string with every occurrence of one string replaced by another.
 *
 * Throws an exception if the string to replace is empty.
 * @return The string with the replacements made.
 * @param arg The string to search.
 * @param oldValue The string to replace.
 * @param newValue The string to replace it with.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringReplace(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t oldValue, krpc_KRPC_Expression_t newValue);

/**
 * The parts of a string separated by another string.
 * @return The parts of the string.
 * @param arg The string to split.
 * @param separator The string that separates the parts.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringSplit(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t separator);

/**
 * Whether a string starts with another.
 * @return Whether the string starts with the value.
 * @param arg The string to test.
 * @param value The string to test for.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringStartsWith(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value);

/**
 * Part of a string.
 *
 * Throws an exception if the start or the length is out of range.
 * @return The substring.
 * @param arg The string.
 * @param start The position to start at, counting from zero.
 * @param length The number of characters to take.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringSubstring(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t start, krpc_KRPC_Expression_t length);

/**
 * A string in lower case, independently of the game's language.
 * @return The string in lower case.
 * @param arg The string.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringToLower(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * A string in upper case, independently of the game's language.
 * @return The string in upper case.
 * @param arg The string.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringToUpper(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * A string with white space removed from both ends.
 * @return The trimmed string.
 * @param arg The string.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringTrim(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * A string with white space removed from its end.
 * @return The trimmed string.
 * @param arg The string.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringTrimEnd(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * A string with white space removed from its start.
 * @return The trimmed string.
 * @param arg The string.
 */
static inline krpc_error_t krpc_KRPC_Expression_StringTrimStart(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * Numerical subtraction.
 * @param arg0 The first operand.
 * @param arg1 The second operand.
 */
static inline krpc_error_t krpc_KRPC_Expression_Subtract(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1);

/**
 * The sum of the values of a collection.
 * @return The sum of the elements in the collection.
 * @param arg The list or set.
 */
static inline krpc_error_t krpc_KRPC_Expression_Sum(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * The first count values of a collection.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The first count values of the collection.
 * @param arg The collection.
 * @param count The number of values to take.
 */
static inline krpc_error_t krpc_KRPC_Expression_Take(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t count);

/**
 * A throw statement, which raises the named exception.
 *
 * The client receives the exception as an error from the function, or from the stream
 * or event evaluating it. Throws an exception if the service does not declare it.
 * @param service The name of the service the exception is defined in.
 * @param name The name of the exception.
 * @param message The message of the exception.
 */
static inline krpc_error_t krpc_KRPC_Expression_Throw(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * service, const char * name, krpc_KRPC_Expression_t message);

/**
 * A collection as a list.
 * @return The collection as a list.
 * @param arg The collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_ToList(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * A collection as a set.
 *
 * Throws an exception if a value is null.
 * @return The collection as a set.
 * @param arg The collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_ToSet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg);

/**
 * A try-catch statement that handles the named exception.
 *
 * Only an exception a service declares can be named, and KRPC::Expression::try_catch_all
 * handles any exception. Throws an exception if the service does not declare it.
 * @param body The statement to evaluate.
 * @param service The name of the service the exception is defined in.
 * @param name The name of the exception.
 * @param message A string variable assigned the exception's message, or null.
 * @param handler The statement evaluated when the exception is caught.
 */
static inline krpc_error_t krpc_KRPC_Expression_TryCatch(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t body, const char * service, const char * name, krpc_KRPC_Expression_t message, krpc_KRPC_Expression_t handler);

/**
 * A try-catch statement that handles any exception.
 * @param body The statement to evaluate.
 * @param message A string variable assigned the exception's message, or null.
 * @param handler The statement evaluated when an exception is caught.
 */
static inline krpc_error_t krpc_KRPC_Expression_TryCatchAll(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t body, krpc_KRPC_Expression_t message, krpc_KRPC_Expression_t handler);

/**
 * A try-finally statement.
 *
 * The finalizer is evaluated after the body, whether or not the body raises an
 * exception. A break, continue or return cannot leave the finalizer.
 * @param body The statement to evaluate.
 * @param finalizer The statement evaluated after the body.
 */
static inline krpc_error_t krpc_KRPC_Expression_TryFinally(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t body, krpc_KRPC_Expression_t finalizer);

/**
 * The values in either of two collections, with duplicates removed.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The union of the two collections.
 * @param arg1 The first collection.
 * @param arg2 The second collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_Union(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2);

/**
 * A local variable, declared by KRPC::Expression::block_with_variables.
 * @return A local variable.
 * @param name The name of the variable.
 * @param type The type of the variable.
 */
static inline krpc_error_t krpc_KRPC_Expression_Variable(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * name, krpc_KRPC_Type_t type);

/**
 * The values of a collection for which a predicate function returns true.
 *
 * The result is lazily evaluated. KRPC::Expression::to_list makes it concrete.
 * @return The filtered collection.
 * @param arg The list or set.
 * @param func The predicate function, taking an element of the collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_Where(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func);

/**
 * A while loop, which evaluates the body while the condition is true.
 *
 * A KRPC::Expression::break or KRPC::Expression::continue in the body applies to this loop.
 * @param condition The boolean condition.
 * @param body The statement evaluated on each iteration.
 */
static inline krpc_error_t krpc_KRPC_Expression_While(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t body);

/**
 * The results of a function applied to the values of two collections in pairs.
 *
 * The result is as long as the shorter of the two collections. It is lazily
 * evaluated, and KRPC::Expression::to_list makes it concrete.
 * @return The combined collection.
 * @param arg1 The first collection.
 * @param arg2 The second collection.
 * @param func A function combining a value from each collection.
 */
static inline krpc_error_t krpc_KRPC_Expression_Zip(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2, krpc_KRPC_Expression_t func);

/**
 * Whether the expression evaluates to a value.
 *
 * An expression with no value has no KRPC::Expression::return_type. An empty
 * collection and no value both encode to an empty sequence of bytes.
 */
static inline krpc_error_t krpc_KRPC_Expression_HasReturnType(krpc_connection_t connection, bool * returnValue, krpc_KRPC_Expression_t instance);

/**
 * The types of the function's parameters.
 *
 * Empty for a function that is not created with KRPC::Expression::lambda.
 */
static inline krpc_error_t krpc_KRPC_Expression_ParameterTypes(krpc_connection_t connection, krpc_list_object_t * returnValue, krpc_KRPC_Expression_t instance);

/**
 * The type of the value the expression evaluates to. Throws an exception if the
 * value cannot be sent to a client.
 *
 * A function created with KRPC::Expression::lambda evaluates to the value it returns. A
 * lazily evaluated collection, such as from KRPC::Expression::select, is sent once
 * converted by KRPC::Expression::to_list or KRPC::Expression::to_set.
 */
static inline krpc_error_t krpc_KRPC_Expression_ReturnType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Expression_t instance);

/**
 * A boolean type.
 */
static inline krpc_error_t krpc_KRPC_Type_Bool(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue);

/**
 * A byte sequence type.
 */
static inline krpc_error_t krpc_KRPC_Type_Bytes(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue);

/**
 * The type of a class defined by a service.
 * @param service The name of the service the class is defined in.
 * @param name The name of the class.
 */
static inline krpc_error_t krpc_KRPC_Type_ClassType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const char * service, const char * name);

/**
 * A dictionary type. Throws an exception if the key type is nullable or not a valid
 * key type.
 * @param keyType The type of the keys of the dictionary.
 * @param valueType The type of the values of the dictionary.
 */
static inline krpc_error_t krpc_KRPC_Type_DictionaryType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t keyType, krpc_KRPC_Type_t valueType);

/**
 * A double precision floating point number type.
 */
static inline krpc_error_t krpc_KRPC_Type_Double(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue);

/**
 * The type of an enumeration defined by a service.
 * @param service The name of the service the enumeration is defined in.
 * @param name The name of the enumeration.
 */
static inline krpc_error_t krpc_KRPC_Type_EnumerationType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const char * service, const char * name);

/**
 * A single precision floating point number type.
 */
static inline krpc_error_t krpc_KRPC_Type_Float(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue);

/**
 * A signed 32-bit integer type.
 */
static inline krpc_error_t krpc_KRPC_Type_Int(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue);

/**
 * A list type.
 * @param valueType The type of the elements of the list.
 */
static inline krpc_error_t krpc_KRPC_Type_ListType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t valueType);

/**
 * A signed 64-bit integer type.
 */
static inline krpc_error_t krpc_KRPC_Type_Long(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue);

/**
 * A nullable type, holding a value of the given type or null.
 * @param valueType The type of the value.
 */
static inline krpc_error_t krpc_KRPC_Type_NullableType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t valueType);

/**
 * A set type. Throws an exception if the element type is nullable.
 * @param valueType The type of the elements of the set.
 */
static inline krpc_error_t krpc_KRPC_Type_SetType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t valueType);

/**
 * A string type.
 */
static inline krpc_error_t krpc_KRPC_Type_String(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue);

/**
 * The type of a structure defined by a service.
 * @param service The name of the service the structure is defined in.
 * @param name The name of the structure.
 */
static inline krpc_error_t krpc_KRPC_Type_StructType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const char * service, const char * name);

/**
 * A tuple type. Throws an exception if there are no element types or more than seven.
 * @param valueTypes The types of the elements of the tuple.
 */
static inline krpc_error_t krpc_KRPC_Type_TupleType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const krpc_list_object_t * valueTypes);

/**
 * An unsigned 32-bit integer type.
 */
static inline krpc_error_t krpc_KRPC_Type_UInt(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue);

/**
 * An unsigned 64-bit integer type.
 */
static inline krpc_error_t krpc_KRPC_Type_ULong(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue);

/**
 * The kind of the type.
 */
static inline krpc_error_t krpc_KRPC_Type_Code(krpc_connection_t connection, krpc_KRPC_TypeCode_t * returnValue, krpc_KRPC_Type_t instance);

/**
 * The name of a class, enumeration or structure type, or an empty string.
 */
static inline krpc_error_t krpc_KRPC_Type_Name(krpc_connection_t connection, char * * returnValue, krpc_KRPC_Type_t instance);

/**
 * Whether a value of the type may be null.
 *
 * Any type can be nullable. A string or class value of a type that is not nullable
 * is never null.
 */
static inline krpc_error_t krpc_KRPC_Type_Nullable(krpc_connection_t connection, bool * returnValue, krpc_KRPC_Type_t instance);

/**
 * The service a class, enumeration or structure type is defined in, or an empty string.
 */
static inline krpc_error_t krpc_KRPC_Type_Service(krpc_connection_t connection, char * * returnValue, krpc_KRPC_Type_t instance);

/**
 * The types of the elements of a tuple, list, set or dictionary type.
 *
 * The value types of a tuple, the value type of a list or set, or the key and value
 * types of a dictionary. An empty list for any other type.
 */
static inline krpc_error_t krpc_KRPC_Type_Types(krpc_connection_t connection, krpc_list_object_t * returnValue, krpc_KRPC_Type_t instance);

// Implementation

#ifndef KRPC_IMPL_TYPE_DICTIONARY_INT32_OBJECT
#define KRPC_IMPL_TYPE_DICTIONARY_INT32_OBJECT

static inline bool krpc_encode_callback_key_dictionary_int32_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for dictionary entry key");
  const int32_t * key = (const int32_t*)(*arg);
  size_t size = 0;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_int32(&size, *key));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for dictionary entry key");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_int32(stream, *key));
  return true;
}

static inline bool krpc_encode_callback_value_dictionary_int32_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for dictionary entry value");
  const krpc_object_t * value = (const krpc_object_t*)(*arg);
  size_t size = 0;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_object(&size, *value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for dictionary entry value");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_object(stream, *value));
  return true;
}

static inline bool krpc_encode_callback_entry_dictionary_int32_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  const krpc_dictionary_int32_object_t * value = (const krpc_dictionary_int32_object_t*)(*arg);
  krpc_schema_DictionaryEntry entry = krpc_schema_DictionaryEntry_init_default;
  entry.key.funcs.encode = &krpc_encode_callback_key_dictionary_int32_object;
  entry.value.funcs.encode = &krpc_encode_callback_value_dictionary_int32_object;
  size_t i = 0;
  for (; i < value->size; i++) {
    entry.key.arg = &value->entries[i].key;
    entry.value.arg = &value->entries[i].value;
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for dictionary entry");
    size_t size;
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_message_DictionaryEntry(&size, &entry));
    if (!pb_encode_varint(stream, size))
      KRPC_CALLBACK_RETURN_ERROR("encoding size for dictionary item");
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_message_DictionaryEntry(stream, &entry));
  }
  return true;
}

static inline krpc_error_t krpc_encode_dictionary_int32_object(
  pb_ostream_t * stream, const krpc_dictionary_int32_object_t * value) {
  krpc_schema_Dictionary message = krpc_schema_Dictionary_init_default;
  message.entries.funcs.encode = &krpc_encode_callback_entry_dictionary_int32_object;
  message.entries.arg = (krpc_dictionary_int32_object_t*)value;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_Dictionary(stream, &message));
  return KRPC_OK;
}

static inline krpc_error_t krpc_encode_size_dictionary_int32_object(
  size_t * size, const krpc_dictionary_int32_object_t * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_dictionary_int32_object(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

static inline bool krpc_encode_callback_dictionary_int32_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for dictionary_int32_object");
  krpc_dictionary_int32_object_t * value = (krpc_dictionary_int32_object_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_dictionary_int32_object(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for dictionary_int32_object");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_dictionary_int32_object(stream, value));
  return true;
}

static inline bool krpc_decode_callback_key_dictionary_int32_object(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  int32_t * key = (int32_t*)(*arg);
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_int32(stream, key));
  return true;
}

static inline bool krpc_decode_callback_value_dictionary_int32_object(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  krpc_object_t * value = (krpc_object_t*)(*arg);
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_object(stream, value));
  return true;
}

static inline bool krpc_decode_callback_entry_dictionary_int32_object(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  typedef struct State { size_t capacity; krpc_dictionary_int32_object_t * value; } State;
  State * state = (State*)(*arg);
  krpc_dictionary_int32_object_t * value = state->value;
  size_t i = value->size;
  value->size++;
  if (state->capacity > 0 && state->value->size > state->capacity) {
    state->value->entries = (krpc_dictionary_entry_int32_object_t*)krpc_recalloc(state->value->entries, state->capacity, KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_dictionary_entry_int32_object_t));
    state->capacity += KRPC_ALLOC_BLOCK_SIZE;
  }
  krpc_schema_DictionaryEntry message = krpc_schema_DictionaryEntry_init_default;
  message.key.funcs.decode = &krpc_decode_callback_key_dictionary_int32_object;
  message.key.arg = &value->entries[i].key;
  message.value.funcs.decode = &krpc_decode_callback_value_dictionary_int32_object;
  message.value.arg = &value->entries[i].value;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_message_DictionaryEntry(stream, &message));
  return true;
}

static inline krpc_error_t krpc_decode_dictionary_int32_object(
  pb_istream_t * stream, krpc_dictionary_int32_object_t * value) {
  typedef struct State { size_t capacity; krpc_dictionary_int32_object_t * value; } State;
  State state = { 0, value };
  value->size = 0;
  if (value->entries == NULL) {
    value->entries = (krpc_dictionary_entry_int32_object_t*)krpc_calloc(KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_dictionary_entry_int32_object_t));
    state.capacity = KRPC_ALLOC_BLOCK_SIZE;
  }
  krpc_schema_Dictionary message = krpc_schema_Dictionary_init_default;
  message.entries.funcs.decode = &krpc_decode_callback_entry_dictionary_int32_object;
  message.entries.arg = &state;
  KRPC_RETURN_ON_ERROR(krpc_decode_message_Dictionary(stream, &message));
  return KRPC_OK;
}

#endif  // KRPC_IMPL_TYPE_DICTIONARY_INT32_OBJECT

#ifndef KRPC_IMPL_TYPE_DICTIONARY_STRING_OBJECT
#define KRPC_IMPL_TYPE_DICTIONARY_STRING_OBJECT

static inline bool krpc_encode_callback_key_dictionary_string_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for dictionary entry key");
  const char * * key = (const char **)(*arg);
  size_t size = 0;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_string(&size, *key));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for dictionary entry key");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_string(stream, *key));
  return true;
}

static inline bool krpc_encode_callback_value_dictionary_string_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for dictionary entry value");
  const krpc_object_t * value = (const krpc_object_t*)(*arg);
  size_t size = 0;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_object(&size, *value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for dictionary entry value");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_object(stream, *value));
  return true;
}

static inline bool krpc_encode_callback_entry_dictionary_string_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  const krpc_dictionary_string_object_t * value = (const krpc_dictionary_string_object_t*)(*arg);
  krpc_schema_DictionaryEntry entry = krpc_schema_DictionaryEntry_init_default;
  entry.key.funcs.encode = &krpc_encode_callback_key_dictionary_string_object;
  entry.value.funcs.encode = &krpc_encode_callback_value_dictionary_string_object;
  size_t i = 0;
  for (; i < value->size; i++) {
    entry.key.arg = &value->entries[i].key;
    entry.value.arg = &value->entries[i].value;
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for dictionary entry");
    size_t size;
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_message_DictionaryEntry(&size, &entry));
    if (!pb_encode_varint(stream, size))
      KRPC_CALLBACK_RETURN_ERROR("encoding size for dictionary item");
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_message_DictionaryEntry(stream, &entry));
  }
  return true;
}

static inline krpc_error_t krpc_encode_dictionary_string_object(
  pb_ostream_t * stream, const krpc_dictionary_string_object_t * value) {
  krpc_schema_Dictionary message = krpc_schema_Dictionary_init_default;
  message.entries.funcs.encode = &krpc_encode_callback_entry_dictionary_string_object;
  message.entries.arg = (krpc_dictionary_string_object_t*)value;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_Dictionary(stream, &message));
  return KRPC_OK;
}

static inline krpc_error_t krpc_encode_size_dictionary_string_object(
  size_t * size, const krpc_dictionary_string_object_t * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_dictionary_string_object(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

static inline bool krpc_encode_callback_dictionary_string_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for dictionary_string_object");
  krpc_dictionary_string_object_t * value = (krpc_dictionary_string_object_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_dictionary_string_object(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for dictionary_string_object");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_dictionary_string_object(stream, value));
  return true;
}

static inline bool krpc_decode_callback_key_dictionary_string_object(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  char * * key = (char **)(*arg);
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_string(stream, key));
  return true;
}

static inline bool krpc_decode_callback_value_dictionary_string_object(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  krpc_object_t * value = (krpc_object_t*)(*arg);
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_object(stream, value));
  return true;
}

static inline bool krpc_decode_callback_entry_dictionary_string_object(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  typedef struct State { size_t capacity; krpc_dictionary_string_object_t * value; } State;
  State * state = (State*)(*arg);
  krpc_dictionary_string_object_t * value = state->value;
  size_t i = value->size;
  value->size++;
  if (state->capacity > 0 && state->value->size > state->capacity) {
    state->value->entries = (krpc_dictionary_entry_string_object_t*)krpc_recalloc(state->value->entries, state->capacity, KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_dictionary_entry_string_object_t));
    state->capacity += KRPC_ALLOC_BLOCK_SIZE;
  }
  krpc_schema_DictionaryEntry message = krpc_schema_DictionaryEntry_init_default;
  message.key.funcs.decode = &krpc_decode_callback_key_dictionary_string_object;
  message.key.arg = &value->entries[i].key;
  message.value.funcs.decode = &krpc_decode_callback_value_dictionary_string_object;
  message.value.arg = &value->entries[i].value;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_message_DictionaryEntry(stream, &message));
  return true;
}

static inline krpc_error_t krpc_decode_dictionary_string_object(
  pb_istream_t * stream, krpc_dictionary_string_object_t * value) {
  typedef struct State { size_t capacity; krpc_dictionary_string_object_t * value; } State;
  State state = { 0, value };
  value->size = 0;
  if (value->entries == NULL) {
    value->entries = (krpc_dictionary_entry_string_object_t*)krpc_calloc(KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_dictionary_entry_string_object_t));
    state.capacity = KRPC_ALLOC_BLOCK_SIZE;
  }
  krpc_schema_Dictionary message = krpc_schema_Dictionary_init_default;
  message.entries.funcs.decode = &krpc_decode_callback_entry_dictionary_string_object;
  message.entries.arg = &state;
  KRPC_RETURN_ON_ERROR(krpc_decode_message_Dictionary(stream, &message));
  return KRPC_OK;
}

#endif  // KRPC_IMPL_TYPE_DICTIONARY_STRING_OBJECT

#ifndef KRPC_IMPL_TYPE_KRPC_CLIENTINFO
#define KRPC_IMPL_TYPE_KRPC_CLIENTINFO

static inline bool krpc_encode_callback_items_KRPC_ClientInfo(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  const krpc_KRPC_ClientInfo_t * value = (const krpc_KRPC_ClientInfo_t*)(*arg);
  {
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for tuple item");
    size_t size;
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_bytes(&size, value->id));
    if (!pb_encode_varint(stream, size))
      KRPC_CALLBACK_RETURN_ERROR("encoding size for tuple item");
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_bytes(stream, value->id));
  }
  {
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for tuple item");
    size_t size;
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_string(&size, value->name));
    if (!pb_encode_varint(stream, size))
      KRPC_CALLBACK_RETURN_ERROR("encoding size for tuple item");
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_string(stream, value->name));
  }
  {
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for tuple item");
    size_t size;
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_string(&size, value->address));
    if (!pb_encode_varint(stream, size))
      KRPC_CALLBACK_RETURN_ERROR("encoding size for tuple item");
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_string(stream, value->address));
  }
  return true;
}

static inline krpc_error_t krpc_encode_KRPC_ClientInfo(
  pb_ostream_t * stream, const krpc_KRPC_ClientInfo_t * value) {
  krpc_schema_Tuple message = krpc_schema_Tuple_init_default;
  message.items.funcs.encode = &krpc_encode_callback_items_KRPC_ClientInfo;
  message.items.arg = (krpc_KRPC_ClientInfo_t*)value;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_Tuple(stream, &message));
  return KRPC_OK;
}

static inline krpc_error_t krpc_encode_size_KRPC_ClientInfo(
  size_t * size, const krpc_KRPC_ClientInfo_t * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_KRPC_ClientInfo(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

static inline bool krpc_encode_callback_KRPC_ClientInfo(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for KRPC_ClientInfo");
  krpc_KRPC_ClientInfo_t * value = (krpc_KRPC_ClientInfo_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_KRPC_ClientInfo(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for KRPC_ClientInfo");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_KRPC_ClientInfo(stream, value));
  return true;
}

static inline bool krpc_decode_callback_item_KRPC_ClientInfo(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  typedef struct State {size_t pos; krpc_KRPC_ClientInfo_t * value;} State;
  State * state = (State*)(*arg);
  krpc_KRPC_ClientInfo_t * value = state->value;
  switch (state->pos) {
  case 0:
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_bytes(stream, &value->id));
    break;
  case 1:
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_string(stream, &value->name));
    break;
  case 2:
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_string(stream, &value->address));
    break;
  default:
    // Fields are only ever appended to a structure, so a value from a newer server may
    // carry more items than this client knows about, and those are skipped
    if (!pb_read(stream, NULL, stream->bytes_left))
      KRPC_CALLBACK_RETURN_ERROR("skipping an appended struct field");
    break;
  }
  state->pos++;
  return true;
}

static inline krpc_error_t krpc_decode_KRPC_ClientInfo(
  pb_istream_t * stream, krpc_KRPC_ClientInfo_t * value) {
  krpc_schema_Tuple message = krpc_schema_Tuple_init_default;
  message.items.funcs.decode = &krpc_decode_callback_item_KRPC_ClientInfo;
  typedef struct State { size_t pos; krpc_KRPC_ClientInfo_t * value; } State;
  State state = { 0, value };
  message.items.arg = &state;
  KRPC_RETURN_ON_ERROR(krpc_decode_message_Tuple(stream, &message));
  if (state.pos < 3)
    KRPC_RETURN_ERROR(DECODING_FAILED, "too few fields for a KRPC_ClientInfo");
  return KRPC_OK;
}

#endif  // KRPC_IMPL_TYPE_KRPC_CLIENTINFO

#ifndef KRPC_IMPL_TYPE_LIST_KRPC_CLIENTINFO
#define KRPC_IMPL_TYPE_LIST_KRPC_CLIENTINFO

static inline bool krpc_encode_callback_items_list_KRPC_ClientInfo(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  const krpc_list_KRPC_ClientInfo_t * value = (const krpc_list_KRPC_ClientInfo_t*)(*arg);
  size_t i = 0;
  for (; i < value->size; i++) {
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for list item");
    size_t size;
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_KRPC_ClientInfo(&size, &value->items[i]));
    if (!pb_encode_varint(stream, size))
      KRPC_CALLBACK_RETURN_ERROR("encoding size for list item");
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_KRPC_ClientInfo(stream, &value->items[i]));
  }
  return true;
}

static inline krpc_error_t krpc_encode_list_KRPC_ClientInfo(
  pb_ostream_t * stream, const krpc_list_KRPC_ClientInfo_t * value) {
  krpc_schema_List message = krpc_schema_List_init_default;
  message.items.funcs.encode = &krpc_encode_callback_items_list_KRPC_ClientInfo;
  message.items.arg = (krpc_list_KRPC_ClientInfo_t*)value;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_List(stream, &message));
  return KRPC_OK;
}

static inline krpc_error_t krpc_encode_size_list_KRPC_ClientInfo(
  size_t * size, const krpc_list_KRPC_ClientInfo_t * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_list_KRPC_ClientInfo(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

static inline bool krpc_encode_callback_list_KRPC_ClientInfo(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for list_KRPC_ClientInfo");
  krpc_list_KRPC_ClientInfo_t * value = (krpc_list_KRPC_ClientInfo_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_list_KRPC_ClientInfo(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for list_KRPC_ClientInfo");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_list_KRPC_ClientInfo(stream, value));
  return true;
}

static inline bool krpc_decode_callback_item_list_KRPC_ClientInfo(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  typedef struct State { size_t capacity; krpc_list_KRPC_ClientInfo_t * value; } State;
  State * state = (State*)(*arg);
  size_t i = state->value->size;
  state->value->size++;
  if (state->capacity > 0 && state->value->size > state->capacity) {
    state->value->items = (krpc_KRPC_ClientInfo_t*)krpc_recalloc(state->value->items, state->capacity, KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_KRPC_ClientInfo_t));
    state->capacity += KRPC_ALLOC_BLOCK_SIZE;
  }
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_KRPC_ClientInfo(stream, &state->value->items[i]));
  return true;
}

static inline krpc_error_t krpc_decode_list_KRPC_ClientInfo(
  pb_istream_t * stream, krpc_list_KRPC_ClientInfo_t * value) {
  typedef struct State { size_t capacity; krpc_list_KRPC_ClientInfo_t * value; } State;
  State state = { 0, value };
  value->size = 0;
  if (value->items == NULL) {
    value->items = (krpc_KRPC_ClientInfo_t*)krpc_calloc(KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_KRPC_ClientInfo_t));
    state.capacity = KRPC_ALLOC_BLOCK_SIZE;
  }
  krpc_schema_List message = krpc_schema_List_init_default;
  message.items.funcs.decode = &krpc_decode_callback_item_list_KRPC_ClientInfo;
  message.items.arg = &state;
  KRPC_RETURN_ON_ERROR(krpc_decode_message_List(stream, &message));
  return KRPC_OK;
}

#endif  // KRPC_IMPL_TYPE_LIST_KRPC_CLIENTINFO

#ifndef KRPC_IMPL_TYPE_NULLABLE_BYTES
#define KRPC_IMPL_TYPE_NULLABLE_BYTES


static inline krpc_error_t krpc_encode_nullable_bytes(
  pb_ostream_t * stream, const krpc_nullable_bytes_t * value) {
  // A nullable position carries a presence bool before its value, and holds nothing else
  // when that bool is false
  KRPC_RETURN_ON_ERROR(krpc_encode_bool(stream, value->has_value));
  if (value->has_value)
    KRPC_RETURN_ON_ERROR(krpc_encode_bytes(stream, value->value));
  return KRPC_OK;
}

static inline krpc_error_t krpc_encode_size_nullable_bytes(
  size_t * size, const krpc_nullable_bytes_t * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_nullable_bytes(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

static inline bool krpc_encode_callback_nullable_bytes(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for nullable_bytes");
  krpc_nullable_bytes_t * value = (krpc_nullable_bytes_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_nullable_bytes(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for nullable_bytes");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_nullable_bytes(stream, value));
  return true;
}


static inline krpc_error_t krpc_decode_nullable_bytes(
  pb_istream_t * stream, krpc_nullable_bytes_t * value) {
  // An absent value is left zeroed, so the member is defined whatever the caller passed in
  memset(&value->value, 0, sizeof(value->value));
  KRPC_RETURN_ON_ERROR(krpc_decode_bool(stream, &value->has_value));
  if (value->has_value)
    KRPC_RETURN_ON_ERROR(krpc_decode_bytes(stream, &value->value));
  return KRPC_OK;
}

#endif  // KRPC_IMPL_TYPE_NULLABLE_BYTES

#ifndef KRPC_IMPL_TYPE_LIST_NULLABLE_BYTES
#define KRPC_IMPL_TYPE_LIST_NULLABLE_BYTES

static inline bool krpc_encode_callback_items_list_nullable_bytes(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  const krpc_list_nullable_bytes_t * value = (const krpc_list_nullable_bytes_t*)(*arg);
  size_t i = 0;
  for (; i < value->size; i++) {
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for list item");
    size_t size;
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_nullable_bytes(&size, &value->items[i]));
    if (!pb_encode_varint(stream, size))
      KRPC_CALLBACK_RETURN_ERROR("encoding size for list item");
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_nullable_bytes(stream, &value->items[i]));
  }
  return true;
}

static inline krpc_error_t krpc_encode_list_nullable_bytes(
  pb_ostream_t * stream, const krpc_list_nullable_bytes_t * value) {
  krpc_schema_List message = krpc_schema_List_init_default;
  message.items.funcs.encode = &krpc_encode_callback_items_list_nullable_bytes;
  message.items.arg = (krpc_list_nullable_bytes_t*)value;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_List(stream, &message));
  return KRPC_OK;
}

static inline krpc_error_t krpc_encode_size_list_nullable_bytes(
  size_t * size, const krpc_list_nullable_bytes_t * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_list_nullable_bytes(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

static inline bool krpc_encode_callback_list_nullable_bytes(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for list_nullable_bytes");
  krpc_list_nullable_bytes_t * value = (krpc_list_nullable_bytes_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_list_nullable_bytes(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for list_nullable_bytes");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_list_nullable_bytes(stream, value));
  return true;
}

static inline bool krpc_decode_callback_item_list_nullable_bytes(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  typedef struct State { size_t capacity; krpc_list_nullable_bytes_t * value; } State;
  State * state = (State*)(*arg);
  size_t i = state->value->size;
  state->value->size++;
  if (state->capacity > 0 && state->value->size > state->capacity) {
    state->value->items = (krpc_nullable_bytes_t*)krpc_recalloc(state->value->items, state->capacity, KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_nullable_bytes_t));
    state->capacity += KRPC_ALLOC_BLOCK_SIZE;
  }
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_nullable_bytes(stream, &state->value->items[i]));
  return true;
}

static inline krpc_error_t krpc_decode_list_nullable_bytes(
  pb_istream_t * stream, krpc_list_nullable_bytes_t * value) {
  typedef struct State { size_t capacity; krpc_list_nullable_bytes_t * value; } State;
  State state = { 0, value };
  value->size = 0;
  if (value->items == NULL) {
    value->items = (krpc_nullable_bytes_t*)krpc_calloc(KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_nullable_bytes_t));
    state.capacity = KRPC_ALLOC_BLOCK_SIZE;
  }
  krpc_schema_List message = krpc_schema_List_init_default;
  message.items.funcs.decode = &krpc_decode_callback_item_list_nullable_bytes;
  message.items.arg = &state;
  KRPC_RETURN_ON_ERROR(krpc_decode_message_List(stream, &message));
  return KRPC_OK;
}

#endif  // KRPC_IMPL_TYPE_LIST_NULLABLE_BYTES

#ifndef KRPC_IMPL_TYPE_LIST_OBJECT
#define KRPC_IMPL_TYPE_LIST_OBJECT

static inline bool krpc_encode_callback_items_list_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  const krpc_list_object_t * value = (const krpc_list_object_t*)(*arg);
  size_t i = 0;
  for (; i < value->size; i++) {
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for list item");
    size_t size;
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_object(&size, value->items[i]));
    if (!pb_encode_varint(stream, size))
      KRPC_CALLBACK_RETURN_ERROR("encoding size for list item");
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_object(stream, value->items[i]));
  }
  return true;
}

static inline krpc_error_t krpc_encode_list_object(
  pb_ostream_t * stream, const krpc_list_object_t * value) {
  krpc_schema_List message = krpc_schema_List_init_default;
  message.items.funcs.encode = &krpc_encode_callback_items_list_object;
  message.items.arg = (krpc_list_object_t*)value;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_List(stream, &message));
  return KRPC_OK;
}

static inline krpc_error_t krpc_encode_size_list_object(
  size_t * size, const krpc_list_object_t * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_list_object(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

static inline bool krpc_encode_callback_list_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for list_object");
  krpc_list_object_t * value = (krpc_list_object_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_list_object(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for list_object");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_list_object(stream, value));
  return true;
}

static inline bool krpc_decode_callback_item_list_object(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  typedef struct State { size_t capacity; krpc_list_object_t * value; } State;
  State * state = (State*)(*arg);
  size_t i = state->value->size;
  state->value->size++;
  if (state->capacity > 0 && state->value->size > state->capacity) {
    state->value->items = (krpc_object_t*)krpc_recalloc(state->value->items, state->capacity, KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_object_t));
    state->capacity += KRPC_ALLOC_BLOCK_SIZE;
  }
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_object(stream, &state->value->items[i]));
  return true;
}

static inline krpc_error_t krpc_decode_list_object(
  pb_istream_t * stream, krpc_list_object_t * value) {
  typedef struct State { size_t capacity; krpc_list_object_t * value; } State;
  State state = { 0, value };
  value->size = 0;
  if (value->items == NULL) {
    value->items = (krpc_object_t*)krpc_calloc(KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_object_t));
    state.capacity = KRPC_ALLOC_BLOCK_SIZE;
  }
  krpc_schema_List message = krpc_schema_List_init_default;
  message.items.funcs.decode = &krpc_decode_callback_item_list_object;
  message.items.arg = &state;
  KRPC_RETURN_ON_ERROR(krpc_decode_message_List(stream, &message));
  return KRPC_OK;
}

#endif  // KRPC_IMPL_TYPE_LIST_OBJECT

#ifndef KRPC_IMPL_TYPE_SET_OBJECT
#define KRPC_IMPL_TYPE_SET_OBJECT

static inline bool krpc_encode_callback_items_set_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  const krpc_set_object_t * value = (const krpc_set_object_t*)(*arg);
  size_t i = 0;
  for (; i < value->size; i++) {
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for set item");
    size_t size;
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_object(&size, value->items[i]));
    if (!pb_encode_varint(stream, size))
      KRPC_CALLBACK_RETURN_ERROR("encoding size for set item");
    KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_object(stream, value->items[i]));
  }
  return true;
}

static inline krpc_error_t krpc_encode_set_object(
  pb_ostream_t * stream, const krpc_set_object_t * value) {
  krpc_schema_Set message = krpc_schema_Set_init_default;
  message.items.funcs.encode = &krpc_encode_callback_items_set_object;
  message.items.arg = (krpc_set_object_t*)value;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_Set(stream, &message));
  return KRPC_OK;
}

static inline krpc_error_t krpc_encode_size_set_object(
  size_t * size, const krpc_set_object_t * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_set_object(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

static inline bool krpc_encode_callback_set_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for set_object");
  krpc_set_object_t * value = (krpc_set_object_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_set_object(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for set_object");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_set_object(stream, value));
  return true;
}

static inline bool krpc_decode_callback_item_set_object(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  typedef struct State { size_t capacity; krpc_set_object_t * value; } State;
  State * state = (State*)(*arg);
  size_t i = state->value->size;
  state->value->size++;
  if (state->capacity > 0 && state->value->size > state->capacity) {
    state->value->items = (krpc_object_t*)krpc_recalloc(state->value->items, state->capacity, KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_object_t));
    state->capacity += KRPC_ALLOC_BLOCK_SIZE;
  }
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_decode_object(stream, &state->value->items[i]));
  return true;
}

static inline krpc_error_t krpc_decode_set_object(
  pb_istream_t * stream, krpc_set_object_t * value) {
  typedef struct State { size_t capacity; krpc_set_object_t * value; } State;
  State state = { 0, value };
  value->size = 0;
  if (value->items == NULL) {
    value->items = (krpc_object_t*)krpc_calloc(KRPC_ALLOC_BLOCK_SIZE, sizeof(krpc_object_t));
    state.capacity = KRPC_ALLOC_BLOCK_SIZE;
  }
  krpc_schema_Set message = krpc_schema_Set_init_default;
  message.items.funcs.decode = &krpc_decode_callback_item_set_object;
  message.items.arg = &state;
  KRPC_RETURN_ON_ERROR(krpc_decode_message_Set(stream, &message));
  return KRPC_OK;
}

#endif  // KRPC_IMPL_TYPE_SET_OBJECT

static inline krpc_error_t krpc_KRPC_AddEvent(krpc_connection_t connection, krpc_schema_Event * returnValue, krpc_KRPC_Expression_t function) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 11, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &function));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_message_Event(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_AddFunctionStream(krpc_connection_t connection, krpc_schema_Stream * returnValue, krpc_KRPC_Expression_t function) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 8, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &function));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_message_Stream(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_AddStream(krpc_connection_t connection, krpc_schema_Stream * returnValue, const krpc_schema_ProcedureCall * call) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 5, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_message_ProcedureCall, call));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_message_Stream(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_CompileFunction(krpc_connection_t connection, krpc_KRPC_Expression_t function) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 9, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &function));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_GetClientID(krpc_connection_t connection, krpc_bytes_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 1, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_bytes(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_GetClientName(krpc_connection_t connection, char * * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 2, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_string(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_GetServices(krpc_connection_t connection, krpc_schema_Services * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 4, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_message_Services(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_GetStatus(krpc_connection_t connection, krpc_schema_Status * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 3, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_message_Status(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_HoldTick(krpc_connection_t connection) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 12, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_NextTick(krpc_connection_t connection) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 14, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_ReleaseTick(krpc_connection_t connection) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 13, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_RemoveStream(krpc_connection_t connection, uint64_t id) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 7, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &id));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_RunFunction(krpc_connection_t connection, krpc_bytes_t * returnValue, bool * returnValueIsNull, krpc_KRPC_Expression_t function, const krpc_list_nullable_bytes_t * arguments) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 10, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &function));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_list_nullable_bytes, arguments));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    if (_result.message.is_null) {
      if (returnValueIsNull)
        *returnValueIsNull = true;
    } else {
      if (returnValueIsNull)
        *returnValueIsNull = false;
      pb_istream_t _stream;
      KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
      KRPC_CHECK(krpc_decode_bytes(&_stream, returnValue));
    }
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_SetStreamRate(krpc_connection_t connection, uint64_t id, float rate) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 6, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &id));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_float, &rate));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Clients(krpc_connection_t connection, krpc_list_KRPC_ClientInfo_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 15, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_list_KRPC_ClientInfo(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_GameScene(krpc_connection_t connection, krpc_KRPC_GameScene_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 16, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_enum(&_stream, (int*)returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_set_GameScene(krpc_connection_t connection, krpc_KRPC_GameScene_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 17, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_enum, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_GameStateLoads(krpc_connection_t connection, uint32_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 20, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_uint32(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Paused(krpc_connection_t connection, bool * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 18, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_bool(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_set_Paused(krpc_connection_t connection, bool value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 19, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_bool, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_HasReturnType(krpc_connection_t connection, bool * returnValue, krpc_KRPC_Expression_t instance) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 148, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &instance));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_bool(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ParameterTypes(krpc_connection_t connection, krpc_list_object_t * returnValue, krpc_KRPC_Expression_t instance) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 147, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &instance));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_list_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ReturnType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Expression_t instance) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 146, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &instance));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Add(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 134, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Aggregate(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 51, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &func));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_AggregateWithSeed(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t seed, krpc_KRPC_Expression_t func) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 52, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &seed));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &func));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_All(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t predicate) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 55, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &predicate));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_And(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 126, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Any(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t predicate) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 56, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &predicate));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Append(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 61, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &collection));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Assign(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t variable, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 68, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &variable));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Average(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 28, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Block(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * statements) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 69, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_list_object, statements));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_BlockWithVariables(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * variables, const krpc_list_object_t * statements) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 70, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_list_object, variables));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_list_object, statements));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Break(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 73, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_BuildDictionary(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t keyFunc, krpc_KRPC_Expression_t valueFunc) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 34, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &keyFunc));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &valueFunc));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Call(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_schema_ProcedureCall * call) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 118, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_message_ProcedureCall, call));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_CallWithArguments(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_schema_ProcedureCall * call, const krpc_dictionary_int32_object_t * args) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 119, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_message_ProcedureCall, call));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_dictionary_int32_object, args));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Cast(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Type_t type) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 142, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &type));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Clear(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 65, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &collection));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Concat(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 53, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg1));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg2));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Conditional(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t ifTrue, krpc_KRPC_Expression_t ifFalse) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 133, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &condition));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &ifTrue));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &ifFalse));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConditionalAnd(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 128, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConditionalOr(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 129, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantBool(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, bool value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 111, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_bool, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantBytes(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_bytes_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 113, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_bytes, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantDouble(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, double value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 105, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_double, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantEnum(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * service, const char * name, int32_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 114, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_string, &service));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_string, &name));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_int32, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantFloat(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, float value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 106, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_float, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantInt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, int32_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 107, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_int32, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantLong(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, int64_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 108, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_int64, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantNull(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t type) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 115, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &type));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantObject(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, uint64_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 116, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantString(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 112, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_string, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantUInt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, uint32_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 109, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint32, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConstantULong(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, uint64_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 110, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Contains(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 49, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ContainsKey(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t dictionary, krpc_KRPC_Expression_t key) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 50, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &dictionary));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &key));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Continue(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 74, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ConvertToString(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 79, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Count(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 24, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_CreateDictionary(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * keys, const krpc_list_object_t * values) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 100, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_list_object, keys));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_list_object, values));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_CreateEmptyDictionary(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t keyType, krpc_KRPC_Type_t valueType) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 103, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &keyType));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &valueType));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_CreateEmptyList(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t valueType) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 101, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &valueType));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_CreateEmptySet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t valueType) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 102, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &valueType));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_CreateList(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * values) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 98, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_list_object, values));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_CreateSet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_set_object_t * values) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 99, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_set_object, values));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_CreateStruct(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t type, const krpc_list_object_t * fieldValues) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 97, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &type));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_list_object, fieldValues));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_CreateTuple(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * elements) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 96, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_list_object, elements));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_DictionaryKeys(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t dictionary) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 35, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &dictionary));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_DictionaryValues(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t dictionary) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 36, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &dictionary));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Distinct(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 42, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Divide(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 137, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ElementAt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t index) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 39, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &index));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Equal(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 120, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Except(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 46, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg1));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg2));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ExclusiveOr(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 130, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_First(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 37, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ForEach(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t variable, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t body) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 78, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &variable));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &collection));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &body));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Get(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t index) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 23, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &index));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_GetField(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, const char * name) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 104, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_string, &name));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_GreaterThan(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 122, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_GreaterThanOrEqual(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 123, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_GroupBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 48, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &key));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_IfThen(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t body) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 71, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &condition));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &body));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_IfThenElse(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t body, krpc_KRPC_Expression_t elseBody) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 72, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &condition));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &body));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &elseBody));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Intersect(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 45, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg1));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg2));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Invoke(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t function, const krpc_dictionary_string_object_t * args) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 145, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &function));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_dictionary_string_object, args));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_IsNull(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 117, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Lambda(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * parameters, krpc_KRPC_Expression_t body) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 144, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_list_object, parameters));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &body));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Last(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 38, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_LeftShift(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 140, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_LessThan(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 124, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_LessThanOrEqual(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 125, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Max(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 26, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_MaxBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 41, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &key));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Min(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 27, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_MinBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 40, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &key));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Modulo(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 138, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Multiply(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 136, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Negate(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 132, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Not(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 131, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_NotEqual(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 121, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Or(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 127, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_OrderBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 54, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &key));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Parameter(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * name, krpc_KRPC_Type_t type) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 143, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_string, &name));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &type));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Power(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 139, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Remove(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 63, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &collection));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_RemoveAt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t list, krpc_KRPC_Expression_t index) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 64, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &list));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &index));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Return(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 75, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ReturnNothing(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 76, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Reverse(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 43, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_RightShift(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 141, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Select(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 29, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &func));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_SelectMany(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 33, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &func));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Set(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t index, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 62, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &collection));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &index));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Skip(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t count) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 31, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &count));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StateVariable(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * name, krpc_KRPC_Expression_t initialValue) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 67, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_string, &name));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &initialValue));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringConcat(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * args) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 80, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_list_object, args));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringContains(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 85, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringEndsWith(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 87, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringGet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t index) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 82, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &index));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringIndexOf(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 84, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringJoin(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t separator, krpc_KRPC_Expression_t values) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 95, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &separator));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &values));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringLength(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 81, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringReplace(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t oldValue, krpc_KRPC_Expression_t newValue) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 93, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &oldValue));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &newValue));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringSplit(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t separator) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 94, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &separator));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringStartsWith(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 86, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &value));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringSubstring(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t start, krpc_KRPC_Expression_t length) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 83, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &start));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &length));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringToLower(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 89, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringToUpper(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 88, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringTrim(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 90, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringTrimEnd(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 92, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_StringTrimStart(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 91, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Subtract(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 135, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg0));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg1));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Sum(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 25, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Take(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t count) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 32, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &count));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Throw(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * service, const char * name, krpc_KRPC_Expression_t message) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 57, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_string, &service));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_string, &name));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &message));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ToList(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 21, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_ToSet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 22, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_TryCatch(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t body, const char * service, const char * name, krpc_KRPC_Expression_t message, krpc_KRPC_Expression_t handler) {
  krpc_call_t _call;
  krpc_argument_t _arguments[5];
  KRPC_CHECK(krpc_call(&_call, 1, 58, 5, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &body));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_string, &service));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_string, &name));
  if (message == KRPC_NULL) {
    KRPC_CHECK(krpc_add_null_argument(&_call, 3));
  } else {
    KRPC_CHECK(krpc_add_argument(&_call, 3, &krpc_encode_callback_object, &message));
  }
  KRPC_CHECK(krpc_add_argument(&_call, 4, &krpc_encode_callback_object, &handler));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_TryCatchAll(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t body, krpc_KRPC_Expression_t message, krpc_KRPC_Expression_t handler) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 59, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &body));
  if (message == KRPC_NULL) {
    KRPC_CHECK(krpc_add_null_argument(&_call, 1));
  } else {
    KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &message));
  }
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &handler));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_TryFinally(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t body, krpc_KRPC_Expression_t finalizer) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 60, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &body));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &finalizer));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Union(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 44, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg1));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg2));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Variable(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * name, krpc_KRPC_Type_t type) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 66, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_string, &name));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &type));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Where(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 30, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &func));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_While(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t body) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 77, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &condition));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &body));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Expression_Zip(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2, krpc_KRPC_Expression_t func) {
  krpc_call_t _call;
  krpc_argument_t _arguments[3];
  KRPC_CHECK(krpc_call(&_call, 1, 47, 3, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &arg1));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &arg2));
  KRPC_CHECK(krpc_add_argument(&_call, 2, &krpc_encode_callback_object, &func));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Code(krpc_connection_t connection, krpc_KRPC_TypeCode_t * returnValue, krpc_KRPC_Type_t instance) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 166, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &instance));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_enum(&_stream, (int*)returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Name(krpc_connection_t connection, char * * returnValue, krpc_KRPC_Type_t instance) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 168, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &instance));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_string(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Nullable(krpc_connection_t connection, bool * returnValue, krpc_KRPC_Type_t instance) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 170, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &instance));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_bool(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Service(krpc_connection_t connection, char * * returnValue, krpc_KRPC_Type_t instance) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 167, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &instance));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_string(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Types(krpc_connection_t connection, krpc_list_object_t * returnValue, krpc_KRPC_Type_t instance) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 169, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_uint64, &instance));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_list_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Bool(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 155, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Bytes(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 157, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_ClassType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const char * service, const char * name) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 158, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_string, &service));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_string, &name));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_DictionaryType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t keyType, krpc_KRPC_Type_t valueType) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 164, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &keyType));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_object, &valueType));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Double(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 149, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_EnumerationType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const char * service, const char * name) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 159, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_string, &service));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_string, &name));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Float(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 150, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Int(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 151, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_ListType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t valueType) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 162, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &valueType));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_Long(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 152, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_NullableType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t valueType) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 165, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &valueType));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_SetType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t valueType) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 163, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &valueType));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_String(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 156, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_StructType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const char * service, const char * name) {
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  KRPC_CHECK(krpc_call(&_call, 1, 160, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_string, &service));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_string, &name));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_TupleType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const krpc_list_object_t * valueTypes) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  KRPC_CHECK(krpc_call(&_call, 1, 161, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_list_object, valueTypes));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_UInt(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 153, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

static inline krpc_error_t krpc_KRPC_Type_ULong(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue) {
  krpc_call_t _call;
  KRPC_CHECK(krpc_call(&_call, 1, 154, 0, NULL));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (returnValue) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_object(&_stream, returnValue));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

#ifdef __cplusplus
}  // extern "C"
#endif
