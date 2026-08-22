package krpc.client;

import org.junit.runner.RunWith;
import org.junit.runners.Suite;

/** The transport itself over TCP/IP, against a server the test listens on. */
@RunWith(Suite.class)
@Suite.SuiteClasses({TcpIpConnectionTest.class})
public class TcpIpConnectionTestSuite {
  /** Entry point for running the test suite directly.
   *
   * @param args Command line arguments.
   */
  public static void main(String[] args) {
    org.junit.runner.JUnitCore.main("krpc.client.TcpIpConnectionTestSuite");
  }
}
