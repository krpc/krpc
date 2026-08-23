package krpc.client;

import org.junit.runner.RunWith;
import org.junit.runners.Suite;

/** The transport itself over a unix domain socket, against a server the test listens on. */
@RunWith(Suite.class)
@Suite.SuiteClasses({LocalSocketConnectionTest.class})
public class LocalSocketConnectionTestSuite {
  /** Entry point for running the test suite directly.
   *
   * @param args Command line arguments.
   */
  public static void main(String[] args) {
    org.junit.runner.JUnitCore.main("krpc.client.LocalSocketConnectionTestSuite");
  }
}
