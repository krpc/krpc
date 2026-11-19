import ksatest

# Doubles a value each time the client bumps the sequence number, and acknowledges it
ECHO = """
#include <kfs.h>
#pragma kfs tick_hz 10
#pragma kfs task step rate_hz=10

typedef struct { uint32_t seq; float value; } command_t;
typedef struct { uint32_t ack; float value; } reply_t;

command_t command;
reply_t reply;
uint32_t ticks;
double precise;
bool flag;

void step(void) {
    ticks++;
    trace("ticks", (float)ticks);
    if (reply.ack != command.seq) {
        reply.value = 2.0f * command.value;
        reply.ack = command.seq;
        kfs_logf("echo %u", command.seq);
    }
}
"""

# Divides by zero on its first tick
TRAP = """
#include <kfs.h>
#pragma kfs tick_hz 10
#pragma kfs task step rate_hz=10

int32_t zero;
int32_t out;

void step(void) {
    out = 1 / zero;
}
"""

# Asks the game's flight computer to hold the vehicle's rotation, once
HOLD = """
#include <kfs.h>
#pragma kfs tick_hz 10
#pragma kfs task step rate_hz=10

uint32_t done;

void step(void) {
    if (!done) {
        kfs_hold_rates();
        done = 1u;
    }
}
"""


class KFSCase(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.kfs = cls.conn.kfs
        cls.kfs.available = True
        cls.vehicle = cls.ksa.active_vehicle
        cls.computer = cls.kfs.computer(cls.vehicle)

    @classmethod
    def tearDownClass(cls):
        cls.kfs.available = False
        cls.load_save()

    def run_program(self, source):
        self.computer.load({"main.c": source})
        self.computer.start()


class TestAvailable(KFSCase):
    def test_off(self):
        self.kfs.available = False
        try:
            self.assertFalse(self.kfs.available)
            with self.assertRaisesRegex(RuntimeError, "kFS is off"):
                self.kfs.computer(self.vehicle)
        finally:
            self.kfs.available = True
        self.assertTrue(self.kfs.available)


class TestBuild(KFSCase):
    def test_build(self):
        diagnostics = self.computer.build({"main.c": ECHO})
        self.assertFalse(any(d.error for d in diagnostics))

    def test_errors(self):
        source = ECHO.replace("ticks++;", "ticks++")
        diagnostics = self.computer.build({"main.c": source})
        errors = [d for d in diagnostics if d.error]
        self.assertGreater(len(errors), 0)
        self.assertEqual("main.c", errors[0].file)
        self.assertGreater(errors[0].line, 0)
        with self.assertRaisesRegex(RuntimeError, "The program has errors"):
            self.computer.load({"main.c": source})

    def test_no_source(self):
        with self.assertRaisesRegex(ValueError, "no .c or .kasm file"):
            self.computer.build({"notes.txt": ""})


class TestProgram(KFSCase):
    def setUp(self):
        self.run_program(ECHO)

    def tearDown(self):
        self.computer.stop()

    def test_state(self):
        states = self.kfs.ComputerState
        self.assertEqual(states.running, self.computer.state)
        self.assertEqual(self.kfs.ControlMode.pilot, self.computer.mode)
        self.assertTrue(self.computer.under_control)
        self.assertEqual(10, self.computer.tick_rate)
        self.assertGreater(self.computer.cycle_budget, 0)
        self.assertIsNone(self.computer.trap)
        self.assertIsNone(self.computer.fault)
        self.computer.stop()
        self.assertEqual(states.standby, self.computer.state)
        self.assertFalse(self.computer.under_control)
        self.computer.reset()
        self.assertEqual(states.running, self.computer.state)

    def test_globals(self):
        names = self.computer.globals
        for name in ("command.seq", "command.value", "reply.ack", "reply.value"):
            self.assertIn(name, names)
        self.computer.write_globals({"command.value": 21, "command.seq": 1})
        ksatest.wait_until(lambda: self.computer.read_global("reply.ack") == 1)
        self.assertEqual(42, self.computer.read_global("reply.value"))
        self.computer.write_globals({"precise": 0.1})
        self.assertEqual(0.1, self.computer.read_global("precise"))

    def test_global_stream(self):
        with self.conn.add_stream(self.computer.read_global, "ticks") as ticks:
            first = ticks()
            ksatest.wait_until(lambda: ticks() > first)

    def test_bad_globals(self):
        with self.assertRaisesRegex(ValueError, "no global variable"):
            self.computer.read_global("missing")
        with self.assertRaisesRegex(ValueError, "is an integer"):
            self.computer.write_globals({"command.seq": 1.5})
        with self.assertRaisesRegex(ValueError, "does not fit"):
            self.computer.write_globals({"command.seq": -1})
        with self.assertRaisesRegex(ValueError, "does not fit"):
            self.computer.write_globals({"flag": 2})
        self.computer.write_globals({"flag": 1})
        self.assertEqual(1, self.computer.read_global("flag"))

    def test_read_after_stop(self):
        ksatest.wait_until(lambda: self.computer.read_global("ticks") > 0)
        self.computer.stop()
        self.assertGreater(self.computer.read_global("ticks"), 0)

    def test_log(self):
        self.computer.clear_log()
        seq = self.computer.read_global("reply.ack") + 1
        self.computer.write_globals({"command.seq": seq})
        ksatest.wait_until(lambda: f"echo {seq:.0f}" in self.computer.log)

    def test_trace(self):
        ksatest.wait_until(lambda: "ticks" in self.computer.trace_channels)
        self.assertGreater(self.computer.trace("ticks"), 0)
        with self.assertRaisesRegex(ValueError, "has not traced"):
            self.computer.trace("missing")

    def test_kill(self):
        self.computer.kill()
        self.assertEqual(self.kfs.ComputerState.halted, self.computer.state)
        self.assertFalse(self.computer.under_control)


class TestTrap(KFSCase):
    def test_trap(self):
        self.run_program(TRAP)
        ksatest.wait_until(lambda: self.computer.trap is not None)
        trap = self.computer.trap
        self.assertEqual(self.kfs.ComputerState.halted, self.computer.state)
        self.assertEqual(self.kfs.TrapCode.division_by_zero, trap.code)
        self.assertEqual("step", trap.function)
        self.assertEqual("main.c", trap.file)
        self.assertEqual(TRAP.splitlines().index("    out = 1 / zero;") + 1, trap.line)
        self.computer.stop()


class TestAutoPilot(KFSCase):
    def tearDown(self):
        self.computer.stop()
        self.vehicle.auto_pilot.disengage()

    def test_auto_pilot_throws(self):
        self.run_program(HOLD)
        auto_pilot = self.vehicle.auto_pilot
        with self.assertRaisesRegex(RuntimeError, "kFS program controls the vehicle"):
            auto_pilot.engage()
        with self.assertRaisesRegex(RuntimeError, "kFS program controls the vehicle"):
            auto_pilot.target_pitch_and_heading(10, 90)
        self.computer.stop()
        auto_pilot.engage()
        self.assertTrue(auto_pilot.engaged)
