/* Fly a rocket's ascent from the pad, for a kRPC client that commands
   the lift off and the staging.

   Once the client asks for lift off, the program lights the first stage,
   flies a gravity turn heading east, and cuts the engines once the
   apoapsis reaches the target. It reports its progress in telemetry, and
   stages when the client asks it to. */
#include <kfs.h>
#include <math.h>
#include <vehicle.h>

#pragma kfs tick_hz 10
#pragma kfs subscribe MSG_FLIGHT_DATA flight
#pragma kfs subscribe MSG_VEHICLE_PROPS props
#pragma kfs task ascent rate_hz=10

#define TURN_START_ALTITUDE 1000.0f
#define TURN_END_ALTITUDE 150000.0f
#define TARGET_ALTITUDE 400000.0f
#define HEADING (90.0f * DEG2RAD)

#define PHASE_LAUNCH 0u
#define PHASE_ASCENT 1u
#define PHASE_COAST 2u

kfs_msg_flight_data_t flight;
kfs_msg_vehicle_props_t props;

/* The telemetry, which the client reads */
typedef struct {
    uint32_t phase;
    float altitude;
    float apoapsis;
    float pitch;
    float thrust;
    uint32_t stages;
} telemetry_t;

telemetry_t telemetry;

/* The client's requests: lift off, and a count of the stagings it wants */
typedef struct {
    uint32_t launch;
    uint32_t stages;
} request_t;

request_t request;

uint32_t launched;
uint32_t staged;

/* The thrust of the active engines that have propellant */
static float available_thrust(void) {
    float thrust = 0.0f;
    for (int i = 0; i < ENGINE_COUNT; i++) {
        if (props.engine[i].active && props.engine[i].propellant_ok)
            thrust += props.engine[i].max_thrust_N;
    }
    return thrust;
}

/* Pitch over from vertical to horizontal, fastest low down */
static float pitch_at(float altitude) {
    if (altitude < TURN_START_ALTITUDE)
        return 90.0f;
    float fraction = (altitude - TURN_START_ALTITUDE) /
                     (TURN_END_ALTITUDE - TURN_START_ALTITUDE);
    return 90.0f * (1.0f - sqrtf(fminf(fraction, 1.0f)));
}

void ascent(void) {
    if (flight.hdr.seq == 0u)
        return; /* No flight data yet */
    float thrust = available_thrust();

    if (telemetry.phase == PHASE_LAUNCH) {
        if (!request.launch) {
            /* Wait on the pad */
        } else if (!launched) {
            kfs_hold_heading(HEADING, 90.0f * DEG2RAD, KFS_ROLL_UP);
            kfs_set_throttle(1.0f);
            kfs_engines_on();
            kfs_stage();
            launched = 1u;
        } else if (thrust > 0.0f) {
            kfs_logf("lift off");
            telemetry.phase = PHASE_ASCENT;
        }
    } else if (telemetry.phase == PHASE_ASCENT) {
        /* Drop the spent stage and light the next one */
        if (request.stages != staged) {
            kfs_stage();
            kfs_stage();
            staged = request.stages;
            kfs_logf("staged %u", staged);
        }
        /* Acknowledge a staging once the next stage has thrust */
        if (thrust > 0.0f)
            telemetry.stages = staged;
        telemetry.pitch = pitch_at(flight.altitude_m);
        kfs_hold_heading(HEADING, telemetry.pitch * DEG2RAD, KFS_ROLL_UP);
        if (flight.apoapsis_m >= TARGET_ALTITUDE) {
            kfs_engines_off();
            kfs_hold_direction(KFS_VLF_PROGRADE, KFS_ROLL_FREE);
            kfs_logf("target apoapsis reached");
            telemetry.phase = PHASE_COAST;
        }
    }

    telemetry.altitude = flight.altitude_m;
    telemetry.apoapsis = flight.apoapsis_m;
    telemetry.thrust = thrust;
}
