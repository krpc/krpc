package krpc.client;

import org.junit.runner.RunWith;
import org.junit.runners.Suite;

/** The client end to end against a server, which the harness running these starts. */
@RunWith(Suite.class)
@Suite.SuiteClasses({ConnectionTest.class, EventTest.class, ExpressionStreamTest.class,
                     RemoteObjectTest.class, StreamTest.class })
public class TestSuite {
  /** Entry point for running the test suite directly. */
  public static void main(String[] args) {
    org.junit.runner.JUnitCore.main("krpc.client.TestSuite");
  }
}
